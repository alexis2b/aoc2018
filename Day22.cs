using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aoc2018
{
    internal class Day22
    {
        public static void Run()
        {
            var depth  = 11109;
            var target = (9, 731);

            var res1 = ComputeRiskLevel(depth, target);
            Console.WriteLine($"Day22 - part1 - result: {res1}");

            var res2 = FindShortestRoute(depth, target);
            Console.WriteLine($"Day22 - part2 - result: {res2}");
        }

        public static long ComputeRiskLevel(int depth, (int, int) target)
        {
            // Initialize Map
            var map = new long[target.Item1 + 1, target.Item2 + 1];
            for (var x = 0; x <= target.Item1; x++)
            for (var y = 0; y <= target.Item2; y++)
                map[x, y] = -1;

            long ErosionLevel(long geologicIndex) => (geologicIndex + depth) % 20183;

            long GeologicIndexAt(int x, int y)
            {
                if (map[x, y] == -1) // net yet calculated
                {
                    if (x == 0 && y == 0) map[x, y] = 0;
                    else if (x == 0) map[x, y] = y * 48271;
                    else if (y == 0) map[x, y] = x * 16807;
                    else map[x, y] = ErosionLevel(GeologicIndexAt(x - 1, y)) * ErosionLevel(GeologicIndexAt(x, y - 1));
                }
                return map[x, y];
            }

            // Compute geologic indexes
            var riskLevel = 0L;
            for (var y = 0; y <= target.Item2; y++)
            for (var x = 0; x <= target.Item1; x++)
            {
                var geologicIndex = GeologicIndexAt(x, y);
                var erosionLevel  = ErosionLevel(geologicIndex);
                var regionType    = erosionLevel % 3;
                if ( x != target.Item1 || y != target.Item2 )
                    riskLevel += regionType;
            }

            return riskLevel;
        }

        public static int FindShortestRoute(int depth, (int x, int y) target)
        {
            // Concepts:
            // Path -> series of "Steps", going from a start to a destination
            //    all intermediate points on a Path are "interim destinations"
            //    all points on the map have a "best possible time" (meaning the optimal paths)
            //    once a "best possible time" is found for a point, any less optimal path should be dropped
            // Steps -> either a "move", going to another direction, or a "changeTool" to switch equipments
            //    moves take 1 minutes, equip take 7 minutes and are required to perform certain moves
            // Since points can support two equipments, it's interesting to keep the "best possible time" carrying a certain type of equipment
            //   since that could be beneficial to reach a further point faster
            // Paths can be dropped if
            //   -> their current accumulated time is larger than the current "best possible time" reaching the Target
            //   -> if the interim destination they reach has already been reached by a better time
            //   -> probably also some kind of "distance to target" heuristic (like further than 2 times the original distance)
            //   -> there is no possible move with the current equipment
            // Each path has a "frontier" which spawns new paths (linked list model to save memory?) with every possible move
            // When generating paths, we should consider two oscillating modes:
            // - generate an "equip" step (+ a non-existent step)
            // - generate a "move" step

            // Algorithm
            // From a current path (frontier)
            // - generate all new "equip"
            // - then generate all new possible "moves" for each "equip" (if possible)
            // - check each destination found:
            //   - if target has been reached and path time is >= current best possible target time, drop
            //   - if destination is 2x as far as initial target distance, drop
            //   - compare with "current best time" for (destination, equip) key of each path
            //     - if better, record new path as "current best time" and add to frontier
            //     - if not better, drop
            var searcher = new PathSearcher(depth, target);
            var shortestTime = searcher.Search();
            return shortestTime;
        }

        /// <summary>
        /// Stores the state of a search, broken down into smaller functions, rather than a big one
        /// </summary>
        internal sealed class PathSearcher
        {
            private readonly (int x, int y) _target;
            private readonly (int x, int y) _searchBoundaries;
            private readonly int[,] _terrainType;

            // State
            private int _time;
            private readonly Dictionary<(int x, int y, Tool tool), Path> _optimums = new Dictionary<(int x, int y, Tool tool), Path>();
            private readonly List<Path> _frontier = new List<Path>();

            public PathSearcher(int depth, (int x, int y) target)
            {
                _target = target;
                _searchBoundaries = (4*target.x, 4*target.y); // max search area distance, reduce if too slow
                Console.WriteLine("Generating terrain type map...");
                _terrainType = GenerateTerrainTypeMap(depth, target, 5);

                // check terrain type map generation that we find the same result as Part 1
                var riskLevel = 0L;
                for (var y = 0; y <= target.y; y++)
                for (var x = 0; x <= target.x; x++)
                    riskLevel += _terrainType[x,y];
                Console.WriteLine($"Terrain type map check: riskLevel={riskLevel} (should match part 1)");
            }

            // Terrain Type (rocky, etc.) builder
            // coverRatio: 3 means covers, 3x the target area dimensions (need to overcover for path search)
            private static int[,] GenerateTerrainTypeMap(int depth, (int x, int y) target, int coverRatio)
            {
                long ErosionLevel(long geologicIndex) => (geologicIndex + depth) % 20183;

                // Initialize Map
                (int x, int y) dims = (coverRatio * target.x + 1, coverRatio * target.y + 1);
                var geologicIndexMap = new long[dims.x, dims.y];
                var terrainTypeMap   = new int [dims.x, dims.y];
                for (var x = 0; x < dims.x; x++)
                for (var y = 0; y < dims.y; y++)
                {
                    if (x == 0 && y == 0) geologicIndexMap[x, y] = 0;
                    else if (y == 0)      geologicIndexMap[x, y] = x * 16807;
                    else if (x == 0)      geologicIndexMap[x, y] = y * 48271;
                    else                  geologicIndexMap[x, y] = ErosionLevel(geologicIndexMap[x-1, y]) * ErosionLevel(geologicIndexMap[x, y-1]);
                    // Map to terrain type
                    terrainTypeMap[x, y] = (int) (ErosionLevel(geologicIndexMap[x, y]) % 3);
                }
                terrainTypeMap[target.x, target.y] = 0; // target position is of type "rocky"

                return terrainTypeMap;
            }



            public int Search()
            {
                Initialize();
                while (true)
                {
                    _time++;
                    var winningPath = RunCycle(_time);
                    if (winningPath != null)
                        return winningPath.Time;
                }
            }

            private void Initialize()
            {
                _time = 0;
                _optimums.Clear();
                _frontier.Clear();
                _frontier.Add(Path.Origin);
            }

            private Path RunCycle(int time)
            {
                // Extract list of "expired" path (i.e. path whose total duration is in the past)
                // Remove them from the frontier
                var expiredPaths = _frontier.Where(p => p.Time < time).ToList();
                expiredPaths.ForEach(p => _frontier.Remove(p));
                Console.WriteLine($"[T:{time:D4}] - Begin Cycle - expiredPaths:{expiredPaths.Count}, postponed:{_frontier.Count}");

                // for each expired paths
                // check if it's a winning path (or ready to win)
                // else generate the new paths
                foreach (var path in expiredPaths)
                {
                    if (IsWinningPath(path))
                    {
                        Console.WriteLine($"winning path found at time {time}: {path}");
                        return path;
                    }

                    // generate all possible Steps
                    // 1st pass -> equip (based on current equipment, and current region)
                    // 2nd pass -> move (based on current equipment, and future region)

                    // for each step, check conditions
                    // if ok, add to frontier, else drop
                    // repeat until frontier is empty
                    // best time is found by finding the result of reaching target with the torch in the "best times" map

                    var childPaths = GenerateChildPaths(path);
                    foreach (var childPath in childPaths)
                    {
                        if (IsWorthyChildPath(childPath))
                        {
                            _optimums[(childPath.X, childPath.Y, childPath.Tool)] = childPath;
                            _frontier.Add(childPath);
                        }
                    }
                }

                Console.WriteLine($"[T:{time:D4}] - End Cycle - frontier:{_frontier.Count}");
                return null;
            }

            private bool IsWinningPath(Path path) => IsTargetPosition(path) && path.Tool == Tool.Torch;

            private bool IsTargetPosition(Path path) => path.X == _target.x && path.Y == _target.y;

            private IEnumerable<Path> GenerateChildPaths(Path path)
            {
                // special case: target position but without a torch, only need to switch to torch to win
                if (IsTargetPosition(path))
                    return new[] {ChangeTool.EquipTorch.NextPath(path)};

                // step 1 - start by switching equipments (or not)
                var step1Paths = new List<Path> {path}; // start with not switching
                if ( path.Tool != Tool.Torch        && CanUseTool(path, Tool.Torch))        step1Paths.Add( ChangeTool.EquipTorch.NextPath(path) );
                if ( path.Tool != Tool.ClimbingGear && CanUseTool(path, Tool.ClimbingGear)) step1Paths.Add( ChangeTool.EquipClimbingGear.NextPath(path) );
                if ( path.Tool != Tool.Neither      && CanUseTool(path, Tool.Neither))      step1Paths.Add( ChangeTool.EquipNeither.NextPath(path) );

                // step 2 - for each, attempt all possible moves
                var step2Paths = new List<Path>();
                foreach (var step1Path in step1Paths)
                foreach (var move in Move.AllMoves)
                {
                    var step2Path = move.NextPath(step1Path);
                    if (IsPossiblePath(step2Path) )
                        step2Paths.Add(step2Path);
                }
                return step2Paths;
            }

            private bool IsPossiblePath(Path path) => path.X >= 0 && path.Y >= 0 && CanUseTool(path, path.Tool);

            private bool CanUseTool(Path path, Tool tool)
            {
                var terrainType = _terrainType[path.X, path.Y];
                switch (tool)
                {
                    case Tool.Torch:        return terrainType == 0 || terrainType == 2;
                    case Tool.ClimbingGear: return terrainType == 0 || terrainType == 1;
                    case Tool.Neither:      return terrainType == 1 || terrainType == 2;
                    default:                throw new NotImplementedException("missing implementation for tool " + tool);
                }
            }

            // true if a path is "worthy" to be followed, some reasons why it might be false:
            // -> if the interim destination they reach has already been reached by a better time
            // -> probably also some kind of "distance to target" heuristic (like further than 2 times the original distance)
            private bool IsWorthyChildPath(Path path)
            {
                if (path.X > _searchBoundaries.x || path.Y > _searchBoundaries.y)
                    return false;

                var key = (path.X, path.Y, path.Tool);
                if (_optimums.TryGetValue(key, out var currentOptimumPath))
                    return path.Time < currentOptimumPath.Time;
                return true;
            }
        }



        internal interface IStep
        {
            Path NextPath(Path start);
        }

        internal sealed class Move : IStep
        {
            public static readonly Move MoveRight = new Move( 1,  0);
            public static readonly Move MoveDown  = new Move( 0,  1);
            public static readonly Move MoveLeft  = new Move(-1,  0);
            public static readonly Move MoveUp    = new Move( 0, -1);
            public static readonly Move[] AllMoves = {MoveRight, MoveDown, MoveLeft, MoveUp};

            private readonly int _dx;
            private readonly int _dy;


            private Move(int dx, int dy)
            {
                _dx = dx;
                _dy = dy;
            }

            public Path NextPath(Path from)
                => new Path(from, this, from.X + _dx, from.Y + _dy, from.Time + 1, from.Tool);
        }

        internal enum Tool { Torch, ClimbingGear, Neither };

        internal sealed class ChangeTool : IStep
        {
            public static readonly ChangeTool EquipTorch        = new ChangeTool(Tool.Torch);
            public static readonly ChangeTool EquipClimbingGear = new ChangeTool(Tool.ClimbingGear);
            public static readonly ChangeTool EquipNeither      = new ChangeTool(Tool.Neither);

            private readonly Tool _newTool;

            public ChangeTool(Tool newTool)
            {
                _newTool = newTool;
            }

            public Path NextPath(Path from)
                => new Path(from, this, from.X, from.Y, from.Time + 7, _newTool);
        }

        // Current state of a search
        internal sealed class Path
        {
            public static readonly Path Origin = new Path(null, null, 0, 0, 0, Tool.Torch);

            private readonly Path _previous;
            private readonly IStep _step;

            public int X { get; }
            public int Y { get; }
            public int Time { get; }
            public Tool Tool { get; }

            public Path(Path previous, IStep step, int x, int y, int time, Tool tool)
            {
                X = x;
                Y = y;
                Time = time;
                Tool = tool;
                _previous = previous;
                _step = step;
            }

            public override string ToString() => $"Path({X},{Y},{Tool},{Time})";
        }
    }


    [TestFixture]
    internal class Day22Tests
    {
        [Test]
        public void Test1()
        {
            var res = Day22.ComputeRiskLevel(510, (10, 10));
            Assert.AreEqual(114, res);
        }

        [Test]
        public void Test2()
        {
            var res = Day22.FindShortestRoute(510, (10, 10));
            Assert.AreEqual(45, res);
        }
    }
}
