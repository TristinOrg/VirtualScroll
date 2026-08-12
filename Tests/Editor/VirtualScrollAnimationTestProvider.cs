//---------------------------------------------------------------------------------------
// Author: Tristin Wen
// Date: 2026-07-28
// Desc: Records virtual-scroll animation callbacks for component tests.
//---------------------------------------------------------------------------------------

namespace TristinOrg.VirtualScroll.Tests
{
    /// <summary>
    /// Records animation lifecycle calls without changing item presentation.
    /// </summary>
    public sealed class VirtualScrollAnimationTestProvider : IVirtualScrollAnimation
    {
        /// <summary>
        /// Presentation position applied when playback begins.
        /// </summary>
        public static readonly UnityEngine.Vector2 PresentationPosition = new(777f, -333f);

        /// <summary>
        /// Gets the number of play callbacks.
        /// </summary>
        public int PlayCount { get; private set; }

        /// <summary>
        /// Gets the number of cancellation callbacks.
        /// </summary>
        public int CancelCount { get; private set; }

        /// <summary>
        /// Gets the most recently started animation type.
        /// </summary>
        public EVirtualScrollAnimationType LastAnimationType { get; private set; }

        /// <summary>
        /// Gets the most recently supplied animation context.
        /// </summary>
        public VirtualScrollAnimationContext LastContext { get; private set; }

        /// <summary>
        /// Gets or sets whether playback changes the item position to verify presentation ownership.
        /// </summary>
        public bool ChangePosition { get; set; }

        /// <summary>
        /// Records provider-owned playback.
        /// </summary>
        /// <param name="context">Animation context.</param>
        public void Play(VirtualScrollAnimationContext context)
        {
            PlayCount++;
            LastAnimationType = context.AnimationType;
            LastContext       = context;
            if (ChangePosition)
            {
                context.Item.anchoredPosition = PresentationPosition;
            }
        }

        /// <summary>
        /// Records provider cancellation.
        /// </summary>
        /// <param name="context">Animation context.</param>
        public void Cancel(VirtualScrollAnimationContext context)
        {
            CancelCount++;
        }

        /// <summary>
        /// Completes the most recently supplied context.
        /// </summary>
        public void CompleteLast()
        {
            LastContext.Complete();
        }
    }
}
