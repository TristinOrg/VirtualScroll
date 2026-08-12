//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Indexes variable-size items across equal-width masonry lanes.
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace TristinOrg.VirtualScroll
{
    /// <summary>
    /// Assigns each variable-size item to the currently shortest lane and supports lane-local visibility lookup.
    /// </summary>
    internal sealed class MasonrySizeIndex : IVirtualSizeIndex
    {
        /// <summary>
        /// Main-axis item sizes.
        /// </summary>
        private readonly float[] mSizes;

        /// <summary>
        /// Main-axis start offsets.
        /// </summary>
        private readonly float[] mOffsets;

        /// <summary>
        /// Lane assigned to each data index.
        /// </summary>
        private readonly int[] mLanes;

        /// <summary>
        /// Tracks whether each item has obtained its real size.
        /// </summary>
        private readonly bool[] mResolved;

        /// <summary>
        /// Source queried lazily as items approach the viewport.
        /// </summary>
        private readonly IVirtualScrollDataSource mDataSource;

        /// <summary>
        /// Data indices ordered within each lane.
        /// </summary>
        private readonly List<int>[] mLaneIndices;

        /// <summary>
        /// Uniform main-axis spacing.
        /// </summary>
        private readonly float mSpacing;

        /// <summary>
        /// Total content size along the scrolling axis.
        /// </summary>
        private float mTotalSize;

        /// <summary>
        /// Initializes a variable-size masonry index.
        /// </summary>
        /// <param name="dataSource">Source used to resolve initial item sizes.</param>
        /// <param name="spacing">Uniform main-axis spacing.</param>
        /// <param name="crossAxisCount">Number of equal-width lanes.</param>
        /// <param name="estimatedSize">Initial size used for items that have not been measured.</param>
        public MasonrySizeIndex(IVirtualScrollDataSource dataSource, float spacing, int crossAxisCount, float estimatedSize)
        {
            var count    = Mathf.Max(0, dataSource.Count);
            mDataSource  = dataSource;
            mSpacing     = Mathf.Max(0f, spacing);
            mSizes       = new float[count];
            mOffsets     = new float[count];
            mLanes       = new int[count];
            mResolved    = new bool[count];
            mLaneIndices = new List<int>[Mathf.Max(1, crossAxisCount)];
            for (var lane = 0; lane < mLaneIndices.Length; lane++)
            {
                mLaneIndices[lane] = new List<int>();
            }

            var laneSizes          = new float[mLaneIndices.Length];
            var validEstimatedSize = Mathf.Max(0.01f, estimatedSize);
            for (var index = 0; index < count; index++)
            {
                var lane        = GetShortestLane(laneSizes);
                mSizes[index]   = validEstimatedSize;
                mOffsets[index] = laneSizes[lane];
                mLanes[index]   = lane;
                mLaneIndices[lane].Add(index);
                laneSizes[lane] += validEstimatedSize + mSpacing;
            }

            mTotalSize = GetMaxLaneSize(laneSizes);
        }

        /// <summary>
        /// Gets the indexed item count.
        /// </summary>
        public int Count => mSizes.Length;

        /// <summary>
        /// Gets the total main-axis content size.
        /// </summary>
        public float TotalSize => mTotalSize;

        /// <summary>
        /// Gets the number of masonry lanes.
        /// </summary>
        public int CrossAxisCount => mLaneIndices.Length;

        /// <summary>
        /// Gets the number of real-size updates applied to the index.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Gets an item's main-axis start offset.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Main-axis start offset.</returns>
        public float GetOffset(int index)
        {
            return index >= 0 && index < Count ? mOffsets[index] : 0f;
        }

        /// <summary>
        /// Gets an item's main-axis size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Item size.</returns>
        public float GetSize(int index)
        {
            return index >= 0 && index < Count ? ResolveSize(index) : 0f;
        }

        /// <summary>
        /// Finds the lowest data index visible at or after an offset.
        /// </summary>
        /// <param name="offset">Main-axis offset.</param>
        /// <returns>Data index.</returns>
        public int FindIndex(float offset)
        {
            var result = Count == 0 ? -1 : Count - 1;
            for (var lane = 0; lane < CrossAxisCount; lane++)
            {
                var lanePosition = FindFirstVisibleLanePosition(lane, offset);
                if (lanePosition >= 0 && lanePosition < mLaneIndices[lane].Count)
                {
                    result = Mathf.Min(result, mLaneIndices[lane][lanePosition]);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the lane occupied by an item.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Zero-based lane index.</returns>
        public int GetCrossAxisIndex(int index)
        {
            return index >= 0 && index < Count ? mLanes[index] : 0;
        }

        /// <summary>
        /// Collects visible items independently inside every masonry lane.
        /// </summary>
        /// <param name="startOffset">Viewport start offset.</param>
        /// <param name="endOffset">Viewport end offset.</param>
        /// <param name="overscan">Additional retained items per lane.</param>
        /// <param name="results">Reusable destination list.</param>
        public void CollectVisibleIndices(float startOffset, float endOffset, int overscan, List<int> results)
        {
            var validOverscan = Mathf.Max(0, overscan);
            while (true)
            {
                results.Clear();
                var version = Version;
                for (var lane = 0; lane < CrossAxisCount; lane++)
                {
                    CollectVisibleLaneIndices(lane, startOffset, endOffset, validOverscan, results);
                }

                if (version == Version)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Updates one item and shifts later items in its stable lane.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <param name="size">New main-axis size.</param>
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
            var lane      = mLanes[index];
            var laneItems = mLaneIndices[lane];
            var found     = false;
            foreach (var laneIndex in laneItems)
            {
                if (laneIndex == index)
                {
                    found = true;
                    continue;
                }

                if (found)
                {
                    mOffsets[laneIndex] += delta;
                }
            }

            RecalculateTotalSize();
        }

        /// <summary>
        /// Collects and resolves visible indices for one lane.
        /// </summary>
        /// <param name="lane">Lane index.</param>
        /// <param name="startOffset">Viewport start offset.</param>
        /// <param name="endOffset">Viewport end offset.</param>
        /// <param name="overscan">Additional retained items.</param>
        /// <param name="results">Reusable destination list.</param>
        private void CollectVisibleLaneIndices(int lane, float startOffset, float endOffset, int overscan, List<int> results)
        {
            var laneItems = mLaneIndices[lane];
            if (laneItems.Count == 0)
            {
                return;
            }

            var firstPosition = Mathf.Max(0, FindFirstVisibleLanePosition(lane, startOffset) - overscan);
            for (var position = firstPosition; position < laneItems.Count; position++)
            {
                var index = laneItems[position];
                if (mOffsets[index] > endOffset)
                {
                    var trailingEnd = Mathf.Min(laneItems.Count, position + overscan);
                    for (var trailing = position; trailing < trailingEnd; trailing++)
                    {
                        ResolveSize(laneItems[trailing]);
                        results.Add(laneItems[trailing]);
                    }

                    return;
                }

                ResolveSize(index);
                results.Add(index);
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
        /// Finds the first lane-local item whose end reaches an offset.
        /// </summary>
        /// <param name="lane">Lane index.</param>
        /// <param name="offset">Main-axis offset.</param>
        /// <returns>Lane-local list position.</returns>
        private int FindFirstVisibleLanePosition(int lane, float offset)
        {
            var items = mLaneIndices[lane];
            if (items.Count == 0)
            {
                return -1;
            }

            var low  = 0;
            var high = items.Count - 1;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                var index  = items[middle];
                if (mOffsets[index] + mSizes[index] < offset)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        /// <summary>
        /// Gets the lane with the smallest accumulated size.
        /// </summary>
        /// <param name="laneSizes">Accumulated lane sizes.</param>
        /// <returns>Shortest lane index.</returns>
        private static int GetShortestLane(float[] laneSizes)
        {
            var result = 0;
            for (var lane = 1; lane < laneSizes.Length; lane++)
            {
                if (laneSizes[lane] < laneSizes[result])
                {
                    result = lane;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets total content size from accumulated lane sizes.
        /// </summary>
        /// <param name="laneSizes">Accumulated lane sizes including trailing spacing.</param>
        /// <returns>Total content size without trailing spacing.</returns>
        private float GetMaxLaneSize(float[] laneSizes)
        {
            var result = 0f;
            foreach (var laneSize in laneSizes)
            {
                result = Mathf.Max(result, laneSize);
            }

            return result > 0f ? result - mSpacing : 0f;
        }

        /// <summary>
        /// Recalculates total size after an item size change.
        /// </summary>
        private void RecalculateTotalSize()
        {
            mTotalSize = 0f;
            foreach (var laneItems in mLaneIndices)
            {
                if (laneItems.Count == 0)
                {
                    continue;
                }

                var lastIndex = laneItems[laneItems.Count - 1];
                mTotalSize    = Mathf.Max(mTotalSize, mOffsets[lastIndex] + mSizes[lastIndex]);
            }
        }
    }
}
