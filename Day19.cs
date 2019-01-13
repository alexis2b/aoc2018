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
// ReSharper disable IdentifierTypo

namespace aoc2018
{
    internal class Day19
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day19.txt");

            var res1a = ExecuteProgram(input);
            var res1b = TranspiledCode(0);
            Console.WriteLine($"Day19 - part1 - result: {res1a}  (transpiled: {res1b})");

            var res2 = TranspiledCode(1);
            Console.WriteLine($"Day19 - part2 - result: {res2}");
        }

        public static int ExecuteProgram(string[] code, int reg0InitialValue = 0)
        {
            var ipBinding    = code[0].Split(' ').Skip(1).Select(int.Parse).First();
            var instructions = code.Skip(1).Select(Instruction.FromString).ToList();;
            var computer     = new Computer(ipBinding, reg0InitialValue);
            return computer.Execute(instructions);
        }

        public static int TranspiledCode(int seed)
        {
            // Initialization Block
            var f = seed == 0 ? 967 : 10551367;  // problem complexity, massively increases when a != 0

            var a = 0; // accumulator
            for (var b = 1; b <= f; b++)
            {
                // find E such that B*E == F, if found,  increase A by B
                // E = F / B - needs to have modulo of 0 (perfect divisor)

                if (f % b == 0)
                    a += b;

                //var e = f / b; // integer division!
                //if (b * e == f)
                //    a += b;

                //for (var e = 1; e <= f; e++)
                //{
                //    var c = b * e;
                //    if (c == f)
                //        a += b;
                //}
            }

            return a;
        }


        // The device
        internal sealed class Computer
        {
            private static readonly int[] BadState = {-1, -1, -1, -1, -1, -1};

            public const           int      RegisterCount = 6;
            public static readonly string[] OpNames       = Enum.GetNames(typeof(OpCodes));
            public static readonly int      OpCodesCount  = OpNames.Length;

            public readonly Register[] Registers = new Register[RegisterCount];
            public readonly Register   Ip;

            public Computer(int ipReg, int reg0InitialValue=0)
            {
                for (var i = 0; i < RegisterCount; i++)
                    Registers[i] = new Register(i);
                Ip = Registers[ipReg]; // bind IP to the given register
                Registers[0].Value = reg0InitialValue; // for part2
            }

            public int Execute(Instruction instruction)
            {
                switch (instruction.OpCode)
                {
                    case OpCodes.addr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value + GetRegister(instruction.InputB).Value;
                        break;
                    case OpCodes.addi:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value + instruction.InputB;
                        break;
                    case OpCodes.mulr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value * GetRegister(instruction.InputB).Value;
                        break;
                    case OpCodes.muli:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value * instruction.InputB;
                        break;
                    case OpCodes.banr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value & GetRegister(instruction.InputB).Value;
                        break;
                    case OpCodes.bani:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value & instruction.InputB;
                        break;
                    case OpCodes.borr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value | GetRegister(instruction.InputB).Value;
                        break;
                    case OpCodes.bori:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value | instruction.InputB;
                        break;
                    case OpCodes.setr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value;
                        break;
                    case OpCodes.seti:
                        GetRegister(instruction.OutputC).Value = instruction.InputA;
                        break;
                    case OpCodes.gtir:
                        GetRegister(instruction.OutputC).Value = instruction.InputA > GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;
                    case OpCodes.gtri:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value > instruction.InputB ? 1 : 0;
                        break;
                    case OpCodes.gtrr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value > GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;
                    case OpCodes.eqir:
                        GetRegister(instruction.OutputC).Value = instruction.InputA == GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;
                    case OpCodes.eqri:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value == instruction.InputB ? 1 : 0;
                        break;
                    case OpCodes.eqrr:
                        GetRegister(instruction.OutputC).Value = GetRegister(instruction.InputA).Value == GetRegister(instruction.InputB).Value ? 1 : 0;
                        break;

                    default:
                        throw new InstructionException($"value {instruction.OpCode} is not a valid opcode");
                }

                return Registers[0].Value;
            }

            public int Execute(List<Instruction> program)
            {
                string PrintRegs() => $"{Registers[0].Value,3} {Registers[1].Value,3} {Registers[2].Value,3} {Registers[3].Value,3} {Registers[4].Value,3} {Registers[5].Value,3}";

                //Console.WriteLine( "Executing a program...");
                //Console.WriteLine( "Instruction             ; R:  A   B   C   D   E   F");
                //Console.WriteLine($"00 START                ;   {PrintRegs()}");
                var res = -1;
                while (Ip.Value < program.Count)
                {
                    var instruction = program[Ip.Value];
                    res = Execute(instruction);
                    //Console.WriteLine($"{Ip.Value:D2} {instruction,-21};   {PrintRegs()}");
                    Ip.Value++;
                }

                return res;
            }


            private Register GetRegister(int id)
            {
                if ( id < 0 || id >= RegisterCount) throw new InstructionException($"register {id} does not exist");
                return Registers[id];
            }
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

        internal enum OpCodes {addr, addi, mulr, muli, banr, bani, borr, bori, setr, seti, gtir, gtri, gtrr, eqir, eqri, eqrr}

        // A computer instruction
        internal sealed class Instruction
        {
            public OpCodes OpCode  { get; }
            public int     InputA  { get; }
            public int     InputB  { get; }
            public int     OutputC { get; }

            public Instruction(OpCodes opCode, int inputA, int inputB, int outputC)
            {
                OpCode  = opCode;
                InputA  = inputA;
                InputB  = inputB;
                OutputC = outputC;
            }

            public override string ToString()
            {
                var r = new[] {'A', 'B', 'C', 'D', 'E', 'F'};

                switch (OpCode)
                {
                    case OpCodes.addr: return $"addr {r[InputA],-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.addi: return $"addi {r[InputA],-3}, {InputB,-3} -> {r[OutputC]}";
                    case OpCodes.mulr: return $"mulr {r[InputA],-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.muli: return $"muli {r[InputA],-3}, {InputB,-3} -> {r[OutputC]}";
                    case OpCodes.banr: return $"banr {r[InputA],-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.bani: return $"bani {r[InputA],-3}, {InputB,-3} -> {r[OutputC]}";
                    case OpCodes.borr: return $"borr {r[InputA],-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.bori: return $"bori {r[InputA],-3}, {InputB,-3} -> {r[OutputC]}";
                    case OpCodes.setr: return $"setr {r[InputA],-3}      -> {r[OutputC]}";
                    case OpCodes.seti: return $"seti {InputA,-3}      -> {r[OutputC]}";
                    case OpCodes.gtir: return $"gtir {InputA,-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.gtri: return $"gtri {r[InputA],-3}, {InputB,-3} -> {r[OutputC]}";
                    case OpCodes.gtrr: return $"gtrr {r[InputA],-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.eqir: return $"eqir {InputA,-3}, {r[InputB],-3} -> {r[OutputC]}";
                    case OpCodes.eqri: return $"eqri {r[InputA],-3}, {InputB,-3} -> {r[OutputC]}";
                    case OpCodes.eqrr: return $"eqrr {r[InputA],-3}, {r[InputB],-3} -> {r[OutputC]}";

                    default:
                        throw new InstructionException($"value {OpCode} is not a valid opcode");
                }
            }

            public static Instruction FromString(string text)
            {
                var parts = text.Split(' ');
                return new Instruction(
                    Enum.Parse<OpCodes>(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    int.Parse(parts[3])
                    );
            }
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
    internal class Day19Tests
    {
        private static readonly string[] TestInput =
        {
            "#ip 0",
            "seti 5 0 1",
            "seti 6 0 2",
            "addi 0 1 0",
            "addr 1 2 3",
            "setr 1 0 0",
            "seti 8 0 4",
            "seti 9 0 5"
        };

        [Test]
        public void Test1()
        {
            var res = Day19.ExecuteProgram(TestInput);
            Assert.AreEqual(6, res);
        }
    }
}
