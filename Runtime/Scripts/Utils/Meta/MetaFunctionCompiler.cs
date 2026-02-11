using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Dalichrome.RandomGenerator.Utils
{
    /// <summary>
    /// Compiles numeric meta expressions into reusable AST nodes.
    /// - Variables read meta from ColumnZ by default (grid.GetData(x,y,ColumnZ,key)).
    /// - Evaluation uses float and rounds to int at the end.
    ///
    /// Grammar:
    ///   expr    := add
    ///   add     := mul (('+'|'-') mul)*
    ///   mul     := unary (('*'|'/') unary)*
    ///   unary   := ('+'|'-') unary | primary
    ///   primary := number | ident | funcCall | '(' expr ')'
    ///
    /// Examples:
    ///   (temperature + valley*0.6) - slope*0.5
    ///   clamp01(baseWet + flow*0.8 - slope*0.6 + (1-sun)*0.2)
    ///   sqrt((sample(height,1,0)-sample(height,-1,0))*(sample(height,1,0)-sample(height,-1,0)) + ...)
    /// </summary>
    public static class MetaFunctionCompiler
    {
        public const int ColumnZ = -1;

        public static CompiledMetaFunction Compile(
            MetaFunction metaFunction,
            TileGrid grid,
            int outputZ = ColumnZ)
        {
            string outputKey = metaFunction.outputKey;
            string expression = metaFunction.expression;

            if (string.IsNullOrWhiteSpace(outputKey))
                throw new ArgumentException("outputKey cannot be null/empty.", nameof(outputKey));

            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("expression cannot be null/empty.", nameof(expression));

            var parser = new Parser(expression.AsSpan());
            var root = parser.ParseExpression();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new FormatException($"Unexpected trailing characters in expression: '{expression}' at index {parser.Index}.");

            // Optimization pass (constant folding + peephole)
            root = Optimize(root);

            MetaInstr[] code = EmitBytecode(root);

            // Bind hot indices using grid helper
            BindHotIndices(code, grid);

            var program = new MetaBytecodeProgram(code);

            FixedString64Bytes outKey = (FixedString64Bytes)outputKey;
            return new CompiledMetaFunction(outKey, outputZ, program);
        }

        public static List<FixedString64Bytes> ExtractKeys(MetaFunction metaFunction)
        {
            string outputKey = metaFunction.outputKey;
            string expression = metaFunction.expression;

            if (string.IsNullOrWhiteSpace(expression))
                return new List<FixedString64Bytes>(0);

            var parser = new Parser(expression.AsSpan());
            var root = parser.ParseExpression();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new FormatException($"Unexpected trailing characters in expression: '{expression}' at index {parser.Index}.");

            // Run same optimize you do in Compile so sample(...) const nodes become SampleConstOffsetNode etc.
            root = Optimize(root);

            var set = new HashSet<FixedString64Bytes>();
            CollectKeys(root, set);

            if (!string.IsNullOrWhiteSpace(outputKey))
                set.Add(new FixedString64Bytes(outputKey));

            return new List<FixedString64Bytes>(set);
        }

        private static void CollectKeys(MetaFunctionNode node, HashSet<FixedString64Bytes> outKeys)
        {
            switch (node)
            {
                case null:
                    return;

                case ConstNode:
                    return;

                case VarNode v:
                    outKeys.Add(v.Key);
                    return;

                case NegateNode n:
                    CollectKeys(n.Child, outKeys);
                    return;

                case BinaryNode b:
                    CollectKeys(b.A, outKeys);
                    CollectKeys(b.B, outKeys);
                    return;

                case SampleConstOffsetNode s:
                    outKeys.Add(s.Key);
                    return;

                case SampleNode s2:
                    outKeys.Add(s2.Key);
                    CollectKeys(s2.DxNode, outKeys);
                    CollectKeys(s2.DyNode, outKeys);
                    return;

                case FuncNode f:
                    for (int i = 0; i < f.Args.Length; i++)
                        CollectKeys(f.Args[i], outKeys);
                    return;

                default:
                    throw new Exception($"Unknown MetaFunctionNode type: {node.GetType().Name}");
            }
        }

        // ============================================================
        // Meta Byte Code
        // ============================================================

        private static MetaInstr[] EmitBytecode(MetaFunctionNode node)
        {
            var list = new System.Collections.Generic.List<MetaInstr>(64);
            Emit(node, list);
            return list.ToArray();
        }

        private static void Emit(MetaFunctionNode node, System.Collections.Generic.List<MetaInstr> outCode)
        {
            switch (node)
            {
                case ConstNode c:
                    outCode.Add(new MetaInstr { Op = MetaOpCode.PushConst, F = c.Value });
                    return;

                case VarNode v:
                    // VarNode’s key is private; easiest is to expose it as internal readonly on VarNode
                    // OR add a method on VarNode to return key. See note below.
                    outCode.Add(new MetaInstr { Op = MetaOpCode.LoadVar, Key = v.Key });
                    return;

                case NegateNode n:
                    Emit(n.Child, outCode);
                    outCode.Add(new MetaInstr { Op = MetaOpCode.Neg });
                    return;

                case BinaryNode b:
                    Emit(b.A, outCode);
                    Emit(b.B, outCode);
                    outCode.Add(new MetaInstr
                    {
                        Op = b.Op switch
                        {
                            BinOp.Add => MetaOpCode.Add,
                            BinOp.Sub => MetaOpCode.Sub,
                            BinOp.Mul => MetaOpCode.Mul,
                            BinOp.Div => MetaOpCode.Div,
                            _ => MetaOpCode.Add
                        }
                    });
                    return;

                case SampleConstOffsetNode s:
                    outCode.Add(new MetaInstr
                    {
                        Op = MetaOpCode.SampleConst,
                        Key = s.Key,
                        Dx = (short)s.Dx,
                        Dy = (short)s.Dy
                    });
                    return;

                case SampleNode sd:
                    Emit(sd.DxNode, outCode);
                    Emit(sd.DyNode, outCode);
                    outCode.Add(new MetaInstr { Op = MetaOpCode.Sample, Key = sd.Key });
                    return;

                case FuncNode fn:
                    // Emit args first (RPN)
                    for (int i = 0; i < fn.Args.Length; i++)
                        Emit(fn.Args[i], outCode);

                    EmitFunc(fn, outCode);
                    return;

                default:
                    throw new Exception("Unknown MetaFunctionNode type in Emit().");
            }
        }

        private static void EmitFunc(FuncNode fn, System.Collections.Generic.List<MetaInstr> outCode)
        {
            switch (fn.Id)
            {
                case FuncId.Min: outCode.Add(new MetaInstr { Op = MetaOpCode.Min }); break;
                case FuncId.Max: outCode.Add(new MetaInstr { Op = MetaOpCode.Max }); break;
                case FuncId.Abs: outCode.Add(new MetaInstr { Op = MetaOpCode.Abs }); break;
                case FuncId.Clamp: outCode.Add(new MetaInstr { Op = MetaOpCode.Clamp }); break;
                case FuncId.Clamp01: outCode.Add(new MetaInstr { Op = MetaOpCode.Clamp01 }); break;
                case FuncId.Lerp: outCode.Add(new MetaInstr { Op = MetaOpCode.Lerp }); break;
                case FuncId.Smoothstep: outCode.Add(new MetaInstr { Op = MetaOpCode.Smoothstep }); break;
                case FuncId.Remap: outCode.Add(new MetaInstr { Op = MetaOpCode.Remap }); break;
                case FuncId.Sqrt: outCode.Add(new MetaInstr { Op = MetaOpCode.Sqrt }); break;
                case FuncId.Pow: outCode.Add(new MetaInstr { Op = MetaOpCode.Pow }); break;

                case FuncId.Rand01:
                    outCode.Add(new MetaInstr { Op = MetaOpCode.Rand01, Salt = fn.Salt });
                    break;

                case FuncId.RandRange:
                    outCode.Add(new MetaInstr { Op = MetaOpCode.RandRange, Salt = fn.Salt });
                    break;

                default:
                    throw new FormatException($"Unsupported func in VM emitter: {fn.Id}");
            }
        }
        private static void BindHotIndices(MetaInstr[] code, TileGrid grid)
        {
            if (grid == null)
                throw new ArgumentNullException(nameof(grid), "Force-hot compile requires a grid to bind hot indices.");

            for (int i = 0; i < code.Length; i++)
            {
                ref MetaInstr ins = ref code[i];

                // Only ops that read meta need binding
                if (ins.Op == MetaOpCode.LoadVar ||
                    ins.Op == MetaOpCode.Sample ||
                    ins.Op == MetaOpCode.SampleConst)
                {
                    int idx = grid.GetMetaIndex(ins.Key);
                    if ((uint)idx > short.MaxValue) // catches -1 and too-large
                    {
                        // Make this error message *actionable*
                        throw new InvalidOperationException(
                            $"Meta bytecode requires hot meta key '{ins.Key.ToString()}', but it was not initialized as hot. " +
                            $"Ensure TileGrid.InitializeMeta(...) includes all keys from ExtractKeys() before Compile().");
                    }

                    ins.HotIndex = (short)idx;
                }
                else
                {
                    ins.HotIndex = -1; // irrelevant
                }
            }
        }

        // ============================================================
        // AST Nodes
        // ============================================================

        private sealed class ConstNode : MetaFunctionNode
        {
            public readonly float Value;
            public ConstNode(float v) => Value = v;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override float Eval(ref TileGrid grid, int x, int y) => Value;
        }

        private sealed class VarNode : MetaFunctionNode
        {
            private readonly FixedString64Bytes _key;
            private readonly int _z; // default ColumnZ, but could support other z later

            internal FixedString64Bytes Key { get { return _key; } }

            public VarNode(FixedString64Bytes key, int z)
            {
                _key = key;
                _z = z;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override float Eval(ref TileGrid grid, int x, int y)
            {
                float v = grid.GetData(x, y, _z, _key);
                return v;
            }
        }

        private enum BinOp : byte { Add, Sub, Mul, Div }

        private sealed class BinaryNode : MetaFunctionNode
        {
            public readonly MetaFunctionNode A;
            public readonly MetaFunctionNode B;
            public readonly BinOp Op;

            public BinaryNode(MetaFunctionNode a, MetaFunctionNode b, BinOp op)
            {
                A = a; B = b; Op = op;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override float Eval(ref TileGrid grid, int x, int y)
            {
                float av = A.Eval(ref grid, x, y);
                float bv = B.Eval(ref grid, x, y);

                return Op switch
                {
                    BinOp.Add => av + bv,
                    BinOp.Sub => av - bv,
                    BinOp.Mul => av * bv,
                    BinOp.Div => bv == 0f ? 0f : (av / bv),
                    _ => 0f
                };
            }
        }

        private sealed class NegateNode : MetaFunctionNode
        {
            public readonly MetaFunctionNode Child;
            public NegateNode(MetaFunctionNode child) => Child = child;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override float Eval(ref TileGrid grid, int x, int y)
                => -Child.Eval(ref grid, x, y);
        }

        // sample(key, dx, dy) where dx/dy are expressions (rounded to int)
        private sealed class SampleNode : MetaFunctionNode
        {
            private readonly FixedString64Bytes _key;
            private readonly int _z;
            private readonly MetaFunctionNode _dx;
            private readonly MetaFunctionNode _dy;

            internal FixedString64Bytes Key { get { return _key; } }
            internal MetaFunctionNode DxNode { get { return _dx; } }
            internal MetaFunctionNode DyNode { get { return _dy; } }

            public SampleNode(FixedString64Bytes key, int z, MetaFunctionNode dx, MetaFunctionNode dy)
            {
                _key = key;
                _z = z;
                _dx = dx;
                _dy = dy;
            }

            internal override float Eval(ref TileGrid grid, int x, int y)
            {
                int dx = (int)math.round(_dx.Eval(ref grid, x, y));
                int dy = (int)math.round(_dy.Eval(ref grid, x, y));

                int sx = math.clamp(x + dx, 0, grid.width - 1);
                int sy = math.clamp(y + dy, 0, grid.height - 1);

                return grid.GetData(sx, sy, _z, _key);
            }
        }

        // Fast-path sample(key, constDx, constDy) (no per-tile dx/dy eval)
        private sealed class SampleConstOffsetNode : MetaFunctionNode
        {
            private readonly FixedString64Bytes _key;
            private readonly int _z;
            private readonly int _dx;
            private readonly int _dy;

            internal FixedString64Bytes Key { get { return _key; } }
            internal int Dx { get { return _dx; } }
            internal int Dy { get { return _dy; } }

            public SampleConstOffsetNode(FixedString64Bytes key, int z, int dx, int dy)
            {
                _key = key;
                _z = z;
                _dx = dx;
                _dy = dy;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override float Eval(ref TileGrid grid, int x, int y)
            {
                int sx = x + _dx;
                int sy = y + _dy;

                if (sx < 0) sx = 0;
                else if (sx >= grid.width) sx = grid.width - 1;

                if (sy < 0) sy = 0;
                else if (sy >= grid.height) sy = grid.height - 1;

                return grid.GetData(sx, sy, _z, _key);
            }
        }

        private enum FuncId : byte
        {
            Min, Max, Abs,
            Clamp, Clamp01,
            Lerp, Smoothstep, Remap,
            Sqrt, Pow,
            Rand01, RandRange
        }

        private sealed class FuncNode : MetaFunctionNode
        {
            public readonly FuncId Id;
            public readonly MetaFunctionNode[] Args;
            public readonly int Salt; // used for rand*

            public FuncNode(FuncId id, MetaFunctionNode[] args, int salt = 0)
            {
                Id = id;
                Args = args;
                Salt = salt;
            }

            internal override float Eval(ref TileGrid grid, int x, int y)
            {
                switch (Id)
                {
                    case FuncId.Min:
                        {
                            float a = Args[0].Eval(ref grid, x, y);
                            float b = Args[1].Eval(ref grid, x, y);
                            return math.min(a, b);
                        }
                    case FuncId.Max:
                        {
                            float a = Args[0].Eval(ref grid, x, y);
                            float b = Args[1].Eval(ref grid, x, y);
                            return math.max(a, b);
                        }
                    case FuncId.Abs:
                        {
                            float a = Args[0].Eval(ref grid, x, y);
                            return math.abs(a);
                        }
                    case FuncId.Clamp:
                        {
                            float v = Args[0].Eval(ref grid, x, y);
                            float lo = Args[1].Eval(ref grid, x, y);
                            float hi = Args[2].Eval(ref grid, x, y);
                            return math.clamp(v, lo, hi);
                        }
                    case FuncId.Clamp01:
                        {
                            float v = Args[0].Eval(ref grid, x, y);
                            return math.clamp(v, 0f, 1f);
                        }
                    case FuncId.Lerp:
                        {
                            float a = Args[0].Eval(ref grid, x, y);
                            float b = Args[1].Eval(ref grid, x, y);
                            float t = Args[2].Eval(ref grid, x, y);
                            t = math.clamp(t, 0f, 1f);
                            return a + (b - a) * t;
                        }
                    case FuncId.Smoothstep:
                        {
                            float e0 = Args[0].Eval(ref grid, x, y);
                            float e1 = Args[1].Eval(ref grid, x, y);
                            float v = Args[2].Eval(ref grid, x, y);

                            if (e0 == e1) return v < e0 ? 0f : 1f;
                            float t = math.clamp((v - e0) / (e1 - e0), 0f, 1f);
                            return t * t * (3f - 2f * t);
                        }
                    case FuncId.Remap:
                        {
                            float v = Args[0].Eval(ref grid, x, y);
                            float in0 = Args[1].Eval(ref grid, x, y);
                            float in1 = Args[2].Eval(ref grid, x, y);
                            float out0 = Args[3].Eval(ref grid, x, y);
                            float out1 = Args[4].Eval(ref grid, x, y);

                            if (in0 == in1) return out0;
                            float t = (v - in0) / (in1 - in0);
                            return out0 + (out1 - out0) * t;
                        }
                    case FuncId.Sqrt:
                        {
                            float v = Args[0].Eval(ref grid, x, y);
                            return v <= 0f ? 0f : math.sqrt(v);
                        }
                    case FuncId.Pow:
                        {
                            float a = Args[0].Eval(ref grid, x, y);
                            float b = Args[1].Eval(ref grid, x, y);
                            return math.pow(a, b);
                        }
                    case FuncId.Rand01:
                        {
                            uint h = HashTile(grid.seed, x, y, Salt);
                            return (h & 0x00FFFFFFu) / 16777216f;
                        }
                    case FuncId.RandRange:
                        {
                            float lo = Args[0].Eval(ref grid, x, y);
                            float hi = Args[1].Eval(ref grid, x, y);
                            uint h = HashTile(grid.seed, x, y, Salt);
                            float t = (h & 0x00FFFFFFu) / 16777216f;
                            return lo + (hi - lo) * t;
                        }
                }

                return 0f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint HashTile(uint seed, int x, int y, int salt)
            {
                return (uint)math.hash(new int4(
                    (int)seed,
                    x * 73856093,
                    y * 19349663,
                    salt * 83492791
                ));
            }
        }

        // ============================================================
        // Optimization Pass (constant folding + peepholes)
        // ============================================================

        private static MetaFunctionNode Optimize(MetaFunctionNode node)
        {
            if (node == null) return null;

            // Const/Var are already minimal
            if (node is ConstNode || node is VarNode || node is SampleConstOffsetNode)
                return node;

            if (node is NegateNode neg)
            {
                MetaFunctionNode c = Optimize(neg.Child);

                // -const => const
                if (c is ConstNode cc)
                    return new ConstNode(-cc.Value);

                // -(-x) => x
                if (c is NegateNode nn)
                    return nn.Child;

                return ReferenceEquals(c, neg.Child) ? node : new NegateNode(c);
            }

            if (node is BinaryNode bin)
            {
                MetaFunctionNode a = Optimize(bin.A);
                MetaFunctionNode b = Optimize(bin.B);

                // Const fold: const op const
                if (a is ConstNode ca && b is ConstNode cb)
                {
                    float av = ca.Value;
                    float bv = cb.Value;

                    return bin.Op switch
                    {
                        BinOp.Add => new ConstNode(av + bv),
                        BinOp.Sub => new ConstNode(av - bv),
                        BinOp.Mul => new ConstNode(av * bv),
                        BinOp.Div => new ConstNode(bv == 0f ? 0f : (av / bv)),
                        _ => node
                    };
                }

                // Assoc constant combine:
                // (x + c1) + c2 => x + (c1+c2)
                // (x * c1) * c2 => x * (c1*c2)
                if (bin.Op == BinOp.Add)
                {
                    if (TryAssocConstCombineAdd(a, b, out var combined)) return combined;
                }
                else if (bin.Op == BinOp.Mul)
                {
                    if (TryAssocConstCombineMul(a, b, out var combined)) return combined;
                }

                // Peepholes
                if (bin.Op == BinOp.Add)
                {
                    // 0 + x => x
                    if (IsZero(a)) return b;
                    // x + 0 => x
                    if (IsZero(b)) return a;
                }
                else if (bin.Op == BinOp.Sub)
                {
                    // x - 0 => x
                    if (IsZero(b)) return a;
                    // 0 - x => -x
                    if (IsZero(a)) return new NegateNode(b);
                }
                else if (bin.Op == BinOp.Mul)
                {
                    // 0 * x => 0
                    if (IsZero(a) || IsZero(b)) return new ConstNode(0f);
                    // 1 * x => x
                    if (IsOne(a)) return b;
                    // x * 1 => x
                    if (IsOne(b)) return a;
                }
                else if (bin.Op == BinOp.Div)
                {
                    // 0 / x => 0
                    if (IsZero(a)) return new ConstNode(0f);
                    // x / 1 => x
                    if (IsOne(b)) return a;
                    // x / 0 => 0 (matches runtime behavior)
                    if (b is ConstNode cdiv && cdiv.Value == 0f) return new ConstNode(0f);
                }

                // If children changed, rebuild node
                if (!ReferenceEquals(a, bin.A) || !ReferenceEquals(b, bin.B))
                    return new BinaryNode(a, b, bin.Op);

                return node;
            }

            // SampleNode stays as-is (parser already produces SampleConstOffsetNode when possible)
            if (node is SampleNode || node is SampleConstOffsetNode)
                return node;

            if (node is FuncNode fn)
            {
                // Optimize args
                var args = fn.Args;
                bool changed = false;
                for (int i = 0; i < args.Length; i++)
                {
                    var opt = Optimize(args[i]);
                    if (!ReferenceEquals(opt, args[i]))
                    {
                        args[i] = opt;
                        changed = true;
                    }
                }

                // -------- Peepholes that don't require all-const --------

                // min(x,x) / max(x,x)
                if ((fn.Id == FuncId.Min || fn.Id == FuncId.Max) && args.Length == 2)
                {
                    if (ReferenceEquals(args[0], args[1]))
                        return args[0];
                }

                // lerp(a,b,0) => a ; lerp(a,b,1) => b
                if (fn.Id == FuncId.Lerp && args.Length == 3 && args[2] is ConstNode tC)
                {
                    if (IsNearly(tC.Value, 0f)) return args[0];
                    if (IsNearly(tC.Value, 1f)) return args[1];
                }

                // clamp(x, c, c) => c
                if (fn.Id == FuncId.Clamp && args.Length == 3)
                {
                    if (args[1] is ConstNode lo && args[2] is ConstNode hi && IsNearly(lo.Value, hi.Value))
                        return new ConstNode(lo.Value);
                }

                // sqrt(x*x) => abs(x)  (only when provably same node instance)
                if (fn.Id == FuncId.Sqrt && args.Length == 1)
                {
                    if (args[0] is BinaryNode m && m.Op == BinOp.Mul && ReferenceEquals(m.A, m.B))
                    {
                        // abs(x) exists, so use it
                        return new FuncNode(FuncId.Abs, new[] { m.A });
                    }
                }

                // pow peepholes (big win)
                if (fn.Id == FuncId.Pow && args.Length == 2 && args[1] is ConstNode expC)
                {
                    var baseExpr = args[0];
                    float e = expC.Value;

                    // pow(x, 0) => 1
                    if (IsNearly(e, 0f)) return new ConstNode(1f);

                    // pow(x, 1) => x
                    if (IsNearly(e, 1f)) return baseExpr;

                    // pow(x, 2) => x*x
                    if (IsNearly(e, 2f)) return new BinaryNode(baseExpr, baseExpr, BinOp.Mul);

                    // pow(x, 3) => x*x*x (associative)
                    if (IsNearly(e, 3f))
                    {
                        var sq = new BinaryNode(baseExpr, baseExpr, BinOp.Mul);
                        return new BinaryNode(sq, baseExpr, BinOp.Mul);
                    }

                    // pow(x, 0.5) => sqrt(x)
                    // NOTE: differs from pow for negative x (pow may yield NaN; your sqrt clamps <=0 to 0).
                    if (IsNearly(e, 0.5f))
                        return new FuncNode(FuncId.Sqrt, new[] { baseExpr });

                    // pow(x, -1) => 1/x (uses your div behavior => 0 when x==0)
                    if (IsNearly(e, -1f))
                        return new BinaryNode(new ConstNode(1f), baseExpr, BinOp.Div);
                }

                // pow(1, x) => 1 ; pow(0, x) => 0 (except 0^0)
                if (fn.Id == FuncId.Pow && args.Length == 2 && args[0] is ConstNode baseC)
                {
                    if (IsNearly(baseC.Value, 1f))
                        return new ConstNode(1f);

                    if (IsNearly(baseC.Value, 0f))
                    {
                        // keep 0^0 as 1? (math.pow(0,0) returns 1 in many libs)
                        // We'll preserve typical behavior by folding only if exponent is not 0.
                        if (args[1] is ConstNode expC2 && IsNearly(expC2.Value, 0f))
                            return new ConstNode(1f);

                        return new ConstNode(0f);
                    }
                }

                // -------- Full const fold for deterministic funcs --------
                if (fn.Id != FuncId.Rand01 && fn.Id != FuncId.RandRange && AllConst(args, out var constVals))
                {
                    float folded = FoldDeterministicFunc(fn.Id, constVals);
                    return new ConstNode(folded);
                }

                if (changed)
                    return new FuncNode(fn.Id, args, fn.Salt);

                return node;
            }

            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZero(MetaFunctionNode n)
            => n is ConstNode c && c.Value == 0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOne(MetaFunctionNode n)
            => n is ConstNode c && c.Value == 1f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsNearly(float a, float b)
            => math.abs(a - b) <= 1e-6f;

        // (x + c1) + c2 => x + (c1+c2)   OR   (c1 + x) + c2 => x + (c1+c2) etc
        private static bool TryAssocConstCombineAdd(MetaFunctionNode a, MetaFunctionNode b, out MetaFunctionNode combined)
        {
            combined = null;

            // Right const
            if (b is ConstNode c2)
            {
                // (x + c1) + c2
                if (a is BinaryNode inner && inner.Op == BinOp.Add)
                {
                    if (inner.B is ConstNode c1) { combined = new BinaryNode(inner.A, new ConstNode(c1.Value + c2.Value), BinOp.Add); return true; }
                    if (inner.A is ConstNode c1a) { combined = new BinaryNode(inner.B, new ConstNode(c1a.Value + c2.Value), BinOp.Add); return true; }
                }

                // x + c2 (no assoc)
                return false;
            }

            // Left const: (c2 + a)
            if (a is ConstNode lc2)
            {
                if (b is BinaryNode inner && inner.Op == BinOp.Add)
                {
                    if (inner.B is ConstNode c1) { combined = new BinaryNode(inner.A, new ConstNode(c1.Value + lc2.Value), BinOp.Add); return true; }
                    if (inner.A is ConstNode c1a) { combined = new BinaryNode(inner.B, new ConstNode(c1a.Value + lc2.Value), BinOp.Add); return true; }
                }
            }

            return false;
        }

        // (x * c1) * c2 => x * (c1*c2)
        private static bool TryAssocConstCombineMul(MetaFunctionNode a, MetaFunctionNode b, out MetaFunctionNode combined)
        {
            combined = null;

            if (b is ConstNode c2)
            {
                if (a is BinaryNode inner && inner.Op == BinOp.Mul)
                {
                    if (inner.B is ConstNode c1) { combined = new BinaryNode(inner.A, new ConstNode(c1.Value * c2.Value), BinOp.Mul); return true; }
                    if (inner.A is ConstNode c1a) { combined = new BinaryNode(inner.B, new ConstNode(c1a.Value * c2.Value), BinOp.Mul); return true; }
                }
                return false;
            }

            if (a is ConstNode lc2)
            {
                if (b is BinaryNode inner && inner.Op == BinOp.Mul)
                {
                    if (inner.B is ConstNode c1) { combined = new BinaryNode(inner.A, new ConstNode(c1.Value * lc2.Value), BinOp.Mul); return true; }
                    if (inner.A is ConstNode c1a) { combined = new BinaryNode(inner.B, new ConstNode(c1a.Value * lc2.Value), BinOp.Mul); return true; }
                }
            }

            return false;
        }

        private static bool AllConst(MetaFunctionNode[] args, out float[] values)
        {
            values = null;
            if (args == null || args.Length == 0)
            {
                values = Array.Empty<float>();
                return true;
            }

            values = new float[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is ConstNode c)
                    values[i] = c.Value;
                else
                    return false;
            }
            return true;
        }

        private static float FoldDeterministicFunc(FuncId id, float[] v)
        {
            switch (id)
            {
                case FuncId.Min: return math.min(v[0], v[1]);
                case FuncId.Max: return math.max(v[0], v[1]);
                case FuncId.Abs: return math.abs(v[0]);
                case FuncId.Clamp: return math.clamp(v[0], v[1], v[2]);
                case FuncId.Clamp01: return math.clamp(v[0], 0f, 1f);
                case FuncId.Lerp:
                    {
                        float t = math.clamp(v[2], 0f, 1f);
                        return v[0] + (v[1] - v[0]) * t;
                    }
                case FuncId.Smoothstep:
                    {
                        float e0 = v[0], e1 = v[1], x = v[2];
                        if (e0 == e1) return x < e0 ? 0f : 1f;
                        float t = math.clamp((x - e0) / (e1 - e0), 0f, 1f);
                        return t * t * (3f - 2f * t);
                    }
                case FuncId.Remap:
                    {
                        float x = v[0], in0 = v[1], in1 = v[2], out0 = v[3], out1 = v[4];
                        if (in0 == in1) return out0;
                        float t = (x - in0) / (in1 - in0);
                        return out0 + (out1 - out0) * t;
                    }
                case FuncId.Sqrt:
                    return v[0] <= 0f ? 0f : math.sqrt(v[0]);
                case FuncId.Pow:
                    return math.pow(v[0], v[1]);
                default:
                    return 0f;
            }
        }


        // ============================================================
        // Parser / Tokenizer
        // ============================================================

        private ref struct Parser
        {
            private ReadOnlySpan<char> _text;
            private int _i;
            private int _randCallIndex;

            public Parser(ReadOnlySpan<char> text)
            {
                _text = text;
                _i = 0;
                _randCallIndex = 0;
            }

            public int Index => _i;
            public bool End => _i >= _text.Length;

            public void SkipWhitespace()
            {
                while (!End && char.IsWhiteSpace(_text[_i])) _i++;
            }

            private char Peek()
            {
                SkipWhitespace();
                return End ? '\0' : _text[_i];
            }

            private bool Match(char c)
            {
                SkipWhitespace();
                if (!End && _text[_i] == c)
                {
                    _i++;
                    return true;
                }
                return false;
            }

            private void Expect(char c, string message)
            {
                if (!Match(c)) throw new FormatException($"{message} (at index {_i})");
            }

            public MetaFunctionNode ParseExpression() => ParseAdd();

            private MetaFunctionNode ParseAdd()
            {
                MetaFunctionNode left = ParseMul();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('+'))
                    {
                        MetaFunctionNode right = ParseMul();
                        left = new BinaryNode(left, right, BinOp.Add);
                    }
                    else if (Match('-'))
                    {
                        MetaFunctionNode right = ParseMul();
                        left = new BinaryNode(left, right, BinOp.Sub);
                    }
                    else break;
                }
                return left;
            }

            private MetaFunctionNode ParseMul()
            {
                MetaFunctionNode left = ParseUnary();
                while (true)
                {
                    SkipWhitespace();
                    if (Match('*'))
                    {
                        MetaFunctionNode right = ParseUnary();
                        left = new BinaryNode(left, right, BinOp.Mul);
                    }
                    else if (Match('/'))
                    {
                        MetaFunctionNode right = ParseUnary();
                        left = new BinaryNode(left, right, BinOp.Div);
                    }
                    else break;
                }
                return left;
            }

            private MetaFunctionNode ParseUnary()
            {
                SkipWhitespace();
                if (Match('+')) return ParseUnary();
                if (Match('-')) return new NegateNode(ParseUnary());
                return ParsePrimary();
            }

            private MetaFunctionNode ParsePrimary()
            {
                SkipWhitespace();

                if (Match('('))
                {
                    MetaFunctionNode inner = ParseExpression();
                    Expect(')', "Missing closing ')'");
                    return inner;
                }

                char p = Peek();
                if (char.IsDigit(p) || p == '.')
                {
                    float f = ParseFloat();
                    return new ConstNode(f);
                }

                if (IsIdentStart(p))
                {
                    ReadOnlySpan<char> ident = ParseIdentSpan();

                    SkipWhitespace();
                    if (Match('('))
                        return ParseFunctionCall(ident);

                    FixedString64Bytes key = (FixedString64Bytes)ident.ToString();
                    return new VarNode(key, ColumnZ);
                }

                throw new FormatException($"Unexpected token '{p}' in expression (at index {_i}).");
            }

            private MetaFunctionNode ParseFunctionCall(ReadOnlySpan<char> name)
            {
                // Special-case: sample(varName, dx, dy)
                if (EqualsIdent(name, "sample"))
                {
                    SkipWhitespace();
                    char p = Peek();
                    if (!IsIdentStart(p))
                        throw new FormatException($"sample() first argument must be an identifier (at index {_i}).");

                    ReadOnlySpan<char> varName = ParseIdentSpan();
                    FixedString64Bytes key = (FixedString64Bytes)varName.ToString();

                    Expect(',', "Expected ',' after sample(varName");
                    MetaFunctionNode dx = ParseExpression();
                    Expect(',', "Expected ',' after sample(varName, dx");
                    MetaFunctionNode dy = ParseExpression();
                    Expect(')', "Expected ')' to close sample(...)");

                    // ✅ If dx/dy are constants, emit the fast-path node
                    dx = Optimize(dx);
                    dy = Optimize(dy);
                    if (dx is ConstNode cdx && dy is ConstNode cdy)
                    {
                        int ddx = (int)math.round(cdx.Value);
                        int ddy = (int)math.round(cdy.Value);
                        return new SampleConstOffsetNode(key, ColumnZ, ddx, ddy);
                    }

                    return new SampleNode(key, ColumnZ, dx, dy);
                }

                FuncId id = ResolveFunc(name);
                int expectedArgs = ExpectedArgCount(id);

                if (Match(')'))
                {
                    if (expectedArgs != 0)
                        throw new FormatException($"Function '{name.ToString()}' expects {expectedArgs} arguments (at index {_i}).");

                    return CreateFuncNode(id, Array.Empty<MetaFunctionNode>());
                }

                MetaFunctionNode[] args = expectedArgs == 0 ? new MetaFunctionNode[0] : new MetaFunctionNode[expectedArgs];

                if (expectedArgs > 0)
                {
                    args[0] = ParseExpression();
                    for (int a = 1; a < expectedArgs; a++)
                    {
                        Expect(',', $"Expected ',' in argument list for '{name.ToString()}'");
                        args[a] = ParseExpression();
                    }
                }

                SkipWhitespace();
                Expect(')', $"Expected ')' to close '{name.ToString()}'");

                return CreateFuncNode(id, args);
            }

            private MetaFunctionNode CreateFuncNode(FuncId id, MetaFunctionNode[] args)
            {
                if (id == FuncId.Rand01 || id == FuncId.RandRange)
                {
                    int salt = _randCallIndex++;
                    return new FuncNode(id, args, salt);
                }

                return new FuncNode(id, args);
            }

            private static int ExpectedArgCount(FuncId id)
            {
                return id switch
                {
                    FuncId.Min => 2,
                    FuncId.Max => 2,
                    FuncId.Abs => 1,
                    FuncId.Clamp => 3,
                    FuncId.Clamp01 => 1,
                    FuncId.Lerp => 3,
                    FuncId.Smoothstep => 3,
                    FuncId.Remap => 5,
                    FuncId.Sqrt => 1,
                    FuncId.Pow => 2,
                    FuncId.Rand01 => 0,
                    FuncId.RandRange => 2,
                    _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
                };
            }

            private static FuncId ResolveFunc(ReadOnlySpan<char> name)
            {
                if (EqualsIdent(name, "min")) return FuncId.Min;
                if (EqualsIdent(name, "max")) return FuncId.Max;
                if (EqualsIdent(name, "abs")) return FuncId.Abs;
                if (EqualsIdent(name, "clamp")) return FuncId.Clamp;
                if (EqualsIdent(name, "clamp01")) return FuncId.Clamp01;
                if (EqualsIdent(name, "lerp")) return FuncId.Lerp;
                if (EqualsIdent(name, "smoothstep")) return FuncId.Smoothstep;
                if (EqualsIdent(name, "remap")) return FuncId.Remap;
                if (EqualsIdent(name, "sqrt")) return FuncId.Sqrt;
                if (EqualsIdent(name, "pow")) return FuncId.Pow;
                if (EqualsIdent(name, "rand01")) return FuncId.Rand01;
                if (EqualsIdent(name, "randRange")) return FuncId.RandRange;

                throw new FormatException($"Unknown function '{name.ToString()}'.");
            }

            private float ParseFloat()
            {
                SkipWhitespace();
                int start = _i;

                bool sawDigit = false;

                while (!End && char.IsDigit(_text[_i]))
                {
                    sawDigit = true;
                    _i++;
                }

                if (!End && _text[_i] == '.')
                {
                    _i++;
                    while (!End && char.IsDigit(_text[_i]))
                    {
                        sawDigit = true;
                        _i++;
                    }
                }

                if (!sawDigit)
                    throw new FormatException($"Invalid number literal (at index {start}).");

                ReadOnlySpan<char> span = _text.Slice(start, _i - start);
                if (!float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    throw new FormatException($"Invalid number literal '{span.ToString()}' (at index {start}).");

                return f;
            }

            private ReadOnlySpan<char> ParseIdentSpan()
            {
                SkipWhitespace();
                int start = _i;
                if (End || !IsIdentStart(_text[_i]))
                    throw new FormatException($"Expected identifier (at index {_i}).");

                _i++;
                while (!End && IsIdentPart(_text[_i]))
                    _i++;

                return _text.Slice(start, _i - start);
            }

            private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';
            private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-';

            private static bool EqualsIdent(ReadOnlySpan<char> a, string b)
                => a.Equals(b.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
