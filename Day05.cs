using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace aoc2018
{
    internal class Day05
    {
        public static void Run()
        {
            var input = File.ReadAllText("input\\day05.txt");

            var res1 = ReducePolymer(input);
            Console.WriteLine($"Day05 - part1 - result: {res1.Length}");

            var res2 = FindBestPolymer(input);
            Console.WriteLine($"Day05 - part2 - result: {res2.Length}");
        }

        public static string ReducePolymer(string polymer)
        {
            // Perform in-place reduction
            var buffer = new StringBuilder(polymer);
            var index  = 0;
            while(index < buffer.Length - 1)
            {
                var c1 = buffer[index];
                var c2 = buffer[index + 1];
                var isSameChar = char.ToLowerInvariant(c1) == char.ToLowerInvariant(c2);
                var isReduction = isSameChar && char.IsUpper(c1) != char.IsUpper(c2);

                if (isReduction)
                {
                    buffer.Remove(index, 2);
                    index = Math.Max(index - 1, 0); // go back 1 character since we might have created a new reduction
                }
                else index++;
            }
            return buffer.ToString();
        }

        public static string FindBestPolymer(string polymer)
        {
            var newPolymers = new List<string>();
            var chars = polymer.Select(c => char.ToLowerInvariant(c)).Distinct().ToArray();
            foreach(var c in chars)
            {
                var newPolymer = polymer.Replace(c.ToString(), string.Empty).Replace(c.ToString().ToUpperInvariant(), string.Empty);
                var reduced = ReducePolymer(newPolymer);
                newPolymers.Add(reduced);
            }

            var minPolymerSize = newPolymers.Min(p => p.Length);
            var minPolymer = newPolymers.First(p => p.Length == minPolymerSize);

            return minPolymer;
        }
    }




    [TestFixture]
    internal class Day05Tests
    {
        [Test]
        public void Test1_1()
        {
            var res = Day05.ReducePolymer("dabAcCaCBAcCcaDA");
            Assert.AreEqual("dabCBAcaDA", res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day05.FindBestPolymer("dabAcCaCBAcCcaDA");
            Assert.AreEqual("daDA", res);
        }
    }
}
