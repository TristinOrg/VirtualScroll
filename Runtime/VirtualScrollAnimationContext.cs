//---------------------------------------------------------------------------------------
// Author: Tristin Wen
// Date: 2026-07-28
// Desc: Supplies animation providers with item state and safe completion signaling.
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Describes one provider-owned collection animation without allocating a closure or delegate.
    /// </summary>
    public readonly struct VirtualScrollAnimationContext
    {
        /// <summary>
        /// Completion receiver owned by the scroll view.
        /// </summary>
        private readonly IVirtualScrollAnimationCallback mCallback;

        /// <summary>
        /// Gets the item being animated.
        /// </summary>
        public RectTransform Item { get; }

        /// <summary>
        /// Gets the collection change being represented.
        /// </summary>
        public EVirtualScrollAnimationType AnimationType { get; }

        /// <summary>
        /// Gets the configured animation duration in seconds.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// Gets the unique identifier used to reject stale completion signals.
        /// </summary>
        public int AnimationId { get; }

        /// <summary>
        /// Creates one immutable provider animation context.
        /// </summary>
        /// <param name="item">Item being animated.</param>
        /// <param name="animationType">Collection change being represented.</param>
        /// <param name="duration">Configured duration in seconds.</param>
        /// <param name="animationId">Unique animation identifier.</param>
        /// <param name="callback">Completion receiver.</param>
        internal VirtualScrollAnimationContext(RectTransform item, EVirtualScrollAnimationType animationType, float duration, int animationId, IVirtualScrollAnimationCallback callback)
        {
            Item          = item;
            AnimationType = animationType;
            Duration      = duration;
            AnimationId   = animationId;
            mCallback     = callback;
        }

        /// <summary>
        /// Signals that provider playback completed naturally.
        /// Stale or duplicate calls are ignored by the scroll view.
        /// </summary>
        public void Complete()
        {
            mCallback?.CompleteAnimation(AnimationId);
        }
    }
}
