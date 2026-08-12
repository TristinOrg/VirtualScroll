//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Verifies fixed-size offset lookup behavior.
//---------------------------------------------------------------------------------------

using NUnit.Framework;

namespace TristinOrg.VirtualScroll.Tests
{
    /// <summary>
    /// Verifies fixed-size virtual indexing.
    /// </summary>
    public sealed class FixedSizeIndexTests
    {
        /// <summary>
        /// Verifies offsets, spacing, total size, and reverse lookup.
        /// </summary>
        [Test]
        public void FixedIndexMapsOffsetsInConstantSteps()
        {
            var index = new FixedSizeIndex(4, 100f, 10f);

            Assert.AreEqual(0f, index.GetOffset(0));
            Assert.AreEqual(110f, index.GetOffset(1));
            Assert.AreEqual(430f, index.TotalSize);
            Assert.AreEqual(0, index.FindIndex(0f));
            Assert.AreEqual(1, index.FindIndex(110f));
            Assert.AreEqual(3, index.FindIndex(10000f));
        }

        /// <summary>
        /// Verifies that an empty index has no visible result.
        /// </summary>
        [Test]
        public void EmptyFixedIndexReturnsNoItem()
        {
            var index = new FixedSizeIndex(0, 100f, 10f);

            Assert.AreEqual(0f, index.TotalSize);
            Assert.AreEqual(-1, index.FindIndex(0f));
        }

        /// <summary>
        /// Verifies fixed-size rows share offsets across equal-width lanes.
        /// </summary>
        [Test]
        public void FixedGridMapsRowsAndLanes()
        {
            var index   = new FixedSizeIndex(8, 100f, 10f, 3);
            var visible = new System.Collections.Generic.List<int>();

            Assert.AreEqual(0f, index.GetOffset(2));
            Assert.AreEqual(110f, index.GetOffset(3));
            Assert.AreEqual(320f, index.TotalSize);
            Assert.AreEqual(2, index.GetCrossAxisIndex(5));
            index.CollectVisibleIndices(105f, 215f, 0, visible);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, visible);
        }
    }
}
