using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace aoc2018
{
    internal class Day25
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day25.txt");

            var res1 = CountConstellations(input);
            Console.WriteLine($"Day25 - part1 - result: {res1}");

            //var res2 = FindMinimumBoostToWin();
            //Console.WriteLine($"Day25 - part2 - result: {res2}");
        }

        public static int CountConstellations(string[] input)
        {
            var stars = input.Select(s => s.Split(',').Select(int.Parse).ToArray()).Select(a => new Star(a[0], a[1], a[2], a[3])).ToList();
            var count = 0;
            while (stars.Count > 0)
            {
                var baseStar = stars[0];
                stars.RemoveAt(0);
                MakeConstellationFrom(baseStar, stars);
                count++;
            }

            return count;
        }

        private static void MakeConstellationFrom(Star baseStar, List<Star> stars)
        {
            var frontier = new List<Star> {baseStar};

            while (frontier.Count > 0)
            {
                var newFrontier = new List<Star>();
                foreach (var star in frontier)
                {
                    // Filter between "near" and "far" stars based on distance
                    var starDistances = stars.Select(s => new { Star = s, Distance = DistanceBetween(star, s) }).ToList();
                    var nearStars     = starDistances.Where(sd => sd.Distance <= 3).Select(sd => sd.Star).ToList();
                    newFrontier.AddRange(nearStars);
                    nearStars.ForEach(s => stars.Remove(s));
                }
                frontier = newFrontier;
            }
        }

        private static int DistanceBetween(Star a, Star b)
            => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z) + Math.Abs(a.A - b.A);

        private sealed class Star : IEquatable<Star>
        {
            public int X  { get; }
            public int Y  { get; }
            public int Z  { get; }
            public int A  { get; }

            public Star(int x, int y, int z, int a)
            {
                X = x;
                Y = y;
                Z = z;
                A = a;
            }

            public bool Equals(Star other) => other != null && other.X == X && other.Y == Y && other.Z == Z && other.A == A;
            public override int GetHashCode() => 42 ^ X ^ Y ^ Z ^ A;
            public override bool Equals(object obj) => Equals(obj as Star);
            public override string ToString() => $"Star({X},{Y},{Z},{A})";
        }
    }


    [TestFixture]
    internal class Day25Tests
    {
        private static readonly string[] TestInput1 = new[]
        {
             "0,0,0,0",
             "0,3,0,0",
             "0,0,3,0",
             "0,0,0,3",
             "0,0,0,6",
             "3,0,0,0",
             "9,0,0,0",
            "12,0,0,0"
        };
        private static readonly string[] TestInput2 = new[]
        {
            "-1,2,2,0",
            "0,0,2,-2",
            "0,0,0,-2",
            "-1,2,0,0",
            "-2,-2,-2,2",
            "3,0,2,-1",
            "-1,3,2,2",
            "-1,0,-1,0",
            "0,2,1,-2",
            "3,0,0,0"
        };
        private static readonly string[] TestInput3 = new[]
        {
            "1,-1,0,1",
            "2,0,-1,0",
            "3,2,-1,0",
            "0,0,3,1",
            "0,0,-1,-1",
            "2,3,-2,0",
            "-2,2,0,0",
            "2,-2,0,-1",
            "1,-1,0,-1",
            "3,2,0,2"
        };
        private static readonly string[] TestInput4 = new[]
        {
            "1,-1,-1,-2",
            "-2,-2,0,1",
            "0,2,1,3",
            "-2,3,-2,1",
            "0,2,3,-2",
            "-1,-1,1,-2",
            "0,-2,-1,0",
            "-2,2,3,-1",
            "1,2,2,0",
            "-1,-2,0,-2"
        };


        [Test]
        public void Test1_1()
        {
            var res = Day25.CountConstellations(TestInput1);
            Assert.AreEqual(2, res);
        }

        [Test]
        public void Test1_2()
        {
            var res = Day25.CountConstellations(TestInput2);
            Assert.AreEqual(4, res);
        }

        [Test]
        public void Test1_3()
        {
            var res = Day25.CountConstellations(TestInput3);
            Assert.AreEqual(3, res);
        }

        [Test]
        public void Test1_4()
        {
            var res = Day25.CountConstellations(TestInput4);
            Assert.AreEqual(8, res);
        }

        [Test]
        public void Test2()
        {
        }
    }
}
