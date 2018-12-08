using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace aoc2018
{
    internal class Day03
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day03.txt").ToList();

            var res1 = ComputeOverlappingArea(input);
            Console.WriteLine($"Day03 - part1 - result: {res1}");

            var res2 = FindNonOverlappingClaim(input);
            Console.WriteLine($"Day03 - part2 - result: {res2}");
        }

        public static int ComputeOverlappingArea(IEnumerable<string> input)
         => // get full list of coordinates, group them, and count where >= 2
            input.Select(Claim.FromString).SelectMany(c => c.GetSquares())
                .GroupBy(s => s)
                .Count(g => g.Count() >= 2);

        public static int FindNonOverlappingClaim(IEnumerable<string> input)
        {
            // Idea: get the list of all Squares that have only 1 claim (non-overlapping)
            // Then, if a claim has ALL of its square in that list, then it is non-overlapping
            // (1 overlapping claim would have at least 1 of its square in the overlapping list)
            var claims = input.Select(Claim.FromString).ToList();
            var nonOverlappingSquares = claims.SelectMany(c => c.GetSquares())
                                            .GroupBy(s => s)
                                                .Where(g => g.Count() == 1)
                                                    .Select(g => g.First()).ToHashSet();
            var nonOverlappingClaims = claims
                .Where(c => c.GetSquares().All(s => nonOverlappingSquares.Contains(s))).ToList();

            if (nonOverlappingClaims.Count == 0)
                throw new Exception("No non-overlapping claim found");
            if (nonOverlappingClaims.Count > 1)
                throw new Exception("More than 1 non-overapping claims found: " + String.Join(", ", nonOverlappingClaims));

            return nonOverlappingClaims.First().Id;
        }
    }

    // Immutable class representing a claim
    internal sealed class Claim
    {
        private static readonly Regex ClaimEx = new Regex(@"#(?<id>[\d]+) @ (?<x>[\d]+),(?<y>[\d]+): (?<w>[\d]+)x(?<h>[\d]+)");
        private readonly int _id, _x, _y, _w, _h;

        public int Id => _id;

        private Claim(int id, int x, int y, int w, int h)
        {
            _id = id;
            _x = x;
            _y = y;
            _w = w;
            _h = h;
        }

        public static Claim FromString(string claimStr)
        {
            var match = ClaimEx.Match(claimStr);
            if (match.Success)
                return new Claim(
                    int.Parse(match.Groups["id"].Value),
                    int.Parse(match.Groups["x"].Value),
                    int.Parse(match.Groups["y"].Value),
                    int.Parse(match.Groups["w"].Value),
                    int.Parse(match.Groups["h"].Value));

            throw new ArgumentException($"Not a valid claim: {claimStr}");
        }

        public IEnumerable<Square> GetSquares()
        {
            return Enumerable.Range(_x, _w).SelectMany(x => Enumerable.Range(_y, _h).Select(y => new Square(x, y))).ToList();
        }
    }


    internal sealed class Square : IEquatable<Square>
    {
        private readonly int _x, _y;

        public Square(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Square);
        }

        public bool Equals(Square other)
        {
            return other != null && other._x == _x && other._y == _y;
        }

        public override int GetHashCode()
        {
            return 42 ^ _x ^ _y;
        }
    }




    [TestFixture]
    internal class Day03Tests
    {
        private readonly string[] _input = new[] { "#1 @ 1,3: 4x4", "#2 @ 3,1: 4x4", "#3 @ 5,5: 2x2" };

        [Test]
        public void Test1_1()
        {
            var res = Day03.ComputeOverlappingArea(_input);
            Assert.AreEqual(4, res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day03.FindNonOverlappingClaim(_input);
            Assert.AreEqual(3, res);
        }
    }
}
