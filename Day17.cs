using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day17
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day17.txt");

            var (res1, res2) = CountTilesReachedByWater(input);
            Console.WriteLine($"Day17 - part1 - result: {res1}");
            Console.WriteLine($"Day17 - part2 - result: {res2}");
        }

        public static (int all, int quietOnly) CountTilesReachedByWater(string[] input)
        {
            var mapBuilder = new MapBuilder();
            foreach(var scan in input)
                mapBuilder.AddClaySegment(scan);

            var map = mapBuilder.Build();
            Console.WriteLine("Before:\n" + map.Draw());
            map.SimulateWaterFlow();
            Console.WriteLine("\nAfter:\n" + map.Draw());

            return map.CountTilesReachedByWater();
        }

        // Helper class to build the Map
        internal sealed class MapBuilder
        {
            private static readonly Regex ScanEx = new Regex(@"^([xy])=(\d+), [yx]=(\d+)\.\.(\d+)$");
            private readonly HashSet<(int x, int y)> _claySquares = new HashSet<(int x, int y)>();

            public void AddClaySegment(string scan)
            {
                var match = ScanEx.Match(scan);
                if (!match.Success) throw new ArgumentException("Failed to match syntax of " + scan);
                var dim1 = match.Groups[1].Value[0];
                var coord1 = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var coord2From = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                var coord2To = int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture);

                for (var coord2 = coord2From; coord2 <= coord2To; coord2++)
                    _claySquares.Add(dim1 == 'x' ? (coord1, coord2) : (coord2, coord1));
            }

            public Map Build()
            {
                var minX = _claySquares.Min(p => p.x) - 1;
                var maxX = _claySquares.Max(p => p.x) + 1;
                var minY = _claySquares.Min(p => p.y);
                var maxY = _claySquares.Max(p => p.y);
                var map  = new char[maxX + 1, maxY + 1];
                for(var y = 0; y <= maxY; y++)
                    for(var x = 0; x <= maxX; x++)
                        map[x, y]  = (y == Map.SpringY && x == Map.SpringX) ? '+' : _claySquares.Contains((x,y)) ? '#' : '.';

                return new Map(minX, maxX, minY, maxY, map);
            }
        }


        /// <summary>
        /// Map of the terrain + simulation of water flow
        /// Stateful model is necessary because game map has some infinite cycles if operating in stateless mode
        /// </summary>
        internal sealed class Map
        {
            public const int SpringX = 500;
            public const int SpringY = 0;
            private readonly int _minX; // for drawing only
            private readonly int _maxX;
            private readonly int _minY; // for counting only
            private readonly int _maxY;
            private readonly char[,] _map;

            public Map(int minX, int maxX, int minY, int maxY, char[,] map)
            {
                _minX = minX;
                _maxX = maxX;
                _minY = minY;
                _maxY = maxY;
                _map  = map;
            }
            
            public string Draw()
            {
                var sb   = new StringBuilder();
                for (var y = 0; y <= _maxY; y++)
                {
                    for (var x = _minX; x <= _maxX; x++)
                        sb.Append(_map[x, y]);
                    sb.AppendLine();
                }
                return sb.ToString();
            }

            public void SimulateWaterFlow()
            {
                // Possible algorithms
                // water can:
                // - fall following gravity (when it has no floor below)
                // - fill up spaces, providing there are two walls on each side and a continuous floor between the two 
                //    or a floor of "water at rest" -> +1
                //    spaces are filled up upon touching a floor and going up until the condition is not met anymore +n where n are the "water at rest" stacks
                // - spill left/right/both if touching a floor and no filling condition exists / +1 per flow tile (i.e. until a border or a hole in the floor is found)
                // the sequence is always: fall - fill - spill that we can traverse like a graph
                //   exit condition for "fall" is based on reaching a certain y
                Fall((SpringX, SpringY), 0);
            }

            public (int all, int quietOnly) CountTilesReachedByWater()
            {
                var countWet   = 0;
                var countQuiet = 0;
                for(var x =     0; x <= _maxX; x++)
                for (var y = _minY; y <= _maxY; y++)
                {
                    if (_map[x, y] == '~' ) countQuiet++;
                    if (_map[x, y] == '|' ) countWet++;
                }
                return (countWet+countQuiet, countQuiet);
            }

            // "Falling" leg - water is going down, touching one additional square of sand until:
            // - reaching clay  -> in which case we move into Fill leg
            // - reaching max y -> in which case we exit with the current count
            // Assumes fromPosition has already been counted (start counting from the 1st next position - if it can be reached)
            private void Fall((int x, int y) fromPosition, int leg)
            {
                // Fall
                var position = fromPosition;
                while(true)
                {
                    position = (x: position.x, y: position.y + 1);
                    if (position.y > _maxY)
                    {
                        Console.WriteLine($"L{leg:D3}: fallen from {fromPosition} to {position} - exit (bottom reached)");
                        return;
                    }

                    var squareCode = _map[position.x, position.y];
                    if (squareCode == '|')
                    {
                        Console.WriteLine($"L{leg:D3}: fallen from {fromPosition} to {position} - exit (loop reached)");
                        return;
                    }

                    if (squareCode == '#' || squareCode == '@')
                        break;

                    _map[position.x, position.y] = '|';
                }

                // Clay or quiet water reached, move into "fill" leg from last position
                Console.WriteLine($"L{leg:D3}: fallen {fromPosition} to {position} - filling");
                Fill((position.x, position.y - 1), leg+1);
            }

            // "Filling" leg - water is going left and right as long as there are walls on both sides and a floor below (or a filled square)
            // we fill going upward until the canFill condition is not matched, and move into "Spill" leg
            // fromPosition is always one row above the found floor, so we start the search at fromPosition.y
            private void Fill((int x, int y) fromPosition, int leg)
            {
                var position = fromPosition;
                for (;; position.y--)
                {
                    var leftBorder  = FindFillBorder(position, -1);
                    var rightBorder = FindFillBorder(position, +1);

                    // At least one side does not have a proper border, spill!
                    if (!leftBorder.isBorder || !rightBorder.isBorder)
                    {
                        Console.WriteLine($"L{leg:D3}: filled space from {fromPosition} to row {position.y} - spilling left from {leftBorder} and right from {rightBorder}");
                        Spill(position, leftBorder,  leg + 1);
                        Spill(position, rightBorder, leg + 1);
                        return;
                    }

                    // Borders on both sides - we fill the row and continue with the one immediately above
                    for (var x = leftBorder.x + 1; x < rightBorder.x; x++)
                        _map[x, position.y] = '~';
                }
            }

            // Spill in a given direction, starting from given position, until:
            // - either we have a border, then we can stop spilling
            // - or we have no border, and we start falling again
            private void Spill((int x, int y) fromPosition, (int x, bool isBorder) toPosition, int leg)
            {
                var xDirection = toPosition.x > fromPosition.x ? +1 : -1;
                for (var x = fromPosition.x; x != toPosition.x; x += xDirection)
                {
                    if ( _map[x, fromPosition.y] != '~' )
                        _map[x, fromPosition.y] = '|';
                    else
                    {
                        Console.WriteLine($"L{leg:D3}: aborting spill from {fromPosition} to {toPosition} - exiting (loop found)");
                        return; // could have already been filled in a different situation
                    }
                }

                if (toPosition.isBorder)
                {
                    Console.WriteLine($"L{leg:D3}: spilled from {fromPosition} to {toPosition} - exiting (border found)");
                }
                else
                {
                    // start falling again
                    _map[toPosition.x, fromPosition.y] = '|';
                    Console.WriteLine($"L{leg:D3}: spilled from {fromPosition} to {toPosition} - resuming fall");
                    Fall((toPosition.x, fromPosition.y), leg + 1);
                }
            }

            private (int x, bool isBorder) FindFillBorder((int x, int y) aroundPosition, int xDirection)
            {
                var (x, y) = aroundPosition;
                for (;;)
                {
                    x += xDirection;
                    if (_map[x, y] == '#')
                        return (x, true); // found the border

                    var squareBelowCode = _map[x, y+1];
                    if (squareBelowCode != '#' && squareBelowCode != '~')
                        return (x, false); // no floor, start spilling
                }
            }
        }
    }




    [TestFixture]
    internal class Day17Tests
    {
        [Test]
        public void Test1()
        {
            string[] input =
            {
                "x=495, y=2..7",
                "y=7, x=495..501",
                "x=501, y=3..7",
                "x=498, y=2..4",
                "x=506, y=1..2",
                "x=498, y=10..13",
                "x=504, y=10..13",
                "y=13, x=498..504"
            };

            var(res1, res2) = Day17.CountTilesReachedByWater(input);
            Assert.AreEqual(57, res1);
            Assert.AreEqual(29, res2);
        }

        [Test]
        public void TestBug1()
        {
            // bug: some squares inside a filled zone appear as wet rather than quiet
            string[] bug1 =
            {
                "x=489, y=3..16",
                "x=509, y=3..16",
                "y=16, x=489..509",
                "x=494, y=7..10",
                "x=499, y=7..10",
                "y=7, x=494..499",
                "y=10, x=494..499"
            };

            var (res1, res2) = Day17.CountTilesReachedByWater(bug1);
            Assert.AreEqual(251, res1);
            Assert.AreEqual(223, res2);
        }
    }
}
