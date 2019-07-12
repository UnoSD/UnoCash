using System.Linq;
using NUnit.Framework;
using UnoCash.Core;

namespace UnoCash.Test
{
    public class AsyncLinqTest
    {
        [Test]
        public void Unfold()
        {
            int[] expected =
            {
                0, 2, 4, 6, 8
            };

            var actual =
                0.Unfold(i => (i, i + 2), 
                         i => i > 8)
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void UnfoldNullable()
        {
            int[] expected =
            {
                0, 2, 4, 6, 8
            };

            var actual =
                0.Unfold(i => i > 8 ?
                         new (int, int)?() :
                         (i, i + 2))
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}