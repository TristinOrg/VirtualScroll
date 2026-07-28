//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-07-28
// Desc: Stores one active slide, fade, and scale sample animation.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll.Sample
{
    /// <summary>
    /// Stores presentation state owned by one slide animation.
    /// </summary>
    internal struct SlideAnimationState
    {
        /// <summary>
        /// Animation context supplied by the scroll view.
        /// </summary>
        public VirtualScrollAnimationContext Context;

        /// <summary>
        /// Canvas group changed by the animation.
        /// </summary>
        public CanvasGroup CanvasGroup;

        /// <summary>
        /// Item position restored after playback.
        /// </summary>
        public Vector2 RestingPosition;

        /// <summary>
        /// Item scale restored after playback.
        /// </summary>
        public Vector3 RestingScale;

        /// <summary>
        /// Item opacity restored after playback.
        /// </summary>
        public float RestingAlpha;

        /// <summary>
        /// Elapsed unscaled playback time.
        /// </summary>
        public float Elapsed;
    }
}
