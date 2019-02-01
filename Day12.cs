using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day12
    {
        public static void Run()
        {
            var input1 = File.ReadAllText("input\\day12_1.txt");
            var input2 = File.ReadAllLines("input\\day12_2.txt");

            var res1 = GetPlantPotsSum(input1, input2, 20);
            Console.WriteLine($"Day12 - part1 - result: {res1}");

            var res2 = GetOptimizedPlantPotsSum(input1, input2, 50000000000L);
            Console.WriteLine($"Day12 - part2 - result: {res2}");
        }

        public static int GetPlantPotsSum(string initialState, string[] rulesDescription, int iterations)
        {
            var rules = rulesDescription.ToDictionary(r => r.Substring(0, 5), r => r.Substring(9, 1)[0]);

            // extend on left-hand and right-hand sides
            var state = new string('.', iterations+5) + initialState + new string('.', iterations + 5);
            Console.WriteLine($" 0: {state}");
            for (var i = 0; i < iterations; i++)
            {
                state = NextGeneration(state, rules);
                Console.WriteLine($"{i+1,2}: {state}");
            }

            // sum (including offset)
            var res = state.Select((c, i) => c == '#' ? i - (iterations + 5) : 0).Sum();
            return res;
        }

        private static string NextGeneration(string state, IReadOnlyDictionary<string, char> rules)
        {
            var newState = Enumerable.Repeat('.', state.Length).ToArray();
            for (var i = 0; i < state.Length - 5; i++)
            {
                var pattern   = state.Substring(i, 5);
                newState[i+2] = rules.ContainsKey(pattern) ? rules[pattern] : '.'; // assume if pattern is not present, there will be no plant
            }
            return new string(newState);
        }

        // Approche pour phase 2
        // stocke seulement la première et la dernière plante (et retient l'offset)
        // après on ajoute deux "vides" avant et deux "vides" après et on fait 
        // cherche un cycle pour ne pas avoir à faire les 50 milliards
        public static long GetOptimizedPlantPotsSum(string initialState, string[] rulesDescription, long iterations)
        {
            var simulation = Simulation.FromInput(initialState, rulesDescription);
            simulation.RunIterations(iterations);
            return simulation.PlantPotsSum();
        }


        internal sealed class Simulation
        {
            private const string Padding  = "....";
            private const char   Flower   = '#';
            private const char   NoFlower = '.';
            private readonly IReadOnlyDictionary<string, char> _rules;
            private string _state;
            private long   _offset;

            private Simulation(IReadOnlyDictionary<string, char> rules, string state, long offset)
            {
                _rules  = rules;
                _state  = state;
                _offset = offset;
            }

            public static Simulation FromInput(string initialState, string[] rulesDescription)
            {
                var rules = rulesDescription.ToDictionary(r => r.Substring(0, 5), r => r.Substring(9, 1)[0]);
                var (state, offset) = CompressState(initialState, 0);
                return new Simulation(rules, state, offset);
            }

            private static (string newState, long newOffset) CompressState(string state, long offset)
            {
                var first     = state.IndexOf(Flower);
                var last      = state.LastIndexOf(Flower);
                var newState  = state.Substring(first, last - first + 1);
                var newOffset = offset + first;
                return (newState, newOffset);
            }

            public void RunIterations(long iterations)
            {
                var cycleCache = new Dictionary<string, (long iteration, long offset)>();

                for (var it = 1L; it <= iterations; it++)
                {
                    // prepare string by padding
                    var stateBefore  = Padding + _state + Padding;
                    var offsetBefore = _offset - Padding.Length;

                    // run the transformation
                    var stateTransform = new char[stateBefore.Length];
                    stateTransform[0] = NoFlower;
                    stateTransform[1] = NoFlower;
                    stateTransform[stateTransform.Length-2] = NoFlower;
                    stateTransform[stateTransform.Length-1] = NoFlower;
                    for (var i = 0; i < stateTransform.Length - 4; i++)
                    {
                        var pattern = stateBefore.Substring(i, 5);
                        stateTransform[i + 2] = _rules.TryGetValue(pattern, out var outcome) ? outcome : NoFlower; // assume if pattern is not present, there will be no plant
                    }

                    // compress to get the new state
                    var (stateAfter, offsetAfter) = CompressState(new string(stateTransform), offsetBefore);
                    
                    // check cycles
                    if (cycleCache.TryGetValue(stateAfter, out var previousMatch))
                    {
                        // cycle found
                        Console.WriteLine($"Cycle found at iteration {it} / offset {offsetAfter} with previous iteration {previousMatch.iteration} / offset {previousMatch.offset}");

                        // leap-frog to target state
                        var cycleStartsAt        = previousMatch.iteration;
                        var cyclePeriod          = it - cycleStartsAt;
                        var cycleOffsetDelta     = offsetAfter - previousMatch.offset;
                        var targetStateIteration = cycleStartsAt + (iterations - cycleStartsAt) % cyclePeriod;
                        var targetState          = cycleCache.First(kvp => kvp.Value.iteration == targetStateIteration);
                        var targetOffset         = targetState.Value.offset + cycleOffsetDelta * (iterations - cycleStartsAt);

                        _state  = targetState.Key;
                        _offset = targetOffset;
                        break;
                    }

                    cycleCache[stateAfter] = (it, offsetAfter);
                    _state  = stateAfter;
                    _offset = offsetAfter;
                }
            }

            public long PlantPotsSum() => _state.Select((c, i) => c == Flower ? i + _offset : 0).Sum();
        }
    }


    [TestFixture]
    internal class Day12Tests
    {
        private const string InitialState = "#..#.#..##......###...###";
        private static readonly string[] Rules = {
            "...## => #",
            "..#.. => #",
            ".#... => #",
            ".#.#. => #",
            ".#.## => #",
            ".##.. => #",
            ".#### => #",
            "#.#.# => #",
            "#.### => #",
            "##.#. => #",
            "##.## => #",
            "###.. => #",
            "###.# => #",
            "####. => #"
        };

        [Test]
        public void Test1()
        {
            var res = Day12.GetPlantPotsSum(InitialState, Rules, 20);
            Assert.AreEqual(325, res);
        }

        [Test]
        public void Test2()
        {
            var res = Day12.GetOptimizedPlantPotsSum(InitialState, Rules, 20);
            Assert.AreEqual(325, res);
        }
    }
}
