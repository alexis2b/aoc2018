using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace aoc2018
{
    internal class Day07
    {
        private static readonly Regex StepEx = new Regex(@"Step (\w) must be finished before step (\w) can begin\.");

        public static void Run()
        {
            var input = File.ReadAllLines("input\\day07.txt");

            var res1 = FindStepsOrder(input);
            Console.WriteLine($"Day07 - part1 - result: {res1}");

            var res2 = FindTimeToComplete(res1, input, 5, 60);
            Console.WriteLine($"Day07 - part2 - result: {res2}");
        }

        // Return Tuple<char, char> where Item1 is the Step, and Item2 is the dependency step that must be completed first
        private static IEnumerable<Tuple<char, char>> ParseStepDependencies(IEnumerable<string> input)
            => input.Select(r => StepEx.Match(r)).Select(m => Tuple.Create(m.Groups[2].Value[0], m.Groups[1].Value[0]));

        public static string FindStepsOrder(IEnumerable<string> input)
        {
            var stepDeps = ParseStepDependencies(input).ToList(); // returns Tuple<A, B> where A depends on B
            var allSteps = stepDeps.SelectMany(s => new[] { s.Item1, s.Item2 }).ToArray(); // complete step list

            var completedSteps = string.Empty;
            while(true)
            {
                var readySteps = allSteps
                    .Where(s => !completedSteps.Contains(s)) // not completed yet
                    .Where(s => stepDeps.Where(sd => sd.Item1 == s).All(sd => completedSteps.Contains(sd.Item2))) // all dependencies completed (or no deps)
                    .ToList();
                readySteps.Sort();

                if (readySteps.Count > 0) completedSteps += readySteps[0]; else break;
            }

            return completedSteps;
        }

        public static int FindTimeToComplete(string sequence, IEnumerable<string> input, int workerCount, int baseDelay)
        {
            // while we have a sequence to complete
            // is 1st letter ready to be started (check all dependencies have completed)
            //    if yes, assign to an idle worker with the work time - remove from sequence
            // look for first working finishing his work (min time)
            //    add time to elapsed time (jump to completion)
            //    make step as completed, release worker
            // loop until sequence is completed
            var stepDeps  = ParseStepDependencies(input).ToList(); // returns Tuple<A, B> where A depends on B
            var workers   = Enumerable.Range(1, workerCount).Select(i => new Worker()).ToList();
            var completed = string.Empty;
            var pending   = sequence.ToList();
            var clock     = 0;
            while(true)
            {
                // Start on all ready steps, depending on worker's availability
                var nextSteps = pending.Where(p => stepDeps.Where(sd => sd.Item1 == p).All(sd => completed.Contains(sd.Item2))).ToList();
                foreach(var nextStep in nextSteps)
                {
                    var worker = workers.FirstOrDefault(w => w.IsIdle);
                    if ( worker != null )
                    {
                        worker.StartWork(nextStep, clock + baseDelay + (nextStep - 64));
                        pending.Remove(nextStep);
                    }
                }

                // Move clock to next finishing work (might be multiple workers)
                clock = workers.Where(w => !w.IsIdle).OrderBy(w => w.CompletionTime).First().CompletionTime;
                foreach(var worker in workers.Where(w => !w.IsIdle && w.CompletionTime == clock))
                    completed += worker.EndWork();

                if (completed.Length == sequence.Length) break; // job done
            }

            return clock;
        }

        private sealed class Worker
        {
            private char? _workingOn;

            public int  CompletionTime { get; private set; }
            public bool IsIdle   => !_workingOn.HasValue;

            public void StartWork(char step, int completionTime)
            {
                _workingOn     = step;
                CompletionTime = completionTime;
            }

            public char EndWork()
            {
                var step      = _workingOn.Value;
                _workingOn    = null;
                return step;
            }
        }
    }




    [TestFixture]
    internal class Day07Tests
    {
        private readonly string[] _input = new[]
        {
            "Step C must be finished before step A can begin.",
            "Step C must be finished before step F can begin.",
            "Step A must be finished before step B can begin.",
            "Step A must be finished before step D can begin.",
            "Step B must be finished before step E can begin.",
            "Step D must be finished before step E can begin.",
            "Step F must be finished before step E can begin."
        };

        [Test]
        public void Test1_1()
        {
            var res = Day07.FindStepsOrder(_input);
            Assert.AreEqual("CABDFE", res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day07.FindTimeToComplete("CABDFE", _input, 2, 0);
            Assert.AreEqual(15, res);
        }
    }
}
