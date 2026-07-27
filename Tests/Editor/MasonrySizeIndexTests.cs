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
        /// Verifies items use stable estimated lanes and resolve their real sizes lazily.
        /// </summary>
        [Test]
        public void MasonryAssignsStableEstimatedLanes()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 100f, 50f, 60f, 40f, 30f });
            var index      = new MasonrySizeIndex(dataSource, 10f, 2, 100f);
            var visible    = new List<int>();

            index.CollectVisibleIndices(0f, 1000f, 0, visible);

            Assert.AreEqual(0, index.GetCrossAxisIndex(0));
            Assert.AreEqual(1, index.GetCrossAxisIndex(1));
            Assert.AreEqual(0, index.GetCrossAxisIndex(2));
            Assert.AreEqual(1, index.GetCrossAxisIndex(3));
            Assert.AreEqual(110f, index.GetOffset(2));
            Assert.AreEqual(60f, index.GetOffset(3));
            Assert.AreEqual(210f, index.TotalSize);
        }

        /// <summary>
        /// Verifies lane-local visibility lookup does not scan or return distant items.
        /// </summary>
        [Test]
        public void MasonryCollectsVisibleItemsPerLane()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 100f, 50f, 60f, 40f, 30f });
            var index      = new MasonrySizeIndex(dataSource, 10f, 2, 100f);
            var visible    = new List<int>();

            index.CollectVisibleIndices(105f, 145f, 0, visible);

            CollectionAssert.AreEquivalent(new[] { 2, 3 }, visible);
        }

        /// <summary>
        /// Verifies a measured height update shifts only later items in the same lane.
        /// </summary>
        [Test]
        public void MasonryUpdatesStableLaneOffsets()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 100f, 50f, 60f, 40f, 30f });
            var index      = new MasonrySizeIndex(dataSource, 10f, 2, 100f);
            var visible    = new List<int>();

            index.CollectVisibleIndices(0f, 1000f, 0, visible);

            index.UpdateSize(1, 80f);

            Assert.AreEqual(110f, index.GetOffset(2));
            Assert.AreEqual(180f, index.GetOffset(4));
            Assert.AreEqual(210f, index.TotalSize);
        }

        /// <summary>
        /// Verifies a large masonry data set only requests sizes near the viewport.
        /// </summary>
        [Test]
        public void MasonryIndexResolvesVisibleSizesLazily()
        {
            var sizes = new float[10000];
            for (var index = 0; index < sizes.Length; index++)
            {
                sizes[index] = 50f;
            }

            var dataSource = new VirtualSizeIndexTestDataSource(sizes);
            var sizeIndex  = new MasonrySizeIndex(dataSource, 10f, 2, 50f);
            var visible    = new List<int>();

            Assert.AreEqual(0, dataSource.SizeRequestCount);

            sizeIndex.CollectVisibleIndices(0f, 300f, 1, visible);

            Assert.LessOrEqual(dataSource.SizeRequestCount, 14);
            var resolvedRequestCount = dataSource.SizeRequestCount;
            sizeIndex.CollectVisibleIndices(0f, 300f, 1, visible);
            Assert.AreEqual(resolvedRequestCount, dataSource.SizeRequestCount);
        }
    }
}
