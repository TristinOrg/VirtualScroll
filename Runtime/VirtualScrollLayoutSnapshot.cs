//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Captures supported LayoutGroup parameters for allocation-free virtual positioning.
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace TristinWen.VirtualScroll
{
    /// <summary>
    /// Stores the runtime layout values copied from a supported uGUI LayoutGroup.
    /// </summary>
    internal sealed class VirtualScrollLayoutSnapshot
    {
        /// <summary>
        /// Captured scrolling direction.
        /// </summary>
        public EVirtualScrollDirection Direction;

        /// <summary>
        /// Main-axis leading padding.
        /// </summary>
        public float MainStartPadding;

        /// <summary>
        /// Main-axis trailing padding.
        /// </summary>
        public float MainEndPadding;

        /// <summary>
        /// Cross-axis leading padding.
        /// </summary>
        public float CrossStartPadding;

        /// <summary>
        /// Cross-axis trailing padding.
        /// </summary>
        public float CrossEndPadding;

        /// <summary>
        /// Main-axis item spacing.
        /// </summary>
        public float MainSpacing;

        /// <summary>
        /// Cross-axis lane spacing.
        /// </summary>
        public float CrossSpacing;

        /// <summary>
        /// Number of cross-axis lanes.
        /// </summary>
        public int CrossAxisCount = 1;

        /// <summary>
        /// Fixed main-axis cell size supplied by GridLayoutGroup.
        /// </summary>
        public float FixedMainSize;

        /// <summary>
        /// Fixed cross-axis cell size supplied by GridLayoutGroup.
        /// </summary>
        public float FixedCrossSize;

        /// <summary>
        /// Whether GridLayoutGroup supplied a fixed main-axis size.
        /// </summary>
        public bool HasFixedMainSize;

        /// <summary>
        /// Whether GridLayoutGroup supplied a fixed cross-axis size.
        /// </summary>
        public bool HasFixedCrossSize;

        /// <summary>
        /// Whether lane order starts from the far cross-axis edge.
        /// </summary>
        public bool ReverseCrossAxis;

        /// <summary>
        /// Alignment copied from the source LayoutGroup.
        /// </summary>
        public TextAnchor ChildAlignment = TextAnchor.UpperLeft;

        /// <summary>
        /// Captures a supported LayoutGroup.
        /// </summary>
        /// <param name="layoutGroup">Vertical, horizontal, or grid layout group.</param>
        /// <param name="viewportSize">Current viewport size.</param>
        /// <returns>Captured snapshot, or null when the group is unsupported.</returns>
        public static VirtualScrollLayoutSnapshot Capture(LayoutGroup layoutGroup, Vector2 viewportSize)
        {
            if (layoutGroup is GridLayoutGroup gridLayoutGroup)
            {
                return CaptureGrid(gridLayoutGroup, viewportSize);
            }

            if (layoutGroup is VerticalLayoutGroup verticalLayoutGroup)
            {
                return CaptureLinear(verticalLayoutGroup, EVirtualScrollDirection.Vertical);
            }

            if (layoutGroup is HorizontalLayoutGroup horizontalLayoutGroup)
            {
                return CaptureLinear(horizontalLayoutGroup, EVirtualScrollDirection.Horizontal);
            }

            return null;
        }

        /// <summary>
        /// Gets the zero-to-one cross-axis alignment factor.
        /// </summary>
        /// <returns>Zero for near, one half for center, or one for far alignment.</returns>
        public float GetCrossAlignmentFactor()
        {
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                if (ChildAlignment is TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter)
                {
                    return 0.5f;
                }

                if (ChildAlignment is TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight)
                {
                    return 1f;
                }
            }
            else
            {
                if (ChildAlignment is TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight)
                {
                    return 0.5f;
                }

                if (ChildAlignment is TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight)
                {
                    return 1f;
                }
            }

            return 0f;
        }

        /// <summary>
        /// Captures a vertical or horizontal layout group.
        /// </summary>
        /// <param name="layoutGroup">Source layout group.</param>
        /// <param name="direction">Captured direction.</param>
        /// <returns>Captured snapshot.</returns>
        private static VirtualScrollLayoutSnapshot CaptureLinear(HorizontalOrVerticalLayoutGroup layoutGroup, EVirtualScrollDirection direction)
        {
            var snapshot = new VirtualScrollLayoutSnapshot
            {
                Direction      = direction,
                MainSpacing    = layoutGroup.spacing,
                ChildAlignment = layoutGroup.childAlignment
            };
            snapshot.CapturePadding(layoutGroup.padding);
            return snapshot;
        }

        /// <summary>
        /// Captures grid cell, constraint, spacing, padding, corner, and alignment values.
        /// </summary>
        /// <param name="layoutGroup">Source grid layout.</param>
        /// <param name="viewportSize">Current viewport size.</param>
        /// <returns>Captured snapshot.</returns>
        private static VirtualScrollLayoutSnapshot CaptureGrid(GridLayoutGroup layoutGroup, Vector2 viewportSize)
        {
            var direction = layoutGroup.startAxis == GridLayoutGroup.Axis.Horizontal ? EVirtualScrollDirection.Vertical : EVirtualScrollDirection.Horizontal;
            var snapshot  = new VirtualScrollLayoutSnapshot
            {
                Direction         = direction,
                ChildAlignment    = layoutGroup.childAlignment,
                HasFixedMainSize  = true,
                HasFixedCrossSize = true
            };
            if (direction == EVirtualScrollDirection.Vertical)
            {
                snapshot.MainSpacing      = layoutGroup.spacing.y;
                snapshot.CrossSpacing     = layoutGroup.spacing.x;
                snapshot.FixedMainSize    = layoutGroup.cellSize.y;
                snapshot.FixedCrossSize   = layoutGroup.cellSize.x;
                snapshot.ReverseCrossAxis = layoutGroup.startCorner is GridLayoutGroup.Corner.UpperRight or GridLayoutGroup.Corner.LowerRight;
            }
            else
            {
                snapshot.MainSpacing      = layoutGroup.spacing.x;
                snapshot.CrossSpacing     = layoutGroup.spacing.y;
                snapshot.FixedMainSize    = layoutGroup.cellSize.x;
                snapshot.FixedCrossSize   = layoutGroup.cellSize.y;
                snapshot.ReverseCrossAxis = layoutGroup.startCorner is GridLayoutGroup.Corner.LowerLeft or GridLayoutGroup.Corner.LowerRight;
            }

            snapshot.CapturePadding(layoutGroup.padding);
            snapshot.CrossAxisCount = snapshot.CalculateCrossAxisCount(layoutGroup, viewportSize);
            return snapshot;
        }

        /// <summary>
        /// Captures padding according to the selected main axis.
        /// </summary>
        /// <param name="padding">Source padding.</param>
        private void CapturePadding(RectOffset padding)
        {
            if (Direction == EVirtualScrollDirection.Vertical)
            {
                MainStartPadding  = padding.top;
                MainEndPadding    = padding.bottom;
                CrossStartPadding = padding.left;
                CrossEndPadding   = padding.right;
            }
            else
            {
                MainStartPadding  = padding.left;
                MainEndPadding    = padding.right;
                CrossStartPadding = padding.top;
                CrossEndPadding   = padding.bottom;
            }
        }

        /// <summary>
        /// Resolves a fixed or flexible grid lane count.
        /// </summary>
        /// <param name="layoutGroup">Source grid layout.</param>
        /// <param name="viewportSize">Current viewport size.</param>
        /// <returns>Positive lane count.</returns>
        private int CalculateCrossAxisCount(GridLayoutGroup layoutGroup, Vector2 viewportSize)
        {
            if (layoutGroup.constraint != GridLayoutGroup.Constraint.Flexible)
            {
                return Mathf.Max(1, layoutGroup.constraintCount);
            }

            var viewportCrossSize = Direction == EVirtualScrollDirection.Vertical ? viewportSize.x : viewportSize.y;
            var availableSize     = Mathf.Max(0f, viewportCrossSize - CrossStartPadding - CrossEndPadding);
            return Mathf.Max(1, Mathf.FloorToInt((availableSize + CrossSpacing) / Mathf.Max(0.01f, FixedCrossSize + CrossSpacing)));
        }
    }
}
