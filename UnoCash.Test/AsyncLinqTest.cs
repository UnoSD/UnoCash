using System.Linq;
using System.Threading.Tasks;
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
        public void UnfoldSingle()
        {
            int[] expected = { };

            var actual =
                0.Unfold(i => (i, i + 2),
                         i => true)
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

        [Test]
        public void UnfoldNullableSingle()
        {
            int[] expected = { };

            var actual =
                0.Unfold(i => new (int, int)?())
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void UnfoldAsync()
        {
            int[] expected =
            {
                0, 2, 4, 6, 8
            };

            var actual =
                0.UnfoldAsync(i => Task.FromResult((i, i + 2)),
                              i => Task.FromResult(i > 8))
                 .Result
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void UnfoldAsyncSingle()
        {
            int[] expected = { };

            var actual =
                0.UnfoldAsync(i => Task.FromResult((i, i + 2)),
                              i => Task.FromResult(true))
                 .Result
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void UnfoldAsync2()
        {
            int[] expected =
            {
                0, 2, 4, 6, 8
            };

            var actual =
                0.UnfoldAsync2(i => Task.FromResult((i, i + 2)),
                               i => Task.FromResult(i > 8))
                 .Result
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void UnfoldAsync2Single()
        {
            int[] expected = { };

            var actual =
                0.UnfoldAsync2(i => Task.FromResult((i, i + 2)),
                               i => Task.FromResult(true))
                 .Result
                 .ToList();

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}