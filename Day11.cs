using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day11
    {
        public static void Run()
        {
            const int input = 7689;

            var res1 = GetHighestPower3X3Square(input);
            Console.WriteLine($"Day11 - part1 - result: {res1}");

            var res2 = GetHighestPowerAnySizeSquare(input);
            Console.WriteLine($"Day11 - part2 - result: {res2}");
        }

        public static int PowerOf(int x, int y, int serialNumber)
            => ((x + 10) * y + serialNumber) * (x + 10) / 100 % 10 - 5;

        public static (int power, (int x, int y)) GetHighestPower3X3Square(int serialNumber)
        {
            var bestValue  = int.MinValue;
            var bestCoords = (x: 0, y: 0);
            for(var x = 1; x <= 298; x++)
            for(var y = 1; y <= 298; y++)
            {
                var power = PowerOf(x, y    , serialNumber) + PowerOf(x + 1, y    , serialNumber) + PowerOf(x + 2, y    , serialNumber)
                          + PowerOf(x, y + 1, serialNumber) + PowerOf(x + 1, y + 1, serialNumber) + PowerOf(x + 2, y + 1, serialNumber)
                          + PowerOf(x, y + 2, serialNumber) + PowerOf(x + 1, y + 2, serialNumber) + PowerOf(x + 2, y + 2, serialNumber);
                if (power > bestValue)
                {
                    bestValue  = power;
                    bestCoords = (x, y);
                }
            }

            return (bestValue, bestCoords);
        }

        public static (int power, (int x, int y), int size) GetHighestPowerAnySizeSquare(int serialNumber)
        {
            // generate the power grid to accelerate performances
            var grid = new int[301, 301];
            for(var x = 1; x <= 300; x++)
            for (var y = 1; y <= 300; y++)
                grid[x, y] = PowerOf(x, y, serialNumber);

            var bestValue = int.MinValue;
            var bestCoords = (x: 0, y: 0);
            var bestSize   = 0;
            for (var s = 1; s <= 300; s++)
            {
                Console.WriteLine($"s = {s}, best = ({bestValue}, {bestCoords}, {bestSize})");
                for (var x = 1; x <= 301 - s; x++)
                for (var y = 1; y <= 301 - s; y++)
                {
                    var power = 0;
                    for (var dx = 0; dx < s; dx++)
                    for (var dy = 0; dy < s; dy++)
                        power += grid[x+dx, y+dy];

                    if (power > bestValue)
                    {
                        bestValue = power;
                        bestCoords = (x, y);
                        bestSize = s;
                    }
                }
            }

            return (bestValue, bestCoords, bestSize);
        }
    }


    [TestFixture]
    internal class Day11Tests
    {
        [TestCase(3, 5, 8, 4)]
        [TestCase(122, 79, 57, -5)]
        [TestCase(217, 196, 39, 0)]
        [TestCase(101, 153, 71, 4)]
        public void Test_PowerOf(int x, int y, int serialNumber, int expected)
        {
            var res = Day11.PowerOf(x, y, serialNumber);
            Assert.AreEqual(expected, res);
        }

        [Test]
        public void Test1_1()
        {
            var res = Day11.GetHighestPower3X3Square(18);
            Assert.AreEqual(29, res.power);
            Assert.AreEqual((33, 45), res.Item2);
        }

        [Test]
        public void Test1_2()
        {
            var res = Day11.GetHighestPower3X3Square(42);
            Assert.AreEqual(30, res.power);
            Assert.AreEqual((21, 61), res.Item2);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day11.GetHighestPowerAnySizeSquare(18);
            Assert.AreEqual(113, res.power);
            Assert.AreEqual((90, 269), res.Item2);
            Assert.AreEqual(16, res.size);
        }

        [Test]
        public void Test2_2()
        {
            var res = Day11.GetHighestPowerAnySizeSquare(42);
            Assert.AreEqual(119, res.power);
            Assert.AreEqual((232, 251), res.Item2);
            Assert.AreEqual(12, res.size);
        }
    }
}
