//---------------------------------------------------------------------------------------
// Copyright (c) 2026 Tristin Wen
// Author: Tristin Wen
// E-Mail: Tristin_Wen@outlook.com
// Date: 2026-07-27
// Desc: Verifies VirtualScrollView refresh positioning and collection anchoring.
//---------------------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;

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
        /// Creates a vertical VirtualScrollView with a 300-by-300 viewport.
        /// </summary>
        /// <returns>Configured scroll view.</returns>
        private VirtualScrollView CreateScrollView()
        {
            mRoot = new GameObject("Virtual Scroll Test", typeof(RectTransform), typeof(VirtualScrollView));
            var rootRect = mRoot.transform as RectTransform;
            rootRect.sizeDelta = new Vector2(300f, 300f);
            var viewportObject = new GameObject("Viewport", typeof(RectTransform));
            var viewport = viewportObject.transform as RectTransform;
            viewport.SetParent(rootRect, false);
            viewport.sizeDelta = new Vector2(300f, 300f);
            var contentObject = new GameObject("Content", typeof(RectTransform));
            var content = contentObject.transform as RectTransform;
            content.SetParent(viewport, false);
            var scrollView = mRoot.GetComponent<VirtualScrollView>();
            scrollView.viewport = viewport;
            scrollView.content = content;
            scrollView.FixedItemSize = 50f;
            scrollView.SizeMode = EVirtualScrollSizeMode.Fixed;
            scrollView.Overscan = 1;
            return scrollView;
        }
    }
}
