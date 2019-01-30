using NUnit.Framework;

// ReSharper disable StringLiteralTypo


namespace aoc2018
{
    public static class StringExtensions
    {
        // Returns the input (string) where the character at (index) has been replaced by (newChar)
        public static string ReplaceCharAt(this string str, int index, char newChar)
            => str.Substring(0, index) + newChar + str.Substring(index + 1);

        // Cut a string into two parts (before, after) at index i - character at index is is not in before, after
        public static (string, string) SplitAt(this string s, int i)
            => (s.Substring(0, i), s.Substring(i + 1));
    }


    [TestFixture]
    internal class StringExtensionsText
    {
        [Test]
        public void ReplaceCharAt_Should_ReplaceMiddleChar()
        {
            var res = "abcdef".ReplaceCharAt(2, 'C');
            Assert.AreEqual("abCdef", res);
        }

        [Test]
        public void ReplaceCharAt_Should_ReplaceFirstChar()
        {
            var res = "abcdef".ReplaceCharAt(0, 'A');
            Assert.AreEqual("Abcdef", res);
        }

        [Test]
        public void ReplaceCharAt_Should_ReplaceLastChar()
        {
            var res = "abcdef".ReplaceCharAt(5, 'F');
            Assert.AreEqual("abcdeF", res);
        }

        [Test]
        public void SplitAt_Should_CutStringInTwo()
        {
            var (s1, s2) = "ABC|DEF".SplitAt(3);
            Assert.AreEqual(s1, "ABC");
            Assert.AreEqual(s2, "DEF");
        }

        [Test]
        public void SplitAt_Should_ReturnEmptyS1StringIfCutAtBeginning()
        {
            var (s1, s2) = "|ABCDEF".SplitAt(0);
            Assert.AreEqual(s1, string.Empty);
            Assert.AreEqual(s2, "ABCDEF");
        }

        [Test]
        public void SplitAt_Should_ReturnEmptyS2StringIfCutAtEnd()
        {
            var (s1, s2) = "ABCDEF|".SplitAt(6);
            Assert.AreEqual(s1, "ABCDEF");
            Assert.AreEqual(s2, string.Empty);
        }

        [Test]
        public void SplitAt_Should_ReturnEmptyStringsIfCutAtOnlyCharacter()
        {
            var (s1, s2) = "|".SplitAt(0);
            Assert.AreEqual(s1, string.Empty);
            Assert.AreEqual(s2, string.Empty);
        }
    }

}