//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: High-performance virtualized uGUI list for fixed-size and variable-size items.
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Recycles visible uGUI items and resolves their offsets without traversing all data while scrolling.
    /// </summary>
    [AddComponentMenu("UI/Virtual Scroll View")]
    [DisallowMultipleComponent]
    public sealed class VirtualScrollView : ScrollRect, IVirtualScrollAnimationCallback
    {
        /// <summary>
        /// Main scrolling direction.
        /// </summary>
        [Header("Virtual Layout")]
        public EVirtualScrollDirection Direction = EVirtualScrollDirection.Vertical;

        /// <summary>
        /// Item sizing strategy.
        /// </summary>
        public EVirtualScrollSizeMode SizeMode = EVirtualScrollSizeMode.Fixed;

        /// <summary>
        /// Main-axis item size used in fixed mode.
        /// </summary>
        [Min(0.01f)]
        [FormerlySerializedAs("FixedItemSize")]
        public float FixedMainAxisSize = 100f;

        /// <summary>
        /// Estimated main-axis size used until a variable item approaches the viewport.
        /// </summary>
        [Min(0.01f)]
        public float EstimatedMainAxisSize = 100f;

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
        /// Number of equal-width lanes across the scrolling axis.
        /// Fixed-size lists become grids and variable-size lists become masonry layouts when greater than one.
        /// </summary>
        [Min(1)]
        public int CrossAxisCount = 1;

        /// <summary>
        /// Uniform distance between adjacent cross-axis lanes.
        /// </summary>
        [Min(0f)]
        public float CrossAxisSpacing;

        /// <summary>
        /// Automatically captures and disables a supported LayoutGroup on the content transform.
        /// </summary>
        [Header("LayoutGroup Integration")]
        public bool UseLayoutGroupSettings = true;

        /// <summary>
        /// Keeps <see cref="FixedMainAxisSize"/> instead of using GridLayoutGroup cell size.
        /// Variable-size mode always uses sizes supplied by the data source.
        /// </summary>
        public bool OverrideLayoutItemSize;

        /// <summary>
        /// Enables scale and opacity animations for visible inserted and removed items.
        /// </summary>
        [Header("Collection Animation")]
        public bool AnimateChanges;

        /// <summary>
        /// Duration of insertion and removal animations in seconds.
        /// </summary>
        [Min(0.01f)]
        public float ChangeAnimationDuration = 0.2f;

        /// <summary>
        /// Optional component implementing <see cref="IVirtualScrollAnimation"/>; leave empty to use the built-in scale and opacity animation.
        /// </summary>
        public MonoBehaviour AnimationProvider;

        /// <summary>
        /// Gets or sets a runtime animation provider that takes precedence over <see cref="AnimationProvider"/>.
        /// </summary>
        public IVirtualScrollAnimation Animation { get; set; }

        /// <summary>
        /// Captures authoring-time layout values outside the scrolling hot path.
        /// </summary>
        private readonly VirtualScrollLayoutCapture mLayoutCapture = new();

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
        /// Reusable destination populated by the current layout index.
        /// </summary>
        private readonly List<int> mDesiredIndices = new();

        /// <summary>
        /// Reusable membership set for non-contiguous masonry visibility.
        /// </summary>
        private readonly HashSet<int> mDesiredIndexSet = new();

        /// <summary>
        /// Removed slots currently completing their exit animation.
        /// </summary>
        private readonly List<VirtualScrollSlot> mAnimatingRemovalSlots = new();

        /// <summary>
        /// Slots using the built-in animation, updated without coroutines or per-frame allocations.
        /// </summary>
        private readonly List<VirtualScrollSlot> mDefaultAnimationSlots = new();

        /// <summary>
        /// Animations keyed by unique identifier for allocation-free provider completion.
        /// </summary>
        private readonly Dictionary<int, VirtualScrollSlot> mAnimatingSlots = new();

        /// <summary>
        /// Reusable slot buffer used while remapping indices after collection changes.
        /// </summary>
        private readonly List<VirtualScrollSlot> mRemapSlots = new();

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
        /// Inclusive first inserted index awaiting an entrance animation.
        /// </summary>
        private int mAnimatedInsertFirst = -1;

        /// <summary>
        /// Exclusive end of the inserted range awaiting an entrance animation.
        /// </summary>
        private int mAnimatedInsertEnd = -1;

        /// <summary>
        /// Last issued animation identifier.
        /// </summary>
        private int mLastAnimationId;

        /// <summary>
        /// Captured parameters used after the source LayoutGroup is disabled.
        /// </summary>
        private VirtualScrollLayoutSnapshot mLayoutSnapshot;

        /// <summary>
        /// Gets the first currently materialized data index.
        /// </summary>
        public int FirstMaterializedIndex => mFirstVisible;

        /// <summary>
        /// Gets the last currently materialized data index.
        /// </summary>
        public int LastMaterializedIndex => mLastVisible;

        /// <summary>
        /// Gets the first data index intersecting the viewport without overscan.
        /// </summary>
        public int FirstViewportIndex
        {
            get
            {
                if (mSizeIndex is null || mSizeIndex.Count == 0)
                {
                    return -1;
                }

                var offset = Mathf.Max(0f, GetScrollOffset() - GetMainStartPadding());
                return Mathf.Clamp(mSizeIndex.FindIndex(offset + 0.001f), 0, mSizeIndex.Count - 1);
            }
        }

        /// <summary>
        /// Gets the first currently materialized data index.
        /// </summary>
        public int FirstVisibleIndex => FirstMaterializedIndex;

        /// <summary>
        /// Gets the last currently materialized data index.
        /// </summary>
        public int LastVisibleIndex => LastMaterializedIndex;

        /// <summary>
        /// Gets or sets the legacy fixed main-axis size name.
        /// </summary>
        [System.Obsolete("Use FixedMainAxisSize instead.")]
        public float FixedItemSize
        {
            get => FixedMainAxisSize;
            set => FixedMainAxisSize = value;
        }

        /// <summary>
        /// Recaptures supported LayoutGroup parameters and rebuilds the current data layout.
        /// Use this after changing LayoutGroup values at runtime.
        /// </summary>
        /// <param name="positionMode">Position strategy applied after recapturing layout.</param>
        public void RecaptureLayoutGroup(EVirtualScrollPositionMode positionMode = EVirtualScrollPositionMode.KeepAnchor)
        {
            mLayoutCapture.Reset();
            mLayoutSnapshot = null;
            CaptureAndDisableLayoutGroup();
            if (mDataSource != null)
            {
                ReloadData(positionMode);
            }
        }

        /// <summary>
        /// Sets the data source and rebuilds the virtual size index.
        /// </summary>
        /// <param name="dataSource">Data source used for item creation and binding.</param>
        public void SetDataSource(IVirtualScrollDataSource dataSource)
        {
            if (ReferenceEquals(mDataSource, dataSource))
            {
                SetDataSource(dataSource, EVirtualScrollPositionMode.KeepOffset);
                return;
            }

            SetDataSource(dataSource, EVirtualScrollPositionMode.Reset);
        }

        /// <summary>
        /// Sets the data source and applies an explicit initial or replacement position strategy.
        /// </summary>
        /// <param name="dataSource">Data source used for item creation and binding.</param>
        /// <param name="positionMode">Position strategy applied after initializing the source.</param>
        public void SetDataSource(IVirtualScrollDataSource dataSource, EVirtualScrollPositionMode positionMode)
        {
            if (!ReferenceEquals(mDataSource, dataSource))
            {
                RecycleAllActive();
            }

            mDataSource = dataSource;
            ReloadData(positionMode);
        }

        /// <summary>
        /// Rebuilds sizes and visible items after the data collection changes.
        /// </summary>
        /// <param name="keepScrollPosition">Whether to preserve the current main-axis offset.</param>
        public void ReloadData(bool keepScrollPosition = true)
        {
            ReloadData(keepScrollPosition ? EVirtualScrollPositionMode.KeepOffset : EVirtualScrollPositionMode.Reset);
        }

        /// <summary>
        /// Rebuilds sizes and visible items using an explicit scroll-position strategy.
        /// </summary>
        /// <param name="positionMode">Position strategy applied after rebuilding data.</param>
        public void ReloadData(EVirtualScrollPositionMode positionMode)
        {
            CapturePosition(out var oldOffset, out var anchorIndex, out var anchorDelta);
            RecycleAllActive();

            if (mDataSource is null || !content)
            {
                mSizeIndex = null;
                return;
            }

            ConfigureTransforms();
            RebuildSizeIndex();
            UpdateContentSize();
            ApplyPositionMode(positionMode, oldOffset, anchorIndex, anchorDelta);
            RefreshVisible(true);
        }

        /// <summary>
        /// Applies a collection insertion after the data source has already inserted its items.
        /// Existing visible views are remapped instead of rebuilt.
        /// </summary>
        /// <param name="index">First inserted data index.</param>
        /// <param name="count">Number of inserted items.</param>
        /// <param name="positionMode">Position strategy applied after insertion.</param>
        /// <param name="animate">Whether visible inserted items may animate.</param>
        public void NotifyItemsInserted(int index, int count, EVirtualScrollPositionMode positionMode = EVirtualScrollPositionMode.KeepAnchor, bool animate = true)
        {
            if (!CanApplyCollectionChange(count) || index < 0 || index > mSizeIndex.Count || mDataSource.Count != mSizeIndex.Count + count)
            {
                Debug.LogError("NotifyItemsInserted must be called after the data source inserts the same item count.", this);
                return;
            }

            CapturePosition(out var oldOffset, out var anchorIndex, out var anchorDelta);
            RemapActiveForInsertion(index, count);
            if (anchorIndex >= index)
            {
                anchorIndex += count;
            }

            RebuildSizeIndex();
            UpdateContentSize();
            ApplyPositionMode(positionMode, oldOffset, anchorIndex, anchorDelta);
            mAnimatedInsertFirst = animate && AnimateChanges ? index : -1;
            mAnimatedInsertEnd   = mAnimatedInsertFirst >= 0 ? index + count : -1;
            RefreshVisible(true);
            ClearPendingInsertionAnimation();
        }

        /// <summary>
        /// Applies a collection removal after the data source has already removed its items.
        /// Existing visible views are remapped instead of rebuilt.
        /// </summary>
        /// <param name="index">First removed data index.</param>
        /// <param name="count">Number of removed items.</param>
        /// <param name="positionMode">Position strategy applied after removal.</param>
        /// <param name="animate">Whether visible removed items may animate.</param>
        public void NotifyItemsRemoved(int index, int count, EVirtualScrollPositionMode positionMode = EVirtualScrollPositionMode.KeepAnchor, bool animate = true)
        {
            if (!CanApplyCollectionChange(count) || index < 0 || index + count > mSizeIndex.Count || mDataSource.Count != mSizeIndex.Count - count)
            {
                Debug.LogError("NotifyItemsRemoved must be called after the data source removes the same item count.", this);
                return;
            }

            CapturePosition(out var oldOffset, out var anchorIndex, out var anchorDelta);
            RemapActiveForRemoval(index, count, animate && AnimateChanges);
            if (anchorIndex >= index + count)
            {
                anchorIndex -= count;
            }
            else if (anchorIndex >= index)
            {
                anchorIndex = Mathf.Min(index, mDataSource.Count - 1);
                anchorDelta = 0f;
            }

            RebuildSizeIndex();
            UpdateContentSize();
            ApplyPositionMode(positionMode, oldOffset, anchorIndex, anchorDelta);
            RefreshVisible(true);
        }

        /// <summary>
        /// Applies a collection move after the data source has already moved one item.
        /// </summary>
        /// <param name="oldIndex">Previous item index.</param>
        /// <param name="newIndex">New item index.</param>
        /// <param name="positionMode">Position strategy applied after the move.</param>
        /// <param name="animate">Whether the moved visible item may animate at its destination.</param>
        public void NotifyItemMoved(int oldIndex, int newIndex, EVirtualScrollPositionMode positionMode = EVirtualScrollPositionMode.KeepAnchor, bool animate = true)
        {
            if (mDataSource is null || mSizeIndex is null || oldIndex < 0 || oldIndex >= mSizeIndex.Count || newIndex < 0 || newIndex >= mSizeIndex.Count || mDataSource.Count != mSizeIndex.Count)
            {
                Debug.LogError("NotifyItemMoved requires valid indices and an unchanged item count.", this);
                return;
            }

            if (oldIndex == newIndex)
            {
                return;
            }

            CapturePosition(out var oldOffset, out var anchorIndex, out var anchorDelta);
            RemapActiveForMove(oldIndex, newIndex);
            var movedSlotWasActive = mActiveSlots.ContainsKey(newIndex);
            anchorIndex            = RemapMovedIndex(anchorIndex, oldIndex, newIndex);
            RebuildSizeIndex();
            UpdateContentSize();
            ApplyPositionMode(positionMode, oldOffset, anchorIndex, anchorDelta);
            mAnimatedInsertFirst = animate && AnimateChanges ? newIndex : -1;
            mAnimatedInsertEnd   = mAnimatedInsertFirst >= 0 ? newIndex + 1 : -1;
            RefreshVisible(true);
            if (movedSlotWasActive && mAnimatedInsertFirst >= 0 && mActiveSlots.TryGetValue(newIndex, out var movedSlot))
            {
                StartInsertionAnimation(movedSlot);
            }

            ClearPendingInsertionAnimation();
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

            var anchorIndex = Mathf.Max(0, mSizeIndex.FindIndex(Mathf.Max(0f, GetScrollOffset() - GetMainStartPadding())));
            var anchorDelta = GetScrollOffset() - GetItemMainOffset(anchorIndex);
            mSizeIndex.UpdateSize(index, newSize);
            UpdateContentSize();
            SetScrollOffset(GetItemMainOffset(anchorIndex) + anchorDelta);
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
            var offset     = GetItemMainOffset(validIndex);
            var freeSpace  = Mathf.Max(0f, GetViewportSize() - mSizeIndex.GetSize(validIndex));
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
        /// Advances built-in collection animations without creating coroutines.
        /// </summary>
        protected override void LateUpdate()
        {
            base.LateUpdate();
            UpdateDefaultAnimations();
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
            RecycleAnimatingRemovalSlots();
            mPools.Clear();
            mSlotPool.Clear();
            mLayoutCapture.Restore();
            base.OnDestroy();
        }

        /// <summary>
        /// Rebuilds the selected fixed, variable, grid, or masonry size index.
        /// </summary>
        private void RebuildSizeIndex()
        {
            var crossAxisCount = Mathf.Max(1, CrossAxisCount);
            if (SizeMode == EVirtualScrollSizeMode.Fixed)
            {
                mSizeIndex = new FixedSizeIndex(mDataSource.Count, FixedMainAxisSize, Spacing, crossAxisCount);
            }
            else if (crossAxisCount == 1)
            {
                mSizeIndex = new VariableSizeIndex(mDataSource, Spacing, EstimatedMainAxisSize);
            }
            else
            {
                mSizeIndex = new MasonrySizeIndex(mDataSource, Spacing, crossAxisCount, EstimatedMainAxisSize);
            }
        }

        /// <summary>
        /// Captures numeric offset and the current first-item anchor.
        /// </summary>
        /// <param name="offset">Current numeric scroll offset.</param>
        /// <param name="anchorIndex">First visible anchor index.</param>
        /// <param name="anchorDelta">Offset relative to the anchor start.</param>
        private void CapturePosition(out float offset, out int anchorIndex, out float anchorDelta)
        {
            offset      = GetScrollOffset();
            anchorIndex = mSizeIndex is null || mSizeIndex.Count == 0 ? -1 : Mathf.Max(0, mSizeIndex.FindIndex(Mathf.Max(0f, offset - GetMainStartPadding())));
            anchorDelta = anchorIndex < 0 ? 0f : offset - GetItemMainOffset(anchorIndex);
        }

        /// <summary>
        /// Applies a requested scroll position after rebuilding layout data.
        /// </summary>
        /// <param name="positionMode">Requested position strategy.</param>
        /// <param name="oldOffset">Previous numeric offset.</param>
        /// <param name="anchorIndex">Mapped anchor index.</param>
        /// <param name="anchorDelta">Viewport-relative anchor offset.</param>
        private void ApplyPositionMode(EVirtualScrollPositionMode positionMode, float oldOffset, int anchorIndex, float anchorDelta)
        {
            var offset = 0f;
            if (positionMode == EVirtualScrollPositionMode.KeepOffset)
            {
                offset = oldOffset;
            }
            else if (positionMode == EVirtualScrollPositionMode.KeepAnchor && mSizeIndex.Count > 0)
            {
                var validAnchor = Mathf.Clamp(anchorIndex, 0, mSizeIndex.Count - 1);
                offset          = GetItemMainOffset(validAnchor) + anchorDelta;
            }
            else if (positionMode == EVirtualScrollPositionMode.StickToEnd)
            {
                offset = GetMaxScrollOffset();
            }

            SetScrollOffset(Mathf.Clamp(offset, 0f, GetMaxScrollOffset()));
        }

        /// <summary>
        /// Validates shared collection-change preconditions.
        /// </summary>
        /// <param name="count">Changed item count.</param>
        /// <returns>True when collection state can be updated incrementally.</returns>
        private bool CanApplyCollectionChange(int count)
        {
            return mDataSource != null && mSizeIndex != null && count > 0;
        }

        /// <summary>
        /// Shifts active logical items after an insertion.
        /// </summary>
        /// <param name="index">First inserted index.</param>
        /// <param name="count">Inserted item count.</param>
        private void RemapActiveForInsertion(int index, int count)
        {
            ExtractActiveSlotsForRemap();
            foreach (var slot in mRemapSlots)
            {
                if (slot.Index >= index)
                {
                    slot.Index += count;
                }

                mActiveSlots.Add(slot.Index, slot);
            }
        }

        /// <summary>
        /// Removes active views in a deleted range and shifts later logical items.
        /// </summary>
        /// <param name="index">First removed index.</param>
        /// <param name="count">Removed item count.</param>
        /// <param name="animate">Whether visible removed items animate before pooling.</param>
        private void RemapActiveForRemoval(int index, int count, bool animate)
        {
            var endIndex = index + count;
            ExtractActiveSlotsForRemap();
            foreach (var slot in mRemapSlots)
            {
                if (slot.Index >= index && slot.Index < endIndex)
                {
                    if (animate)
                    {
                        StartRemovalAnimation(slot);
                    }
                    else
                    {
                        mDataSource.UnbindItem(slot.Item, slot.Index);
                        PoolDetachedSlot(slot);
                    }

                    continue;
                }

                if (slot.Index >= endIndex)
                {
                    slot.Index -= count;
                }

                mActiveSlots.Add(slot.Index, slot);
            }
        }

        /// <summary>
        /// Remaps active logical items after one collection item moves.
        /// </summary>
        /// <param name="oldIndex">Previous item index.</param>
        /// <param name="newIndex">New item index.</param>
        private void RemapActiveForMove(int oldIndex, int newIndex)
        {
            ExtractActiveSlotsForRemap();
            foreach (var slot in mRemapSlots)
            {
                slot.Index = RemapMovedIndex(slot.Index, oldIndex, newIndex);
                mActiveSlots.Add(slot.Index, slot);
            }
        }

        /// <summary>
        /// Extracts every active slot into a reusable remapping buffer.
        /// </summary>
        private void ExtractActiveSlotsForRemap()
        {
            mRemapSlots.Clear();
            foreach (var pair in mActiveSlots)
            {
                mRemapSlots.Add(pair.Value);
            }

            mActiveSlots.Clear();
        }

        /// <summary>
        /// Maps an index through a single collection move.
        /// </summary>
        /// <param name="index">Index to map.</param>
        /// <param name="oldIndex">Previous moved index.</param>
        /// <param name="newIndex">New moved index.</param>
        /// <returns>Mapped index.</returns>
        private static int RemapMovedIndex(int index, int oldIndex, int newIndex)
        {
            if (index == oldIndex)
            {
                return newIndex;
            }

            if (oldIndex < newIndex && index > oldIndex && index <= newIndex)
            {
                return index - 1;
            }

            if (oldIndex > newIndex && index >= newIndex && index < oldIndex)
            {
                return index + 1;
            }

            return index;
        }

        /// <summary>
        /// Clears the one-refresh insertion animation marker.
        /// </summary>
        private void ClearPendingInsertionAnimation()
        {
            mAnimatedInsertFirst = -1;
            mAnimatedInsertEnd   = -1;
        }

        /// <summary>
        /// Configures ScrollRect direction and top-left content coordinates.
        /// </summary>
        private void ConfigureTransforms()
        {
            vertical          = Direction == EVirtualScrollDirection.Vertical;
            horizontal        = Direction == EVirtualScrollDirection.Horizontal;
            mResolvedViewport = viewport ? viewport : transform as RectTransform;
            CaptureAndDisableLayoutGroup();
            vertical          = Direction == EVirtualScrollDirection.Vertical;
            horizontal        = Direction == EVirtualScrollDirection.Horizontal;
            content.anchorMin = Vector2.up;
            content.anchorMax = Vector2.up;
            content.pivot     = Vector2.up;
        }

        /// <summary>
        /// Captures supported content layout parameters once and disables runtime layout rebuilding.
        /// </summary>
        private void CaptureAndDisableLayoutGroup()
        {
            if (mLayoutCapture.IsCompleted || !UseLayoutGroupSettings || !content || !mResolvedViewport)
            {
                return;
            }

            var snapshot = mLayoutCapture.Capture(content, mResolvedViewport.rect.size);
            if (snapshot is null)
            {
                var unsupportedLayoutGroup = mLayoutCapture.UnsupportedLayoutGroup;
                if (unsupportedLayoutGroup)
                {
                    Debug.LogWarning($"VirtualScrollView does not support automatic capture for {unsupportedLayoutGroup.GetType().Name}.", this);
                }

                return;
            }

            mLayoutSnapshot  = snapshot;
            Direction        = snapshot.Direction;
            Spacing          = snapshot.MainSpacing;
            CrossAxisSpacing = snapshot.CrossSpacing;
            CrossAxisCount   = snapshot.CrossAxisCount;
            if (SizeMode == EVirtualScrollSizeMode.Fixed && snapshot.HasFixedMainSize && !OverrideLayoutItemSize)
            {
                FixedMainAxisSize = snapshot.FixedMainSize;
            }
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

            var scrollOffset     = Mathf.Clamp(GetScrollOffset(), 0f, Mathf.Max(0f, GetLayoutTotalSize()));
            var localStartOffset = Mathf.Max(0f, scrollOffset - GetMainStartPadding());
            var localEndOffset   = Mathf.Max(0f, scrollOffset + GetViewportSize() - GetMainStartPadding());
            var sizeIndexVersion = mSizeIndex.Version;
            mSizeIndex.CollectVisibleIndices(localStartOffset, localEndOffset, Overscan, mDesiredIndices);
            if (sizeIndexVersion != mSizeIndex.Version)
            {
                UpdateContentSize();
            }
            mDesiredIndexSet.Clear();
            var first = mSizeIndex.Count;
            var last  = -1;
            foreach (var index in mDesiredIndices)
            {
                mDesiredIndexSet.Add(index);
                first = Mathf.Min(first, index);
                last  = Mathf.Max(last, index);
            }

            if (!forcePosition && IsDesiredSetActive())
            {
                return;
            }

            RecycleOutsideDesiredSet();
            PositionActiveItems();
            foreach (var index in mDesiredIndices)
            {
                if (!mActiveSlots.ContainsKey(index))
                {
                    CreateVisibleSlot(index);
                }
            }

            mFirstVisible = first;
            mLastVisible  = last;
            BringAnimatingRemovalSlotsToFront();
        }

        /// <summary>
        /// Keeps detached removal views above replacement views until their exit animations complete.
        /// </summary>
        private void BringAnimatingRemovalSlotsToFront()
        {
            foreach (var slot in mAnimatingRemovalSlots)
            {
                if (slot.Item)
                {
                    slot.Item.SetAsLastSibling();
                }
            }
        }

        /// <summary>
        /// Creates or reuses and binds one visible slot.
        /// </summary>
        /// <param name="index">Data index.</param>
        private void CreateVisibleSlot(int index)
        {
            var itemType = mDataSource.GetItemType(index);
            var item     = GetPooledItem(itemType);
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
            item.pivot     = Vector2.up;
            item.gameObject.SetActive(true);

            var slot      = mSlotPool.Count > 0 ? mSlotPool.Pop() : new VirtualScrollSlot();
            slot.Item     = item;
            slot.Index    = index;
            slot.ItemType = itemType;
            mActiveSlots.Add(index, slot);
            PositionSlot(slot);
            mDataSource.BindItem(item, index);
            if (index >= mAnimatedInsertFirst && index < mAnimatedInsertEnd)
            {
                StartInsertionAnimation(slot);
            }
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
            var offset             = GetItemMainOffset(slot.Index);
            var size               = mSizeIndex.GetSize(slot.Index);
            var crossAxisCount     = Mathf.Max(1, mSizeIndex.CrossAxisCount);
            var availableCrossSize = Mathf.Max(0.01f, GetViewportCrossAxisSize() - GetCrossStartPadding() - GetCrossEndPadding());
            var crossAxisSize      = mLayoutSnapshot != null && mLayoutSnapshot.HasFixedCrossSize ? mLayoutSnapshot.FixedCrossSize : Mathf.Max(0.01f, (availableCrossSize - (crossAxisCount - 1) * Mathf.Max(0f, CrossAxisSpacing)) / crossAxisCount);
            var occupiedCrossSize  = crossAxisCount * crossAxisSize + (crossAxisCount - 1) * Mathf.Max(0f, CrossAxisSpacing);
            var alignmentOffset    = mLayoutSnapshot is null ? 0f : Mathf.Max(0f, availableCrossSize - occupiedCrossSize) * mLayoutSnapshot.GetCrossAlignmentFactor();
            var crossAxisIndex     = mSizeIndex.GetCrossAxisIndex(slot.Index);
            if (mLayoutSnapshot != null && mLayoutSnapshot.ReverseCrossAxis)
            {
                crossAxisIndex = crossAxisCount - crossAxisIndex - 1;
            }

            var crossOffset = GetCrossStartPadding() + alignmentOffset + crossAxisIndex * (crossAxisSize + Mathf.Max(0f, CrossAxisSpacing));
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                slot.Item.anchoredPosition = new Vector2(crossOffset, -offset);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, crossAxisSize);
            }
            else
            {
                slot.Item.anchoredPosition = new Vector2(offset, -crossOffset);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
                slot.Item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, crossAxisSize);
            }
        }

        /// <summary>
        /// Gets whether every desired index is already active and no extra slot remains.
        /// </summary>
        /// <returns>True when the active set matches the desired set.</returns>
        private bool IsDesiredSetActive()
        {
            if (mDesiredIndexSet.Count != mActiveSlots.Count)
            {
                return false;
            }

            foreach (var index in mDesiredIndices)
            {
                if (!mActiveSlots.ContainsKey(index))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Recycles active slots outside the desired visibility set.
        /// </summary>
        private void RecycleOutsideDesiredSet()
        {
            mRecycleIndices.Clear();
            foreach (var pair in mActiveSlots)
            {
                if (!mDesiredIndexSet.Contains(pair.Key))
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
            mLastVisible  = -1;
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

            CompleteAnimation(slot, true);
            mDataSource?.UnbindItem(slot.Item, index);
            PoolDetachedSlot(slot);
        }

        /// <summary>
        /// Returns an already-unbound detached slot to its typed pool.
        /// </summary>
        /// <param name="slot">Detached slot.</param>
        private void PoolDetachedSlot(VirtualScrollSlot slot)
        {
            CompleteAnimation(slot, true);
            slot.Item.gameObject.SetActive(false);
            if (!mPools.TryGetValue(slot.ItemType, out var pool))
            {
                pool = new Stack<RectTransform>();
                mPools.Add(slot.ItemType, pool);
            }

            pool.Push(slot.Item);
            slot.Item             = null;
            slot.Index            = -1;
            slot.ItemType         = 0;
            slot.CanvasGroup      = null;
            slot.Animation        = null;
            slot.AnimationId      = 0;
            slot.AnimationContext = default;
            slot.IsAnimating      = false;
            slot.AnimationElapsed = 0f;
            slot.RestingScale     = Vector3.one;
            slot.RestingAlpha     = 1f;
            mSlotPool.Push(slot);
        }

        /// <summary>
        /// Starts an entrance animation for a newly materialized or moved item.
        /// </summary>
        /// <param name="slot">Active slot to animate.</param>
        private void StartInsertionAnimation(VirtualScrollSlot slot)
        {
            CompleteAnimation(slot, true);
            BeginAnimation(slot, EVirtualScrollAnimationType.Insert);
        }

        /// <summary>
        /// Starts an exit animation for an already-unbound visible slot.
        /// </summary>
        /// <param name="slot">Detached slot to animate.</param>
        private void StartRemovalAnimation(VirtualScrollSlot slot)
        {
            CompleteAnimation(slot, true);
            BeginAnimation(slot, EVirtualScrollAnimationType.Remove);
        }

        /// <summary>
        /// Gives item presentation ownership to the configured provider or built-in animation.
        /// </summary>
        /// <param name="slot">Slot beginning animation.</param>
        /// <param name="animationType">Collection change being represented.</param>
        private void BeginAnimation(VirtualScrollSlot slot, EVirtualScrollAnimationType animationType)
        {
            var duration          = Mathf.Max(0.01f, ChangeAnimationDuration);
            slot.Animation        = ResolveAnimationProvider(out var useBuiltInAnimation);
            slot.AnimationId      = GetNextAnimationId();
            slot.AnimationContext = new VirtualScrollAnimationContext(slot.Item, animationType, duration, slot.AnimationId, this);
            slot.IsAnimating      = true;
            slot.AnimationElapsed = 0f;
            mAnimatingSlots.Add(slot.AnimationId, slot);
            if (animationType == EVirtualScrollAnimationType.Remove)
            {
                mAnimatingRemovalSlots.Add(slot);
            }

            if (slot.Animation != null)
            {
                slot.Animation.Play(slot.AnimationContext);
                return;
            }

            if (!useBuiltInAnimation)
            {
                CompleteAnimation(slot, false);
                return;
            }

            PrepareAnimatedItem(slot);
            mDefaultAnimationSlots.Add(slot);
        }

        /// <summary>
        /// Gets a nonzero animation identifier unique among active animations.
        /// </summary>
        /// <returns>Unique animation identifier.</returns>
        private int GetNextAnimationId()
        {
            do
            {
                mLastAnimationId++;
                if (mLastAnimationId == 0)
                {
                    mLastAnimationId++;
                }
            }
            while (mAnimatingSlots.ContainsKey(mLastAnimationId));

            return mLastAnimationId;
        }

        /// <summary>
        /// Resolves the runtime or Inspector animation provider outside the scrolling hot path.
        /// </summary>
        /// <param name="useBuiltInAnimation">Whether absence of a provider requests built-in presentation.</param>
        /// <returns>Configured provider, or null when built-in or disabled presentation should be used.</returns>
        private IVirtualScrollAnimation ResolveAnimationProvider(out bool useBuiltInAnimation)
        {
            useBuiltInAnimation = false;
            if (Animation != null)
            {
                return Animation;
            }

            if (!AnimationProvider)
            {
                useBuiltInAnimation = true;
                return null;
            }

            var animation = AnimationProvider as IVirtualScrollAnimation;
            if (animation == null)
            {
                Debug.LogError($"Animation provider {AnimationProvider.GetType().Name} must implement {nameof(IVirtualScrollAnimation)}. Collection animation was skipped.", AnimationProvider);
            }

            return animation;
        }

        /// <summary>
        /// Advances built-in animations and completes them without coroutine allocation.
        /// </summary>
        private void UpdateDefaultAnimations()
        {
            for (var i = mDefaultAnimationSlots.Count - 1; i >= 0; i--)
            {
                var slot = mDefaultAnimationSlots[i];
                slot.AnimationElapsed += Time.unscaledDeltaTime;
                var progress           = Mathf.Clamp01(slot.AnimationElapsed / slot.AnimationContext.Duration);
                EvaluateDefaultAnimation(slot, progress);
                if (progress >= 1f)
                {
                    CompleteAnimation(slot, false);
                }
            }
        }

        /// <summary>
        /// Applies one normalized built-in animation sample.
        /// </summary>
        /// <param name="slot">Slot being animated.</param>
        /// <param name="progress">Normalized progress.</param>
        private static void EvaluateDefaultAnimation(VirtualScrollSlot slot, float progress)
        {
            if (slot.AnimationContext.AnimationType == EVirtualScrollAnimationType.Insert)
            {
                slot.Item.localScale   = Vector3.Lerp(slot.RestingScale * 0.9f, slot.RestingScale, progress);
                slot.CanvasGroup.alpha = Mathf.Lerp(0f, slot.RestingAlpha, progress);
            }
            else
            {
                slot.Item.localScale   = Vector3.Lerp(slot.RestingScale, slot.RestingScale * 0.9f, progress);
                slot.CanvasGroup.alpha = Mathf.Lerp(slot.RestingAlpha, 0f, progress);
            }
        }

        /// <summary>
        /// Ends item presentation ownership and restores a reusable state.
        /// </summary>
        /// <param name="slot">Slot whose animation ended.</param>
        /// <param name="canceled">Whether playback was interrupted.</param>
        private void CompleteAnimation(VirtualScrollSlot slot, bool canceled)
        {
            if (!slot.IsAnimating)
            {
                return;
            }

            var animationType = slot.AnimationContext.AnimationType;
            mAnimatingSlots.Remove(slot.AnimationId);
            mDefaultAnimationSlots.Remove(slot);
            mAnimatingRemovalSlots.Remove(slot);
            if (slot.Animation != null && canceled)
            {
                slot.Animation.Cancel(slot.AnimationContext);
            }
            else if (slot.Animation == null)
            {
                ResetAnimatedItem(slot);
            }

            slot.Animation        = null;
            slot.AnimationId      = 0;
            slot.AnimationContext = default;
            slot.IsAnimating      = false;
            slot.AnimationElapsed = 0f;
            if (!canceled && animationType == EVirtualScrollAnimationType.Remove)
            {
                UnbindAndPoolRemovalSlot(slot);
            }
        }

        /// <summary>
        /// Releases the retained removal presentation and returns its detached view to the pool.
        /// </summary>
        /// <param name="slot">Detached removal slot whose animation has ended.</param>
        private void UnbindAndPoolRemovalSlot(VirtualScrollSlot slot)
        {
            mDataSource?.UnbindItem(slot.Item, slot.Index);
            PoolDetachedSlot(slot);
        }

        /// <summary>
        /// Caches or creates animation state for a slot.
        /// </summary>
        /// <param name="slot">Slot to prepare.</param>
        private static void PrepareAnimatedItem(VirtualScrollSlot slot)
        {
            if (!slot.CanvasGroup)
            {
                slot.CanvasGroup = slot.Item.GetComponent<CanvasGroup>();
                if (!slot.CanvasGroup)
                {
                    slot.CanvasGroup = slot.Item.gameObject.AddComponent<CanvasGroup>();
                }
            }

            slot.RestingScale = slot.Item.localScale;
            slot.RestingAlpha = slot.CanvasGroup.alpha;
        }

        /// <summary>
        /// Restores item presentation after an animation.
        /// </summary>
        /// <param name="slot">Animated slot.</param>
        private static void ResetAnimatedItem(VirtualScrollSlot slot)
        {
            if (!slot.Item)
            {
                return;
            }

            if (slot.CanvasGroup)
            {
                slot.Item.localScale   = slot.RestingScale;
                slot.CanvasGroup.alpha = slot.RestingAlpha;
            }
        }

        /// <summary>
        /// Completes provider-owned playback when its unique identifier is still current.
        /// </summary>
        /// <param name="animationId">Unique animation identifier.</param>
        void IVirtualScrollAnimationCallback.CompleteAnimation(int animationId)
        {
            if (mAnimatingSlots.TryGetValue(animationId, out var slot) && slot.AnimationId == animationId)
            {
                CompleteAnimation(slot, false);
            }
        }

        /// <summary>
        /// Pools every detached removal slot during destruction.
        /// </summary>
        private void RecycleAnimatingRemovalSlots()
        {
            while (mAnimatingRemovalSlots.Count > 0)
            {
                var slot = mAnimatingRemovalSlots[mAnimatingRemovalSlots.Count - 1];
                CompleteAnimation(slot, true);
                UnbindAndPoolRemovalSlot(slot);
            }
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
            var size        = content.sizeDelta;
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                size.x = mResolvedViewport.rect.width;
                size.y = Mathf.Max(mResolvedViewport.rect.height, GetLayoutTotalSize());
            }
            else
            {
                size.x = Mathf.Max(mResolvedViewport.rect.width, GetLayoutTotalSize());
                size.y = mResolvedViewport.rect.height;
            }

            content.sizeDelta = size;
            mUpdatingLayout   = false;
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
        /// Gets the viewport size perpendicular to the scrolling axis.
        /// </summary>
        /// <returns>Cross-axis viewport size.</returns>
        private float GetViewportCrossAxisSize()
        {
            if (!mResolvedViewport)
            {
                return 0f;
            }

            return Direction == EVirtualScrollDirection.Vertical ? mResolvedViewport.rect.width : mResolvedViewport.rect.height;
        }

        /// <summary>
        /// Gets an item's main-axis offset including captured leading padding.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Main-axis content offset.</returns>
        private float GetItemMainOffset(int index)
        {
            return GetMainStartPadding() + mSizeIndex.GetOffset(index);
        }

        /// <summary>
        /// Gets total content size including captured leading and trailing padding.
        /// </summary>
        /// <returns>Total main-axis layout size.</returns>
        private float GetLayoutTotalSize()
        {
            return GetMainStartPadding() + mSizeIndex.TotalSize + GetMainEndPadding();
        }

        /// <summary>
        /// Gets captured leading main-axis padding.
        /// </summary>
        /// <returns>Leading padding.</returns>
        private float GetMainStartPadding()
        {
            return mLayoutSnapshot?.MainStartPadding ?? 0f;
        }

        /// <summary>
        /// Gets captured trailing main-axis padding.
        /// </summary>
        /// <returns>Trailing padding.</returns>
        private float GetMainEndPadding()
        {
            return mLayoutSnapshot?.MainEndPadding ?? 0f;
        }

        /// <summary>
        /// Gets captured leading cross-axis padding.
        /// </summary>
        /// <returns>Leading cross-axis padding.</returns>
        private float GetCrossStartPadding()
        {
            return mLayoutSnapshot?.CrossStartPadding ?? 0f;
        }

        /// <summary>
        /// Gets captured trailing cross-axis padding.
        /// </summary>
        /// <returns>Trailing cross-axis padding.</returns>
        private float GetCrossEndPadding()
        {
            return mLayoutSnapshot?.CrossEndPadding ?? 0f;
        }

        /// <summary>
        /// Gets the greatest legal main-axis scroll offset.
        /// </summary>
        /// <returns>Maximum scroll offset.</returns>
        private float GetMaxScrollOffset()
        {
            return mSizeIndex is null ? 0f : Mathf.Max(0f, GetLayoutTotalSize() - GetViewportSize());
        }
    }
}
