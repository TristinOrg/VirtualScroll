//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Provides logarithmic offset lookup and updates for variable-size virtual items.
//---------------------------------------------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Maps variable-size items to offsets with a Fenwick tree.
    /// </summary>
    internal sealed class VariableSizeIndex : IVirtualSizeIndex
    {
        /// <summary>
        /// Item sizes without spacing.
        /// </summary>
        private readonly float[] mSizes;

        /// <summary>
        /// Fenwick tree containing item extents including spacing.
        /// </summary>
        private readonly float[] mTree;

        /// <summary>
        /// Tracks whether each item has obtained its real size.
        /// </summary>
        private readonly bool[] mResolved;

        /// <summary>
        /// Source queried lazily as items approach the viewport.
        /// </summary>
        private readonly IVirtualScrollDataSource mDataSource;

        /// <summary>
        /// Uniform distance between adjacent items.
        /// </summary>
        private readonly float mSpacing;

        /// <summary>
        /// Initializes a variable-size index from a data source.
        /// </summary>
        /// <param name="dataSource">Source used to resolve initial sizes.</param>
        /// <param name="spacing">Distance between adjacent items.</param>
        /// <param name="estimatedSize">Initial size used for items that have not been measured.</param>
        public VariableSizeIndex(IVirtualScrollDataSource dataSource, float spacing, float estimatedSize)
        {
            var count = Mathf.Max(0, dataSource.Count);
            mDataSource = dataSource;
            mSpacing    = Mathf.Max(0f, spacing);
            mSizes      = new float[count];
            mTree       = new float[count + 1];
            mResolved   = new bool[count];
            var validEstimatedSize = Mathf.Max(0.01f, estimatedSize);

            for (var i = 0; i < count; i++)
            {
                mSizes[i]    = validEstimatedSize;
                mTree[i + 1] += mSizes[i] + mSpacing;
                var parent = (i + 1) + ((i + 1) & -(i + 1));
                if (parent <= count)
                {
                    mTree[parent] += mTree[i + 1];
                }
            }
        }

        /// <summary>
        /// Gets the indexed item count.
        /// </summary>
        public int Count => mSizes.Length;

        /// <summary>
        /// Gets the total content size.
        /// </summary>
        public float TotalSize => Count == 0 ? 0f : GetPrefixSum(Count) - mSpacing;

        /// <summary>
        /// Gets the single cross-axis lane used by a linear list.
        /// </summary>
        public int CrossAxisCount => 1;

        /// <summary>
        /// Gets the number of real-size updates applied to the index.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Gets the offset at which an item starts.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Main-axis offset.</returns>
        public float GetOffset(int index)
        {
            return GetPrefixSum(Mathf.Clamp(index, 0, Count));
        }

        /// <summary>
        /// Gets an item's main-axis size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Item size without spacing.</returns>
        public float GetSize(int index)
        {
            return index >= 0 && index < Count ? ResolveSize(index) : 0f;
        }

        /// <summary>
        /// Finds the item at a content offset in logarithmic time.
        /// </summary>
        /// <param name="offset">Main-axis content offset.</param>
        /// <returns>Data index.</returns>
        public int FindIndex(float offset)
        {
            if (Count == 0)
            {
                return -1;
            }

            var target = Mathf.Clamp(offset, 0f, TotalSize);
            var index = 0;
            var accumulated = 0f;
            var bit = HighestOneBit(Count);
            while (bit != 0)
            {
                var next = index + bit;
                if (next <= Count && accumulated + mTree[next] <= target)
                {
                    index = next;
                    accumulated += mTree[next];
                }

                bit >>= 1;
            }

            return Mathf.Clamp(index, 0, Count - 1);
        }

        /// <summary>
        /// Gets the only lane occupied by a linear item.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Zero.</returns>
        public int GetCrossAxisIndex(int index)
        {
            return 0;
        }

        /// <summary>
        /// Collects the contiguous indices intersecting a viewport range.
        /// </summary>
        /// <param name="startOffset">Viewport start offset.</param>
        /// <param name="endOffset">Viewport end offset.</param>
        /// <param name="overscan">Additional retained items.</param>
        /// <param name="results">Reusable destination list.</param>
        public void CollectVisibleIndices(float startOffset, float endOffset, int overscan, List<int> results)
        {
            results.Clear();
            if (Count == 0)
            {
                return;
            }

            var validOverscan = Mathf.Max(0, overscan);
            var first         = 0;
            var last          = 0;
            while (true)
            {
                first       = Mathf.Max(0, FindIndex(startOffset) - validOverscan);
                last        = Mathf.Min(Count - 1, FindIndex(endOffset) + validOverscan);
                var version = Version;
                for (var index = first; index <= last; index++)
                {
                    ResolveSize(index);
                }

                var resolvedFirst = Mathf.Max(0, FindIndex(startOffset) - validOverscan);
                var resolvedLast  = Mathf.Min(Count - 1, FindIndex(endOffset) + validOverscan);
                if (version == Version || first == resolvedFirst && last == resolvedLast)
                {
                    first = resolvedFirst;
                    last  = resolvedLast;
                    break;
                }
            }

            for (var index = first; index <= last; index++)
            {
                results.Add(index);
            }
        }

        /// <summary>
        /// Updates an item's size in logarithmic time.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <param name="size">New item size.</param>
        public void UpdateSize(int index, float size)
        {
            if (index < 0 || index >= Count)
            {
                return;
            }

            var validSize    = Mathf.Max(0.01f, size);
            var delta        = validSize - mSizes[index];
            var wasResolved  = mResolved[index];
            mResolved[index] = true;
            if (Mathf.Approximately(delta, 0f))
            {
                if (!wasResolved)
                {
                    Version++;
                }

                return;
            }

            mSizes[index] = validSize;
            Version++;
            for (var treeIndex = index + 1; treeIndex <= Count; treeIndex += treeIndex & -treeIndex)
            {
                mTree[treeIndex] += delta;
            }
        }

        /// <summary>
        /// Resolves an item's real size once.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Resolved main-axis size.</returns>
        private float ResolveSize(int index)
        {
            if (mResolved[index])
            {
                return mSizes[index];
            }

            var size = Mathf.Max(0.01f, mDataSource.GetItemSize(index));
            UpdateSize(index, size);
            return mSizes[index];
        }

        /// <summary>
        /// Gets the sum of item extents before an exclusive index.
        /// </summary>
        /// <param name="exclusiveIndex">Exclusive item index.</param>
        /// <returns>Accumulated extent.</returns>
        private float GetPrefixSum(int exclusiveIndex)
        {
            var sum = 0f;
            for (var treeIndex = exclusiveIndex; treeIndex > 0; treeIndex -= treeIndex & -treeIndex)
            {
                sum += mTree[treeIndex];
            }

            return sum;
        }

        /// <summary>
        /// Gets the greatest power of two not larger than a value.
        /// </summary>
        /// <param name="value">Positive input value.</param>
        /// <returns>Power of two.</returns>
        private static int HighestOneBit(int value)
        {
            var bit = 1;
            while ((bit << 1) <= value)
            {
                bit <<= 1;
            }

            return bit;
        }
    }
}
