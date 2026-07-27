//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Verifies variable-size multi-lane masonry indexing.
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;

namespace TristinWen.VirtualScroll.Tests
{
    /// <summary>
    /// Verifies masonry lane assignment, visibility, and measured-size updates.
    /// </summary>
    public sealed class MasonrySizeIndexTests
    {
        /// <summary>
        /// Verifies items are assigned to the shortest lane with uniform spacing.
        /// </summary>
        [Test]
        public void MasonryAssignsShortestLane()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 100f, 50f, 60f, 40f, 30f });
            var index = new MasonrySizeIndex(dataSource, 10f, 2);

            Assert.AreEqual(0, index.GetCrossAxisIndex(0));
            Assert.AreEqual(1, index.GetCrossAxisIndex(1));
            Assert.AreEqual(1, index.GetCrossAxisIndex(2));
            Assert.AreEqual(0, index.GetCrossAxisIndex(3));
            Assert.AreEqual(60f, index.GetOffset(2));
            Assert.AreEqual(110f, index.GetOffset(3));
            Assert.AreEqual(160f, index.TotalSize);
        }

        /// <summary>
        /// Verifies lane-local visibility lookup does not scan or return distant items.
        /// </summary>
        [Test]
        public void MasonryCollectsVisibleItemsPerLane()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 100f, 50f, 60f, 40f, 30f });
            var index = new MasonrySizeIndex(dataSource, 10f, 2);
            var visible = new List<int>();

            index.CollectVisibleIndices(105f, 145f, 0, visible);

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, visible);
        }

        /// <summary>
        /// Verifies a measured height update shifts only later items in the same lane.
        /// </summary>
        [Test]
        public void MasonryUpdatesStableLaneOffsets()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 100f, 50f, 60f, 40f, 30f });
            var index = new MasonrySizeIndex(dataSource, 10f, 2);

            index.UpdateSize(1, 80f);

            Assert.AreEqual(90f, index.GetOffset(2));
            Assert.AreEqual(160f, index.GetOffset(4));
            Assert.AreEqual(190f, index.TotalSize);
        }
    }
}
