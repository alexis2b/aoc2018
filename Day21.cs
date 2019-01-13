using System;
using System.IO;

// ReSharper disable StringLiteralTypo
// ReSharper disable IdentifierTypo

namespace aoc2018
{
    internal class Day21
    {
        public static void Run()
        {
            var input = File.ReadAllLines("input\\day21.txt");

            // Part 1 - approach: debug through transpiled code to find the right value of A
            // reverse engineering shows that the code returns when A == F so let's find the value of F after the 1st loop
            var dummy1 = TranspiledCode(11513432); // that's your solution (found with debugger)
            Console.WriteLine("step 1 over");

            // Part 2 - look for the highest value of f as we go through the loops, at some point it will block
            var dummy2 = TranspiledCode(7434231); // that's your solution (found with Excel to look for cycles in the serie)
            Console.WriteLine("step 2 over");
        }

        // Literal transpilation, not optimized since it is not the point of the exercise
        public static int TranspiledCode(int seed)
        {
            int a = 0, b = 0, c = 0, e = 0, f = 0;

            a = seed;

            f = 123;

            // sanity check here, will always pass given 123 & 456 = 72
            l01:
            f = f & 456;
            if (f != 72)
                goto l01;

            f = 0;

            l08:
            e = f | 65536;
            f = 8858047;

            l07:
            c = e & 255;
            f = f + c;
            f = f & 16777215;
            f = f * 65899;
            f = f & 16777215;
            if (256 > e)
                goto l03;
            else
                goto l02;
                    
            l02:
            c = 0;

            l06:
            b = c + 1;
            b = b * 256;   // could be a shift by 8
            if (b > e)
                goto l05;
            else
                goto l04;

            l04:
            c = c + 1;
            goto l06;

            l05:
            e = c;
            goto l07;

            l03:
            Console.WriteLine(f);
            if (f == a)
                return a;
            else
                goto l08;
        }
    }
}
