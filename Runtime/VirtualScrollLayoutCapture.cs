//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Owns the capture, disabling, and restoration lifecycle of content layout components.
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Manages supported content layout components outside the scrolling hot path.
    /// </summary>
    internal sealed class VirtualScrollLayoutCapture
    {
        /// <summary>
        /// Captured source layout group.
        /// </summary>
        private LayoutGroup mLayoutGroup;

        /// <summary>
        /// Original layout group enabled state.
        /// </summary>
        private bool mLayoutGroupWasEnabled;

        /// <summary>
        /// Captured content size fitter.
        /// </summary>
        private ContentSizeFitter mContentSizeFitter;

        /// <summary>
        /// Original content size fitter enabled state.
        /// </summary>
        private bool mContentSizeFitterWasEnabled;

        /// <summary>
        /// Gets whether capture has already been attempted.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// Gets the immutable values used by virtual positioning.
        /// </summary>
        public VirtualScrollLayoutSnapshot Snapshot { get; private set; }

        /// <summary>
        /// Gets an unsupported LayoutGroup found during the last capture attempt.
        /// </summary>
        public LayoutGroup UnsupportedLayoutGroup { get; private set; }

        /// <summary>
        /// Captures and disables supported layout components once.
        /// </summary>
        /// <param name="content">Virtual content transform.</param>
        /// <param name="viewportSize">Current viewport dimensions.</param>
        /// <returns>Captured layout values, or null when no supported group exists.</returns>
        public VirtualScrollLayoutSnapshot Capture(RectTransform content, Vector2 viewportSize)
        {
            if (IsCompleted)
            {
                return Snapshot;
            }

            IsCompleted     = true;
            var layoutGroup = content.GetComponent<LayoutGroup>();
            if (!layoutGroup)
            {
                return null;
            }

            var snapshot = VirtualScrollLayoutSnapshot.Capture(layoutGroup, viewportSize);
            if (snapshot is null)
            {
                UnsupportedLayoutGroup = layoutGroup;
                return null;
            }

            mLayoutGroup           = layoutGroup;
            mLayoutGroupWasEnabled = layoutGroup.enabled;
            Snapshot               = snapshot;
            layoutGroup.enabled    = false;
            mContentSizeFitter     = content.GetComponent<ContentSizeFitter>();
            if (mContentSizeFitter)
            {
                mContentSizeFitterWasEnabled = mContentSizeFitter.enabled;
                mContentSizeFitter.enabled   = false;
            }

            return snapshot;
        }

        /// <summary>
        /// Restores captured components and allows a fresh capture.
        /// </summary>
        public void Reset()
        {
            Restore();
            IsCompleted            = false;
            UnsupportedLayoutGroup = null;
        }

        /// <summary>
        /// Restores captured components to their original enabled states.
        /// </summary>
        public void Restore()
        {
            if (mLayoutGroup)
            {
                mLayoutGroup.enabled = mLayoutGroupWasEnabled;
            }

            if (mContentSizeFitter)
            {
                mContentSizeFitter.enabled = mContentSizeFitterWasEnabled;
            }

            mLayoutGroup       = null;
            mContentSizeFitter = null;
            Snapshot           = null;
        }
    }
}
