using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day16
    {
        private static readonly Regex ExperimentEx = new Regex(
            @"Before: \[(?<before>[\d, ]+)\]..(?<instr>[\d ]+)..After:  \[(?<after>[-\d, ]+)\]",
            //@" ^ Before: \[(?<before>[-\d, ]+)\]$^(?<instr>[-\d ]+)$^After:  \[(?<after>[-\d, ]+)\]$",
            RegexOptions.Singleline);


        public static void Run()
        {
            var input1 = File.ReadAllText("input\\day16_1.txt");
            var input2 = File.ReadAllLines("input\\day16_2.txt");

            var res1 = SolveOpCodes(input1);
            Console.WriteLine($"Day16 - part1 - result: {res1}");

            var res2 = ExecuteProgram(input2);
            Console.WriteLine($"Day16 - part2 - result: {res2}");
        }

        public static int SolveOpCodes(string input1)
        {
            var experiments = ParseExperiments(input1).ToList();
            var res = experiments.Select(FindPossibleOpCodes).ToList();

            // Solve op-codes matrix (intersection of possibilities) and make sure it's aligned
            var opCodesMatching = res
                .GroupBy(r => r.code)
                .Select(g => new
                {
                    OpCode     = g.Key,
                    Operations = g.Select(gr => gr.possibleOps).Aggregate(
                        Enumerable.Range(0, Computer.OpCodesCount),
                        (intersect, ops) => intersect.Intersect(ops))
                });
            Console.WriteLine("\n== Op Codes Matching ==");
            foreach(var opCode in opCodesMatching)
                Console.WriteLine($" {opCode.OpCode, -2} -> {string.Join(", ", opCode.Operations.Select(c => Computer.OpNames[c]))}");
            Console.WriteLine();

            return res.Count(r => r.possibleOps.Length >= 3);
        }

        private static IEnumerable<Experiment> ParseExperiments(string text)
        {
            var matches = ExperimentEx.Matches(text);
            foreach (Match match in matches)
            {
                var before = match.Groups["before"].Value.Split(", ").Select(int.Parse).ToArray();
                var instr  = match.Groups["instr"].Value.Split(' ').Select(int.Parse).ToArray();
                var after  = match.Groups["after"].Value.Split(", ").Select(int.Parse).ToArray();
                yield return new Experiment(
                    before,
                    new Instruction(instr[0], instr[1], instr[2], instr[3]),
                    after);
            }
        }

        private static (int code, int[] possibleOps) FindPossibleOpCodes(Experiment experiment)
        {
            var i = experiment.Instruction;
            // count all results which are same than after for every possible instruction
            var allOpsCombinations = Enumerable.Range(0, Computer.OpCodesCount)
                .Select(c => new Instruction(c, i.InputA, i.InputB, i.OutputC))
                .ToList();
            var computer = new Computer();
            var possible = allOpsCombinations.Select(instr =>
            {
                computer.SetRegisterValues(experiment.Before);
                computer.ExecuteSilentFail(instr);
                return (instr.OpCode, computer.GetRegisterValues());
            })
            .Where(r => r.Item2.SequenceEqual(experiment.After))
            .Select(r => r.Item1)
            .ToArray();

            // Debug
            var opCodeNames = Enum.GetNames(typeof(OpCodes));
            var codes = string.Join(", ", possible.Select(p => opCodeNames[p]));
            Debug.WriteLine($"\nExperiment: {experiment}");
            Debug.WriteLine($"Possible ops: {codes}");

            return (i.OpCode, possible);
        }


        public static int ExecuteProgram(string[] code)
        {
            var instructions = code
                .Select(line => line.Split(' ').Select(int.Parse).ToArray()) // parse line to array of ints
                .Select(ints => new Instruction(ints[0], ints[1], ints[2], ints[3]));
            var computer = new Computer();
            return computer.Execute(instructions);
        }



        // The device
        internal sealed class Computer
        {
            private static readonly int[] BadState = {-1, -1, -1, -1};

            public const           int      RegisterCount = 4;
            public static readonly string[] OpNames       = Enum.GetNames(typeof(OpCodes));
            public static readonly int      OpCodesCount  = OpNames.Length;

            public readonly Register[] Registers = new Register[RegisterCount];
            public readonly Register   Ip = new Register(RegisterCount+1);

            public Computer()
            {
                for (var i = 0; i < RegisterCount; i++)
                    Registers[i] = new Register(i);
            }

            // Same as Execute but will catch an exception and set registers in "BadState"
            // (Needed for part1)
            public void ExecuteSilentFail(Instruction instr)
            {
                try
                {
                    Execute(instr);
                }
                catch (InstructionException)
                {
                    SetRegisterValues(BadState);
                }
            }

            public int Execute(Instruction instruction)
            {
                switch (instruction.OpCode)
                {
                    case (int)OpCodes.addr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value + GetRegister(instruction.InputB).Value;
                        break;
                    case (int)OpCodes.addi:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value + instruction.InputB;
                        break;
                    case (int)OpCodes.mulr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value * GetRegister(instruction.InputB).Value;
                        break;
                    case (int)OpCodes.muli:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value * instruction.InputB;
                        break;
                    case (int)OpCodes.banr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value & GetRegister(instruction.InputB).Value;
                        break;
                    case (int)OpCodes.bani:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value & instruction.InputB;
                        break;
                    case (int)OpCodes.borr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value | GetRegister(instruction.InputB).Value;
                        break;
                    case (int)OpCodes.bori:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value | instruction.InputB;
                        break;
                    case (int)OpCodes.setr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value;
                        break;
                    case (int)OpCodes.seti:
                        GetRegister(instruction.OutputC).Value = instruction.InputA;
                        break;
                    case (int)OpCodes.gtir:
                        GetRegister(instruction.OutputC).Value = instruction.InputA > GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;
                    case (int)OpCodes.gtri:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value > instruction.InputB ? 1 : 0;
                        break;
                    case (int)OpCodes.gtrr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value > GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;
                    case (int)OpCodes.eqir:
                        GetRegister(instruction.OutputC).Value = instruction.InputA == GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;
                    case (int)OpCodes.eqri:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value == instruction.InputB ? 1 : 0;
                        break;
                    case (int)OpCodes.eqrr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value == GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;

                    default:
                        throw new InstructionException($"value {instruction.OpCode} is not a valid opcode");
                }

                return Registers[0].Value;
            }

            public int Execute(IEnumerable<Instruction> instructions)
            {
                string PrintRegs() => $"{Registers[0].Value,3} {Registers[1].Value,3} {Registers[2].Value,3} {Registers[3].Value,3}";

                Console.WriteLine( "Executing a program...");
                Console.WriteLine( "Instruction             ; R:  A   B   C   D");
                Console.WriteLine($"START                   ;   {PrintRegs()}");
                var res = -1;
                foreach (var instruction in instructions)
                {
                    res = Execute(instruction);
                    Console.WriteLine($"{instruction,-24};   {PrintRegs()}");
                }

                return res;
            }


            private Register GetRegister(int id)
            {
                if ( id < 0 || id >= RegisterCount) throw new InstructionException($"register {id} does not exist");
                return Registers[id];
            }

            public void SetRegisterValues(int[] values)
            {
                for (var i = 0; i < RegisterCount; i++)
                    Registers[i].Value = values[i];
            }

            public int[] GetRegisterValues()
                => Registers.Select(r => r.Value).ToArray();
        }


        // A CPU register
        internal sealed class Register
        {
            public int Id    { get; }
            public int Value { get; set; }

            public Register(int id)
            {
                Id = id;
            }
        }

        // Commented story order in favor of resolved order
        //internal enum OpCodes {addr, addi, mulr, muli, banr, bani, borr, bori, setr, seti, gtir, gtri, gtrr, eqir, eqri, eqrr}
        internal enum OpCodes { seti, eqir, setr, gtir, addi, muli, mulr, gtrr, bani, gtri, bori, banr, borr, eqri, eqrr, addr }

        // A computer instruction
        internal sealed class Instruction
        {
            public int OpCode  { get; }
            public int InputA  { get; }
            public int InputB  { get; }
            public int OutputC { get; }

            public Instruction(int opCode, int inputA, int inputB, int outputC)
            {
                OpCode  = opCode;
                InputA  = inputA;
                InputB  = inputB;
                OutputC = outputC;
            }

            public override string ToString()
            {
                var R = new[] {'A', 'B', 'C', 'D'};

                switch (OpCode)
                {
                    case (int)OpCodes.addr: return $"addr {R[InputA],-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.addi: return $"addi {R[InputA],-3}, {InputB,-3} -> {R[OutputC]}";
                    case (int)OpCodes.mulr: return $"mulr {R[InputA],-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.muli: return $"muli {R[InputA],-3}, {InputB,-3} -> {R[OutputC]}";
                    case (int)OpCodes.banr: return $"banr {R[InputA],-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.bani: return $"bani {R[InputA],-3}, {InputB,-3} -> {R[OutputC]}";
                    case (int)OpCodes.borr: return $"borr {R[InputA],-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.bori: return $"bori {R[InputA],-3}, {InputB,-3} -> {R[OutputC]}";
                    case (int)OpCodes.setr: return $"setr {R[InputA],-3}      -> {R[OutputC]}";
                    case (int)OpCodes.seti: return $"seti {InputA,-3}      -> {R[OutputC]}";
                    case (int)OpCodes.gtir: return $"gtir {InputA,-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.gtri: return $"gtri {R[InputA],-3}, {InputB,-3} -> {R[OutputC]}";
                    case (int)OpCodes.gtrr: return $"gtrr {R[InputA],-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.eqir: return $"eqir {InputA,-3}, {R[InputB],-3} -> {R[OutputC]}";
                    case (int)OpCodes.eqri: return $"eqri {R[InputA],-3}, {InputB,-3} -> {R[OutputC]}";
                    case (int)OpCodes.eqrr: return $"eqrr {R[InputA],-3}, {R[InputB],-3} -> {R[OutputC]}";

                    default:
                        throw new InstructionException($"value {OpCode} is not a valid opcode");
                }
            }
        }

        // Represents an "experiment" from input1
        internal sealed class Experiment
        {
            public int[] Before { get; }
            public Instruction Instruction { get; }
            public int[] After { get; }

            public Experiment(int[] before, Instruction instruction, int[] after)
            {
                Before = before;
                Instruction = instruction;
                After = after;
            }

            public override string ToString()
                => $"[{string.Join(", ", Before)}] ---( {Instruction.OpCode} {Instruction.InputA} {Instruction.InputB} {Instruction.OutputC} )--> [{string.Join(", ", After)}]";
        }



        [Serializable]
        public class InstructionException : Exception
        {
            public InstructionException()
            {
            }

            public InstructionException(string message) : base(message)
            {
            }

            public InstructionException(string message, Exception inner) : base(message, inner)
            {
            }

            protected InstructionException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
        }
    }


    [TestFixture]
    internal class Day16Tests
    {
        private const string TestInput = @"Before: [3, 2, 1, 1]
9 2 1 2
After:  [3, 2, 2, 1]";


        [Test]
        public void Test1()
        {
            var res = Day16.SolveOpCodes(TestInput);
            Assert.AreEqual(1, res);
        }
    }
}
