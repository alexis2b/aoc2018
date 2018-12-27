using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static System.String;

namespace aoc2018
{
    // Part 1 - answer 22784 is too low
    internal class Day24
    {
        public static void Run()
        {
            File.Delete("game.log");
            var game = BuildGameState(); // hardcoded
            Debug.WriteLine(game.ReportGroupState());

            var res1 = game.PlayUntilEnd();
            Console.WriteLine($"Day24 - part1 - result: {res1}");

            var res2 = FindMinimumBoostToWin();
            Console.WriteLine($"Day24 - part2 - result: {res2}");
        }

        public static Game BuildGameState()
        => new Game(
            // Immune System
            // (Radiation, Slashing, Bludgeoning, Fire, Cold)
            new List<Group>
            {
                new Group( 1, 3916,  3260, 16, new[] {  8,   0,   0,   0, 0}, new[] {1, 1, 1, 1, 1}),
                new Group( 2, 4737,  2664, 13, new[] {  0,   5,   0,   0, 0}, new[] {0, 1, 0, 1, 0}),
                new Group( 3,  272, 10137, 10, new[] {  0, 331,   0,   0, 0}, new[] {1, 1, 1, 1, 1}),
                new Group( 4,   92,  2085,  1, new[] {  0,   0, 223,   0, 0}, new[] {1, 1, 1, 0, 1}),
                new Group( 5,  126, 11001,  8, new[] {  0,   0, 717,   0, 0}, new[] {1, 1, 0, 2, 2}),
                new Group( 6,  378,  4669, 17, new[] {  0,   0,   0, 117, 0}, new[] {1, 0, 1, 1, 0}),
                new Group( 7, 4408, 11172,  5, new[] {  0,   0,  21,   0, 0}, new[] {1, 0, 2, 1, 1}),
                new Group( 8,  905, 11617, 20, new[] {  0,   0,   0, 100, 0}, new[] {1, 1, 1, 2, 1}),
                new Group( 9, 3574, 12385, 19, new[] { 27,   0,   0,   0, 0}, new[] {0, 1, 2, 1, 1}),
                new Group(10, 8186,  3139,  9, new[] {  0,   0,   3,   0, 0}, new[] {1, 1, 0, 0, 1})
            },
            // Infection
            // (Radiation, Slashing, Bludgeoning, Fire, Cold)
            new List<Group>
            {
                new Group( 1,  273, 26361, 18, new[] {172,   0,   0,   0, 0}, new[] {0, 2, 1, 1, 1}),
                new Group( 2,  536, 44206, 12, new[] {  0,   0, 130,   0, 0}, new[] {1, 1, 1, 2, 2}),
                new Group( 3, 1005, 12555,  6, new[] { 24,   0,   0,   0, 0}, new[] {0, 1, 0, 0, 1}),
                new Group( 4, 2381, 29521,  4, new[] {  0,  23,   0,   0, 0}, new[] {0, 1, 0, 1, 1}),
                new Group( 5, 5162, 54111,  2, new[] {  0,   0,   0,  19, 0}, new[] {2, 1, 1, 1, 1}),
                new Group( 6,  469, 45035, 15, new[] {163,   0,   0,   0, 0}, new[] {1, 2, 1, 2, 1}),
                new Group( 7,  281, 23265, 11, new[] {135,   0,   0,   0, 0}, new[] {1, 2, 0, 1, 1}),
                new Group( 8, 4350, 46138, 14, new[] {  0,   0,  18,   0, 0}, new[] {1, 1, 1, 2, 1}),
                new Group( 9, 3139, 48062,  3, new[] {  0,   0,  28,   0, 0}, new[] {1, 0, 0, 0, 2}),
                new Group(10, 9326, 41181,  7, new[] {  0,   0,   0,   0, 8}, new[] {1, 1, 2, 2, 1})
            }
        );

        public static Game BuildGameTestState()
        => new Game(
            // Immune System
            // (Radiation, Slashing, Bludgeoning, Fire, Cold)
            new List<Group>
            {
                new Group(1,   17, 5390, 2, new[] {  0,   0,   0, 4507, 0}, new[] {2, 1, 2, 1, 1}),
                new Group(2,  989, 1274, 3, new[] {  0,  25,   0,    0, 0}, new[] {1, 2, 2, 0, 1})
            },
            // Infection
            // (Radiation, Slashing, Bludgeoning, Fire, Cold)
            new List<Group>
            {
                new Group(1,  801, 4706, 1, new[] {  0,   0, 116,   0, 0}, new[] {2, 1, 1, 1, 1}),
                new Group(2, 4485, 2961, 4, new[] {  0,  12,   0,   0, 0}, new[] {0, 1, 1, 2, 2}),
            }
        );

