using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aoc2018
{
    internal class Day09
    {
        public static void Run()
        {
            // 476 players; last marble is worth 71431 points

            var res1 = GetHighScore(476, 71431);
            Console.WriteLine($"Day09 - part1 - result: {res1}");

            var res2 = GetHighScore2(476, 7143100);
            Console.WriteLine($"Day09 - part2 - result: {res2}");
        }

        public static int GetHighScore(int playersCount, int lastMarbleWorth)
        {
            var game = new Game();
            var scores = new int[playersCount];
            for (var m = 1; m <= lastMarbleWorth; m++)
            {
                scores[m % playersCount] += game.AddMarble(m);
            }


            return scores.Max();
        }

        internal sealed class Game
        {
            private readonly List<int> _ring;
            private int _current;

            public Game()
            {
                _ring = new List<int> {0};
                _current = 0;
            }

            public int AddMarble(int n)
            {
                if (n % 23 == 0)
                {
                    var removePos = (_current - 7 + _ring.Count) % _ring.Count;
                    var removedN  = _ring[removePos];
                    _ring.RemoveAt(removePos);
                    _current = removePos;
                    //Console.WriteLine(string.Join(", ", _ring));
                    return n + removedN;
                }

                var nextPosition = (_current + 1) % _ring.Count + 1;
                _ring.Insert(nextPosition, n);
                _current = nextPosition;
                //Console.WriteLine(string.Join(", ", _ring));

                return 0;
            }
        }

        public static long GetHighScore2(int playersCount, int lastMarbleWorth)
        {
            var game = new Game2();
            var scores = new long[playersCount];
            for (var m = 1; m <= lastMarbleWorth; m++)
            {
                scores[m % playersCount] += game.AddMarble(m);
                //Console.WriteLine($"{m,3}: {game.Describe()}");
            }
            return scores.Max();
        }

        internal sealed class Game2
        {
            private readonly DoubleLinkedRing<int> _ring;

            public Game2()
            {
                _ring = new DoubleLinkedRing<int>(0);
            }

            public int AddMarble(int n)
            {
                if (n % 23 == 0)
                {
                    var removedN = _ring.Move(-7);
                    _ring.RemoveCurrent();
                    return n + removedN;
                }
                _ring.Move(1);
                _ring.InsertAfter(n);
                _ring.Move(1);
                return 0;
            }

            public string Describe() => string.Join(", ", _ring.GetEnumerable());
        }

        // A double-linked ring structure (i.e: no start or end)
        // optimized for part2 which requires O(1) complexity!
        internal sealed class DoubleLinkedRing<T>
        {
            private Node _current;

            // A Node
            internal class Node
            {
                public Guid Id { get; }
                public T Value { get; }
                public Node PreviousNode { get; set; }
                public Node NextNode { get; set; }

                public Node(T value)
                {
                    Id = Guid.NewGuid();
                    Value = value;
                }

                public override string ToString() => $"{Value}:{Id}";
            }

            // Initializes a new double-linked ring with a single Node pointing to itself in both directions
            public DoubleLinkedRing(T firstNodeValue)
            {
                _current = new Node(firstNodeValue);
                _current.NextNode = _current;
                _current.PreviousNode = _current;
            }

            public T Move(int offset)
            {
                if (offset ==  1)      _current = _current.NextNode;
                else if (offset == -1) _current = _current.PreviousNode;
                else if (offset < 0)   for (var i = 0; i > offset; i--) _current = _current.PreviousNode;
                else if (offset > 0)   for (var i = 0; i < offset; i++) _current = _current.NextNode;
                return _current.Value;
            }

            // Remove Current, the Next becomes the new Current
            public void RemoveCurrent()
            {
                var nodeBefore = _current.PreviousNode;
                var nodeAfter  = _current.NextNode;

                nodeBefore.NextNode    = nodeAfter;
                nodeAfter.PreviousNode = nodeBefore;

                _current = nodeAfter;
            }

            public void InsertAfter(T value)
            {
                var nodeBefore         = _current;
                var nodeAfter          = _current.NextNode;
                var newNode            = new Node(value) { PreviousNode = nodeBefore, NextNode = nodeAfter};
                nodeBefore.NextNode    = newNode;
                nodeAfter.PreviousNode = newNode;
            }

            public IEnumerable<T> GetEnumerable()
            {
                yield return _current.Value;
                for (var pointer = _current.NextNode; pointer != _current; pointer = pointer.NextNode)
                    yield return pointer.Value;
            }
        }
    }




    [TestFixture]
    internal class Day09Tests
    {
        [TestCase(9, 25, 32)]
        [TestCase(10, 1618, 8317)]
        [TestCase(476, 71431, 384205)]
        public void Test1(int players, int lastMarble, int expected)
        {
            var res = Day09.GetHighScore(players, lastMarble);
            Assert.AreEqual(expected, res);
        }

        [TestCase(9, 25, 32)]
        [TestCase(10, 1618, 8317)]
        [TestCase(476, 71431, 384205)]
        public void Test2(int players, int lastMarble, int expected)
        {
            var res = Day09.GetHighScore2(players, lastMarble);
            Assert.AreEqual(expected, res);
        }
    }
}
