//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Defines how scrolling position is handled when list data changes.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Defines the scroll-position behavior applied during a data update.
    /// </summary>
    public enum EVirtualScrollPositionMode
    {
        /// <summary>
        /// Returns to the beginning of the list.
        /// </summary>
        Reset,

        /// <summary>
        /// Preserves the current numeric content offset.
        /// </summary>
        KeepOffset,

        /// <summary>
        /// Preserves the first visible data item and its viewport-relative offset.
        /// </summary>
        KeepAnchor,

        /// <summary>
        /// Keeps the list pinned to its end.
        /// </summary>
        StickToEnd
    }
}
