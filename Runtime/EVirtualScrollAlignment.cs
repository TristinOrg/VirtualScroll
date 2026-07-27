//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Defines item alignment when scrolling to an index.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Defines where a target item is placed inside the viewport.
    /// </summary>
    public enum EVirtualScrollAlignment
    {
        /// <summary>
        /// Places the item at the viewport start.
        /// </summary>
        Start,

        /// <summary>
        /// Places the item at the viewport center.
        /// </summary>
        Center,

        /// <summary>
        /// Places the item at the viewport end.
        /// </summary>
        End
    }
}
