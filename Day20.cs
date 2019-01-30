using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day20
    {
        private static readonly char[] SpecialChars = { '(', '|', ')' };

        public static void Run()
        {
            var input = File.ReadAllText("input\\day20.txt");

            var res1 = FindFurthestRoom(input);
            Console.WriteLine($"Day20 - part1 - result: {res1}");

            var res2 = FindRoomCountOverThreshold(input, 1000);
            Console.WriteLine($"Day20 - part2 - result: {res2}");
        }

        public static int FindFurthestRoom(string input)
        {
            // Pattern: (trunk, options, remainder) - compile to avoid repetitive string parsing
            var cleanInput = input.Substring(1, input.Length - 2);
            var pattern    = Pattern.Compile(cleanInput);
            //Console.WriteLine(pattern.Describe());

            // Build the game map
            var maze = new Maze();
            BuildMazeRecursive(Maze.Origin, pattern, maze);
            //Console.WriteLine(maze.Describe());

            // Perform a breadth-first search to find the deepest level
            var furthestRoomDistance = maze.FindFurthestRoomFrom(Maze.Origin);
            return furthestRoomDistance;
        }

        public static int FindRoomCountOverThreshold(string input, int threshold)
        {
            // Pattern: (trunk, options, remainder) - compile to avoid repetitive string parsing
            var cleanInput = input.Substring(1, input.Length - 2);
            var pattern = Pattern.Compile(cleanInput);
            //Console.WriteLine(pattern.Describe());

            // Build the game map
            var maze = new Maze();
            BuildMazeRecursive(Maze.Origin, pattern, maze);
            //Console.WriteLine(maze.Describe());

            // Perform a breadth-first search to find the deepest level
            var furthestRoomDistance = maze.FindFurthestRoomThan(Maze.Origin, threshold);
            return furthestRoomDistance;
        }

        private static IEnumerable<(int x, int y)> BuildMazeRecursive((int x, int y) startPos, Pattern directions, Maze maze)
        {
            // start by scanning the trunk (until we reach a branch or the end of the string)
            var pos = startPos;
            foreach (var c in directions.Trunk)
                pos = maze.AddRoomTo(pos, c);

            if (directions.Options.Length == 0)
                return new[] { pos };

            // scan the options
            var optionsEndPos = new HashSet<(int x, int y)>();
            foreach (var option in directions.Options)
            foreach (var endPos in BuildMazeRecursive(pos, option, maze))
                optionsEndPos.Add(endPos);

            if (directions.Remainder == null)
                return optionsEndPos;

            // now apply the remainder to each ending positions
            var endPositions = new List<(int x, int y)>();
            foreach (var optionEndPos in optionsEndPos)
                endPositions.AddRange(BuildMazeRecursive(optionEndPos, directions.Remainder, maze));

            return endPositions.Distinct().ToList();
        }


        // Every direction string can be broken down into: "trunk(option1|option2|..|optionN)remainder"
        // - a Trunk (directions up to first branch - only simple moves
        // - 0 or more options which are themselves patterns
        // - an optional Remainder which is itself a pattern
        // Created by calling the static method Compile()
        internal sealed class Pattern
        {
            private Pattern(string trunk, Pattern[] options, Pattern remainder)
            {
                Trunk     = trunk;
                Options   = options;
                Remainder = remainder;
            }

            public string    Trunk     { get; }
            public Pattern[] Options   { get; }
            public Pattern   Remainder { get; }

            // create a pattern from directions
            public static Pattern Compile(string directions)
            {
                var nextBranch = directions.IndexOf('(');
                if (nextBranch == -1)
                    return new Pattern(directions, Array.Empty<Pattern>(), null); // nothing left to parse

                // split the branches and remainder and iterate recursively
                var trunk                = directions.Substring(0, nextBranch);
                var (options, remainder) = SplitBranches(directions.Substring(nextBranch + 1));

                var optionPatterns = new List<Pattern>(options.Length);
                foreach(var option in options)
                    optionPatterns.Add( Compile(option) );

                var remainderPattern = Compile(remainder);

                return new Pattern(trunk, optionPatterns.ToArray(), remainderPattern);
            }

            // split a branch opening point into its options and remainder parts
            private static (string[] options, string remainder) SplitBranches(string branch)
            {
                var b = 0; // option begin at
                var s = 0; // search from index
                var d = 0; // depth level (each new branch starts a new depth)
                var o = new List<string>(); // options
                while (true)
                {
                    var i = branch.IndexOfAny(SpecialChars, s);
                    switch (branch[i])
                    {
                        case '(': d++; break;  // new branch - will escape all special chars inside until we get back to our branch (d==0)
                        case '|': if (d == 0) { o.Add(branch.Substring(b, i - b)); b = i + 1; } break;
                        case ')': if (d == 0) { o.Add(branch.Substring(b, i - b)); return (o.ToArray(), branch.Substring(i + 1)); } d--; break;
                    }
                    s = i + 1; // find next
                }
            }

            // creates (recursively) a printable string describing the Pattern tree
            public string Describe(string prefix = "ROOT", int depth = 0)
            {
                var builder = new StringBuilder().Append(' ', depth).Append(prefix).AppendLine($"-T={Trunk.Length:D2},O={Options.Length:D1}");
                for (var i = 0; i < Options.Length; i++)
                    builder.Append(Options[i].Describe("OPT" + i, depth + 1));
                if ( Remainder != null )
                    builder.Append(Remainder.Describe("REMA", depth + 1));
                return builder.ToString();
            }
        }





        // Maze is made of Rooms
        internal sealed class Maze
        {
            public static readonly (int x, int y) Origin = (0, 0);
            private readonly Dictionary<(int x, int y), Room> _rooms = new Dictionary<(int x, int y), Room>();

            public Maze()
            {
                // Add entry room
                _rooms.Add(Origin, new Room(Origin));
            }


            public (int x, int y) AddRoomTo((int x, int y) from, char direction)
            {
                var fromRoom = _rooms[from];
                var to       = Position(from, direction );
                if (!_rooms.TryGetValue(to, out var toRoom))
                    _rooms[to] = toRoom = new Room(to);
                switch (direction)
                {
                    case 'N': fromRoom.North = toRoom; toRoom.South = fromRoom; break;
                    case 'E': fromRoom.East  = toRoom; toRoom.West  = fromRoom; break;
                    case 'S': fromRoom.South = toRoom; toRoom.North = fromRoom; break;
                    case 'W': fromRoom.West  = toRoom; toRoom.East  = fromRoom; break;
                    default: throw new ArgumentException($"{direction} is not a recognized direction value");
                }
                return to;
            }

            private static (int x, int y) Position((int x, int y) from, char direction)
            {
                switch (direction)
                {
                    case 'N': return (from.x,     from.y + 1);
                    case 'E': return (from.x + 1, from.y    );
                    case 'S': return (from.x,     from.y - 1);
                    case 'W': return (from.x - 1, from.y    );
                    default: throw new ArgumentException($"{direction} is not a recognized direction value");
                }
            }

            // Provide a string map of the maze
            public string Describe()
            {
                const char wall  = '#';
                const char hdoor = '-';
                const char vdoor = '|';
                const char mroom = '.';
                int fromX = 0, toX = 0, fromY = 0, toY = 0;
                foreach (var (x, y) in _rooms.Keys)
                {
                    if (x < fromX) fromX = x;
                    if (x > toX  ) toX   = x;
                    if (y < fromY) fromY = y;
                    if (y > toY  ) toY   = y;
                }

                var res = new StringBuilder();
                for (var y = fromY; y <= toY; y++)
                {
                    var row1 = new StringBuilder();
                    var row2 = new StringBuilder();
                    var row3 = new StringBuilder();

                    for (var x = fromX; x <= toX; x++)
                    {
                        var room = _rooms[(x, y)];
                        row1.Append(wall).Append(room.North != null ? hdoor : wall);
                        row2.Append(room.West != null ? vdoor : wall).Append(mroom);
                        row3.Append(wall).Append(room.North != null ? hdoor : wall);
                        if (x == toX)
                        {   // Last wall side
                            row1.Append(wall);
                            row2.Append(room.East != null ? vdoor : wall);
                            row3.Append(wall);
                        }
                    }

                    res.Append(row1).AppendLine();
                    res.Append(row2).AppendLine();
                    if ( y == toY )
                        res.Append(row3).AppendLine();
                }

                return res.ToString();
            }

            public int FindFurthestRoomFrom((int x, int y) startPosition)
            {
                // Perform a breadth-first search until all maze has been searched
                var frontier = new List<Room> { _rooms[startPosition] };
                var visited  = new HashSet<(int, int)> { startPosition };
                for (var i = 0;; i++)
                {
                    // use side-effect of Add to fill-up the Visited list AND find those who have not been visited yet
                    var newFrontier = frontier.SelectMany(r => r.GetAdjacentRooms()).Where(r => visited.Add(r.Position)).ToList();
                    if (newFrontier.Count == 0)
                        return i;

                    frontier = newFrontier;
                }
            }

            // for part2
            public int FindFurthestRoomThan((int x, int y) startPosition, int threshold)
            {
                // Perform a breadth-first search until all maze has been searched
                var frontier = new List<Room> { _rooms[startPosition] };
                var visited  = new HashSet<(int, int)> { startPosition };
                var count    = 0;
                for (var i = 0; ; i++)
                {
                    if (i >= threshold)
                        count += frontier.Count;

                    // use side-effect of Add to fill-up the Visited list AND find those who have not been visited yet
                    var newFrontier = frontier.SelectMany(r => r.GetAdjacentRooms()).Where(r => visited.Add(r.Position)).ToList();
                    if (newFrontier.Count == 0)
                        return count;

                    frontier = newFrontier;
                }
            }
        }

        // Basic components of a Maze, it has a location and doors
        internal sealed class Room
        {
            public (int x, int y) Position { get; }
            public Room North { get; set; }
            public Room East  { get; set; }
            public Room South { get; set; }
            public Room West  { get; set; }

            public Room((int, int) position)
            {
                Position = position;
            }

            public IEnumerable<Room> GetAdjacentRooms()
            {
                var rooms = new List<Room>();
                if ( North != null ) rooms.Add(North);
                if ( East  != null ) rooms.Add(East );
                if ( South != null ) rooms.Add(South);
                if ( West  != null ) rooms.Add(West );
                return rooms;
            }
        }
    }


    [TestFixture]
    internal class Day20Tests
    {
        [Test]
        public void Test1_1()
        {
            var res = Day20.FindFurthestRoom("^WNE$");
            Assert.AreEqual(3, res);
        }

        [Test]
        public void Test1_2()
        {
            var res = Day20.FindFurthestRoom("^ENWWW(NEEE|SSE(EE|N))$");
            Assert.AreEqual(10, res);
        }

        [Test]
        public void Test1_3()
        {
            var res = Day20.FindFurthestRoom("^ENNWSWW(NEWS|)SSSEEN(WNSE|)EE(SWEN|)NNN$");
            Assert.AreEqual(18, res);
        }

        [Test]
        public void Test1_4()
        {
            var res = Day20.FindFurthestRoom("^ESSWWN(E|NNENN(EESS(WNSE|)SSS|WWWSSSSE(SW|NNNE)))$");
            Assert.AreEqual(23, res);
        }

        [Test]
        public void Test1_5()
        {
            var res = Day20.FindFurthestRoom("^WSSEESWWWNW(S|NENNEEEENN(ESSSSW(NWSW|SSEN)|WSWWN(E|WWS(E|SS))))$");
            Assert.AreEqual(31, res);
        }
    }
}
