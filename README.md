# Virtual Scroll

`Virtual Scroll` is a high-performance virtualized list for Unity uGUI. It materializes only the items near the viewport and supports both fixed-size and variable-size content.

## Features

- Vertical and horizontal lists.
- O(1) visible-index lookup for fixed-size items.
- O(log N) lookup and size updates for variable-size items using a Fenwick tree.
- Uniform spacing for fixed and variable items.
- Typed item pools for lists with multiple visual templates.
- Runtime item creation through a small data-source interface.
- No LINQ or per-scroll-frame collection allocation in the virtualization path.
- Refresh, range refresh, scroll-to-index, and measured-size update APIs.
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
4. Do not add a `LayoutGroup` or `ContentSizeFitter` to the content. `VirtualScrollView` owns item positions and content size.
5. Implement `IVirtualScrollDataSource` and call `SetDataSource` after your data is ready.

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
ScrollView.RefreshItem(index);
ScrollView.RefreshRange(startIndex, count);
ScrollView.NotifyItemSizeChanged(index, newSize);
ScrollView.ScrollToIndex(index, EVirtualScrollAlignment.Center);
```

## Performance model

- Fixed-size offset lookup: O(1).
- Variable-size offset lookup: O(log N).
- Variable-size update: O(log N).
- Scroll work: proportional to items entering or leaving the viewport, not total data count.
- Created GameObjects: proportional to the largest observed visible range plus overscan.

Actual frame time depends on item binding, text generation, shaders, and Canvas topology. Profile representative UI on target hardware before setting budgets.

## Limitations in 1.0

- The first release supports one-dimensional lists. Variable-size grids and masonry layouts are not included.
- Collection insert/remove operations currently use `ReloadData`; fine-grained collection diffs are planned.
- Animated insertion and removal are not included.

## License

MIT. See [LICENSE.md](LICENSE.md).
