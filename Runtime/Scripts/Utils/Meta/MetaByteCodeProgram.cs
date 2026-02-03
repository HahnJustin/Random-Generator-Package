using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using Dalichrome.RandomGenerator.Core;

namespace Dalichrome.RandomGenerator.Utils
{
    internal enum MetaOpCode : byte
    {
        PushConst,

        LoadVar,        // push meta value for Key at ColumnZ
        Add, Sub, Mul, Div,
        Neg,

        Sample,         // Key + pops dx, dy
        SampleConst,    // Key + Dx/Dy inline

        Min, Max, Abs,
        Clamp, Clamp01,
        Lerp, Smoothstep, Remap,
        Sqrt, Pow,

        Rand01, RandRange
    }

    internal struct MetaInstr
    {
        public MetaOpCode Op;
        public float F;

        public FixedString64Bytes Key;
        public short Dx, Dy;
        public int Salt;

        // -1 = not hot, otherwise hot meta index
        public short HotIndex;
    }


    /// <summary>
    /// Simple stack-based bytecode program.
    /// Uses managed array for now (no allocator/dispose), but you can upgrade to NativeArray later.
    /// </summary>
    internal readonly struct MetaBytecodeProgram
    {
        private readonly MetaInstr[] _code;

        public MetaBytecodeProgram(MetaInstr[] code)
        {
            _code = code ?? Array.Empty<MetaInstr>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Eval(ref TileGrid grid, int x, int y)
        {
            // 64 is usually plenty for your grammar; bump if you build huge expressions.
            Span<float> stack = stackalloc float[64];
            int sp = 0;

            for (int ip = 0; ip < _code.Length; ip++)
            {
                ref readonly MetaInstr ins = ref _code[ip];

                switch (ins.Op)
                {
                    case MetaOpCode.PushConst:
                        stack[sp++] = ins.F;
                        break;

                    case MetaOpCode.LoadVar:
                        float val = (ins.HotIndex >= 0)
                            ? grid.GetData(new int2(x, y), ins.HotIndex)
                            : grid.GetData(x, y, MetaFunctionCompiler.ColumnZ, ins.Key);

                        stack[sp++] = val;
                        break;

                    case MetaOpCode.Add: { float b = stack[--sp]; float a = stack[--sp]; stack[sp++] = a + b; } break;
                    case MetaOpCode.Sub: { float b = stack[--sp]; float a = stack[--sp]; stack[sp++] = a - b; } break;
                    case MetaOpCode.Mul: { float b = stack[--sp]; float a = stack[--sp]; stack[sp++] = a * b; } break;
                    case MetaOpCode.Div:
                        {
                            float b = stack[--sp];
                            float a = stack[--sp];
                            stack[sp++] = (b == 0f) ? 0f : (a / b);
                        }
                        break;

                    case MetaOpCode.Neg:
                        stack[sp - 1] = -stack[sp - 1];
                        break;

                    case MetaOpCode.Sample:
                        {
                            int dy = (int)math.round(stack[--sp]);
                            int dx = (int)math.round(stack[--sp]);

                            int sx = x + dx;
                            int sy = y + dy;

                            if (sx < 0) sx = 0;
                            else if (sx >= grid.width) sx = grid.width - 1;

                            if (sy < 0) sy = 0;
                            else if (sy >= grid.height) sy = grid.height - 1;

                            float v = (ins.HotIndex >= 0)
                                ? grid.GetData(new int2(sx, sy), ins.HotIndex)
                                : grid.GetData(sx, sy, MetaFunctionCompiler.ColumnZ, ins.Key);

                            stack[sp++] = v;
                            break;
                        }

                    case MetaOpCode.SampleConst:
                        {
                            int sx = x + ins.Dx;
                            int sy = y + ins.Dy;

                            if (sx < 0) sx = 0;
                            else if (sx >= grid.width) sx = grid.width - 1;

                            if (sy < 0) sy = 0;
                            else if (sy >= grid.height) sy = grid.height - 1;

                            float v = (ins.HotIndex >= 0)
                                ? grid.GetData(new int2(sx, sy), ins.HotIndex)
                                : grid.GetData(sx, sy, MetaFunctionCompiler.ColumnZ, ins.Key);

                            stack[sp++] = v;
                            break;
                        }

                    case MetaOpCode.Min: { float b = stack[--sp]; float a = stack[--sp]; stack[sp++] = math.min(a, b); } break;
                    case MetaOpCode.Max: { float b = stack[--sp]; float a = stack[--sp]; stack[sp++] = math.max(a, b); } break;
                    case MetaOpCode.Abs: stack[sp - 1] = math.abs(stack[sp - 1]); break;

                    case MetaOpCode.Clamp:
                        {
                            float hi = stack[--sp];
                            float lo = stack[--sp];
                            float v = stack[--sp];
                            stack[sp++] = math.clamp(v, lo, hi);
                        }
                        break;

                    case MetaOpCode.Clamp01:
                        stack[sp - 1] = math.clamp(stack[sp - 1], 0f, 1f);
                        break;

                    case MetaOpCode.Lerp:
                        {
                            float t = math.clamp(stack[--sp], 0f, 1f);
                            float b = stack[--sp];
                            float a = stack[--sp];
                            stack[sp++] = a + (b - a) * t;
                        }
                        break;

                    case MetaOpCode.Smoothstep:
                        {
                            float v = stack[--sp];
                            float e1 = stack[--sp];
                            float e0 = stack[--sp];
                            if (e0 == e1)
                            {
                                stack[sp++] = v < e0 ? 0f : 1f;
                            }
                            else
                            {
                                float t = math.clamp((v - e0) / (e1 - e0), 0f, 1f);
                                stack[sp++] = t * t * (3f - 2f * t);
                            }
                        }
                        break;

                    case MetaOpCode.Remap:
                        {
                            float out1 = stack[--sp];
                            float out0 = stack[--sp];
                            float in1 = stack[--sp];
                            float in0 = stack[--sp];
                            float v = stack[--sp];

                            if (in0 == in1) { stack[sp++] = out0; }
                            else
                            {
                                float t = (v - in0) / (in1 - in0);
                                stack[sp++] = out0 + (out1 - out0) * t;
                            }
                        }
                        break;

                    case MetaOpCode.Sqrt:
                        {
                            float v = stack[sp - 1];
                            stack[sp - 1] = (v <= 0f) ? 0f : math.sqrt(v);
                        }
                        break;

                    case MetaOpCode.Pow:
                        {
                            float e = stack[--sp];
                            float b = stack[--sp];
                            stack[sp++] = math.pow(b, e);
                        }
                        break;

                    case MetaOpCode.Rand01:
                        {
                            uint h = HashTile(grid.seed, x, y, ins.Salt);
                            stack[sp++] = (h & 0x00FFFFFFu) / 16777216f;
                        }
                        break;

                    case MetaOpCode.RandRange:
                        {
                            float hi = stack[--sp];
                            float lo = stack[--sp];
                            uint h = HashTile(grid.seed, x, y, ins.Salt);
                            float t = (h & 0x00FFFFFFu) / 16777216f;
                            stack[sp++] = lo + (hi - lo) * t;
                        }
                        break;
                }
            }

            return stack[sp - 1];
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
}
