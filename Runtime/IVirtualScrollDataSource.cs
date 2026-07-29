//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Defines data binding and dynamic item creation for a virtual scroll view.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Supplies data, sizes, and item instances to a <see cref="VirtualScrollView"/>.
    /// </summary>
    public interface IVirtualScrollDataSource
    {
        /// <summary>
        /// Gets the number of data items.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the pool type for an item index.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Stable item type identifier.</returns>
        int GetItemType(int index);

        /// <summary>
        /// Gets the main-axis size for an item.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Item height for vertical lists or width for horizontal lists.</returns>
        float GetItemSize(int index);

        /// <summary>
        /// Creates an item instance for a pool type.
        /// </summary>
        /// <param name="itemType">Pool type identifier returned by <see cref="GetItemType"/>.</param>
        /// <param name="parent">Content transform that owns the item.</param>
        /// <returns>Created reusable item.</returns>
        IVirtualScrollItem CreateItem(int itemType, Transform parent);

        /// <summary>
        /// Binds an item instance to a data index.
        /// </summary>
        /// <param name="item">Reusable item instance.</param>
        /// <param name="index">Data index.</param>
        void BindItem(IVirtualScrollItem item, int index);

        /// <summary>
        /// Clears transient state before an item returns to its pool.
        /// </summary>
        /// <param name="item">Reusable item instance.</param>
        /// <param name="index">Previous data index.</param>
        void UnbindItem(IVirtualScrollItem item, int index);
    }
}
