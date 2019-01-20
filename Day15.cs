using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day15
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day15.txt");

            var res1 = CombatOutcome(input);
            Console.WriteLine($"Day15 - part1 - result: {res1}");

            var res2 = CombatOutcomeWithElvesBoost(input);
            Console.WriteLine($"Day15 - part2 - result: {res2}");
        }

        public static int CombatOutcome(string[] input) => Game.BuildFrom(input).Simulate().score;

        public static int CombatOutcomeWithElvesBoost(string[] input)
        {
            for (var boost = 1;; boost++)
            {
                Console.WriteLine($"!!! Giving Elves a boot of {boost}");
                var (team, score) = Game.BuildFrom(input).WithElfBoostMode(boost).Simulate();
                if (team == 'E')
                    return score;
            }
        }

        internal sealed class Game
        {
            private readonly int _width;
            private readonly int _height;
            private readonly List<Unit> _units = new List<Unit>();
            private readonly HashSet<(int r, int c)> _walls = new HashSet<(int x, int y)>();
            private bool _elfBoostMode;


            private Game(int width, int height)
            {
                _width  = width;
                _height = height;
            }

            private int RemainingHitPoints => _units.Where(u => u.IsAlive).Sum(u => u.HitPoints);

            public static Game BuildFrom(string[] input)
            {
                var height = input.Length;
                var width  = input[0].Length;
                var game   = new Game(width, height);
                var unitId = 0;
                for(var r = 0; r < height; r++)
                for(var c = 0; c < width; c++)
                switch (input[r][c])
                {
                    case '#': game._walls.Add((r, c)); break;
                    case 'E':
                    case 'G': game._units.Add(new Unit(input[r][c], unitId++, (r, c))); break;
                }
                return game;
            }


            // Sets game mode to "Elf Boost" (problem part 2):
            // - Gobelins win as soon as the first Elf dies
            // - A constant Elf Boost is added to Elf Attack Power
            public Game WithElfBoostMode(int boost)
            {
                _elfBoostMode = true;
                foreach (var elfUnit in _units.Where(u => u.Army == 'E'))
                    elfUnit.AttackPower += boost;
                return this;
            }

            public (char team, int score) Simulate()
            {
                var completedRounds = 0;
                while (true)
                {
                    if (!PlayRound(completedRounds + 1))
                        break;
                    completedRounds++;
                }
                // Outcome
                var elvesLoose = _units.Any(u => u.IsAlive && u.Army == 'G');
                var score      = completedRounds * RemainingHitPoints;
                return (elvesLoose ? 'G':'E', score);
            }

            private bool PlayRound(int round)
            {
                Console.SetCursorPosition(0, 0);
                Console.Clear();
                Console.WriteLine($"\n= Playing Round#{round}");
                var (elfTotal, elfAlive, elfStrength, elfMinHp, gobTotal, gobAlive, gobStrength) = GameStats();
                Console.WriteLine($"Elves   : {elfAlive}/{elfTotal} - {elfStrength}HP - weakest: {elfMinHp}HP");
                Console.WriteLine($"Gobelins: {gobAlive}/{gobTotal} - {gobStrength}HP");
                Console.Write(Describe());

                var hasMoved     = false;
                var turnSequence = _units.OrderBy(u => u.Position.r).ThenBy(u => u.Position.c).ToList();
                var elvesCount   = _units.Count(u => u.Army == 'E' && u.IsAlive);
                foreach (var activeUnit in turnSequence)
                {
                    if (!activeUnit.IsAlive) continue;
                    var targets = _units.Where(u => u.IsAlive && u.Army != activeUnit.Army).ToList();
                    if (targets.Count == 0) return false; // stops immediately the combat
                    hasMoved = PlayTurn(activeUnit, targets) || hasMoved;

                    // In Elf Boost mode, if an Elf has died, combat stops immediately
                    var newElvesCount = _units.Count(u => u.Army == 'E' && u.IsAlive);
                    if (_elfBoostMode && newElvesCount < elvesCount)
                    {
                        Console.WriteLine("(Elf Boost Mode) An Elf is dead, stopping");
                        return false;
                    }
                }
                return hasMoved;
            }

            private (int elfTotal, int elfAlive, int elfStrength, int elfMinHp, int gobTotal, int gobAlive, int gobStrength)
                GameStats()
            {
                int elfTotal = 0, elfAlive = 0, gobTotal = 0, gobAlive = 0, elfStrength = 0, gobStrength = 0, elfMinHp = Int32.MaxValue;
                foreach (var unit in _units)
                {
                    if (unit.Army == 'E')
                    {
                        elfTotal++;
                        if (unit.IsAlive)
                        {
                            elfAlive++;
                            elfStrength += unit.HitPoints;
                            if (unit.HitPoints < elfMinHp)
                                elfMinHp = unit.HitPoints;
                        }
                    }
                    else
                    {
                        gobTotal++;
                        if (unit.IsAlive)
                        {
                            gobAlive++;
                            gobStrength += unit.HitPoints;
                        }
                    }
                }

                return (elfTotal, elfAlive, elfStrength, elfMinHp, gobTotal, gobAlive, gobStrength);
            }

            private bool PlayTurn(Unit activeUnit, List<Unit> targets)
            {
                // try to attack immediately
                var myAdjacentSquares      = activeUnit.GetAdjacentSquares().ToList();
                var inImmediateAttackRange = targets.Where(t => myAdjacentSquares.Contains(t.Position))
                    .OrderBy(t => t.HitPoints)
                    .ThenBy(t => t.Position.r).ThenBy(t => t.Position.c).ToList();
                if (inImmediateAttackRange.Any())
                {
                    var attackedUnit = inImmediateAttackRange.First();
                    Console.WriteLine($"{activeUnit} attacks immediately {attackedUnit}");
                    attackedUnit.Attacked(activeUnit.AttackPower);
                    return true;
                }

                // no immediate attack, make a move
                // algorithm:
                // start for each "in range of unit" position and search the closest "in range of active unit"
                // objective is to retrieve all of the the (from, distance, to) tuples
                var inRangePositions        = targets.SelectMany(GetAttackRangePositions).ToList();
                var nextPossiblePositions   = GetMoveRangePositions(activeUnit.Position).ToList();
                var possibleAttackPositions = inRangePositions.Select(p => (p.r, p.c)).ToList();
                var resolvedMoves = FindPossibleMoves(possibleAttackPositions, nextPossiblePositions);
                var orderedMoves  = resolvedMoves
                    .OrderBy(m => m.steps)
                    .ThenBy(m => m.to.r).ThenBy(m => m.to.c)
                    .ThenBy(m => m.from.r).ThenBy(m => m.from.c).ToList();
                if (orderedMoves.Any())
                {
                    var bestMove = orderedMoves[0];
                    //Console.WriteLine($"{activeUnit} follows best move {bestMove}");
                    activeUnit.MoveTo((bestMove.from.r, bestMove.from.c));

                    // now attempt an attack again
                    var myAdjacentSquares2      = activeUnit.GetAdjacentSquares().ToList();
                    var inImmediateAttackRange2 = targets.Where(t => myAdjacentSquares2.Contains(t.Position))
                        .OrderBy(t => t.HitPoints)
                        .ThenBy(t => t.Position.r).ThenBy(t => t.Position.c).ToList();
                    if (inImmediateAttackRange2.Any())
                    {
                        var attackedUnit = inImmediateAttackRange2.First();
                        Console.WriteLine($"{activeUnit} attacks {attackedUnit}");
                        attackedUnit.Attacked(activeUnit.AttackPower);
                    }

                    return true;
                }
                //Console.WriteLine($"{activeUnit} can not move");
                return false;
            }

            // find all open positions where the given unit is in range (to move or to be attacked)
            private IEnumerable<(int r, int c, Unit target)> GetAttackRangePositions(Unit unit)
                => unit.GetAdjacentSquares()
                    .Except(_walls)
                    .Except(_units.Where(u => u.IsAlive).Select(u => u.Position))
                    .Select(p => (p.r, p.c, unit));

            private IEnumerable<(int r, int c)> GetMoveRangePositions((int r, int c) p)
                => new[] {(p.r, p.c + 1),(p.r - 1, p.c),(p.r, p.c - 1),(p.r + 1, p.c)}
                    .Except(_walls)
                    .Except(_units.Where(u => u.IsAlive).Select(u => u.Position));

            private IEnumerable<((int r, int c) from, (int r, int c) to, int steps)> FindPossibleMoves(
                List<(int r, int c)> objectives, List<(int r, int c)> starts)
                => starts.SelectMany(s => FindPossibleMoves(s, objectives));

            private IEnumerable<((int r, int c) from, (int r, int c) to, int steps)> FindPossibleMoves(
                (int r, int c) start, List<(int r, int c)> objectives)
            {
                // perform a breadth first search from objective until one of the "objectives" is reached
                var frontier = new List<(int r, int c)> { start };
                var visited = new HashSet<(int r, int c)>();
                for (var i = 1; ; i++)
                {
                    var reached = objectives.Intersect(frontier).ToList();
                    if (reached.Count > 0)
                        return reached.Select(r => (start, r, i));
                    // expand the frontier
                    frontier = frontier.SelectMany(GetMoveRangePositions).Where(visited.Add).ToList();
                    if (frontier.Count == 0) // no path found
                        return Enumerable.Empty<((int r, int c) from, (int r, int c) to, int steps)>();
                }
            }



            public string Describe()
            {
                string UnitInfo(Unit u) => $"{u.Army}{u.Id}({u.HitPoints})";

                var sb = new StringBuilder();
                var unitsPositions = _units.Where(u => u.IsAlive).ToDictionary(u => u.Position);

                for (var r = 0; r < _height; r++)
                {
                    var rowUnits = new List<Unit>();
                    for (var c = 0; c < _width; c++)
                    {
                        if (_walls.Contains((r, c))) sb.Append('#');
                        else if (unitsPositions.TryGetValue((r, c), out Unit unit))
                        {
                            rowUnits.Add(unit);
                            sb.Append(unit.Army);
                        }
                        else sb.Append('.');
                    }

                    if (rowUnits.Any())
                        sb.Append("   ").Append(string.Join(", ", rowUnits.Select(UnitInfo)));

                    sb.AppendLine();
                }

                return sb.ToString();
            }
        }

        internal sealed class Unit
        {
            private const int StartHitPoints  = 200;
            private const int BaseAttackPower = 3;

            public Unit(char army, int id, (int r, int c) startPosition)
            {
                Army = army;
                Id = id;
                Position = startPosition;
                HitPoints = StartHitPoints;
                AttackPower = BaseAttackPower;
            }
            
            public char Army { get; }
            public int Id { get; }
            public (int r, int c) Position { get; private set; }
            public int HitPoints { get; set; }
            public int AttackPower { get; set; }
            public bool IsAlive => HitPoints > 0;

            public IEnumerable<(int r, int c)> GetAdjacentSquares()
                => new[]
                {
                    (Position.r, Position.c + 1),
                    (Position.r - 1, Position.c),
                    (Position.r, Position.c - 1),
                    (Position.r + 1, Position.c)
                };


            public void MoveTo((int r, int c) to)
            {
                Position = to;
            }

            public void Attacked(int attackPower)
            {
                // In Elf Boost Mode, Gobelins take more hits
                HitPoints -= attackPower;
                if (!IsAlive)
                    Console.WriteLine($"{this} dies");
            }

            public override string ToString()
                => $"{Army}/{Id}/{HitPoints}HP@{Position}";
        }
    }




    [TestFixture]
    internal class Day15Tests
    {
        [Test]
        public void Test1_1()
        {
            string[] input = {
                "#######",
                "#.G...#",
                "#...EG#",
                "#.#.#G#",
                "#..G#E#",
                "#.....#",
                "#######"
            };

            var res = Day15.CombatOutcome(input);
            Assert.AreEqual(27730, res);
        }

        [Test]
        public void Test1_2()
        {
            string[] input = {
                "#######",
                "#G..#E#",
                "#E#E.E#",
                "#G.##.#",
                "#...#E#",
                "#...E.#",
                "#######"
            };

            var res = Day15.CombatOutcome(input);
            Assert.AreEqual(36334, res);
        }

        [Test]
        public void Test1_3()
        {
            string[] input = {
                "#######",
                "#E..EG#",
                "#.#G.E#",
                "#E.##E#",
                "#G..#.#",
                "#..E#.#",
                "#######"
            };

            var res = Day15.CombatOutcome(input);
            Assert.AreEqual(39514, res);
        }

        [Test]
        public void Test1_4()
        {
            string[] input = {
                "#######",
                "#E.G#.#",
                "#.#G..#",
                "#G.#.G#",
                "#G..#.#",
                "#...E.#",
                "#######"
            };

            var res = Day15.CombatOutcome(input);
            Assert.AreEqual(27755, res);
        }

        [Test]
        public void Test1_5()
        {
            string[] input = {
                "#######",
                "#.E...#",
                "#.#..G#",
                "#.###.#",
                "#E#G#G#",
                "#...#G#",
                "#######"
            };

            var res = Day15.CombatOutcome(input);
            Assert.AreEqual(28944, res);
        }

        [Test]
        public void Test1_6()
        {
            string[] input = {
                "#########",
                "#G......#",
                "#.E.#...#",
                "#..##..G#",
                "#...##..#",
                "#...#...#",
                "#.G...G.#",
                "#.....G.#",
                "#########"
            };

            var res = Day15.CombatOutcome(input);
            Assert.AreEqual(18740, res);
        }

        // ------------- Part 2 -----------
        
        [Test]
        public void Test2_1()
        {
            string[] input = {
                "#######",
                "#.G...#",
                "#...EG#",
                "#.#.#G#",
                "#..G#E#",
                "#.....#",
                "#######"
            };

            var res = Day15.CombatOutcomeWithElvesBoost(input);
            Assert.AreEqual(4988, res);
        }

        [Test]
        public void Test2_2()
        {
            string[] input = {
                "#######",
                "#E..EG#",
                "#.#G.E#",
                "#E.##E#",
                "#G..#.#",
                "#..E#.#",
                "#######"
            };

            var res = Day15.CombatOutcomeWithElvesBoost(input);
            Assert.AreEqual(31284, res);
        }

        [Test]
        public void Test2_3()
        {
            string[] input = {
                "#######",
                "#E.G#.#",
                "#.#G..#",
                "#G.#.G#",
                "#G..#.#",
                "#...E.#",
                "#######"
            };

            var res = Day15.CombatOutcomeWithElvesBoost(input);
            Assert.AreEqual(3478, res);
        }

        [Test]
        public void Test2_4()
        {
            string[] input = {
                "#######",
                "#.E...#",
                "#.#..G#",
                "#.###.#",
                "#E#G#G#",
                "#...#G#",
                "#######"
            };

            var res = Day15.CombatOutcomeWithElvesBoost(input);
            Assert.AreEqual(6474, res);
        }

        [Test]
        public void Test2_5()
        {
            string[] input = {
                "#########",
                "#G......#",
                "#.E.#...#",
                "#..##..G#",
                "#...##..#",
                "#...#...#",
                "#.G...G.#",
                "#.....G.#",
                "#########"
            };

            var res = Day15.CombatOutcomeWithElvesBoost(input);
            Assert.AreEqual(1140, res);
        }
    }
}