        private static int FindMinimumBoostToWin()
        {
            int PlayGame(int boost)
            {
                var gameWatch = Stopwatch.StartNew();
                File.AppendAllText("game.log", $"NEW GAME WITH BOOST={boost}\n");
                var game = BuildGameState();
                game.AddImmunityBoost(boost);
                var result = game.PlayUntilEnd();
                var gameDuration = gameWatch.Elapsed;
                Console.WriteLine($"{DateTime.Now:T} - Boost: {boost}, Result: {result}, Duration: {gameDuration:g}" );
                return result;
            }
            
            var range   = (0, 10000); // initial set
            var results = (PlayGame(range.Item1), PlayGame(range.Item2));
            while (range.Item2 != range.Item1 + 1)
            {
                var nextGuess = (int) Math.Round((0.0-results.Item1)/(results.Item2-results.Item1)*(range.Item2-range.Item1))+range.Item1;
                nextGuess = Math.Max(nextGuess, range.Item1 + 1);
                Console.WriteLine($"{DateTime.Now:T} - Next guess: {nextGuess}");
                var result = PlayGame(nextGuess);
                if (result <= 0)
                {
                    range.Item1   = nextGuess;
                    results.Item1 = result;
                }
                else
                {
                    range.Item2   = nextGuess;
                    results.Item2 = result;
                }
                Console.WriteLine($"{DateTime.Now:T} - New search range: {range} -> {results}");
            }

            return results.Item2;
        }



        internal sealed class Game
        {
            private int         Round           { get; set; }
            private List<Group> ImmuneGroups    { get; }
            private List<Group> InfectionGroups { get; }

            public Game(List<Group> immuneGroups, List<Group> infectionGroups)
            {
                ImmuneGroups    = immuneGroups;
                InfectionGroups = infectionGroups;
            }

            public void AddImmunityBoost(int boost)
            {
                foreach (var immuneGroup in ImmuneGroups)
                    for(var i = 0; i < 5; i++)
                        if ( immuneGroup.Attack[i] != 0 ) immuneGroup.Attack[i] += boost;
            }

            // Part 1 - simulate the game
            // sign indicates winning side (- means infection, + means immune system) and value indicates number of units left
            public int PlayUntilEnd()
            {
                while (ImmuneGroups.Any(g => g.Units > 0) && InfectionGroups.Any(g => g.Units > 0))
                {
                    Round++;

                    // Active groups for this phase
                    var immuneGroups    = ImmuneGroups   .Where(g => g.Units > 0).ToList();
                    var infectionGroups = InfectionGroups.Where(g => g.Units > 0).ToList();

                    // TARGET SELECTION PHASE
                    var targetsImmuneSystem = TargetSelection(immuneGroups, infectionGroups);
                    var targetsInfection    = TargetSelection(infectionGroups, immuneGroups);
                    if (targetsImmuneSystem.Count == 0 && targetsInfection.Count == 0)
                        return 0; // draw!

                    // ATTACKING PHASE
                    var totalUnitsCountBefore = ImmuneGroups.Sum(g => g.Units) + InfectionGroups.Sum(g => g.Units);
                    var attackingSequence = targetsImmuneSystem.Union(targetsInfection).OrderByDescending(t => t.Item1.Initiative);
                    foreach (var attack in attackingSequence)
                    {
                        var attacker = attack.Item1;
                        var defender = attack.Item2;
                        if (attacker.Units > 0)
                        {
                            var damages = ComputeAttackDamage(attacker, defender);
                            defender.TakeDamage(damages);
                        }
                    }
                    var totalUnitsCountAfter = ImmuneGroups.Sum(g => g.Units) + InfectionGroups.Sum(g => g.Units);
                    if (totalUnitsCountAfter == totalUnitsCountBefore)
                        return 0; // draw

                    File.AppendAllText("game.log", $"\n\nAfter Round {Round}\n"+ReportGroupState());
                    //Debug.WriteLine($"After Round {Round}");
                    //Debug.WriteLine(ReportGroupState());
                }

                // number of units left on winning side, with sign
                return ImmuneGroups.Sum(g => g.Units) - InfectionGroups.Sum(g => g.Units);
            }

