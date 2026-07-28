//---------------------------------------------------------------------------------------
// Author: Tristin Wen
// Date: 2026-07-28
// Desc: Defines replaceable collection-change animation presentation.
//---------------------------------------------------------------------------------------

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Owns custom item playback while the scroll view retains cancellation and pooling ownership.
    /// </summary>
    public interface IVirtualScrollAnimation
    {
        /// <summary>
        /// Starts provider-owned playback and calls <see cref="VirtualScrollAnimationContext.Complete"/> after natural completion.
        /// </summary>
        /// <param name="context">Immutable animation context that may be retained until completion or cancellation.</param>
        void Play(VirtualScrollAnimationContext context);

        /// <summary>
        /// Cancels playback and immediately restores every presentation property changed by <see cref="Play"/>.
        /// </summary>
        /// <param name="context">The same context previously supplied to <see cref="Play"/>.</param>
        void Cancel(VirtualScrollAnimationContext context);
    }
}
