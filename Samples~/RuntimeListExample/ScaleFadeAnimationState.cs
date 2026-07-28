//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-07-28
// Desc: Stores one active scale-and-fade sample animation without allocating callbacks.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll.Sample
{
    /// <summary>
    /// Stores presentation state for one provider-owned sample animation.
    /// </summary>
    internal struct ScaleFadeAnimationState
    {
        /// <summary>
        /// Animation context supplied by the scroll view.
        /// </summary>
        public VirtualScrollAnimationContext Context;

        /// <summary>
        /// Canvas group changed by the sample animation.
        /// </summary>
        public CanvasGroup CanvasGroup;

        /// <summary>
        /// Item scale restored after completion or cancellation.
        /// </summary>
        public Vector3 RestingScale;

        /// <summary>
        /// Item opacity restored after completion or cancellation.
        /// </summary>
        public float RestingAlpha;

        /// <summary>
        /// Elapsed unscaled animation time.
        /// </summary>
        public float Elapsed;
    }
}