            private static List<Tuple<Group, Group, int>> TargetSelection(IEnumerable<Group> attackGroups, IEnumerable<Group> targetGroups)
            {
                var attackersSorted  = attackGroups.OrderByDescending(g => g.EffectivePower).ThenByDescending(g => g.Initiative);
                var remainingTargets = targetGroups.ToList();
                var results = new List<Tuple<Group, Group, int>>(); // (Attacking Group, Defending Group)
                foreach (var attacker in attackersSorted)
                {
                    if (remainingTargets.Count == 0)
                        break;

                    var selectedTargetWithDamage = remainingTargets
                        .Select(t => new { Target = t, Damage = ComputeAttackDamage(attacker, t)})
                        .OrderByDescending(t => t.Damage)
                        .ThenByDescending(t => t.Target.EffectivePower)
                        .ThenByDescending(t => t.Target.Initiative)
                        .First();

                    if (selectedTargetWithDamage.Damage > 0)
                    {
                        results.Add(Tuple.Create(attacker, selectedTargetWithDamage.Target, selectedTargetWithDamage.Damage));
                        remainingTargets.Remove(selectedTargetWithDamage.Target);
                    }
                }

                return results;
            }

            private static int ComputeAttackDamage(Group attacker, Group target)
                => attacker.Units * attacker.Attack.Zip(target.Defense, (a,d) => a*d).Sum();

            public string ReportGroupState()
                => new StringBuilder()
                    .AppendLine("Immune System:")
                    .AppendLine(Join('\n', ImmuneGroups.Select(g => g.ToString())))
                    .AppendLine()
                    .AppendLine("Infection:")
                    .AppendLine(Join('\n', InfectionGroups.Select(g => g.ToString())))
                    .ToString();

        }

        internal sealed class Group
        {
            // Order of attack indexes (in attack / immunity arrays)
            // (Radiation, Slashing, Bludgeoning, Fire, Cold)
            private static readonly string[] AttackTypes = {"radiation", "slashing", "bludgeoning", "fire", "cold"};

            public int Id { get; }
            public int Units { get; private set; }
            public int HitPoints { get; }
            public int Initiative { get; }
            public int[] Attack { get; }  // only one index is normally used
            public int[] Defense { get; }  // 0 -> immune, 1 -> normal, 2 -> weak  (attack points multiplier)

            public int AttackDamage => Attack.Max();
            public int EffectivePower => Units * AttackDamage;

            public Group(int id, int units, int hitPoints, int initiative, int[] attack, int[] defense)
            {
                Id = id;
                Units = units;
                HitPoints = hitPoints;
                Initiative = initiative;
                Attack = attack;
                Defense = defense;
            }

            // Reduce units according to the given damages number
            public void TakeDamage(int damages) => Units -= Math.Min(damages / HitPoints, Units); // integer division

            public override string ToString()
            {
                string ToText(int[] array, Predicate<int> cond)
                    => Join(", ",
                        array.Select((v, i) => (v, i)).Where(t => cond(t.Item1)).Select(t => AttackTypes[t.Item2]));

                var builder = new StringBuilder($"{Units} units each with {HitPoints} hit points");

                if (Defense.Any(d => d != 1))
                {
                    builder.Append(" (");
                    var defenseParts = new List<string>();
                    var immuneTo = ToText(Defense, d => d == 0);
                    if ( immuneTo != Empty ) defenseParts.Add("immune to " + immuneTo);
                    var weakTo = ToText(Defense, d => d == 2);
                    if (weakTo != Empty) defenseParts.Add("weak to " + weakTo);
                    builder.Append(Join("; ", defenseParts));
                    builder.Append(")");
                }

                builder.Append($" with an attack that does {AttackDamage} {ToText(Attack, d => d != 0)} damage");
                builder.Append($" at initiative {Initiative}");
                return builder.ToString();
            }
        }
    }


    [TestFixture]
    internal class Day24Tests
    {
        [Test]
        public void Test1()
        {
            var res = Day24.BuildGameTestState().PlayUntilEnd();
            Assert.AreEqual(-5216, res);
        }

        [Test]
        public void Test2()
        {
            var game = Day24.BuildGameTestState();
            game.AddImmunityBoost(1570);
            var res = game.PlayUntilEnd();
            Assert.AreEqual(51, res);
        }
    }
}
