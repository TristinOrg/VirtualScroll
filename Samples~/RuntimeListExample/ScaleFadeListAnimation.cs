//---------------------------------------------------------------------------------------
// Author: Tristin Wen
// Date: 2026-07-28
// Desc: Demonstrates provider-owned insert and remove animations without coroutines.
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace TristinWen.VirtualScroll.Sample
{
    /// <summary>
    /// Plays concurrent scale-and-fade collection animations using an allocation-free update loop.
    /// </summary>
    [AddComponentMenu("UI/Virtual Scroll Scale Fade Animation")]
    public sealed class ScaleFadeListAnimation : MonoBehaviour, IVirtualScrollAnimation
    {
        /// <summary>
        /// Scale multiplier used at the beginning of insertion and end of removal.
        /// </summary>
        [Min(0f)]
        public float CollapsedScale = 0.8f;

        /// <summary>
        /// Active provider-owned animations.
        /// </summary>
        private readonly List<ScaleFadeAnimationState> mStates = new();

        /// <summary>
        /// Captures reusable presentation state and starts playback.
        /// </summary>
        /// <param name="context">Animation context supplied by the scroll view.</param>
        public void Play(VirtualScrollAnimationContext context)
        {
            var canvasGroup = context.Item.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = context.Item.gameObject.AddComponent<CanvasGroup>();
            }

            var state = new ScaleFadeAnimationState
            {
                Context      = context,
                CanvasGroup  = canvasGroup,
                RestingScale = context.Item.localScale,
                RestingAlpha = canvasGroup.alpha
            };
            mStates.Add(state);
            Evaluate(state, 0f);
        }

        /// <summary>
        /// Stops one animation and restores the item before it can be rebound or pooled.
        /// </summary>
        /// <param name="context">The same context previously supplied to <see cref="Play"/>.</param>
        public void Cancel(VirtualScrollAnimationContext context)
        {
            for (var i = mStates.Count - 1; i >= 0; i--)
            {
                var state = mStates[i];
                if (state.Context.AnimationId != context.AnimationId)
                {
                    continue;
                }

                Restore(state);
                mStates.RemoveAt(i);
                return;
            }
        }

        /// <summary>
        /// Advances all sample animations with unscaled time.
        /// </summary>
        private void Update()
        {
            for (var i = mStates.Count - 1; i >= 0; i--)
            {
                var state = mStates[i];
                if (!state.Context.Item)
                {
                    mStates.RemoveAt(i);
                    continue;
                }

                state.Elapsed += Time.unscaledDeltaTime;
                var progress   = Mathf.Clamp01(state.Elapsed / state.Context.Duration);
                var eased      = progress * progress * (3f - 2f * progress);
                Evaluate(state, eased);
                if (progress < 1f)
                {
                    mStates[i] = state;
                    continue;
                }

                Restore(state);
                mStates.RemoveAt(i);
                state.Context.Complete();
            }
        }

        /// <summary>
        /// Restores all items and releases scroll-view ownership when this provider is disabled.
        /// </summary>
        private void OnDisable()
        {
            while (mStates.Count > 0)
            {
                var index = mStates.Count - 1;
                var state = mStates[index];
                Restore(state);
                mStates.RemoveAt(index);
                state.Context.Complete();
            }
        }

        /// <summary>
        /// Applies scale and opacity for one normalized animation sample.
        /// </summary>
        /// <param name="state">Captured animation state.</param>
        /// <param name="progress">Eased normalized progress.</param>
        private void Evaluate(ScaleFadeAnimationState state, float progress)
        {
            var collapsedScale = state.RestingScale * CollapsedScale;
            if (state.Context.AnimationType == EVirtualScrollAnimationType.Insert)
            {
                state.Context.Item.localScale = Vector3.LerpUnclamped(collapsedScale, state.RestingScale, progress);
                state.CanvasGroup.alpha       = Mathf.LerpUnclamped(0f, state.RestingAlpha, progress);
            }
            else
            {
                state.Context.Item.localScale = Vector3.LerpUnclamped(state.RestingScale, collapsedScale, progress);
                state.CanvasGroup.alpha       = Mathf.LerpUnclamped(state.RestingAlpha, 0f, progress);
            }
        }

        /// <summary>
        /// Restores every presentation property changed by the sample.
        /// </summary>
        /// <param name="state">Captured animation state.</param>
        private static void Restore(ScaleFadeAnimationState state)
        {
            if (!state.Context.Item)
            {
                return;
            }

            state.Context.Item.localScale = state.RestingScale;
            if (state.CanvasGroup)
            {
                state.CanvasGroup.alpha = state.RestingAlpha;
            }
        }
    }
}
