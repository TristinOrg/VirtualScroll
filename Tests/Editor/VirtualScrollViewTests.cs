//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Verifies VirtualScrollView refresh positioning and collection anchoring.
//---------------------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace TristinWen.VirtualScroll.Tests
{
    /// <summary>
    /// Verifies component-level position behavior using runtime-created UI objects.
    /// </summary>
    public sealed class VirtualScrollViewTests
    {
        /// <summary>
        /// Root GameObject destroyed after every test.
        /// </summary>
        private GameObject mRoot;

        /// <summary>
        /// Destroys runtime-created UI state after every test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            if (mRoot)
            {
                Object.DestroyImmediate(mRoot);
            }
        }

        /// <summary>
        /// Verifies the legacy Boolean overload preserves or resets numeric offset.
        /// </summary>
        [Test]
        public void ReloadDataSupportsKeepAndResetPosition()
        {
            var scrollView = CreateScrollView();
            var dataSource = new VirtualScrollViewTestDataSource { Count = 100 };
            scrollView.SetDataSource(dataSource);
            scrollView.ScrollToIndex(20);
            var expectedOffset = scrollView.content.anchoredPosition.y;

            scrollView.ReloadData(true);
            Assert.AreEqual(expectedOffset, scrollView.content.anchoredPosition.y, 0.001f);

            scrollView.ReloadData(false);
            Assert.AreEqual(0f, scrollView.content.anchoredPosition.y, 0.001f);
        }

        /// <summary>
        /// Verifies reinitializing the same source accepts an explicit position strategy.
        /// </summary>
        [Test]
        public void SetDataSourceSupportsExplicitPositionMode()
        {
            var scrollView = CreateScrollView();
            var dataSource = new VirtualScrollViewTestDataSource { Count = 100 };
            scrollView.SetDataSource(dataSource);
            scrollView.ScrollToIndex(20);

            dataSource.Count = 101;
            scrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.Reset);

            Assert.AreEqual(0f, scrollView.content.anchoredPosition.y, 0.001f);
        }

        /// <summary>
        /// Verifies inserting data above the viewport preserves the same logical anchor item.
        /// </summary>
        [Test]
        public void InsertionKeepsLogicalAnchorPosition()
        {
            var scrollView = CreateScrollView();
            var dataSource = new VirtualScrollViewTestDataSource { Count = 100 };
            scrollView.SetDataSource(dataSource);
            scrollView.ScrollToIndex(20);
            var oldOffset = scrollView.content.anchoredPosition.y;

            dataSource.Count += 5;
            scrollView.NotifyItemsInserted(0, 5);

            Assert.AreEqual(oldOffset + 250f, scrollView.content.anchoredPosition.y, 0.001f);
        }

        /// <summary>
        /// Verifies removing data above the viewport preserves the same logical anchor item.
        /// </summary>
        [Test]
        public void RemovalKeepsLogicalAnchorPosition()
        {
            var scrollView = CreateScrollView();
            var dataSource = new VirtualScrollViewTestDataSource { Count = 100 };
            scrollView.SetDataSource(dataSource);
            scrollView.ScrollToIndex(20);
            var oldOffset = scrollView.content.anchoredPosition.y;

            dataSource.Count -= 5;
            scrollView.NotifyItemsRemoved(0, 5);

            Assert.AreEqual(oldOffset - 250f, scrollView.content.anchoredPosition.y, 0.001f);
        }

        /// <summary>
        /// Verifies moving an earlier item remaps the logical anchor without rebuilding its view.
        /// </summary>
        [Test]
        public void MoveKeepsLogicalAnchorPosition()
        {
            var scrollView = CreateScrollView();
            var dataSource = new VirtualScrollViewTestDataSource { Count = 100 };
            scrollView.SetDataSource(dataSource);
            scrollView.ScrollToIndex(20);
            var oldOffset = scrollView.content.anchoredPosition.y;

            scrollView.NotifyItemMoved(0, 30);

            Assert.AreEqual(oldOffset - 50f, scrollView.content.anchoredPosition.y, 0.001f);
        }

        /// <summary>
        /// Verifies GridLayoutGroup parameters are captured before the component is disabled.
        /// </summary>
        [Test]
        public void GridLayoutGroupIsCapturedDisabledAndRestored()
        {
            var scrollView  = CreateScrollView();
            var layoutGroup = AddGridLayout(scrollView.content);
            var dataSource  = new VirtualScrollViewTestDataSource { Count = 10 };

            scrollView.SetDataSource(dataSource);

            Assert.IsFalse(layoutGroup.enabled);
            Assert.AreEqual(EVirtualScrollDirection.Vertical, scrollView.Direction);
            Assert.AreEqual(60f, scrollView.FixedMainAxisSize);
            Assert.AreEqual(11f, scrollView.Spacing);
            Assert.AreEqual(7f, scrollView.CrossAxisSpacing);
            Assert.AreEqual(2, scrollView.CrossAxisCount);
            Assert.AreEqual(414f, scrollView.content.sizeDelta.y, 0.001f);
            var firstItem = scrollView.content.Find("Test Item 0") as RectTransform;
            Assert.NotNull(firstItem);
            Assert.AreEqual(new Vector2(61.5f, -30f), firstItem.anchoredPosition);
            Assert.AreEqual(new Vector2(80f, 60f), firstItem.rect.size);

            Object.DestroyImmediate(scrollView);
            Assert.IsTrue(layoutGroup.enabled);
        }

        /// <summary>
        /// Verifies custom fixed item size overrides only cell main size while layout spacing remains captured.
        /// </summary>
        [Test]
        public void ItemSizeOverrideKeepsLayoutGroupSpacing()
        {
            var scrollView                    = CreateScrollView();
            var layoutGroup                   = AddGridLayout(scrollView.content);
            var dataSource                    = new VirtualScrollViewTestDataSource { Count = 10 };
            scrollView.OverrideLayoutItemSize = true;
            scrollView.FixedMainAxisSize      = 90f;

            scrollView.SetDataSource(dataSource);

            Assert.IsFalse(layoutGroup.enabled);
            Assert.AreEqual(90f, scrollView.FixedMainAxisSize);
            Assert.AreEqual(11f, scrollView.Spacing);
            Assert.AreEqual(7f, scrollView.CrossAxisSpacing);
            Assert.AreEqual(2, scrollView.CrossAxisCount);
            var firstItem = scrollView.content.Find("Test Item 0") as RectTransform;
            Assert.NotNull(firstItem);
            Assert.AreEqual(90f, firstItem.rect.height, 0.001f);
            Assert.AreEqual(80f, firstItem.rect.width, 0.001f);
        }

        /// <summary>
        /// Verifies adaptive item sizes retain VerticalLayoutGroup spacing and padding.
        /// </summary>
        [Test]
        public void VariableItemSizeKeepsVerticalLayoutGroupSpacing()
        {
            var scrollView                   = CreateScrollView();
            var layoutGroup                  = scrollView.content.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing              = 13f;
            layoutGroup.padding              = new RectOffset(5, 7, 20, 30);
            var dataSource                   = new VirtualScrollViewTestDataSource { Count = 10 };
            scrollView.SizeMode              = EVirtualScrollSizeMode.Variable;
            scrollView.EstimatedMainAxisSize = 50f;

            scrollView.SetDataSource(dataSource);

            Assert.IsFalse(layoutGroup.enabled);
            Assert.AreEqual(13f, scrollView.Spacing);
            Assert.AreEqual(667f, scrollView.content.sizeDelta.y, 0.001f);
            var firstItem  = scrollView.content.Find("Test Item 0") as RectTransform;
            var secondItem = scrollView.content.Find("Test Item 1") as RectTransform;
            Assert.NotNull(firstItem);
            Assert.NotNull(secondItem);
            Assert.AreEqual(new Vector2(5f, -20f), firstItem.anchoredPosition);
            Assert.AreEqual(new Vector2(5f, -83f), secondItem.anchoredPosition);
            Assert.AreEqual(new Vector2(288f, 50f), firstItem.rect.size);
        }

        /// <summary>
        /// Verifies setting a large variable data source only resolves first-screen sizes.
        /// </summary>
        [Test]
        public void LargeVariableDataSourceDoesNotResolveEveryItemSize()
        {
            var scrollView                   = CreateScrollView();
            var dataSource                   = new VirtualScrollViewTestDataSource { Count = 10000 };
            scrollView.SizeMode              = EVirtualScrollSizeMode.Variable;
            scrollView.EstimatedMainAxisSize = 50f;

            scrollView.SetDataSource(dataSource);

            Assert.LessOrEqual(dataSource.SizeRequestCount, 8);
            Assert.Less(scrollView.LastVisibleIndex, 10);
        }

        /// <summary>
        /// Verifies a custom provider receives insert, interruption, and removal lifecycle callbacks.
        /// </summary>
        [Test]
        public void CustomAnimationProviderOwnsCollectionChangePresentation()
        {
            var scrollView                     = CreateScrollView();
            var animationProvider              = new VirtualScrollAnimationTestProvider();
            var dataSource                     = new VirtualScrollViewTestDataSource();
            scrollView.AnimateChanges          = true;
            scrollView.Animation               = animationProvider;
            scrollView.ChangeAnimationDuration = 10f;
            scrollView.SetDataSource(dataSource);

            dataSource.Count = 1;
            scrollView.NotifyItemsInserted(0, 1);

            Assert.AreEqual(1, animationProvider.PlayCount);
            Assert.AreEqual(EVirtualScrollAnimationType.Insert, animationProvider.LastAnimationType);
            var staleContext = animationProvider.LastContext;

            dataSource.Count = 0;
            scrollView.NotifyItemsRemoved(0, 1);

            Assert.AreEqual(2, animationProvider.PlayCount);
            Assert.AreEqual(1, animationProvider.CancelCount);
            Assert.AreEqual(EVirtualScrollAnimationType.Remove, animationProvider.LastAnimationType);
            var removedItem = animationProvider.LastContext.Item;
            Assert.AreEqual("Test Item 0", removedItem.name);

            staleContext.Complete();
            Assert.IsTrue(removedItem.gameObject.activeSelf);
            animationProvider.CompleteLast();

            Assert.AreEqual(1, animationProvider.CancelCount);
            Assert.AreEqual("Pooled Test Item", removedItem.name);
            Assert.IsFalse(removedItem.gameObject.activeSelf);
        }

        /// <summary>
        /// Verifies final visibility layout does not overwrite provider-owned insertion presentation.
        /// </summary>
        [Test]
        public void InsertionLayoutPreservesProviderPosition()
        {
            var scrollView             = CreateScrollView();
            var animationProvider      = new VirtualScrollAnimationTestProvider { ChangePosition = true };
            var dataSource             = new VirtualScrollViewTestDataSource();
            scrollView.AnimateChanges = true;
            scrollView.Animation      = animationProvider;
            scrollView.SetDataSource(dataSource);

            dataSource.Count = 1;
            scrollView.NotifyItemsInserted(0, 1);

            Assert.AreEqual(VirtualScrollAnimationTestProvider.PresentationPosition, animationProvider.LastContext.Item.anchoredPosition);
        }

        /// <summary>
        /// Verifies detached removal views render above replacement views until completion.
        /// </summary>
        [Test]
        public void RemovalAnimationStaysAboveReplacementItems()
        {
            var scrollView             = CreateScrollView();
            var animationProvider      = new VirtualScrollAnimationTestProvider();
            var dataSource             = new VirtualScrollViewTestDataSource { Count = 10 };
            scrollView.AnimateChanges = true;
            scrollView.Animation      = animationProvider;
            scrollView.SetDataSource(dataSource);

            dataSource.Count--;
            scrollView.NotifyItemsRemoved(0, 1, EVirtualScrollPositionMode.KeepOffset);

            var removedItem = animationProvider.LastContext.Item;
            Assert.AreEqual(EVirtualScrollAnimationType.Remove, animationProvider.LastAnimationType);
            Assert.AreEqual(scrollView.content.childCount - 1, removedItem.GetSiblingIndex());
        }

        /// <summary>
        /// Creates a vertical VirtualScrollView with a 300-by-300 viewport.
        /// </summary>
        /// <returns>Configured scroll view.</returns>
        private VirtualScrollView CreateScrollView()
        {
            mRoot              = new GameObject("Virtual Scroll Test", typeof(RectTransform), typeof(VirtualScrollView));
            var rootRect       = mRoot.transform as RectTransform;
            rootRect.sizeDelta = new Vector2(300f, 300f);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform));
            var viewport       = viewportObject.transform as RectTransform;
            viewport.SetParent(rootRect, false);
            viewport.sizeDelta = new Vector2(300f, 300f);
            var contentObject  = new GameObject("Content", typeof(RectTransform));
            var content        = contentObject.transform as RectTransform;
            content.SetParent(viewport, false);
            var scrollView               = mRoot.GetComponent<VirtualScrollView>();
            scrollView.viewport          = viewport;
            scrollView.content           = content;
            scrollView.FixedMainAxisSize = 50f;
            scrollView.SizeMode          = EVirtualScrollSizeMode.Fixed;
            scrollView.Overscan          = 1;
            return scrollView;
        }

        /// <summary>
        /// Adds a representative fixed-column GridLayoutGroup to test content.
        /// </summary>
        /// <param name="content">Content transform.</param>
        /// <returns>Configured layout group.</returns>
        private static GridLayoutGroup AddGridLayout(RectTransform content)
        {
            var layoutGroup             = content.gameObject.AddComponent<GridLayoutGroup>();
            layoutGroup.cellSize        = new Vector2(80f, 60f);
            layoutGroup.spacing         = new Vector2(7f, 11f);
            layoutGroup.padding         = new RectOffset(10, 20, 30, 40);
            layoutGroup.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            layoutGroup.constraintCount = 2;
            layoutGroup.startAxis       = GridLayoutGroup.Axis.Horizontal;
            layoutGroup.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            layoutGroup.childAlignment  = TextAnchor.UpperCenter;
            return layoutGroup;
        }
    }
}
