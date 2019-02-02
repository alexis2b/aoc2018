using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Schema;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day10
    {
        public static void Run()
        {
            var input  = File.ReadAllLines("input\\day10.txt");
            var testInput = new string[]
            {
                "position=< 9,  1> velocity=< 0,  2>",
                "position=< 7,  0> velocity=<-1,  0>",
                "position=< 3, -2> velocity=<-1,  1>",
                "position=< 6, 10> velocity=<-2, -1>",
                "position=< 2, -4> velocity=< 2,  2>",
                "position=<-6, 10> velocity=< 2, -2>",
                "position=< 1,  8> velocity=< 1, -1>",
                "position=< 1,  7> velocity=< 1,  0>",
                "position=<-3, 11> velocity=< 1, -2>",
                "position=< 7,  6> velocity=<-1, -1>",
                "position=<-2,  3> velocity=< 1,  0>",
                "position=<-4,  3> velocity=< 2,  0>",
                "position=<10, -3> velocity=<-1,  1>",
                "position=< 5, 11> velocity=< 1, -2>",
                "position=< 4,  7> velocity=< 0, -1>",
                "position=< 8, -2> velocity=< 0,  1>",
                "position=<15,  0> velocity=<-2,  0>",
                "position=< 1,  6> velocity=< 1,  0>",
                "position=< 8,  9> velocity=< 0, -1>",
                "position=< 3,  3> velocity=<-1,  1>",
                "position=< 0,  5> velocity=< 0, -1>",
                "position=<-2,  2> velocity=< 2,  0>",
                "position=< 5, -2> velocity=< 1,  2>",
                "position=< 1,  4> velocity=< 2,  1>",
                "position=<-2,  7> velocity=< 2, -2>",
                "position=< 3,  6> velocity=<-1, -1>",
                "position=< 5,  0> velocity=< 1,  0>",
                "position=<-6,  0> velocity=< 2,  0>",
                "position=< 5,  9> velocity=< 1, -2>",
                "position=<14,  7> velocity=<-2,  0>",
                "position=<-3,  6> velocity=< 2, -1>"
            };

            PlayStepByStepMessage(input);
        }

        public static void PlayStepByStepMessage(string[] input)
        {
            var sky = Sky.BuildFrom(input);
            var minWidth = int.MaxValue;

            for (var i = 1;; i++)
            {
                sky.Tick();
                var (skyWidth, skyHeight) = sky.Dimensions;
                if (skyWidth < minWidth) minWidth = skyWidth;

                //Console.Clear();
                //Console.SetCursorPosition(0, 0);
                if ( i % 10000 == 0 )
                    Console.WriteLine(i);
                if (minWidth > 500)
                    continue;

                Console.WriteLine($"After {i,4} second(s) - width={skyWidth,5} x height={skyHeight,5}  -  minWidth={minWidth,5}");

                // wait until sky has a reasonable size to attempt at drawing it
                if (skyWidth <= 200 && skyHeight <= 100)
                {
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine($"After {i,4} second(s) - width={skyWidth,5} x height={skyHeight,5}  -  minWidth={minWidth,5}");
                    Console.WriteLine(sky.Draw());
                    Console.WriteLine("press 'q' to exit or any key to continue...");
                    var c = Console.ReadKey();
                    if (c.KeyChar == 'q')
                        break;
                }
            }
        }

        internal class Sky
        {
            private static readonly Regex PointEx = new Regex(@"position=<\s*(?<x>-?\d+),\s*(?<y>-?\d+)> velocity=<\s*(?<vx>-?\d+),\s*(?<vy>-?\d+)>");
            private readonly List<Star> _stars;
            private (int xMin, int xMax, int yMin, int yMax) _dimensions;

            private Sky(IEnumerable<Star> stars)
            {
                _stars = stars.ToList();
            }

            public (int width, int height) Dimensions => (_dimensions.xMax - _dimensions.xMin + 1, _dimensions.yMax - _dimensions.yMin + 1);

            public static Sky BuildFrom(string[] input)
            {
                var stars = input
                    .Select(s => PointEx.Match(s))
                    .Where(m => m.Success)
                    .Select(m => new Star(
                        int.Parse(m.Groups["x"].Value, CultureInfo.InvariantCulture),
                        int.Parse(m.Groups["y"].Value, CultureInfo.InvariantCulture),
                        int.Parse(m.Groups["vx"].Value, CultureInfo.InvariantCulture),
                        int.Parse(m.Groups["vy"].Value, CultureInfo.InvariantCulture)
                    ));
                return new Sky(stars);
            }


            public void Tick()
            {
                int xmin = int.MaxValue, xmax = int.MinValue;
                int ymin = int.MaxValue, ymax = int.MinValue;
                foreach (var star in _stars)
                {
                    star.Tick();
                    if (star.X < xmin) xmin = star.X;
                    if (star.X > xmax) xmax = star.X;
                    if (star.Y < ymin) ymin = star.Y;
                    if (star.Y > ymax) ymax = star.Y;
                }
                _dimensions = (xmin, xmax, ymin, ymax);
            }

            public string Draw()
            {
                var stars = _stars.Select(s => (s.X, s.Y)).ToHashSet();
                var sb = new StringBuilder();
                for (var y = _dimensions.yMin - 1; y <= _dimensions.yMax + 1; y++)
                {
                    for (var x = _dimensions.xMin - 1; x <= _dimensions.xMax + 1; x++)
                        sb.Append(stars.Contains((x, y)) ? '#' : '.');
                    sb.AppendLine();
                }
                return sb.ToString();
            }
        }

        internal class Star
        {
            public int X  { get; private set; }
            public int Y  { get; private set; }
            public int Vx { get; }
            public int Vy { get; }

            public Star(int x, int y, int vx, int vy)
            {
                X  = x;
                Y  = y;
                Vx = vx;
                Vy = vy;
            }

            public void Tick()
            {
                X += Vx;
                Y += Vy;
            }
        }
    }



    [TestFixture]
    internal class Day10Tests
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
