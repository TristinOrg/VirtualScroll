//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Stores the runtime binding state of one visible virtual item.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Stores a visible item's view, index, and pool type.
    /// </summary>
    internal sealed class VirtualScrollSlot
    {
        /// <summary>
        /// Bound view instance.
        /// </summary>
        public RectTransform Item;

        /// <summary>
        /// Bound data index.
        /// </summary>
        public int Index;

        /// <summary>
        /// Pool type identifier.
        /// </summary>
        public int ItemType;
    }
}
