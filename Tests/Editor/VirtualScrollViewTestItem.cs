//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-29
// Desc: Wraps a RectTransform for virtual scroll component tests.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinOrg.VirtualScroll.Tests
{
    /// <summary>
    /// Provides the reusable item contract for a generated test view.
    /// </summary>
    internal sealed class VirtualScrollViewTestItem : IVirtualScrollItem
    {
        /// <summary>
        /// Initializes a wrapper around a generated test transform.
        /// </summary>
        /// <param name="itemTransform">Test item transform.</param>
        public VirtualScrollViewTestItem(RectTransform itemTransform)
        {
            Transform = itemTransform;
        }

        /// <summary>
        /// Gets the RectTransform controlled by the virtual scroll view.
        /// </summary>
        public RectTransform Transform { get; }
    }
}
