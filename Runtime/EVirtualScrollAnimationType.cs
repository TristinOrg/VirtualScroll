//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-07-28
// Desc: Identifies a virtual-scroll collection animation phase.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Identifies the collection change represented by an item animation.
    /// </summary>
    public enum EVirtualScrollAnimationType
    {
        /// <summary>
        /// A materialized item is entering after insertion or movement.
        /// </summary>
        Insert,

        /// <summary>
        /// A materialized item is leaving before pooling.
        /// </summary>
        Remove
    }
}
