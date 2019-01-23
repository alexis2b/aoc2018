using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day14
    {
        private static readonly int[] ScoreSeed = {3, 7};
        private const int CharToNum = 48;

        public static void Run()
        {
            const int input = 702831;

            var res1 = GetNext10RecipeScores(input);
            Console.WriteLine($"Day14 - part1 - result: {res1}");

            var res2 = FindScoreSequence(input.ToString());
            Console.WriteLine($"Day14 - part2 - result: {res2}");
        }

        public static string GetNext10RecipeScores(int after)
        {
            var scores = new List<int>(ScoreSeed);
            var elf1CurrentRecipe = 0;
            var elf2CurrentRecipe = 1;

            while (scores.Count < after + 10)
            {
                var elf1CurrentRecipeScore = scores[elf1CurrentRecipe % scores.Count];
                var elf2CurrentRecipeScore = scores[elf2CurrentRecipe % scores.Count];
                var newScore = elf1CurrentRecipeScore + elf2CurrentRecipeScore;
                if (newScore < 10)
                    scores.Add(newScore);
                else
                {
                    scores.Add(1);
                    scores.Add(newScore - 10);
                }
                elf1CurrentRecipe = (elf1CurrentRecipe + 1 + elf1CurrentRecipeScore) % scores.Count;
                elf2CurrentRecipe = (elf2CurrentRecipe + 1 + elf2CurrentRecipeScore) % scores.Count;
            }

            return string.Join(string.Empty, scores.Skip(after).Take(10));
        }

        public static int FindScoreSequence(string seq)
        {
            var scores              = new List<int>(ScoreSeed);
            var seqToMatch          = seq.Select(CharToDigit).ToList();
            var seqToMatchLength    = seqToMatch.Count;
            var seqToMatchLastDigit = seqToMatch[seqToMatchLength - 1];
            var elf1CurrentRecipe   = 0;
            var elf2CurrentRecipe   = 1;
            var loopCount = 0;

            bool MatchSequence()
            {
                var iBase = scores.Count - seqToMatchLength;
                if (iBase < 0) return false;
                for (var i = 0; i < seqToMatchLength; i++)
                    if (scores[iBase + i] != seqToMatch[i])
                        return false;
                return true;
            }


            while(true)
            {
                if (++loopCount % 10000000 == 0)
                    Console.WriteLine(loopCount);

                var elf1CurrentRecipeScore = scores[elf1CurrentRecipe % scores.Count];
                var elf2CurrentRecipeScore = scores[elf2CurrentRecipe % scores.Count];
                var newScore = elf1CurrentRecipeScore + elf2CurrentRecipeScore;
                if (newScore < 10)
                {
                    scores.Add(newScore);
                    if (newScore == seqToMatchLastDigit && MatchSequence())
                        return scores.Count - seqToMatchLength;
                }
                else
                {
                    scores.Add(1);
                    if (1 == seqToMatchLastDigit && MatchSequence())
                        return scores.Count - seqToMatchLength;

                    scores.Add(newScore - 10);
                    if (newScore - 10 == seqToMatchLastDigit && MatchSequence())
                        return scores.Count - seqToMatchLength;
                }
                elf1CurrentRecipe = (elf1CurrentRecipe + 1 + elf1CurrentRecipeScore) % scores.Count;
                elf2CurrentRecipe = (elf2CurrentRecipe + 1 + elf2CurrentRecipeScore) % scores.Count;

                if ( scores.Count == 24 )
                    Console.WriteLine(string.Join(" ", scores));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CharToDigit(char c) => c - CharToNum;
    }




    [TestFixture]
    internal class Day14Tests
    {
        [TestCase(   5, "0124515891")]
        [TestCase(   9, "5158916779")]
        [TestCase(  18, "9251071085")]
        [TestCase(2018, "5941429882")]
        public void Test1(int after, string expected)
        {
            var res = Day14.GetNext10RecipeScores(after);
            Assert.AreEqual(expected, res);
        }

        [TestCase("01245",    5)]
        [TestCase("51589",    9)]
        [TestCase("92510",   18)]
        [TestCase("59414", 2018)]
        public void Test2(string seq, int expected)
        {
            var res = Day14.FindScoreSequence(seq);
            Assert.AreEqual(expected, res);
        }
    }
}
