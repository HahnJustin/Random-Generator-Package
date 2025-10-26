using Dalichrome.RandomGenerator.Configs;
using Dalichrome.RandomGenerator.Core;
using Dalichrome.RandomGenerator.Data;
using Dalichrome.RandomGenerator.Utils;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Dalichrome.RandomGenerator.Generators
{
    public class RoomConnectionGenerator : AbstractGenerator<RoomConnectionConfig>
    {
        private RoomUtil util;
        private readonly int INITIAL_RING_VALUE = 1;

        // Fixed directions (kept for retrace & fallbacks)
        private static readonly Direction[] DIRS = new[]
        {
            Direction.Down, Direction.Up, Direction.Right, Direction.Left
        };

        // visit-stamp offset so we don’t ClearPositiveNumbers() each room
        private int _nextVisitBase = 10_000_000;

        // scratch buffers (reused)
        private readonly List<int2> _ringA = new(256);
        private readonly List<int2> _ringB = new(256);
        private readonly Dictionary<int, RoomInfo> _roomInfos = new(8);

        private class RoomInfo
        {
            public Room Room { get; }
            public int2 Position { get; }     // contact cell (positive ring cell)
            public int RingCount { get; }
            public int TargetRoomId { get; }  // negative id of the other room
            public int VisitBase { get; }     // visit-stamp for retrace

            public RoomInfo(Room room, int2 pos, int ringCount, int targetRoomId, int visitBase)
            {
                Room = room;
                Position = pos;
                RingCount = ringCount;
                TargetRoomId = targetRoomId;
                VisitBase = visitBase;
            }
        }

        private List<Room> currentRooms;

        public RoomConnectionGenerator(RoomConnectionConfig config) : base(config)
        {
            this.config = config;
            util = new(config);
            AddUtil(util);
        }

        protected override Generation Enact(Generation input)
        {
            currentRooms = util.LargestFirstRoomList;

            for (int i = currentRooms.Count - 1; i >= 0; i--)
            {
                Room room = currentRooms[i];
                if (room == null) continue;

                // visitBase for this source room
                int visitBase = _nextVisitBase;
                _nextVisitBase += 1_000_000;
                if (_nextVisitBase > int.MaxValue / 2)
                {
                    TileGrid.ClearPositiveNumbers();
                    _nextVisitBase = 10_000_000;
                    visitBase = _nextVisitBase;
                    _nextVisitBase += 1_000_000;
                }

                // --- compute target centroid (nearest other room) ---
                int2 sourceC = ComputeCentroid(room);
                Room nearest = null;
                int bestD2 = int.MaxValue;
                for (int j = 0; j < currentRooms.Count; j++)
                {
                    var other = currentRooms[j];
                    if (other == null || other == room) continue;
                    int2 oc = ComputeCentroid(other);
                    int dx = oc.x - sourceC.x, dy = oc.y - sourceC.y;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < bestD2) { bestD2 = d2; nearest = other; }
                }
                int2 targetC = nearest != null ? ComputeCentroid(nearest) : sourceC;
                bool flipBias = false;

                // seed ring
                _roomInfos.Clear();
                _ringA.Clear();
                _ringA.AddRange(room.Edges);
                List<int2> ringCurr = _ringA;
                List<int2> ringNext = _ringB;

                int ringValue = INITIAL_RING_VALUE;

                while (((_roomInfos.Count <= 0 && !config.AdditionalConnections) ||
                       ((_roomInfos.Count <= 0 || ringValue < config.AdditionalUpperBound + INITIAL_RING_VALUE) && config.AdditionalConnections)) &&
                       ringCurr.Count > 0)
                {
                    ringNext.Clear();

                    foreach (int2 pos in ringCurr)
                    {
                        // --- biased direction order toward targetC ---
                        GetBiasedOrder(pos, targetC, flipBias, out var d0, out var d1, out var d2, out var d3);

                        // inline loop over the 4 directions without extra allocations
                        // (unrolled for clarity; keep as-is if you prefer a small loop)
                        TryExpand(pos, d0, visitBase, ringValue, room, targetC, _roomInfos, ringNext);
                        if (!config.AdditionalConnections && _roomInfos.Count > 0) break;

                        TryExpand(pos, d1, visitBase, ringValue, room, targetC, _roomInfos, ringNext);
                        if (!config.AdditionalConnections && _roomInfos.Count > 0) break;

                        TryExpand(pos, d2, visitBase, ringValue, room, targetC, _roomInfos, ringNext);
                        if (!config.AdditionalConnections && _roomInfos.Count > 0) break;

                        TryExpand(pos, d3, visitBase, ringValue, room, targetC, _roomInfos, ringNext);
                        if (!config.AdditionalConnections && _roomInfos.Count > 0) break;
                    }

                    (ringCurr, ringNext) = (ringNext, ringCurr);
                    ringValue += 1;
                    flipBias = !flipBias; // small jitter so we don’t tunnel too narrowly
                    CancelCheck();
                }

                bool consolidate = true;
                foreach (RoomInfo info in _roomInfos.Values)
                {
                    CreatePathBetweenRooms(info, consolidate);
                    consolidate = false;
                }
            }

            TileGrid.ClearPositiveNumbers();
            return input;
        }

        // expansion step for a single direction
        private void TryExpand(
            int2 pos,
            Direction direction,
            int visitBase,
            int ringValue,
            Room room,
            int2 targetC,
            Dictionary<int, RoomInfo> roomInfos,
            List<int2> ringNext)
        {
            int2 point = pos.GetPointInDirection(direction);
            if (TileGrid.IsRestricted(point)) return;

            int adjValue = TileGrid.GetTileValue(point);

            // unvisited positive -> visit & push to next ring
            if (adjValue >= 0 && adjValue < visitBase)
            {
                TileGrid.SetTileValue(point, visitBase + ringValue);
                ringNext.Add(point);
                return;
            }

            // hit another room (negative id)
            if (adjValue < 0 && adjValue != room.Value)
            {
                // TryAdd avoids double hash lookup
                if (!roomInfos.ContainsKey(adjValue))
                {
                    roomInfos.TryAdd(adjValue, new RoomInfo(room, point, ringValue, adjValue, visitBase));
                }
            }
        }

        private void CreatePathBetweenRooms(RoomInfo info, bool consolidateRooms)
        {
            Room room = info.Room;
            int2 pos = info.Position;       // contact positive cell
            int value = info.RingCount;
            int visitBase = info.VisitBase;

            int safety = TileGrid.width * TileGrid.height;

            while (value >= INITIAL_RING_VALUE && safety-- > 0)
            {
                int target = visitBase + (value - 1);
                bool stepped = false;

                // retrace can use a fixed order; bias not needed here
                for (int k = 0; k < 4; k++)
                {
                    var d = DIRS[k];
                    var n = pos.GetPointInDirection(d);
                    if (TileGrid.IsRestricted(n)) continue;

                    if (TileGrid.GetTileValue(n) == target)
                    {
                        TileGrid.SetTileId(n, config.HallwayTile);
                        if (consolidateRooms)
                        {
                            room.AddEdge(n);
                            TileGrid.SetTileValue(n, room.Value);
                        }
                        pos = n;
                        value -= 1;
                        stepped = true;
                        break;
                    }
                }

                if (!stepped) break;
            }

            if (!consolidateRooms) return;

            if (!util.Rooms.TryGetValue(info.TargetRoomId, out var room2))
                return;

            currentRooms.Remove(room2);
            util.Rooms.Remove(room2.Value);

            foreach (int2 roomPos in room2)
                TileGrid.SetTileValue(roomPos, room.Value);

            room.AddEdgeRange(room2.Edges);
        }

        // ----- helpers for bias -----

        private static int2 ComputeCentroid(Room r)
        {
            long sx = 0, sy = 0; int count = 0;
            foreach (var p in r) { sx += p.x; sy += p.y; count++; }
            if (count == 0) return int2.zero;
            return new int2((int)(sx / count), (int)(sy / count));
        }

        // Emits a biased order (four directions) without allocations
        private static void GetBiasedOrder(int2 from, int2 target, bool flipBias,
                                           out Direction d0, out Direction d1, out Direction d2, out Direction d3)
        {
            int dx = target.x - from.x;
            int dy = target.y - from.y;

            bool horizFirst = math.abs(dx) >= math.abs(dy);

            Direction hx = dx >= 0 ? Direction.Right : Direction.Left;
            Direction hxOpp = dx >= 0 ? Direction.Left : Direction.Right;
            Direction hy = dy >= 0 ? Direction.Up : Direction.Down;
            Direction hyOpp = dy >= 0 ? Direction.Down : Direction.Up;

            if (horizFirst)
            {
                if (flipBias) { d0 = hx; d1 = hyOpp; d2 = hy; d3 = hxOpp; }
                else { d0 = hx; d1 = hy; d2 = hyOpp; d3 = hxOpp; }
            }
            else
            {
                if (flipBias) { d0 = hy; d1 = hxOpp; d2 = hx; d3 = hyOpp; }
                else { d0 = hy; d1 = hx; d2 = hxOpp; d3 = hyOpp; }
            }
        }
    }
}
