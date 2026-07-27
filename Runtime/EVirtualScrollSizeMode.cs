//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Defines fixed and variable virtual item sizing modes.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Defines how item sizes are resolved.
    /// </summary>
    public enum EVirtualScrollSizeMode
    {
        /// <summary>
        /// Every item uses the component's fixed size.
        /// </summary>
        Fixed,

        /// <summary>
        /// Each item gets its size from the data source.
        /// </summary>
        Variable
    }
}
