using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace aoc2018
{
    internal class Day02
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day02.txt").ToList();

            var res1 = ComputeChecksum(input);
            Console.WriteLine($"Day02 - part1 - result: {res1}");

            var res2 = FindSimilarBoxes(input);
            Console.WriteLine($"Day02 - part2 - result: {res2}");
        }

        public static int ComputeChecksum(IEnumerable<string> input)
        {
            var letterCounts = input.Select(s => s.GroupBy(c => c).Select(g => new { Letter = g.Key, Count = g.Count() })).ToList();
            var has2 = letterCounts.Count(lc => lc.Any(l => l.Count == 2));
            var has3 = letterCounts.Count(lc => lc.Any(l => l.Count == 3));

            return has2 * has3;
        }

        public static string FindSimilarBoxes(IEnumerable<string> input)
        {
            var boxes = input.ToList();
            foreach(var box1 in boxes)
                foreach(var box2 in boxes)
                {
                    var isSimilar = box1.Zip(box2, (c1, c2) => c1 != c2).Count(isDiff => isDiff) == 1;
                    if ( isSimilar )
                    {
                        var result = box1.Zip(box2, (c1, c2) => Tuple.Create(c1, c2)).Where(t => t.Item1 == t.Item2).Select(t => t.Item1).ToArray();
                        return new String(result);
                    }
                }
            throw new Exception("No similar boxes found");
        }
    }


    [TestFixture]
    internal class Day02Tests
    {
        [Test]
        public void Test1_1()
        {
            var res = Day02.ComputeChecksum(new[] {"abcdef", "bababc", "abbcde", "abcccd", "aabcdd", "abcdee", "ababab"});
            Assert.AreEqual(12, res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day02.FindSimilarBoxes(new[] { "abcde", "fghij", "klmno", "pqrst", "fguij", "axcye", "wvxyz" });
            Assert.AreEqual("fgij", res);
        }
    }
}
