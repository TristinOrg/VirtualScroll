//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Defines offset lookup operations used by virtualized layouts.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Maps item indices to main-axis offsets.
    /// </summary>
    internal interface IVirtualSizeIndex
    {
        /// <summary>
        /// Gets the indexed item count.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the total content size.
        /// </summary>
        float TotalSize { get; }

        /// <summary>
        /// Gets the main-axis offset at which an item starts.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Main-axis offset.</returns>
        float GetOffset(int index);

        /// <summary>
        /// Gets an item's main-axis size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Item size without spacing.</returns>
        float GetSize(int index);

        /// <summary>
        /// Finds the item occupying or following an offset.
        /// </summary>
        /// <param name="offset">Main-axis content offset.</param>
        /// <returns>Data index.</returns>
        int FindIndex(float offset);

        /// <summary>
        /// Updates one item's main-axis size.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <param name="size">New item size.</param>
        void UpdateSize(int index, float size);
    }
}
