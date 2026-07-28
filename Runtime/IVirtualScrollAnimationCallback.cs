//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-07-28
// Desc: Receives allocation-free completion signals from animation providers.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Receives completion for one uniquely identified animation.
    /// </summary>
    internal interface IVirtualScrollAnimationCallback
    {
        /// <summary>
        /// Completes an animation when its identifier still owns the item.
        /// </summary>
        /// <param name="animationId">Unique animation identifier.</param>
        void CompleteAnimation(int animationId);
    }
}
