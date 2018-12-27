using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace aoc2018
{
    internal class Day09
    {
        public static void Run()
        {
            // 476 players; last marble is worth 71431 points

            var res1 = GetHighScore(476, 71431);
            Console.WriteLine($"Day09 - part1 - result: {res1}");

            //var res2 = CheckLicenseFile2(input);
            //Console.WriteLine($"Day09 - part2 - result: {res2}");
        }

        public static int GetHighScore(int playersCount, int lastMarbleWorth)
        {
            //var players = new int[playersCount];
            //var player  = 0;
            //for(int m = 0; m <= lastMarbleWorth; m++)
            //{


            //}

            return 0;
        }
    }




    [TestFixture]
    internal class Day09Tests
    {
        [TestCase(9, 25, 32)]
        [TestCase(10, 1618, 8317)]
        public void Test1(int players, int lastMarble, int expected)
        {
            var res = Day09.GetHighScore(players, lastMarble);
            Assert.AreEqual(expected, res);
        }
    }
}
