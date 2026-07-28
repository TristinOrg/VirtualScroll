//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-07-28
// Desc: Demonstrates slide, fade, and scale collection animations without coroutines.
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace TristinWen.VirtualScroll.Sample
{
    /// <summary>
    /// Slides insertions from right to left while fading and scaling item presentation.
    /// </summary>
    [AddComponentMenu("UI/Virtual Scroll Slide Animation")]
    public sealed class SlideListAnimation : MonoBehaviour, IVirtualScrollAnimation
    {
        /// <summary>
        /// Horizontal distance used by insertion and removal playback.
        /// </summary>
        [Min(0f)]
        public float Distance = 160f;

        /// <summary>
        /// Scale multiplier used by collapsed presentation.
        /// </summary>
        [Min(0f)]
        public float CollapsedScale = 0.85f;

        /// <summary>
        /// Active provider-owned animations.
        /// </summary>
        private readonly List<SlideAnimationState> mStates = new();

        /// <summary>
        /// Captures resting presentation and starts playback.
        /// </summary>
        /// <param name="context">Animation context supplied by the scroll view.</param>
        public void Play(VirtualScrollAnimationContext context)
        {
            var canvasGroup = context.Item.GetComponent<CanvasGroup>();
            if (!canvasGroup)
            {
                canvasGroup = context.Item.gameObject.AddComponent<CanvasGroup>();
            }

            var state = new SlideAnimationState
            {
                Context         = context,
                CanvasGroup     = canvasGroup,
                RestingPosition = context.Item.anchoredPosition,
                RestingScale    = context.Item.localScale,
                RestingAlpha    = canvasGroup.alpha
            };
            mStates.Add(state);
            Evaluate(state, 0f);
        }

        /// <summary>
        /// Cancels matching playback and restores reusable presentation.
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
        /// Advances active animations with unscaled time and no coroutine allocations.
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
                var inverse    = 1f - progress;
                var eased      = 1f - inverse * inverse * inverse;
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
        /// Restores active items and releases scroll ownership when disabled.
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
        /// Applies horizontal position, scale, and opacity at one normalized sample.
        /// </summary>
        /// <param name="state">Captured animation state.</param>
        /// <param name="progress">Eased normalized progress.</param>
        private void Evaluate(SlideAnimationState state, float progress)
        {
            var offset         = Vector2.right * Distance;
            var collapsedScale = state.RestingScale * CollapsedScale;
            if (state.Context.AnimationType == EVirtualScrollAnimationType.Insert)
            {
                state.Context.Item.anchoredPosition = Vector2.LerpUnclamped(state.RestingPosition + offset, state.RestingPosition, progress);
                state.Context.Item.localScale       = Vector3.LerpUnclamped(collapsedScale, state.RestingScale, progress);
                state.CanvasGroup.alpha             = Mathf.LerpUnclamped(0f, state.RestingAlpha, progress);
            }
            else
            {
                state.Context.Item.anchoredPosition = Vector2.LerpUnclamped(state.RestingPosition, state.RestingPosition - offset, progress);
                state.Context.Item.localScale       = Vector3.LerpUnclamped(state.RestingScale, collapsedScale, progress);
                state.CanvasGroup.alpha             = Mathf.LerpUnclamped(state.RestingAlpha, 0f, progress);
            }
        }

        /// <summary>
        /// Restores every presentation property changed by the animation.
        /// </summary>
        /// <param name="state">Captured animation state.</param>
        private static void Restore(SlideAnimationState state)
        {
            if (!state.Context.Item)
            {
                return;
            }

            state.Context.Item.anchoredPosition = state.RestingPosition;
            state.Context.Item.localScale       = state.RestingScale;
            if (state.CanvasGroup)
            {
                state.CanvasGroup.alpha = state.RestingAlpha;
            }
        }
    }
}
