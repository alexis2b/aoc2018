using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace aoc2018
{
    internal class Day01
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day01.txt").Select(Int32.Parse).ToList();

            var res1 = ComputeResultingFrequency(input);
            Console.WriteLine($"Day01 - part1 - result: {res1}");

            var res2 = FindCalibrationFrequency(input);
            Console.WriteLine($"Day01 - part2 - result: {res2}");
        }

        public static int ComputeResultingFrequency(IEnumerable<int> input)
        {
            return input.Sum();
        }

        public static int FindCalibrationFrequency(IEnumerable<int> input)
        {
            var freqs = input.ToList();
            var sum   = 0;
            var idx   = 0;
            var known = new HashSet<int>(new[] { 0 });
            while(true)
            {
                sum += freqs[idx++ % freqs.Count];
                if (!known.Add(sum))
                    return sum;
            }
        }
    }


    [TestFixture]
    internal class Day01Tests
    {
        [Test]
        public void Test1_1()
        {
            var res = Day01.ComputeResultingFrequency(new[] {1, -2, 3, 1});
            Assert.AreEqual(3, res);
        }

        [Test]
        public void Test1_2()
        {
            var res = Day01.ComputeResultingFrequency(new[] { 1, 1, 1 });
            Assert.AreEqual(3, res);
        }

        [Test]
        public void Test1_3()
        {
            var res = Day01.ComputeResultingFrequency(new[] { 1, 1, -2 });
            Assert.AreEqual(0, res);
        }

        [Test]
        public void Test1_4()
        {
            var res = Day01.ComputeResultingFrequency(new[] { -1, -2, -3 });
            Assert.AreEqual(-6, res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day01.FindCalibrationFrequency(new[] { 1, -2, 3, 1 });
            Assert.AreEqual(2, res);
        }

        [Test]
        public void Test2_2()
        {
            var res = Day01.FindCalibrationFrequency(new[] { 1, -1 });
            Assert.AreEqual(0, res);
        }

        [Test]
        public void Test2_3()
        {
            var res = Day01.FindCalibrationFrequency(new[] {3, 3, 4, -2, -4 });
            Assert.AreEqual(10, res);
        }

        [Test]
        public void Test2_4()
        {
            var res = Day01.FindCalibrationFrequency(new[] { -6, 3, 8, 5, -6 });
            Assert.AreEqual(5, res);
        }

        [Test]
        public void Test2_5()
        {
            var res = Day01.FindCalibrationFrequency(new[] { 7, 7, -2, -7, -4 });
            Assert.AreEqual(14, res);
        }
    }
}
