//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: High-performance virtualized uGUI list for fixed-size and variable-size items.
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Recycles visible uGUI items and resolves their offsets without traversing all data while scrolling.
    /// </summary>
    [AddComponentMenu("UI/Virtual Scroll View")]
    [DisallowMultipleComponent]
    public sealed class VirtualScrollView : ScrollRect
    {
        /// <summary>
        /// Main scrolling direction.
        /// </summary>
        public EVirtualScrollDirection Direction = EVirtualScrollDirection.Vertical;

        /// <summary>
        /// Item sizing strategy.
        /// </summary>
        public EVirtualScrollSizeMode SizeMode = EVirtualScrollSizeMode.Fixed;

        /// <summary>
        /// Main-axis item size used in fixed mode.
        /// </summary>
        [Min(0.01f)]
        public float FixedItemSize = 100f;

        /// <summary>
        /// Uniform distance between adjacent items.
        /// </summary>
        [Min(0f)]
        public float Spacing;

        /// <summary>
        /// Extra items retained before and after the viewport.
        /// </summary>
        [Min(0)]
        public int Overscan = 1;

        /// <summary>
        /// Active views keyed by data index.
        /// </summary>
        private readonly Dictionary<int, VirtualScrollSlot> mActiveSlots = new();

        /// <summary>
        /// Reusable views grouped by item type.
        /// </summary>
        private readonly Dictionary<int, Stack<RectTransform>> mPools = new();

        /// <summary>
        /// Reusable slot metadata retained after views leave the viewport.
        /// </summary>
        private readonly Stack<VirtualScrollSlot> mSlotPool = new();

        /// <summary>
        /// Reusable index buffer used while recycling views.
        /// </summary>
        private readonly List<int> mRecycleIndices = new();

        /// <summary>
        /// Current data source.
        /// </summary>
        private IVirtualScrollDataSource mDataSource;

        /// <summary>
        /// Current fixed or variable size index.
        /// </summary>
        private IVirtualSizeIndex mSizeIndex;

        /// <summary>
        /// Explicit viewport or the component RectTransform when no viewport is assigned.
        /// </summary>
        private RectTransform mResolvedViewport;

        /// <summary>
        /// First active data index.
        /// </summary>
        private int mFirstVisible = -1;

        /// <summary>
        /// Last active data index.
        /// </summary>
        private int mLastVisible = -1;

        /// <summary>
        /// Prevents refresh while internal dimensions are being updated.
        /// </summary>
        private bool mUpdatingLayout;

        /// <summary>
        /// Gets the first currently materialized data index.
        /// </summary>
        public int FirstVisibleIndex => mFirstVisible;

        /// <summary>
        /// Gets the last currently materialized data index.
        /// </summary>
        public int LastVisibleIndex => mLastVisible;

        /// <summary>
        /// Sets the data source and rebuilds the virtual size index.
        /// </summary>
        /// <param name="dataSource">Data source used for item creation and binding.</param>
        public void SetDataSource(IVirtualScrollDataSource dataSource)
        {
            if (ReferenceEquals(mDataSource, dataSource))
            {
                ReloadData(true);
                return;
            }

            RecycleAllActive();
            mDataSource = dataSource;
            ReloadData(false);
        }

        /// <summary>
        /// Rebuilds sizes and visible items after the data collection changes.
        /// </summary>
        /// <param name="keepScrollPosition">Whether to preserve the current main-axis offset.</param>
        public void ReloadData(bool keepScrollPosition = true)
        {
            var oldOffset = keepScrollPosition ? GetScrollOffset() : 0f;
            RecycleAllActive();

            if (mDataSource is null || !content)
            {
                mSizeIndex = null;
                return;
            }

            mSizeIndex = SizeMode == EVirtualScrollSizeMode.Fixed ? new FixedSizeIndex(mDataSource.Count, FixedItemSize, Spacing) : new VariableSizeIndex(mDataSource, Spacing);
            ConfigureTransforms();
            UpdateContentSize();
            SetScrollOffset(Mathf.Clamp(oldOffset, 0f, GetMaxScrollOffset()));
            RefreshVisible(true);
        }

        /// <summary>
        /// Rebinds one active item without rebuilding the list.
        /// </summary>
        /// <param name="index">Data index to refresh.</param>
        public void RefreshItem(int index)
        {
            if (mDataSource is null || !mActiveSlots.TryGetValue(index, out var slot))
            {
                return;
            }

            mDataSource.BindItem(slot.Item, index);
        }

        /// <summary>
        /// Rebinds active items inside a data range.
        /// </summary>
        /// <param name="startIndex">Inclusive first index.</param>
        /// <param name="count">Number of items to refresh.</param>
        public void RefreshRange(int startIndex, int count)
        {
            if (mDataSource is null || count <= 0)
            {
                return;
            }

            var endIndex = startIndex + count;
            for (var index = startIndex; index < endIndex; index++)
            {
                if (mActiveSlots.TryGetValue(index, out var slot))
                {
                    mDataSource.BindItem(slot.Item, index);
                }
            }
        }

        /// <summary>
        /// Applies a measured size change and preserves the first visible item's screen position.
        /// </summary>
        /// <param name="index">Data index whose size changed.</param>
        /// <param name="newSize">New height for vertical lists or width for horizontal lists.</param>
        public void NotifyItemSizeChanged(int index, float newSize)
        {
            if (SizeMode != EVirtualScrollSizeMode.Variable || mSizeIndex is null || index < 0 || index >= mSizeIndex.Count)
            {
                return;
            }

            var anchorIndex = Mathf.Max(0, mSizeIndex.FindIndex(GetScrollOffset()));
            var anchorDelta = GetScrollOffset() - mSizeIndex.GetOffset(anchorIndex);
            mSizeIndex.UpdateSize(index, newSize);
            UpdateContentSize();
            SetScrollOffset(mSizeIndex.GetOffset(anchorIndex) + anchorDelta);
            PositionActiveItems();
            RefreshVisible(true);
        }

        /// <summary>
        /// Scrolls an item to a requested viewport alignment.
        /// </summary>
        /// <param name="index">Target data index.</param>
        /// <param name="alignment">Target alignment.</param>
        public void ScrollToIndex(int index, EVirtualScrollAlignment alignment = EVirtualScrollAlignment.Start)
        {
            if (mSizeIndex is null || mSizeIndex.Count == 0)
            {
                return;
            }

            var validIndex = Mathf.Clamp(index, 0, mSizeIndex.Count - 1);
            var offset = mSizeIndex.GetOffset(validIndex);
            var freeSpace = Mathf.Max(0f, GetViewportSize() - mSizeIndex.GetSize(validIndex));
            if (alignment == EVirtualScrollAlignment.Center)
            {
                offset -= freeSpace * 0.5f;
            }
            else if (alignment == EVirtualScrollAlignment.End)
            {
                offset -= freeSpace;
            }

            SetScrollOffset(Mathf.Clamp(offset, 0f, GetMaxScrollOffset()));
            RefreshVisible(true);
        }

        /// <summary>
        /// Removes active bindings and retains created views in their typed pools.
        /// </summary>
        public void ClearVisibleItems()
        {
            RecycleAllActive();
        }

        /// <summary>
        /// Responds to ScrollRect movement and updates only when the visible range changes.
        /// </summary>
        /// <param name="position">Requested content anchored position.</param>
        protected override void SetContentAnchoredPosition(Vector2 position)
        {
            base.SetContentAnchoredPosition(position);
            if (!mUpdatingLayout)
            {
                RefreshVisible(false);
            }
        }

        /// <summary>
        /// Recalculates content and visibility when the viewport changes size.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (mUpdatingLayout || mSizeIndex is null)
            {
                return;
            }

            UpdateContentSize();
            PositionActiveItems();
            RefreshVisible(true);
        }

        /// <summary>
        /// Releases data bindings before the component is destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            RecycleAllActive();
            mPools.Clear();
            mSlotPool.Clear();
            base.OnDestroy();
        }

        /// <summary>
        /// Configures ScrollRect direction and top-left content coordinates.
        /// </summary>
        private void ConfigureTransforms()
        {
            vertical = Direction == EVirtualScrollDirection.Vertical;
            horizontal = Direction == EVirtualScrollDirection.Horizontal;
            mResolvedViewport = viewport ? viewport : transform as RectTransform;
            content.anchorMin = Vector2.up;
            content.anchorMax = Vector2.up;
            content.pivot = Vector2.up;
        }

        /// <summary>
        /// Updates materialized items for the current viewport range.
        /// </summary>
        /// <param name="forcePosition">Whether to reposition all active items.</param>
        private void RefreshVisible(bool forcePosition)
        {
            if (mUpdatingLayout || mDataSource is null || mSizeIndex is null || mSizeIndex.Count == 0 || !content || !mResolvedViewport)
            {
                if (mSizeIndex != null && mSizeIndex.Count == 0)
                {
                    RecycleAllActive();
                }

                return;
            }

            var scrollOffset = Mathf.Clamp(GetScrollOffset(), 0f, Mathf.Max(0f, mSizeIndex.TotalSize));
            var first = Mathf.Max(0, mSizeIndex.FindIndex(scrollOffset) - Overscan);
            var last = Mathf.Min(mSizeIndex.Count - 1, mSizeIndex.FindIndex(scrollOffset + GetViewportSize()) + Overscan);
            if (!forcePosition && first == mFirstVisible && last == mLastVisible)
            {
                return;
            }

            RecycleOutsideRange(first, last);
            for (var index = first; index <= last; index++)
            {
                if (!mActiveSlots.ContainsKey(index))
                {
                    CreateVisibleSlot(index);
                }
            }

            mFirstVisible = first;
            mLastVisible = last;
            PositionActiveItems();
        }

        /// <summary>
        /// Creates or reuses and binds one visible slot.
        /// </summary>
        /// <param name="index">Data index.</param>
        private void CreateVisibleSlot(int index)
        {
            var itemType = mDataSource.GetItemType(index);
            var item = GetPooledItem(itemType);
            if (!item)
            {
                item = mDataSource.CreateItem(itemType, content);
            }

            if (!item)
            {
                Debug.LogError($"VirtualScrollView data source returned no item for type {itemType}.", this);
                return;
            }

            item.SetParent(content, false);
            item.anchorMin = Vector2.up;
            item.anchorMax = Vector2.up;
            item.pivot = Vector2.up;
            item.gameObject.SetActive(true);

            var slot = mSlotPool.Count > 0 ? mSlotPool.Pop() : new VirtualScrollSlot();
            slot.Item = item;
            slot.Index = index;
            slot.ItemType = itemType;
            mActiveSlots.Add(index, slot);
            PositionSlot(slot);
            mDataSource.BindItem(item, index);
        }

        /// <summary>
        /// Positions every active view from the size index.
        /// </summary>
        private void PositionActiveItems()
        {
            if (mSizeIndex is null)
            {
                return;
            }

            foreach (var pair in mActiveSlots)
            {
                PositionSlot(pair.Value);
            }
        }

        /// <summary>
        /// Positions and sizes one active view from the size index.
        /// </summary>
        /// <param name="slot">Active slot to position.</param>
        private void PositionSlot(VirtualScrollSlot slot)
        {
            var offset = mSizeIndex.GetOffset(slot.Index);
            var size = mSizeIndex.GetSize(slot.Index);
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                slot.Item.anchoredPosition = new Vector2(0f, -offset);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mResolvedViewport.rect.width);
            }
            else
            {
                slot.Item.anchoredPosition = new Vector2(offset, 0f);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mResolvedViewport.rect.height);
            }
        }

        /// <summary>
        /// Recycles active slots outside a desired range.
        /// </summary>
        /// <param name="first">Inclusive first desired index.</param>
        /// <param name="last">Inclusive last desired index.</param>
        private void RecycleOutsideRange(int first, int last)
        {
            mRecycleIndices.Clear();
            foreach (var pair in mActiveSlots)
            {
                if (pair.Key < first || pair.Key > last)
                {
                    mRecycleIndices.Add(pair.Key);
                }
            }

            foreach (var index in mRecycleIndices)
            {
                RecycleSlot(index);
            }
        }

        /// <summary>
        /// Recycles every active slot.
        /// </summary>
        private void RecycleAllActive()
        {
            mRecycleIndices.Clear();
            foreach (var pair in mActiveSlots)
            {
                mRecycleIndices.Add(pair.Key);
            }

            foreach (var index in mRecycleIndices)
            {
                RecycleSlot(index);
            }

            mFirstVisible = -1;
            mLastVisible = -1;
        }

        /// <summary>
        /// Returns one active slot to its typed pool.
        /// </summary>
        /// <param name="index">Active data index.</param>
        private void RecycleSlot(int index)
        {
            if (!mActiveSlots.Remove(index, out var slot))
            {
                return;
            }

            mDataSource?.UnbindItem(slot.Item, index);
            slot.Item.gameObject.SetActive(false);
            if (!mPools.TryGetValue(slot.ItemType, out var pool))
            {
                pool = new Stack<RectTransform>();
                mPools.Add(slot.ItemType, pool);
            }

            pool.Push(slot.Item);
            slot.Item = null;
            slot.Index = -1;
            slot.ItemType = 0;
            mSlotPool.Push(slot);
        }

        /// <summary>
        /// Gets a reusable item of a requested type.
        /// </summary>
        /// <param name="itemType">Pool type identifier.</param>
        /// <returns>Reusable view, or null when the pool is empty.</returns>
        private RectTransform GetPooledItem(int itemType)
        {
            if (!mPools.TryGetValue(itemType, out var pool))
            {
                return null;
            }

            while (pool.Count > 0)
            {
                var item = pool.Pop();
                if (item)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Updates the content RectTransform along the active axis.
        /// </summary>
        private void UpdateContentSize()
        {
            if (!content || !mResolvedViewport || mSizeIndex is null)
            {
                return;
            }

            mUpdatingLayout = true;
            var size = content.sizeDelta;
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                size.x = mResolvedViewport.rect.width;
                size.y = Mathf.Max(mResolvedViewport.rect.height, mSizeIndex.TotalSize);
            }
            else
            {
                size.x = Mathf.Max(mResolvedViewport.rect.width, mSizeIndex.TotalSize);
                size.y = mResolvedViewport.rect.height;
            }

            content.sizeDelta = size;
            mUpdatingLayout = false;
        }

        /// <summary>
        /// Gets the positive main-axis content offset.
        /// </summary>
        /// <returns>Main-axis content offset.</returns>
        private float GetScrollOffset()
        {
            if (!content)
            {
                return 0f;
            }

            return Direction == EVirtualScrollDirection.Vertical ? content.anchoredPosition.y : -content.anchoredPosition.x;
        }

        /// <summary>
        /// Sets the positive main-axis content offset.
        /// </summary>
        /// <param name="offset">Requested offset.</param>
        private void SetScrollOffset(float offset)
        {
            if (!content)
            {
                return;
            }

            var position = content.anchoredPosition;
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                position.y = offset;
            }
            else
            {
                position.x = -offset;
            }

            content.anchoredPosition = position;
        }

        /// <summary>
        /// Gets the viewport size along the active axis.
        /// </summary>
        /// <returns>Main-axis viewport size.</returns>
        private float GetViewportSize()
        {
            if (!mResolvedViewport)
            {
                return 0f;
            }

            return Direction == EVirtualScrollDirection.Vertical ? mResolvedViewport.rect.height : mResolvedViewport.rect.width;
        }

        /// <summary>
        /// Gets the greatest legal main-axis scroll offset.
        /// </summary>
        /// <returns>Maximum scroll offset.</returns>
        private float GetMaxScrollOffset()
        {
            return mSizeIndex is null ? 0f : Mathf.Max(0f, mSizeIndex.TotalSize - GetViewportSize());
        }
    }
}
