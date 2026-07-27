//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Verifies variable-size Fenwick tree lookup and update behavior.
//---------------------------------------------------------------------------------------

using NUnit.Framework;

namespace TristinWen.VirtualScroll.Tests
{
    /// <summary>
    /// Verifies variable-size virtual indexing.
    /// </summary>
    public sealed class VariableSizeIndexTests
    {
        /// <summary>
        /// Verifies offsets and reverse lookup across variable item sizes.
        /// </summary>
        [Test]
        public void VariableIndexMapsOffsetsWithUniformSpacing()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 50f, 100f, 75f });
            var index      = new VariableSizeIndex(dataSource, 10f, 80f);
            var visible    = new System.Collections.Generic.List<int>();

            index.CollectVisibleIndices(0f, 1000f, 0, visible);

            Assert.AreEqual(0f, index.GetOffset(0));
            Assert.AreEqual(60f, index.GetOffset(1));
            Assert.AreEqual(170f, index.GetOffset(2));
            Assert.AreEqual(245f, index.TotalSize);
            Assert.AreEqual(0, index.FindIndex(59f));
            Assert.AreEqual(1, index.FindIndex(60f));
            Assert.AreEqual(2, index.FindIndex(244f));
        }

        /// <summary>
        /// Verifies logarithmic size updates affect following offsets and total size.
        /// </summary>
        [Test]
        public void VariableIndexUpdatesOneSize()
        {
            var dataSource = new VirtualSizeIndexTestDataSource(new[] { 50f, 100f, 75f });
            var index      = new VariableSizeIndex(dataSource, 10f, 80f);
            var visible    = new System.Collections.Generic.List<int>();

            index.CollectVisibleIndices(0f, 1000f, 0, visible);

            index.UpdateSize(1, 140f);

            Assert.AreEqual(210f, index.GetOffset(2));
            Assert.AreEqual(285f, index.TotalSize);
            Assert.AreEqual(1, index.FindIndex(180f));
            Assert.AreEqual(2, index.FindIndex(210f));
        }

        /// <summary>
        /// Verifies a large data set only requests sizes near the viewport.
        /// </summary>
        [Test]
        public void VariableIndexResolvesVisibleSizesLazily()
        {
            var sizes = new float[10000];
            for (var index = 0; index < sizes.Length; index++)
            {
                sizes[index] = 50f;
            }

            var dataSource = new VirtualSizeIndexTestDataSource(sizes);
            var sizeIndex  = new VariableSizeIndex(dataSource, 10f, 50f);
            var visible    = new System.Collections.Generic.List<int>();

            Assert.AreEqual(0, dataSource.SizeRequestCount);

            sizeIndex.CollectVisibleIndices(0f, 300f, 1, visible);

            Assert.LessOrEqual(dataSource.SizeRequestCount, 8);
            var resolvedRequestCount = dataSource.SizeRequestCount;
            sizeIndex.CollectVisibleIndices(0f, 300f, 1, visible);
            Assert.AreEqual(resolvedRequestCount, dataSource.SizeRequestCount);
        }
    }
}
