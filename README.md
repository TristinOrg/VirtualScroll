# Virtual Scroll

`Virtual Scroll` is a high-performance virtualized list for Unity uGUI. It materializes only the items near the viewport and supports both fixed-size and variable-size content.

## Features

- Vertical and horizontal lists.
- O(1) visible-index lookup for fixed-size items.
- O(log N) lookup and size updates for variable-size items using a Fenwick tree.
- Fixed-size multi-lane grids and variable-size masonry layouts.
- Automatic runtime capture for `VerticalLayoutGroup`, `HorizontalLayoutGroup`, and `GridLayoutGroup`.
- Independent uniform main-axis and cross-axis spacing.
- Typed item pools for lists with multiple visual templates.
- Runtime item creation through a small data-source interface.
- No LINQ or per-scroll-frame collection allocation in the virtualization path.
- Reset, numeric-offset, data-anchor, and end-pinned refresh strategies.
- Incremental insertion, removal, move, range refresh, scroll-to-index, and measured-size update APIs.
- Optional insertion and removal animations using unscaled time.
- Scroll-anchor preservation when a variable item changes size.

## Requirements

- Unity 2021.3 or newer.
- uGUI (`com.unity.ugui`).

## Installation

Open **Window > Package Manager**, select **Add package from git URL**, and enter:

```text
https://github.com/TristinOrg/VirtualScroll.git
```

You can pin a release tag:

```text
https://github.com/TristinOrg/VirtualScroll.git#v1.0.0
```

## Setup

1. Create a normal uGUI `Scroll View`.
2. Replace its `ScrollRect` component with `VirtualScrollView`.
3. Keep the content reference assigned. Viewport may be assigned explicitly or left empty to use the component RectTransform.
4. A supported `LayoutGroup` may remain on the content for familiar authoring. Its parameters are captured and the component is disabled at runtime. A matching `ContentSizeFitter` is also disabled while virtual layout owns content size.
5. Implement `IVirtualScrollDataSource` and call `SetDataSource` after your data is ready.

Initialization can explicitly select its starting position behavior:

```csharp
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.Reset);
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.KeepOffset);
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.KeepAnchor);
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.StickToEnd);
```

```csharp
using TristinWen.VirtualScroll;
using UnityEngine;

public sealed class MailListPresenter : MonoBehaviour, IVirtualScrollDataSource
{
    public VirtualScrollView ScrollView;
    public RectTransform ItemPrefab;

    public int Count => 10000;

    private void Start()
    {
        ScrollView.SizeMode = EVirtualScrollSizeMode.Variable;
        ScrollView.Spacing = 8f;
        ScrollView.SetDataSource(this);
    }

    public int GetItemType(int index)
    {
        return 0;
    }

    public float GetItemSize(int index)
    {
        return 72f + index % 4 * 24f;
    }

    public RectTransform CreateItem(int itemType, Transform parent)
    {
        return Instantiate(ItemPrefab, parent);
    }

    public void BindItem(RectTransform item, int index)
    {
        item.name = $"Mail {index}";
    }

    public void UnbindItem(RectTransform item, int index)
    {
    }
}
```

## Variable-height content

When item height is known from the model, return it from `GetItemSize`. This is the fastest path.

When text or asynchronous content determines the final height:

1. Return a stable estimated height from `GetItemSize`.
2. Bind and lay out the visible item.
3. Measure its final height.
4. Call `NotifyItemSizeChanged(index, measuredHeight)` only when the value actually changes.

The size index updates in O(log N), and the list preserves the first visible item's viewport position.

## Main API

```csharp
ScrollView.ReloadData();
ScrollView.ReloadData(EVirtualScrollPositionMode.Reset);
ScrollView.ReloadData(EVirtualScrollPositionMode.KeepAnchor);
ScrollView.ReloadData(EVirtualScrollPositionMode.StickToEnd);
ScrollView.RefreshItem(index);
ScrollView.RefreshRange(startIndex, count);
ScrollView.NotifyItemSizeChanged(index, newSize);
ScrollView.ScrollToIndex(index, EVirtualScrollAlignment.Center);
```

The legacy convenience overload remains available:

```csharp
ScrollView.ReloadData(true);  // Keep the numeric content offset.
ScrollView.ReloadData(false); // Reset to the beginning.
```

## Collection changes

Mutate your backing collection first, then notify the scroll view with the same indices and counts:

```csharp
Mails.InsertRange(index, incomingMails);
ScrollView.NotifyItemsInserted(index, incomingMails.Count);

Mails.RemoveRange(index, count);
ScrollView.NotifyItemsRemoved(index, count);

var movedMail = Mails[oldIndex];
Mails.RemoveAt(oldIndex);
Mails.Insert(newIndex, movedMail);
ScrollView.NotifyItemMoved(oldIndex, newIndex);
```

Visible views representing unchanged logical items are remapped and retained. `KeepAnchor` is the default for collection changes, so inserting older mail above the viewport does not move the reader's current mail.

Set `AnimateChanges` to enable opacity and scale animations for visible insertions and removals. Animation is optional and does not run in the normal scrolling path.

## Grid and masonry layouts

Set `CrossAxisCount` above one:

- `SizeMode.Fixed` creates an equal-size grid.
- `SizeMode.Variable` assigns equal-width items to the currently shortest lane, producing a masonry layout.
- `Spacing` controls distance along the scrolling axis.
- `CrossAxisSpacing` controls distance between lanes.

## LayoutGroup authoring

With `UseLayoutGroupSettings` enabled (the default), `VirtualScrollView` captures these values from the content at initialization and then disables the source component to avoid continuous layout rebuilding:

- `VerticalLayoutGroup`: vertical direction, padding, spacing, and alignment.
- `HorizontalLayoutGroup`: horizontal direction, padding, spacing, and alignment.
- `GridLayoutGroup`: direction, padding, spacing, cell size, constraint/count, alignment, start axis, and cross-axis start corner.

For a fixed-size grid, leave `OverrideLayoutItemSize` disabled to use `GridLayoutGroup.cellSize`. Enable it to keep `FixedItemSize`; spacing, padding, lane count, alignment, and cross-axis cell size still come from the GridLayoutGroup.

For variable-size items such as mail content, select `SizeMode.Variable`. `GetItemSize(index)` controls each item's main-axis size while spacing and lane parameters continue to come from the captured LayoutGroup.

Call `RecaptureLayoutGroup()` after changing LayoutGroup parameters at runtime. Destroying `VirtualScrollView` restores the captured LayoutGroup and ContentSizeFitter to their original enabled states.

## Performance model

- Fixed-size offset lookup: O(1).
- Variable-size offset lookup: O(log N).
- Variable-size update: O(log N).
- Scroll work: proportional to items entering or leaving the viewport, not total data count.
- Created GameObjects: proportional to the largest observed visible range plus overscan.

Actual frame time depends on item binding, text generation, shaders, and Canvas topology. Profile representative UI on target hardware before setting budgets.

## Current scope

- Lanes use equal cross-axis widths; variable-width masonry items are not supported.
- Insert and remove animations affect currently materialized items only, by design.
- Data mutations must occur before their matching notification method is called.

## License

MIT. See [LICENSE.md](LICENSE.md).
