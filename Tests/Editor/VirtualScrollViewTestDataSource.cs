//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Supplies runtime-created RectTransforms to VirtualScrollView component tests.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll.Tests
{
    /// <summary>
    /// Supplies fixed-size runtime items for component-level tests.
    /// </summary>
    internal sealed class VirtualScrollViewTestDataSource : IVirtualScrollDataSource
    {
        /// <summary>
        /// Gets or sets the mutable test item count.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets the number of main-axis size requests.
        /// </summary>
        public int SizeRequestCount { get; private set; }

        /// <summary>
        /// Gets the single test item type.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Zero.</returns>
        public int GetItemType(int index)
        {
            return 0;
        }

        /// <summary>
        /// Gets the fixed test item size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Fifty pixels.</returns>
        public float GetItemSize(int index)
        {
            SizeRequestCount++;
            return 50f;
        }

        /// <summary>
        /// Creates a RectTransform test item.
        /// </summary>
        /// <param name="itemType">Item type.</param>
        /// <param name="parent">Content transform.</param>
        /// <returns>Created RectTransform.</returns>
        public RectTransform CreateItem(int itemType, Transform parent)
        {
            var itemObject = new GameObject("Test Item", typeof(RectTransform));
            var item       = itemObject.transform as RectTransform;
            item.SetParent(parent, false);
            return item;
        }

        /// <summary>
        /// Names an item after its bound index.
        /// </summary>
        /// <param name="item">Test item.</param>
        /// <param name="index">Data index.</param>
        public void BindItem(RectTransform item, int index)
        {
            item.name = $"Test Item {index}";
        }

        /// <summary>
        /// Clears the bound item name.
        /// </summary>
        /// <param name="item">Test item.</param>
        /// <param name="index">Previous data index.</param>
        public void UnbindItem(RectTransform item, int index)
        {
            item.name = "Pooled Test Item";
        }
    }
}
