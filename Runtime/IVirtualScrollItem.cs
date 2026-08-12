//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-29
// Desc: Defines the reusable item contract consumed by a virtual scroll view.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinOrg.VirtualScroll
{
    /// <summary>
    /// Exposes the RectTransform used to present and recycle a virtual scroll item.
    /// </summary>
    public interface IVirtualScrollItem
    {
        /// <summary>
        /// Gets the RectTransform controlled by the virtual scroll view.
        /// </summary>
        RectTransform Transform { get; }
    }
}
