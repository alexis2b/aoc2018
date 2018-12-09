using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace aoc2018
{
    internal class Day08
    {
        public static void Run()
        {
            var input = File.ReadAllText("input\\day08.txt");

            var res1 = CheckLicenseFile1(input);
            Console.WriteLine($"Day08 - part1 - result: {res1}");

            var res2 = CheckLicenseFile2(input);
            Console.WriteLine($"Day08 - part2 - result: {res2}");
        }

        public static List<int> Decode(string input) => input.Split(' ').Select(int.Parse).ToList();

        private static Node BuildTree(string input)
        {
            var code = new Queue<int>(Decode(input)); // store in a queue to be able to dequeue as we go
            return BuildTree(code, null);
        }

        private static Node BuildTree(Queue<int> code, Node parent)
        {
            var node = new Node(parent, code.Dequeue(), code.Dequeue());
            for (int i = 0; i < node.ChildCount; i++)
                node.Children.Add(BuildTree(code, node));
            for (int i = 0; i < node.DataCount; i++)
                node.Data.Add(code.Dequeue());
            return node;
        }

        public static int CheckLicenseFile1(string input)
        {
            var tree = BuildTree(input);
            return tree.GetDataSum();
        }

        public static int CheckLicenseFile2(string input)
        {
            var tree = BuildTree(input);
            return tree.GetDataSum2();
        }

        private sealed class Node
        {
            public Node Parent    { get; }
            public int ChildCount { get; }
            public int DataCount  { get; }
            public List<Node> Children { get; } = new List<Node>();
            public List<int>  Data     { get; } = new List<int>();

            public Node(Node parent, int childCount, int dataCount)
            {
                Parent     = parent;
                ChildCount = childCount;
                DataCount  = dataCount;
            }

            public int GetDataSum()
            {
                var localSum = Data.Sum();
                return localSum + Children.Select(c => c.GetDataSum()).Sum();
            }

            public int GetDataSum2()
            {
                if (ChildCount == 0)
                    return Data.Sum();

                var sum = 0;
                foreach(var index in Data)
                {
                    if (index > 0 && index <= ChildCount)
                        sum += Children[index - 1].GetDataSum2();
                }
                return sum;
            }
        }
    }




    [TestFixture]
    internal class Day08Tests
    {
        private const string Input = "2 3 0 3 10 11 12 1 1 0 1 99 2 1 1 2";

        [Test]
        public void Test1_1()
        {
            var res = Day08.CheckLicenseFile1(Input);
            Assert.AreEqual(138, res);
        }

        [Test]
        public void Test2_1()
        {
            var res = Day08.CheckLicenseFile2(Input);
            Assert.AreEqual(66, res);
        }
    }
}
