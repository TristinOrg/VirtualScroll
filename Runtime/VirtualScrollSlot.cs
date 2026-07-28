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

        /// <summary>
        /// Canvas group cached when change animations are enabled.
        /// </summary>
        public CanvasGroup CanvasGroup;

        /// <summary>
        /// Unique animation identifier used to reject stale provider completion signals.
        /// </summary>
        public int AnimationId;

        /// <summary>
        /// Custom presentation provider that owns the current animation state.
        /// </summary>
        public IVirtualScrollAnimation Animation;

        /// <summary>
        /// Immutable context supplied to the current animation provider.
        /// </summary>
        public VirtualScrollAnimationContext AnimationContext;

        /// <summary>
        /// Whether item presentation is currently owned by an animation.
        /// </summary>
        public bool IsAnimating;

        /// <summary>
        /// Elapsed unscaled time used only by the built-in animation.
        /// </summary>
        public float AnimationElapsed;

        /// <summary>
        /// Item scale restored after change animation.
        /// </summary>
        public Vector3 RestingScale;

        /// <summary>
        /// Canvas-group opacity restored after change animation.
        /// </summary>
        public float RestingAlpha = 1f;
    }
}
