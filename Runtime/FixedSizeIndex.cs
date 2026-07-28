//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Provides constant-time offset lookup for fixed-size virtual items.
//---------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Maps fixed-size items to offsets using direct arithmetic.
    /// </summary>
    internal sealed class FixedSizeIndex : IVirtualSizeIndex
    {
        /// <summary>
        /// Number of indexed items.
        /// </summary>
        private readonly int mCount;

        /// <summary>
        /// Main-axis item size.
        /// </summary>
        private readonly float mItemSize;

        /// <summary>
        /// Distance between adjacent items.
        /// </summary>
        private readonly float mSpacing;

        /// <summary>
        /// Number of equal-width lanes across the scrolling axis.
        /// </summary>
        private readonly int mCrossAxisCount;

        /// <summary>
        /// Initializes a fixed-size index.
        /// </summary>
        /// <param name="count">Item count.</param>
        /// <param name="itemSize">Main-axis item size.</param>
        /// <param name="spacing">Distance between adjacent items.</param>
        /// <param name="crossAxisCount">Number of equal-width lanes.</param>
        public FixedSizeIndex(int count, float itemSize, float spacing, int crossAxisCount = 1)
        {
            mCount          = Mathf.Max(0, count);
            mItemSize       = Mathf.Max(0.01f, itemSize);
            mSpacing        = Mathf.Max(0f, spacing);
            mCrossAxisCount = Mathf.Max(1, crossAxisCount);
        }

        /// <summary>
        /// Gets the indexed item count.
        /// </summary>
        public int Count => mCount;

        /// <summary>
        /// Gets the total content size.
        /// </summary>
        public float TotalSize
        {
            get
            {
                var rowCount = GetRowCount();
                return rowCount == 0 ? 0f : rowCount * mItemSize + (rowCount - 1) * mSpacing;
            }
        }

        /// <summary>
        /// Gets the number of equal-width lanes.
        /// </summary>
        public int CrossAxisCount => mCrossAxisCount;

        /// <summary>
        /// Gets the immutable fixed-layout revision.
        /// </summary>
        public int Version => 0;

        /// <summary>
        /// Gets the offset at which an item starts.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Main-axis offset.</returns>
        public float GetOffset(int index)
        {
            var validIndex = Mathf.Clamp(index, 0, mCount);
            return validIndex / mCrossAxisCount * (mItemSize + mSpacing);
        }

        /// <summary>
        /// Gets the fixed item size.
        /// </summary>
        /// <param name="index">Unused data index.</param>
        /// <returns>Fixed item size.</returns>
        public float GetSize(int index)
        {
            return mItemSize;
        }

        /// <summary>
        /// Finds the item at a content offset in constant time.
        /// </summary>
        /// <param name="offset">Main-axis content offset.</param>
        /// <returns>Data index.</returns>
        public int FindIndex(float offset)
        {
            if (mCount == 0)
            {
                return -1;
            }

            var row = Mathf.FloorToInt(Mathf.Max(0f, offset) / (mItemSize + mSpacing));
            return Mathf.Clamp(row * mCrossAxisCount, 0, mCount - 1);
        }

        /// <summary>
        /// Gets the lane occupied by an item.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Zero-based lane index.</returns>
        public int GetCrossAxisIndex(int index)
        {
            return Mathf.Max(0, index) % mCrossAxisCount;
        }

        /// <summary>
        /// Collects items in rows intersecting a viewport range.
        /// </summary>
        /// <param name="startOffset">Viewport start offset.</param>
        /// <param name="endOffset">Viewport end offset.</param>
        /// <param name="overscan">Additional retained rows.</param>
        /// <param name="results">Reusable destination list.</param>
        public void CollectVisibleIndices(float startOffset, float endOffset, int overscan, List<int> results)
        {
            results.Clear();
            if (mCount == 0)
            {
                return;
            }

            var first        = Mathf.Max(0, FindIndex(startOffset) - Mathf.Max(0, overscan) * mCrossAxisCount);
            var lastRowFirst = FindIndex(endOffset) + Mathf.Max(0, overscan) * mCrossAxisCount;
            var last         = Mathf.Min(mCount - 1, lastRowFirst + mCrossAxisCount - 1);
            for (var index = first; index <= last; index++)
            {
                results.Add(index);
            }
        }

        /// <summary>
        /// Ignores individual size changes because this index is fixed-size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <param name="size">Requested size.</param>
        public void UpdateSize(int index, float size)
        {
        }

        /// <summary>
        /// Gets the number of occupied rows.
        /// </summary>
        /// <returns>Row count.</returns>
        private int GetRowCount()
        {
            return mCount == 0 ? 0 : (mCount + mCrossAxisCount - 1) / mCrossAxisCount;
        }
    }
}
