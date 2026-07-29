//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Demonstrates runtime item creation for fixed-size and variable-size lists.
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace TristinWen.VirtualScroll.Sample
{
    /// <summary>
    /// Creates and binds simple text items without requiring a prefab.
    /// </summary>
    public sealed class RuntimeListExample : MonoBehaviour, IVirtualScrollDataSource
    {
        /// <summary>
        /// Virtual list configured in the scene.
        /// </summary>
        public VirtualScrollView ScrollView;

        /// <summary>
        /// Optional sample provider assigned to the scroll view at startup.
        /// </summary>
        public MonoBehaviour AnimationProvider;

        /// <summary>
        /// Duration used by the sample collection animation.
        /// </summary>
        [Min(0.01f)]
        public float AnimationDuration = 0.6f;

        /// <summary>
        /// Number of generated data items.
        /// </summary>
        [Min(0)]
        public int ItemCount = 100000;

        /// <summary>
        /// Item index used by the Scroll To Item context-menu action.
        /// </summary>
        [Min(0)]
        public int TargetIndex = 500;

        /// <summary>
        /// Viewport alignment used by the Scroll To Item context-menu action.
        /// </summary>
        public EVirtualScrollAlignment TargetAlignment = EVirtualScrollAlignment.Center;

        /// <summary>
        /// Cached built-in font shared by generated labels.
        /// </summary>
        private static Font sRuntimeFont;

        /// <summary>
        /// Gets the generated item count.
        /// </summary>
        public int Count => ItemCount;

        /// <summary>
        /// Connects this sample data source after the scene starts.
        /// </summary>
        private void Start()
        {
            if (!ScrollView)
            {
                return;
            }

            ScrollView.AnimateChanges          = true;
            ScrollView.ChangeAnimationDuration = AnimationDuration;
            if (!ScrollView.AnimationProvider && AnimationProvider)
            {
                ScrollView.AnimationProvider = AnimationProvider;
            }
            else if (!ScrollView.AnimationProvider)
            {
                var defaultAnimation = GetComponent<SlideListAnimation>();
                if (!defaultAnimation)
                {
                    defaultAnimation = gameObject.AddComponent<SlideListAnimation>();
                }

                ScrollView.AnimationProvider = defaultAnimation;
            }

            ScrollView.SetDataSource(this);
        }

        /// <summary>
        /// Inserts one item at the first visible index so its entrance animation can be observed.
        /// </summary>
        [ContextMenu("Insert Visible Item")]
        public void InsertVisibleItem()
        {
            if (!Application.isPlaying || !ScrollView)
            {
                return;
            }

            var index = Mathf.Max(0, ScrollView.FirstViewportIndex);
            ItemCount++;
            ScrollView.NotifyItemsInserted(index, 1, EVirtualScrollPositionMode.KeepOffset);
        }

        /// <summary>
        /// Removes the first visible item and keeps its view alive until its exit animation completes.
        /// </summary>
        [ContextMenu("Remove Visible Item")]
        public void RemoveVisibleItem()
        {
            if (!Application.isPlaying || !ScrollView || ItemCount <= 0)
            {
                return;
            }

            var index = Mathf.Clamp(ScrollView.FirstViewportIndex, 0, ItemCount - 1);
            ItemCount--;
            ScrollView.NotifyItemsRemoved(index, 1, EVirtualScrollPositionMode.KeepOffset);
        }

        /// <summary>
        /// Positions the configured target item inside the viewport.
        /// </summary>
        [ContextMenu("Scroll To Item")]
        public void ScrollToItem()
        {
            if (!Application.isPlaying || !ScrollView || ItemCount <= 0)
            {
                return;
            }

            var index = Mathf.Clamp(TargetIndex, 0, ItemCount - 1);
            ScrollView.ScrollToIndex(index, TargetAlignment);
        }

        /// <summary>
        /// Gets the single sample item type.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Zero.</returns>
        public int GetItemType(int index)
        {
            return 0;
        }

        /// <summary>
        /// Gets a deterministic sample height.
        /// </summary>
        /// <param name="index">Data index.</param>
        /// <returns>Sample item size.</returns>
        public float GetItemSize(int index)
        {
            return 56f + index % 5 * 18f;
        }

        /// <summary>
        /// Creates a simple text item at runtime.
        /// </summary>
        /// <param name="itemType">Item type.</param>
        /// <param name="parent">Content transform.</param>
        /// <returns>Created reusable item.</returns>
        public IVirtualScrollItem CreateItem(int itemType, Transform parent)
        {
            var itemObject = new GameObject("Virtual Item", typeof(RectTransform), typeof(Image), typeof(RuntimeListItem));
            var item       = itemObject.transform as RectTransform;
            var itemView   = itemObject.GetComponent<RuntimeListItem>();
            item.SetParent(parent, false);
            var image       = itemObject.GetComponent<Image>();
            image.color     = new Color(0.12f, 0.14f, 0.18f, 1f);
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect   = labelObject.transform as RectTransform;
            labelRect.SetParent(item, false);
            labelRect.anchorMin      = Vector2.zero;
            labelRect.anchorMax      = Vector2.one;
            labelRect.offsetMin      = new Vector2(12f, 0f);
            labelRect.offsetMax      = new Vector2(-12f, 0f);
            var label                = labelObject.GetComponent<Text>();
            label.font               = GetRuntimeFont();
            label.fontSize           = 22;
            label.color              = Color.white;
            label.alignment          = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemView.Label           = label;
            return itemView;
        }

        /// <summary>
        /// Gets and caches Unity's built-in runtime font.
        /// </summary>
        /// <returns>Font suitable for a runtime uGUI Text component.</returns>
        private static Font GetRuntimeFont()
        {
            if (sRuntimeFont)
            {
                return sRuntimeFont;
            }

#if UNITY_2022_2_OR_NEWER
            sRuntimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            sRuntimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            return sRuntimeFont;
        }

        /// <summary>
        /// Binds generated text to a recycled item.
        /// </summary>
        /// <param name="item">Recycled item.</param>
        /// <param name="index">Data index.</param>
        public void BindItem(IVirtualScrollItem item, int index)
        {
            var label  = ((RuntimeListItem)item).Label;
            label.text = $"  Item {index:N0} — variable content line {index % 5 + 1}";
        }

        /// <summary>
        /// Clears the generated label before pooling.
        /// </summary>
        /// <param name="item">Recycled item.</param>
        /// <param name="index">Previous data index.</param>
        public void UnbindItem(IVirtualScrollItem item, int index)
        {
            var label  = ((RuntimeListItem)item).Label;
            label.text = string.Empty;
        }
    }
}
