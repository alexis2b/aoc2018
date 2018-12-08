using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace aoc2018
{
    internal class Day04
    {
        private static readonly Regex    DateTimeEx = new Regex(@"^\[1518-([\d]{2})-([\d]{2}) ([\d]{2}):([\d]{2})\]");
        private static readonly DateTime DayOrigin  = new DateTime(1517, 12, 31);

        public static void Run()
        {
            var input = File.ReadAllLines("input\\day04.txt").ToList();
            input.Sort(); // normal input is unsorted

            var res1 = GetGuardMinuteMostAsleep(input);
            Console.WriteLine($"Day04 - part1 - result: {res1}");

            var res2 = GetGuardMinuteMostAsleep2(input);
            Console.WriteLine($"Day04 - part2 - result: {res2}");
        }

        public static int GetGuardMinuteMostAsleep(IEnumerable<string> input)
        {
            // Find guard who slept the most
            var sleepRecord = BuildSleepRecord(input);
            var totalTimeSleptPerGuard = sleepRecord.GroupBy(r => r.Guard)
                .Select(g => new { Guard = g.Key, TimeSlept = g.Sum(r => r.SleepDuration) });
            var maxSleepTime = totalTimeSleptPerGuard.Max(g => g.TimeSlept);
            var maxSleepGuard = totalTimeSleptPerGuard.First(g => g.TimeSlept == maxSleepTime);

            // Find minutes in which he slept the most
            var minutesSlept = sleepRecord.Where(r => r.Guard == maxSleepGuard.Guard)
                .SelectMany(r => Enumerable.Range(r.SleepStart, r.SleepDuration))
                .GroupBy(m => m).Select(g => new { Minute = g.Key, Count = g.Count() });
            var maxMinuteCount = minutesSlept.Max(m => m.Count);
            var maxMinute = minutesSlept.First(m => m.Count == maxMinuteCount);

            return maxSleepGuard.Guard * maxMinute.Minute;

        }

        public static int GetGuardMinuteMostAsleep2(IEnumerable<string> input)
        {
            var sleepRecord = BuildSleepRecord(input);

            // Count all (Guard, Minute) tuples
            var guardMinutesCount = sleepRecord
                .SelectMany(r => Enumerable.Range(r.SleepStart, r.SleepDuration).Select(rr => Tuple.Create(r.Guard, rr)))
                .GroupBy(gm => gm).Select(ggm => new { Guard = ggm.Key.Item1, Minute = ggm.Key.Item2, Count = ggm.Count() });
            var maxMinuteSlept = guardMinutesCount.Max(gm => gm.Count);
            var maxMinute = guardMinutesCount.First(gm => gm.Count == maxMinuteSlept);

            return maxMinute.Guard * maxMinute.Minute;
        }

        // (guard, day, start sleeping, finish sleeping)
        private static List<SleepRecord> BuildSleepRecord(IEnumerable<string> input)
        {
            var record = new List<SleepRecord>();

            // parsing state
            var currentGuard = 0;
            Tuple<int,int> startSleepingOn = null; // (day, minute)

            foreach(var row in input)
            {
                if (row.Contains("begins"))
                {
                    // Find or create based on Guard Id
                    currentGuard = ParseGuardId(row);
                }
                else if (row.Contains("asleep"))
                {
                    startSleepingOn = ParseDayMinute(row);
                }
                else if (row.Contains("wakes"))
                {
                    var finishSleepingOn = ParseDayMinute(row);
                    if (startSleepingOn.Item1 != finishSleepingOn.Item1)
                        throw new Exception("Logic fail: mismatch between sleep and wake day");
                    record.Add(new SleepRecord(currentGuard, startSleepingOn.Item1, startSleepingOn.Item2, finishSleepingOn.Item2));
                }
            }

            return record;
        }


        private static int ParseGuardId(string row)
        {
            var rowEnd = row.Substring(row.IndexOf('#')+1);
            return int.Parse(rowEnd.Substring(0, rowEnd.IndexOf(' ')));
        }

        // Day is day index in the year 1518 (1st Jan = 1, 2nd jan = 2, ..., 31st Dec = 365)
        private static Tuple<int,int> ParseDayMinute(string row)
        {
            var match = DateTimeEx.Match(row);
            var date = new DateTime(1518,
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value),
                int.Parse(match.Groups[4].Value),
                0
                );

            return Tuple.Create((date - DayOrigin).Days, date.Minute);
        }



        // Simple immutable record
        private sealed class SleepRecord
        {
            public int Guard { get; }
            public int Day { get; }
            public int SleepStart { get; }
            public int SleepEnd { get; }
            public int SleepDuration => SleepEnd - SleepStart;

            public SleepRecord(int guard, int day, int sleepStart, int sleepEnd)
            {
                Guard = guard;
                Day = day;
                SleepStart = sleepStart;
                SleepEnd = sleepEnd;
            }
        }
    }




    [TestFixture]
    internal class Day04Tests
    {
        private readonly string[] _input = new[] {
            "[1518-11-01 00:00] Guard #10 begins shift",
            "[1518-11-01 00:05] falls asleep",
            "[1518-11-01 00:25] wakes up",
            "[1518-11-01 00:30] falls asleep",
            "[1518-11-01 00:55] wakes up",
            "[1518-11-01 23:58] Guard #99 begins shift",
            "[1518-11-02 00:40] falls asleep",
            "[1518-11-02 00:50] wakes up",
            "[1518-11-03 00:05] Guard #10 begins shift",
            "[1518-11-03 00:24] falls asleep",
            "[1518-11-03 00:29] wakes up",
            "[1518-11-04 00:02] Guard #99 begins shift",
            "[1518-11-04 00:36] falls asleep",
            "[1518-11-04 00:46] wakes up",
            "[1518-11-05 00:03] Guard #99 begins shift",
            "[1518-11-05 00:45] falls asleep",
            "[1518-11-05 00:55] wakes up"
        };

        [Test]
        public void Test1_1()
        {
            var res = Day04.GetGuardMinuteMostAsleep(_input);
            Assert.AreEqual(240, res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day04.GetGuardMinuteMostAsleep2(_input);
            Assert.AreEqual(4455, res);
        }
    }
}
