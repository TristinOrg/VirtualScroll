//---------------------------------------------------------------------------------------
// Author: Tristin Wen
// Date: 2026-07-28
// Desc: Identifies a virtual-scroll collection animation phase.
//---------------------------------------------------------------------------------------

namespace TristinOrg.VirtualScroll
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
