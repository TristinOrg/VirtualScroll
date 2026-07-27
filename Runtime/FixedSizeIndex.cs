//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Provides constant-time offset lookup for fixed-size virtual items.
//---------------------------------------------------------------------------------------

using UnityEngine;

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
        /// Initializes a fixed-size index.
        /// </summary>
        /// <param name="count">Item count.</param>
        /// <param name="itemSize">Main-axis item size.</param>
        /// <param name="spacing">Distance between adjacent items.</param>
        public FixedSizeIndex(int count, float itemSize, float spacing)
        {
            mCount = Mathf.Max(0, count);
            mItemSize = Mathf.Max(0.01f, itemSize);
            mSpacing = Mathf.Max(0f, spacing);
        }

        /// <summary>
        /// Gets the indexed item count.
        /// </summary>
        public int Count => mCount;

        /// <summary>
        /// Gets the total content size.
        /// </summary>
        public float TotalSize => mCount == 0 ? 0f : mCount * mItemSize + (mCount - 1) * mSpacing;

        /// <summary>
        /// Gets the offset at which an item starts.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Main-axis offset.</returns>
        public float GetOffset(int index)
        {
            return Mathf.Clamp(index, 0, mCount) * (mItemSize + mSpacing);
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

            var index = Mathf.FloorToInt(Mathf.Max(0f, offset) / (mItemSize + mSpacing));
            return Mathf.Clamp(index, 0, mCount - 1);
        }

        /// <summary>
        /// Ignores individual size changes because this index is fixed-size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <param name="size">Requested size.</param>
        public void UpdateSize(int index, float size)
        {
        }
    }
}
