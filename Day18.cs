using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day18
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day18.txt");

            var res1 = CountResourcesAfter(input, 10);
            Console.WriteLine($"Day18 - part1 - result: {res1}");

            var res2 = CountResourcesAfter(input, 1000000000);
            Console.WriteLine($"Day18 - part2 - result: {res2}");
        }


        public static int CountResourcesAfter(string[] input, int until)
        {
            var previousStates = new Dictionary<string, int>();
            var gameState = GameState.BuildFrom(input);
            Console.WriteLine("Initial state:\n" + gameState.Describe());
            for (var i = 1; i <= until; i++)
            {
                gameState = gameState.NextState();
                var stateString = gameState.Describe();
                // Console.WriteLine("\nAfter " + i + " minute:\n" + stateString);

                // Optimization - look for cycles
                if (previousStates.TryGetValue(stateString, out var previousI))
                {
                    // we entered a cycle, directly jump to the relevant step we have stored
                    Console.WriteLine($"Cycle found at step {i} with step {previousI}");
                    var cycleLength      = i - previousI;
                    var finalStateJump   = (until - i) % cycleLength;
                    // reload final state and break out
                    var finalStateString = previousStates.First(kvp => kvp.Value == previousI + finalStateJump).Key;
                    gameState = GameState.BuildFrom(finalStateString.Split(new[] {'\r','\n'}, StringSplitOptions.RemoveEmptyEntries));
                    break;
                }
                previousStates.Add(stateString, i);
            }
            return gameState.WoodedCount * gameState.LumberyardCount;
        }


        internal sealed class GameState
        {
            private enum CellType { Open, Wooded, Lumberyard };
            private readonly CellType[] _map;
            private readonly int        _width;
            private readonly int        _height;

            private GameState(CellType[] map, int width, int height)
            {
                _map    = map;
                _width  = width;
                _height = height;
            }

            public int WoodedCount     => _map.Count(m => m == CellType.Wooded);
            public int LumberyardCount => _map.Count(m => m == CellType.Lumberyard);

            public static GameState BuildFrom(string[] description)
            {
                var width  = description[0].Length;
                var height = description.Length;
                var map    = new CellType[width * height];
                for(var i = 0; i < map.Length; i++)
                    switch (description[i / width][i % width])
                    {
                        case '.': map[i] = CellType.Open; break;
                        case '|': map[i] = CellType.Wooded; break;
                        case '#': map[i] = CellType.Lumberyard; break;
                    }

                return new GameState(map, width, height);
            }

            public GameState NextState()
            {
                var newMap = new CellType[_map.Length];
                for(var i = 0; i < _map.Length; i++)
                {
                    var adjacentCells      = GetAdjacentCells(i);
                    var adjacentWooded     = adjacentCells.Count(c => c == CellType.Wooded);
                    var adjacentLumberyard = adjacentCells.Count(c => c == CellType.Lumberyard);

                    // Evolution rules
                    newMap[i] = _map[i];
                    if (_map[i] == CellType.Open && adjacentWooded >= 3)
                        newMap[i] = CellType.Wooded;
                    if (_map[i] == CellType.Wooded && adjacentLumberyard >= 3)
                        newMap[i] = CellType.Lumberyard;
                    if (_map[i] == CellType.Lumberyard)
                        if (adjacentWooded >= 1 && adjacentLumberyard >= 1)
                            newMap[i] = CellType.Lumberyard;
                        else
                            newMap[i] = CellType.Open;
                }

                return new GameState(newMap, _width, _height);
            }

            public string Describe()
            {
                var sb = new StringBuilder();
                for (var i = 0; i < _map.Length; i++)
                {
                    if (i > 0 && i % _width == 0) sb.AppendLine();
                    switch (_map[i])
                    {
                        case CellType.Open: sb.Append('.'); break;
                        case CellType.Wooded: sb.Append('|'); break;
                        case CellType.Lumberyard: sb.Append('#'); break;
                    }
                }

                return sb.ToString();
            }

            // return all adjacent cells, omitting the ones out of the map
            private List<CellType> GetAdjacentCells(int i)
            {
                IEnumerable<CellType> GetMaybeCell(int r, int c)
                    => r>=0 && r<_height && c>=0 && c<_width ? new[] {_map[r*_width+c]} : Array.Empty<CellType>();

                var ir = i / _width;
                var ic = i % _width;
                var a = new[] {(ir-1,ic-1),(ir-1,ic),(ir-1,ic+1),(ir,ic-1),(ir,ic+1),(ir+1,ic-1),(ir+1,ic),(ir+1,ic+1)};
                return a.SelectMany(x => GetMaybeCell(x.Item1, x.Item2)).ToList();
            }
        }
    }




    [TestFixture]
    internal class Day18Tests
    {
        public static string[] TestInput =
        {
            ".#.#...|#.",
            ".....#|##|",
            ".|..|...#.",
            "..|#.....#",
            "#.#|||#|#|",
            "...#.||...",
            ".|....|...",
            "||...#|.#|",
            "|.||||..|.",
            "...#.|..|."
        };

        [Test]
        public void Test1()
        {
            var res = Day18.CountResourcesAfter(TestInput, 10);
            Assert.AreEqual(1147, res);
        }
    }
}
