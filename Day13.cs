using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ReSharper disable StringLiteralTypo

namespace aoc2018
{
    internal class Day13
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day13.txt");

            var res1 = GetFirstCrashLocation(input);
            Console.WriteLine($"Day13 - part1 - result: {res1}");

            var res2 = GetLastCartLocation(input);
            Console.WriteLine($"Day13 - part2 - result: {res2}");
        }

        public static (int x, int y) GetFirstCrashLocation(string[] input)
            => Simulation.BuildFromMap(input).RunToFirstCrash();

        public static (int x, int y) GetLastCartLocation(string[] input)
            => Simulation.BuildFromMap(input).RunToLastCart();


        internal sealed class Cart
        {
            private int _intersectionState;
            public (int x,  int y)  Position { get; set; }
            public (int vx, int vy) Velocity { get; set; }
            

            public void TurnAtIntersection()
            {
                switch (_intersectionState)
                {
                    case 0: Velocity = (Velocity.vy, -Velocity.vx); break;
                    case 1: break;
                    case 2: Velocity = (-Velocity.vy, Velocity.vx); break;
                }
                _intersectionState = (_intersectionState + 1) % 3;
            }
        }

        internal sealed class Simulation
        {
            private readonly string[]   _map;
            private readonly List<Cart> _carts;

            private Simulation(string[] map, IEnumerable<Cart> carts)
            {
                _map   = map;
                _carts = carts.ToList();
            }

            // Build the map (without carts) and carts from the input
            public static Simulation BuildFromMap(string[] input)
            {
                var carts = new List<Cart>();
                var map   = new string[input.Length];
                for (var y = 0; y < input.Length; y++)
                {
                    var row = input[y];
                    for (var x = 0; x < row.Length; x++)
                    {
                        var c = row[x];
                        switch (c)
                        {
                            case '^': carts.Add(new Cart { Position = (x,y), Velocity = ( 0,-1)}); row = row.ReplaceCharAt(x, '|'); break;
                            case '>': carts.Add(new Cart { Position = (x,y), Velocity = ( 1, 0)}); row = row.ReplaceCharAt(x, '-'); break;
                            case 'v': carts.Add(new Cart { Position = (x,y), Velocity = ( 0, 1)}); row = row.ReplaceCharAt(x, '|'); break;
                            case '<': carts.Add(new Cart { Position = (x,y), Velocity = (-1, 0)}); row = row.ReplaceCharAt(x, '-'); break;
                        }
                    }
                    map[y] = row;
                }
                return new Simulation(map, carts);
            }


            public (int x, int y) RunToFirstCrash()
            {
                while(true)
                {
                    var cartsSequence = _carts.OrderBy(c => c.Position.y).ThenBy(c => c.Position.x);
                    foreach (var cart in cartsSequence)
                    {
                        var newPosition = (x: cart.Position.x + cart.Velocity.vx, y: cart.Position.y + cart.Velocity.vy);
                        if (_carts.Any(c => c.Position.x == newPosition.x && c.Position.y == newPosition.y))
                            return newPosition; // Crash!
                        cart.Position = newPosition;

                        switch (_map[newPosition.y][newPosition.x])
                        {
                            case  '/': cart.Velocity = (-cart.Velocity.vy, -cart.Velocity.vx); break;
                            case '\\': cart.Velocity = ( cart.Velocity.vy,  cart.Velocity.vx); break;
                            case  '+': cart.TurnAtIntersection(); break;
                        }
                    }
                }
            }

            public (int x, int y) RunToLastCart()
            {
                while (true)
                {
                    var cartsSequence = _carts.OrderBy(c => c.Position.y).ThenBy(c => c.Position.x);
                    var crashedCarts  = new List<Cart>();
                    foreach (var cart in cartsSequence)
                    {
                        if (crashedCarts.Contains(cart)) continue;

                        var newPosition  = (x: cart.Position.x + cart.Velocity.vx, y: cart.Position.y + cart.Velocity.vy);
                        var collidedCart = _carts.Except(crashedCarts).FirstOrDefault(c => c.Position.x == newPosition.x && c.Position.y == newPosition.y);
                        if (collidedCart != null)
                        {
                            // Crash, we remove the carts from the sequence
                            crashedCarts.Add(cart);
                            crashedCarts.Add(collidedCart);
                        }
                        cart.Position = newPosition;

                        switch (_map[newPosition.y][newPosition.x])
                        {
                            case '/':  cart.Velocity = (-cart.Velocity.vy, -cart.Velocity.vx); break;
                            case '\\': cart.Velocity = (cart.Velocity.vy, cart.Velocity.vx); break;
                            case '+':  cart.TurnAtIntersection(); break;
                        }
                    }
                    crashedCarts.ForEach(c => _carts.Remove(c));

                    if (_carts.Count == 1)
                        return _carts[0].Position;
                }
            }
        }
    }


    [TestFixture]
    internal class Day13Tests
    {
        [Test]
        public void Test1_1()
        {
            string[] input =
            {
                "|",
                "v",
                "|",
                "|",
                "|",
                "^",
                "|",
            };
            var res = Day13.GetFirstCrashLocation(input);
            Assert.AreEqual((0, 3), res);
        }

        [Test]
        public void Test1_2()
        {
            string[] input =
            {
                @"/->-\        ",
                @"|   |  /----\",
                @"| /-+--+-\  |",
                @"| | |  | v  |",
                @"\-+-/  \-+--/",
                @"\------/     "
            };
            var res = Day13.GetFirstCrashLocation(input);
            Assert.AreEqual((7, 3), res);
        }

        [Test]
        public void Test2_1()
        {
            string[] input =
            {
                @"/>-<\  ",
                @"|   |  ",
                @"| /<+-\",
                @"| | | v",
                @"\>+</ |",
                @"  |   ^",
                @"  \<->/"
            };
            var res = Day13.GetLastCartLocation(input);
            Assert.AreEqual((6, 4), res);
        }
    }
}
