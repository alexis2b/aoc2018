using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 59000441 is too low (guessed wrong), which means 586 is also too low
/// </summary>

namespace aoc2018
{
    internal class Day23
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day23.txt");

            var pgcd = SearchPgcd(input);
            Console.WriteLine($"Day23 - greatest common divisor of input set values is: {pgcd}");

            var res1 = GetInRangeOfStrongest(input);
            Console.WriteLine($"Day23 - part1 - result: {res1}");

            var res2 = GetBestCoordinatesDistance(input);
            Console.WriteLine($"Day23 - part2 - result: {res2}");
        }

        public static int GetInRangeOfStrongest(string[] input)
        {
            var nanobots = input.Select(Bot.FromString).ToList();
            // Find strongest emitting bot
            var bestBotSignal = nanobots.Max(b => b.SignalRadius);
            var bestBot = nanobots.Find(b => b.SignalRadius == bestBotSignal);
            // Count robots in range
            var count = nanobots.Count(n => bestBot.IsInRange(n));
            return count;
        }

        public static int SearchPgcd(string[] input)
        {
            int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);

            var nanobots = input.Select(Bot.FromString).ToList();
            var numbers  = nanobots.SelectMany(n => new[] {n.X, n.Y, n.Z, n.SignalRadius}).Select(Math.Abs).Distinct().OrderBy(n => n).ToArray();
            return numbers.Aggregate(GCD);
        }

        // Part 2 Implem 4 - idea: project radius segments over (x, y, z) and measure the density of coverage
        // (then let's see what to do with that!)
        public static int GetBestCoordinatesDistance(string[] input)
        {
            var bots = input.Select(Bot.FromString).ToList();
            var botsCounter = new BotsCounter(bots);
            // Ranges are made of coordinates [First;Last] and the frequency of occurrence of bots in this range, projected on that axis
            var rangesX = BuildSegments(bots, b => b.X).ToRanges();
            var rangesY = BuildSegments(bots, b => b.Y).ToRanges();
            var rangesZ = BuildSegments(bots, b => b.Z).ToRanges();
            // we create search ranges with the highest possible Freq for that search range (min of FreqX, FreqY, FreqZ)
            // in order to limit the size, we limit ourselves to ranges with 70% or more of the highest possible freq (arbitrary!)
            var bestFreqX = rangesX.Max(r => r.Freq);
            var bestFreqY = rangesX.Max(r => r.Freq);
            var bestFreqZ = rangesX.Max(r => r.Freq);
            var bestPossibleFreq = new[] {bestFreqX, bestFreqY, bestFreqZ}.Min();
            var freqThreshold = Math.Min(95 * bestPossibleFreq / 100 + 1, bestPossibleFreq-1); // 95% with integer operations!
            Console.WriteLine($"BPF={bestPossibleFreq}, threshold={freqThreshold}");

            var searchRanges = new List<SearchRange>();
            foreach(var rangeX in rangesX.Where(r => r.Freq >= freqThreshold))
            foreach(var rangeY in rangesY.Where(r => r.Freq >= freqThreshold))
            foreach(var rangeZ in rangesZ.Where(r => r.Freq >= freqThreshold))
                searchRanges.Add(new SearchRange(rangeX, rangeY, rangeZ));
            Console.WriteLine($"searchRanges.Count = {searchRanges.Count}");

            var bestFreq = 0;
            var bestDistance = int.MaxValue;

            // now we search those ranges in order from highest freq / smallest coord. to lowest freq / higher coord.
            foreach (var searchRange in searchRanges.OrderByDescending(sr => sr.MaxPossibleFreq).ThenBy(sr => sr.MinOriginDistance))
            {
                Console.WriteLine($"Searching {searchRange}...");

                if (searchRange.MaxPossibleFreq < bestFreq) // no point searching...
                    break;

                // Optimization: breadth-first search from origin (to increase distance regularly)
                for (var x = searchRange.X.First; x <= searchRange.X.Last; x++)
                {
                    // Hypothesis:
                    // due to coordinates scale, we can skip a lot of calculations which are "uninteresting"
                    // given a fixed (x,y) values (z) is a line - we can "just" lookup the interesting points on this line for each
                    // bot and quickly jump between those points, skipping a lot in between
                    // the interesting values of Z are the intersections of the circle of magnitude (r) centered around each (bot) for a given
                    // (x,y) if we associate a +1 (entering) and -1 (exiting) the circle of range of the bot, we can quickly sum those points
                    // to find the "interesting" ranges on that axis (i.e. actual max possible values)
                    Console.WriteLine($"Scanning x={x}");
                    botsCounter.SetX(x);
                    var distBaseX = Math.Abs(x);
                    for (var y = searchRange.Y.First; y <= searchRange.Y.Last; y++)
                    {
                        var (bestZFreq, bestZDist) = botsCounter.SetY(y);
                        //Console.WriteLine($"on ({x},{y}), bestZFreq={bestZFreq}, bestZDist={bestZDist}");
                        var distance = distBaseX + Math.Abs(y) + bestZDist;
                        if (bestZFreq > bestFreq)
                        {
                            bestFreq     = bestZFreq;
                            bestDistance = distance;
                            Console.WriteLine($"new optimum found: freq={bestFreq}, dist={bestDistance}");
                        }
                        else if (bestZFreq == bestFreq && distance < bestDistance)
                        {
                            bestDistance = distance;
                            Console.WriteLine($"new optimum found: freq={bestFreq}, dist={bestDistance}");
                        }
                    }
                }

                if (bestFreq == searchRange.MaxPossibleFreq) // no need to search further!
                    break;
            }

            return bestDistance;
        }

        public static Segment BuildSegments(List<Bot> bots, Func<Bot, int> extractor)
        {
            // build origin [Min, Max]
            var origin = new Segment(int.MinValue, 0, new Segment(int.MaxValue, 0, null));
            // add segments one after each other
            foreach (var bot in bots)
            {
                var botCentre = extractor(bot);
                var botStart = botCentre - bot.SignalRadius;
                var botEnd   = botCentre + bot.SignalRadius + 1; // not inclusive due to how we build segments

                // ------- add into the existing segments --------
                // Locate start of segment
                var seg = Segment.FirstAtOrBefore(origin, botStart);
                if (seg.Start < botStart)
                { // insert a new segment starting at botStart
                    seg = seg.Next = new Segment(botStart, seg.Count, seg.Next);
                }
                // -- seg is now the segment starting at BotStart - counter has not been increased yet
                
                // iterate over all segments which are fully covered by [botStart;botEnd[ (including the first one if it is covered)
                while (seg.Next.Start < botEnd)
                {
                    seg.Count++;
                    seg = seg.Next;
                }

                // last segment - either we have an exact close (seg.Next.Start == botEnd) or we need to insert our own End
                // counter for last segment has not been increased yet
                if (seg.Next.Start != botEnd)
                    seg.Next = new Segment(botEnd, seg.Count, seg.Next);
                seg.Count++; // final segment gets additional count as well

                //Debug.WriteLine($"after adding [{botStart},{botEnd}[");
                //Segment.PrintSegments(origin);
            }

            // Compress segments

            return origin;
        }


        // Part 2 Implem 3 - idea: do a "box intersection" between bots radius of actions and count
        // number of intersections to delimit the best area
        public static int GetBestCoordinatesDistanceImplem3(string[] input)
        {
            var bots = input.Select(Bot.FromString).ToList();
            return 0;
        }

        // Part 2 Implem 2 - idea: look up "key points" along x, y, and z by using intersection levels
        // iterate over those points then perform a local search
        // still too slow
        public static int GetBestCoordinatesDistanceImplem2(string[] input)
        {
            var bots = input.Select(Bot.FromString).ToList();

            // utility function - count bots in range
            int CountBotsInRange(int x, int y, int z) => bots.Count(b => b.IsInRange(x, y, z));

            // calculate "key points" along all axis
            var keyPointsX = bots.SelectMany(b => new[] {b.X - b.SignalRadius, b.X, b.X + b.SignalRadius}).Distinct().OrderBy(x => x).ToArray();
            var keyPointsY = bots.SelectMany(b => new[] {b.Y - b.SignalRadius, b.Y, b.Y + b.SignalRadius}).Distinct().OrderBy(y => y).ToArray();
            var keyPointsZ = bots.SelectMany(b => new[] {b.Z - b.SignalRadius, b.Z, b.Z + b.SignalRadius}).Distinct().OrderBy(z => z).ToArray();
            // find "best combination" along those points
            var bestBotsCount = 0;
            var bestPositions = new List<Point>();
            foreach(var x in keyPointsX)
            foreach(var y in keyPointsY)
            foreach(var z in keyPointsZ)
            {
                var botsCount = CountBotsInRange(x, y, z);
                if (botsCount > bestBotsCount)
                {
                    bestBotsCount = botsCount;
                    bestPositions.Clear();
                    bestPositions.Add(new Point(x, y, z));
                }
                else if (botsCount == bestBotsCount)
                    bestPositions.Add(new Point(x, y, z));
            }

            // Find minimum distance to origin
            var minDistance = bestPositions.Select(p => p.OriginDistance).Min();
            
            return minDistance;
        }


        // Part 2 - implem. 1 - too slow
        public static int GetBestCoordinatesDistanceImplem1(string[] input)
        {
            var nanobots = input.Select(Bot.FromString).ToList();
            var bestCount = 0;
            var bestPoints = new Point[] {};

            foreach (var bot in nanobots)
            {
                var pointsInRange = bot
                    .GetAllPointsInRange()
                    .Select(p => Tuple.Create(p, nanobots.Count(b => b.IsInRange(p.X, p.Y, p.Z)))).ToArray();
                var botBestCount = pointsInRange.Max(p => p.Item2);
                if (botBestCount > bestCount)
                {
                    bestCount = botBestCount;
                    bestPoints = pointsInRange.Where(p => p.Item2 == bestCount).Select(p => p.Item1).ToArray();
                }
            }

            // Find smallest position
            var bestDistance = bestPoints.Select(p => Math.Abs(p.X) + Math.Abs(p.Y) + Math.Abs(p.Z)).Max();
            return bestDistance;
        }
    }

    internal sealed class Bot
    {
        private static readonly Regex BotEx = new Regex(@"pos=<(-?\d+),(-?\d+),(-?\d+)>, r=(\d+)");

        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int SignalRadius { get; }

        private Bot(int x, int y, int z, int signalRadius)
        {
            X = x;
            Y = y;
            Z = z;
            SignalRadius = signalRadius;
        }

        public static Bot FromString(string str)
        {
            var match = BotEx.Match(str);
            if (!match.Success)
                throw new Exception($"Failed to parse input '{str}'");
            return new Bot(
                int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture)
            );
        }

        public bool IsInRange(Bot other)
            => IsInRange(other.X, other.Y, other.Z);

        public bool IsInRange(int x, int y, int z)
            => Math.Abs(x - X) + Math.Abs(y - Y) + Math.Abs(z - Z) <= SignalRadius;

        public IEnumerable<Point> GetAllPointsInRange()
        {
            var allX = Enumerable.Range(X - SignalRadius, 2 * SignalRadius + 1);
            var allY = Enumerable.Range(Y - SignalRadius, 2 * SignalRadius + 1);
            var allZ = Enumerable.Range(Z - SignalRadius, 2 * SignalRadius + 1);

            return allX.SelectMany(
                x => allY.SelectMany(
                    y => allZ.Select(
                        z => new Point(x, y, z)).Where(p => IsInRange(p.X, p.Y, p.Z)
            )));
        }
    }

    /// Object using an optimized representation of bots to accelerate the counting
    /// Also precompXTerm is an optimization to calculate only the relevant values of Z (solutions to bot equation)
    internal sealed class BotsCounter
    {
        private readonly int _botCount;
        private readonly int[,] _bots;

        public BotsCounter(IReadOnlyList<Bot> bots)
        {
            _botCount = bots.Count;
            _bots = new int[_botCount, 5]; // 0:X, 1:Y, 2:Z, 3:R, 4:precompXTerm
            for (var i = 0; i < _botCount; i++)
            {
                _bots[i, 0] = bots[i].X;
                _bots[i, 1] = bots[i].Y;
                _bots[i, 2] = bots[i].Z;
                _bots[i, 3] = bots[i].SignalRadius;
            }
        }

        // Number of bots in range of that point
        public int CountInRange(int x, int y, int z)
        {
            var count = 0;
            for (var i = 0; i < _botCount; i++)
                if (Math.Abs(x - _bots[i, 0]) + Math.Abs(y - _bots[i, 1]) + Math.Abs(z - _bots[i, 2]) <= _bots[i, 3])
                    count++;
            return count;
        }

        // Precompute the term R-|x-Bx| since x is known (prepare for SetY which will calculate the Z-values)
        public void SetX(int x)
        {
            for (var i = 0; i < _botCount; i++)
                _bots[i, 4] = _bots[i, 3] - Math.Abs(x - _bots[i, 0]);
        }

        // called after SetX
        // Finish solving the Z equation, that can yield 0, 1 or 2 positions (roots where Z intersects with the (x,y) plane for the bot radius)
        // Returns a list of [coordinate,impact on count], with impact on count - there are no duplicate coordinates
        // optimized code -> ugly!
        public (int bestCount, int bestDist) SetY(int y)
        {
            var res = new List<Tuple<int, int>>(2 * _botCount);
            for (var i = 0; i < _botCount; i++)
            {
                // look up target of |z-Bz|
                var absTarget = _bots[i, 4] - Math.Abs(y - _bots[i, 1]);
                // 0, 1 or 2 solutions
                if (absTarget > 0)
                {
                    var botZ = _bots[i, 2];
                    var sol1 = botZ - absTarget;
                    var sol2 = botZ + absTarget;
                    if (sol1 < sol2) // enter in sol1, exit after sol2
                    {
                        res.Add(new Tuple<int,int>(sol1,    1));
                        res.Add(new Tuple<int,int>(sol2+1, -1));
                    }
                    else // enter in sol2, exist after sol1
                    {
                        res.Add(new Tuple<int, int>(sol2,    1));
                        res.Add(new Tuple<int, int>(sol1+1, -1));
                    }
                }
                else if (absTarget == 0)
                {
                    var sol = _bots[i, 2]; // enter in sol, exist after sol
                    res.Add(new Tuple<int, int>(sol,    1));
                    res.Add(new Tuple<int, int>(sol+1, -1));
                }
                // else no solution
            }

            // sort by values of Z
            res.Sort(ZComparison);

            // accumulate and find the best point (best count with smallest coordinate)
            var bestCount = -1;
            var bestDist  = int.MaxValue;
            var curStart  = res[0].Item1;
            var curCount  = res[0].Item2;
            for (var i = 1; i < res.Count; i++)
            {
                var point = res[i];
                if (curStart == point.Item1)
                    curCount += point.Item2;
                else
                {
                    // we have a new Range such that First = curStart, Last = point.Item1 - 1, Freq curCount
                    // find if this is a better range
                    if (curCount >= bestCount)
                    {
                        var dist = Math.Min(Math.Abs(curStart), Math.Abs(point.Item1));
                        if (curCount == bestCount && dist < bestDist)
                            bestDist = dist;
                        if (curCount > bestCount)
                        {
                            bestCount = curCount;
                            bestDist = dist;
                        }
                    }

                    curStart = point.Item1;
                    curCount += point.Item2;
                }
            }

            // return best range and position
            return (bestCount, bestDist);
        }

        private static int ZComparison(Tuple<int, int> a, Tuple<int, int> b) => a.Item1 - b.Item1;
    }

    internal sealed class Point : IEquatable<Point>
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public int OriginDistance => Math.Abs(X) + Math.Abs(Y) + Math.Abs(Z);

        public Point(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override int GetHashCode()
            => 42 ^ X ^ Y ^ Z;

        public override bool Equals(object other)
            => Equals(other as Point);

        public bool Equals(Point other)
            => other != null && other.X == X && other.Y == Y && other.Z == Z;
    }

    internal sealed class Segment
    {
        public int Start { get; }          // coordinates at which the segment start (inclusive)
        public int Count { get; set; }     // number of bots included in this segment
        public Segment Next { get; set; }  // next segment

        public Segment(int start, int count, Segment next)
        {
            Start = start;
            Count = count;
            Next  = next;
        }

        public Range ToRange()
            => new Range(Start, Next.Start - 1, Count);

        // Find first segment which starts at or before the given segment start
        public static Segment FirstAtOrBefore(Segment origin, int segmentStart)
        {
            var segment = origin;
            while (segment.Next.Start < segmentStart)
                segment = segment.Next;
            return segment.Next.Start == segmentStart ? segment.Next : segment;
        }

        public static void PrintSegments(Segment origin)
        {
            var segment = origin;
            while (segment.Next != null)
            {
                if (segment.Start != int.MinValue && segment.Next.Start != int.MaxValue)
                {
                    if ( segment.Start == segment.Next.Start-1 ) // case of scalar
                        Debug.WriteLine($"[  {segment.Start,3}       ]  => {segment.Count,3}");
                    else
                        Debug.WriteLine($"[  {segment.Start,3} - {segment.Next.Start-1,-3} ]  => {segment.Count,3}");
                }
                segment = segment.Next;
            }
        }

        public List<Range> ToRanges()
        {
            var ranges = new List<Range>();
            var segment = this;
            while (segment.Next != null)
            {
                if (segment.Start != int.MinValue && segment.Next.Start != int.MaxValue)
                    ranges.Add(segment.ToRange());
                segment = segment.Next;
            }

            return ranges;
        }
    }

    // simpler structure in a collection
    internal class Range
    {
        public int First { get; }
        public int Last  { get; }
        public int Freq  { get; }

        public Range(int first, int last, int freq)
        {
            First = first;
            Last  = last;
            Freq  = freq;
        }
    }

    // Search structure over a combination of ranges for each dimension
    internal class SearchRange
    {
        public Range X { get; }
        public Range Y { get; }
        public Range Z { get; }
        public int MaxPossibleFreq { get; }
        public int MinOriginDistance { get; }

        public SearchRange(Range x, Range y, Range z)
        {
            X = x;
            Y = y;
            Z = z;
            MaxPossibleFreq = Math.Min(x.Freq, Math.Min(y.Freq, z.Freq));
            MinOriginDistance =
                Math.Min(Math.Abs(x.First), Math.Abs(x.Last)) +
                Math.Min(Math.Abs(y.First), Math.Abs(y.Last)) +
                Math.Min(Math.Abs(z.First), Math.Abs(z.Last));
        }

        public override string ToString()
            =>
                $"X=[{X.First},{X.Last}], Y=[{Y.First},{Y.Last}], Z=[{Z.First},{Z.Last}], MPF={MaxPossibleFreq}, MOD={MinOriginDistance}";
    }


    [TestFixture]
    internal class Day23Tests
    {
        public static readonly string[] TestInput = new[]
        {
            "pos=<0,0,0>, r=4",
            "pos=<1,0,0>, r=1",
            "pos=<4,0,0>, r=3",
            "pos=<0,2,0>, r=1",
            "pos=<0,5,0>, r=3",
            "pos=<0,0,3>, r=1",
            "pos=<1,1,1>, r=1",
            "pos=<1,1,2>, r=1",
            "pos=<1,3,1>, r=1"
        };

        public static readonly string[] TestInput2 = new[]
        {
            "pos=<10,12,12>, r=2",
            "pos=<12,14,12>, r=2",
            "pos=<16,12,12>, r=4",
            "pos=<14,14,14>, r=6",
            "pos=<50,50,50>, r=200",
            "pos=<10,10,10>, r=5"
        };

        [Test]
        public void Test1()
        {
            var res = Day23.GetInRangeOfStrongest(TestInput);
            Assert.AreEqual(7, res);
        }

        [Test]
        public void Test2()
        {
            var res = Day23.GetBestCoordinatesDistance(TestInput2);
            Assert.AreEqual(36, res);
        }

        [TestCase(-256, int.MinValue)]
        [TestCase(-124, int.MinValue)]
        [TestCase(-123, -123)]
        [TestCase(-122, -123)]
        [TestCase(-6,   -123)]
        [TestCase(-5,   -5)]
        [TestCase(-4,   -5)]
        [TestCase(256,  256)]
        [TestCase(int.MaxValue - 1,  256)]
        public void TestSegmentFirstAtOrBefore(int searchVal, int resultStart)
        {
            var origin =
                new Segment(int.MinValue, 0,
                    new Segment(-123, 0,
                        new Segment(-5, 0,
                            new Segment(12, 0,
                                new Segment(256, 0,
                                    new Segment(int.MaxValue, 0, null))))));
            var seg1 = Segment.FirstAtOrBefore(origin, searchVal);
            Assert.AreEqual(resultStart, seg1.Start);
        }

        [Test]
        public void TestSegment2()
        {
            var bots = TestInput2.Select(Bot.FromString).ToList();
            var segmentsX = Day23.BuildSegments(bots, b => b.X);
            Debug.WriteLine("Along X:");
            Segment.PrintSegments(segmentsX);

            var segmentsY = Day23.BuildSegments(bots, b => b.Y);
            Debug.WriteLine("Along Y:");
            Segment.PrintSegments(segmentsY);

            var segmentsZ = Day23.BuildSegments(bots, b => b.Z);
            Debug.WriteLine("Along Z:");
            Segment.PrintSegments(segmentsZ);
        }
    }
}
