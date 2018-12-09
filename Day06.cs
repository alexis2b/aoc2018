using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace aoc2018
{
    internal class Day06
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day06.txt");

            var res1 = FindBestLocationArea(input);
            Console.WriteLine($"Day06 - part1 - result: {res1}");

            var res2 = FindBestRegion(input, 10000);
            Console.WriteLine($"Day06 - part2 - result: {res2}");
        }

        public static int FindBestLocationArea(IEnumerable<string> input)
        {
            // idea -> iterate over the smallest grid (as defined by (min,max) coordinates)
            // for every point compute the closest coord and increase the counter for it
            // solution is max of those counters for those who are not infinite
            // a location has infinite area when a border point is closest to that location

            var locations = input.Select((s, i) => Loc.FromString(i+1, s)).ToList();

            // borders
            var fromX = locations.Min(l => l.X);
            var toX   = locations.Max(l => l.X);
            var fromY = locations.Min(l => l.Y);
            var toY   = locations.Max(l => l.Y);

            // helper function
            int ClosestLocationFrom(int x, int y)
            {
                var distances = locations.Select(l => new { l.Id, Distance = l.DistanceTo(x, y) });
                var minDistance = distances.Min(d => d.Distance);
                var minDistanceLocs = distances.Where(d => d.Distance == minDistance).ToList();
                return minDistanceLocs.Count == 1 ? minDistanceLocs[0].Id : 0; // 0 means: multiple locations closer
            }

            // compute closest location for each point of the grid
            var closestLocations = Enumerable.Range(fromX, toX - fromX + 1)
                .SelectMany(x => Enumerable.Range(fromY, toY - fromY + 1)
                   .Select(y => new { X = x, Y = y, Closest = ClosestLocationFrom(x, y) }));

            // find all the "infinite locations" (i.e. they touch the border) and remove them, also remove non-clear locations (0)
            var locationsWithInfiniteArea = closestLocations.Where(l => l.Closest == 0 || l.X == fromX || l.X == toX || l.Y == fromY || l.Y == toY).Select(l => l.Closest).ToHashSet();
            var closestLocationsWithoutInfinites = closestLocations.Where(l => !locationsWithInfiniteArea.Contains(l.Closest));

            // count what's left and select the winner
            var areas   = closestLocationsWithoutInfinites.GroupBy(l => l.Closest).Select(gl => new { Id = gl.Key, Count = gl.Count() });
            var maxArea = areas.Max(a => a.Count);
            var maxAreaId = areas.First(a => a.Count == maxArea); // for info only

            return maxArea;
        }

        public static int FindBestRegion(IEnumerable<string> input, int maxDistance)
        {
            var locations = input.Select((s, i) => Loc.FromString(i + 1, s)).ToList();

            // borders
            var fromX = locations.Min(l => l.X);
            var toX   = locations.Max(l => l.X);
            var fromY = locations.Min(l => l.Y);
            var toY   = locations.Max(l => l.Y);

            // for each coordinate, compute the sum of the distances to all coordinates
            var sumDistanceMap = Enumerable.Range(fromX, toX - fromX + 1)
                .SelectMany(x => Enumerable.Range(fromY, toY - fromY + 1)
                   .Select(y => new { X = x, Y = y, SumOfDistances = locations.Select(l => l.DistanceTo(x, y)).Sum() }));

            return sumDistanceMap.Count(c => c.SumOfDistances < maxDistance);
        }


        private sealed class Loc : IEquatable<Loc>
        {
            public int Id { get; }
            public int X  { get; }
            public int Y  { get; }

            private Loc(int id, int x, int y)
            {
                Id = id;
                X = x;
                Y = y;
            }

            // Parsing constructor
            public static Loc FromString(int id, string txt)
            {
                var parts = txt.Split(", ");
                return new Loc(id, int.Parse(parts[0]), int.Parse(parts[1]));
            }

            public int DistanceTo(int x, int y) => Math.Abs(x - X) + Math.Abs(y - Y);

            public override int GetHashCode() => Id;
            public override bool Equals(object obj) => Equals(obj as Loc);
            public bool Equals(Loc other) => other != null && other.Id == Id;

        }
    }




    [TestFixture]
    internal class Day06Tests
    {
        private readonly string[] _input = new[]
        {
            "1, 1",
            "1, 6",
            "8, 3",
            "3, 4",
            "5, 5",
            "8, 9"
        };

        [Test]
        public void Test1_1()
        {
            var res = Day06.FindBestLocationArea(_input);
            Assert.AreEqual(17, res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day06.FindBestRegion(_input, 32);
            Assert.AreEqual(16, res);
        }
    }
}
