//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Supplies deterministic variable sizes to virtual size index tests.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll.Tests
{
    /// <summary>
    /// Supplies deterministic item sizes without creating views.
    /// </summary>
    internal sealed class VirtualSizeIndexTestDataSource : IVirtualScrollDataSource
    {
        /// <summary>
        /// Item sizes used by tests.
        /// </summary>
        private readonly float[] mSizes;

        /// <summary>
        /// Initializes the test data source.
        /// </summary>
        /// <param name="sizes">Item sizes.</param>
        public VirtualSizeIndexTestDataSource(float[] sizes)
        {
            mSizes = sizes;
        }

        /// <summary>
        /// Gets the test item count.
        /// </summary>
        public int Count => mSizes.Length;

        /// <summary>
        /// Gets the number of size requests made by an index.
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
        /// Gets a test item size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Configured size.</returns>
        public float GetItemSize(int index)
        {
            SizeRequestCount++;
            return mSizes[index];
        }

        /// <summary>
        /// Throws because size-index tests do not create views.
        /// </summary>
        /// <param name="itemType">Item type.</param>
        /// <param name="parent">Parent transform.</param>
        /// <returns>Never returns.</returns>
        public RectTransform CreateItem(int itemType, Transform parent)
        {
            throw new System.NotSupportedException();
        }

        /// <summary>
        /// Does nothing because size-index tests do not bind views.
        /// </summary>
        /// <param name="item">Item view.</param>
        /// <param name="index">Data index.</param>
        public void BindItem(RectTransform item, int index)
        {
        }

        /// <summary>
        /// Does nothing because size-index tests do not bind views.
        /// </summary>
        /// <param name="item">Item view.</param>
        /// <param name="index">Data index.</param>
        public void UnbindItem(RectTransform item, int index)
        {
        }
    }
}
