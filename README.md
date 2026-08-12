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
https://github.com/TristinOrg/VirtualScroll.git#v1.1.0
```

The current `main` branch uses the `TristinOrg.VirtualScroll` namespace and the `com.tristinorg.virtual-scroll` package identifier. Version `1.1.0` remains available under the former namespace for existing projects. When upgrading from `1.x`, replace `TristinWen.VirtualScroll` with `TristinOrg.VirtualScroll` and update any manifest dependency key to `com.tristinorg.virtual-scroll`.

## Setup

1. Create a normal uGUI `Scroll View`.
2. Replace its `ScrollRect` component with `VirtualScrollView`.
3. Keep the content reference assigned. Viewport may be assigned explicitly or left empty to use the component RectTransform.
4. A supported `LayoutGroup` may remain on the content for familiar authoring. Its parameters are captured and the component is disabled at runtime. A matching `ContentSizeFitter` is also disabled while virtual layout owns content size.
5. Add an `IVirtualScrollItem` component to the item prefab, implement `IVirtualScrollDataSource`, and call `SetDataSource` after your data is ready.

Initialization can explicitly select its starting position behavior:

```csharp
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.Reset);
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.KeepOffset);
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.KeepAnchor);
ScrollView.SetDataSource(dataSource, EVirtualScrollPositionMode.StickToEnd);
```

```csharp
using TristinOrg.VirtualScroll;
using UnityEngine;

public sealed class MailListItem : MonoBehaviour, IVirtualScrollItem
{
    public RectTransform Transform => transform as RectTransform;

    public void SetIndex(int index)
    {
        name = $"Mail {index}";
    }

    public void Clear()
    {
        name = "Pooled Mail";
    }
}

public sealed class MailListPresenter : MonoBehaviour, IVirtualScrollDataSource
{
    public VirtualScrollView ScrollView;
    public MailListItem ItemPrefab;

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

    public IVirtualScrollItem CreateItem(int itemType, Transform parent)
    {
        return Instantiate(ItemPrefab, parent);
    }

    public void BindItem(IVirtualScrollItem item, int index)
    {
        ((MailListItem)item).SetIndex(index);
    }

    public void UnbindItem(IVirtualScrollItem item, int index)
    {
        ((MailListItem)item).Clear();
    }
}
```

`CreateItem` returns the component implementing `IVirtualScrollItem`, not its `RectTransform`. `VirtualScrollView` caches both the interface and its `Transform` when the item is materialized. `BindItem` and `UnbindItem` can therefore cast directly to the known view type without calling `GetComponent` on every reuse. The same interface instance is retained in the typed pool and supplied again on the next bind.

`IVirtualScrollItem.Transform` must return the root `RectTransform` that the virtual scroll view is allowed to parent, position, size, activate, and recycle. Keep the returned transform stable for the lifetime of the item.

## Variable-height content

Set `EstimatedMainAxisSize` to a representative item height or width. Variable layouts build their initial offset index from this estimate without calling `GetItemSize` for the entire data set. `GetItemSize(index)` is requested once when an item approaches the viewport, and the total content size is calibrated incrementally.

`FixedMainAxisSize` means height for vertical scrolling and width for horizontal scrolling. The former `FixedItemSize` API remains as an obsolete source-compatible alias, and existing serialized values migrate automatically.

When text or asynchronous content determines the final height:

1. Configure a stable `EstimatedMainAxisSize`.
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
ScrollView.ScrollToIndex(index, EVirtualScrollAlignment.Start);  // Item starts at the viewport leading edge.
ScrollView.ScrollToIndex(index, EVirtualScrollAlignment.Center); // Item is centered in the viewport.
ScrollView.ScrollToIndex(index, EVirtualScrollAlignment.End);    // Item ends at the viewport trailing edge.
```

`ScrollToIndex` clamps the requested index and content offset to valid bounds. Near the beginning or end of the list, the exact visual alignment may therefore be limited by the available scroll range.

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

## Collection animations

Set `AnimateChanges` to enable animations for visible insertions, removals, and moved-item entrances. Leave `AnimationProvider` empty to use the built-in unscaled-time opacity and scale animation.

Animations are attached to collection notifications, not `BindItem`. Always perform operations in this order:

1. Change the backing collection or count.
2. Call the matching `NotifyItemsInserted`, `NotifyItemsRemoved`, or `NotifyItemMoved` method.
3. Let `VirtualScrollView` animate only the affected views that are currently materialized.

Normal scrolling and ordinary item reuse do not start collection animations.

### Try the included example

Import **Runtime List Example** from Package Manager, then configure a GameObject as follows:

1. Add `RuntimeListExample` to the same GameObject that should own the sample animation.
2. Assign the scene's `VirtualScrollView` to `RuntimeListExample.ScrollView`.
3. Leave both provider fields empty to let the example add its default `SlideListAnimation`. `VirtualScrollView.AnimationProvider` has priority when configured; `RuntimeListExample.AnimationProvider` is only a fallback and cannot overwrite the ScrollView field.
4. Enter Play Mode.
5. Open the `RuntimeListExample` component context menu and select **Insert Visible Item**, **Remove Visible Item**, or **Scroll To Item**. Set `TargetIndex` and `TargetAlignment` before using the positioning action. The same public methods can be connected directly to uGUI Button `OnClick` events.

The default provider is in `Samples~/RuntimeListExample/SlideListAnimation.cs`. Insertions move from right to left while fading from transparent to opaque and scaling from `CollapsedScale` to the resting scale. Removals perform the inverse presentation toward the left. The example applies its `AnimationDuration` value (`0.6` seconds by default) to make playback easy to observe. It supports concurrent items, uses no coroutines, clamps each unscaled-time step to `Time.maximumDeltaTime`, and restores position, scale, and opacity before pooled reuse.

`RuntimeListExample.AnimationProvider` accepts any `MonoBehaviour`. The assigned component must implement `IVirtualScrollAnimation`, so the same field can be used with the included sample, PrimeTween, DOTween, Animator, or a project-specific provider.

The example insertion and removal methods deliberately change `ItemCount` before notifying the scroll view:

```csharp
public void InsertVisibleItem()
{
    var index = Mathf.Max(0, ScrollView.FirstViewportIndex);
    ItemCount++;
    ScrollView.NotifyItemsInserted(index, 1, EVirtualScrollPositionMode.KeepOffset);
}

public void RemoveVisibleItem()
{
    var index = Mathf.Clamp(ScrollView.FirstViewportIndex, 0, ItemCount - 1);
    ItemCount--;
    ScrollView.NotifyItemsRemoved(index, 1, EVirtualScrollPositionMode.KeepOffset);
}

public void ScrollToItem()
{
    var index = Mathf.Clamp(TargetIndex, 0, ItemCount - 1);
    ScrollView.ScrollToIndex(index, TargetAlignment);
}
```

`KeepOffset` makes the changed visible position easy to observe in the sample. Production code can keep the default `KeepAnchor`, use `StickToEnd` for chat messages, or choose another position mode independently of animation.

`FirstViewportIndex` excludes overscan and is appropriate for actions that must be visibly demonstrated. `FirstMaterializedIndex` and `LastMaterializedIndex` include overscan and are intended for virtualization diagnostics. The legacy `FirstVisibleIndex` and `LastVisibleIndex` properties retain their materialized semantics for compatibility.

### Implement a custom provider

Implement `IVirtualScrollAnimation` when a project wants DOTween, PrimeTween, Animator, or its own update system. Assign the implementing `MonoBehaviour` to `VirtualScrollView.AnimationProvider` in the Inspector:

If a configured component does not implement `IVirtualScrollAnimation`, the scroll view logs an error and skips that collection animation. It does not silently substitute the built-in scale-and-fade presentation. Leave the provider empty explicitly when the built-in animation is desired.

Before `Play` is called, `VirtualScrollView` has already applied the item's resting layout. While `Play` owns the presentation, the current visibility refresh does not overwrite the provider's insertion position. Removed views retain their existing binding, remain materialized, and render above replacement views until `context.Complete()` is called. Retained items keep their previous positions during removal playback and move into the released space only after the final visible removal completes. `UnbindItem` runs only after the removal animation completes.

```csharp
public sealed class CustomListAnimation : MonoBehaviour, IVirtualScrollAnimation
{
    public void Play(VirtualScrollAnimationContext context)
    {
        // Start playback for context.Item.
        // Use context.AnimationType to select insert or remove presentation.
        // Use context.Duration when the scroll-view duration should be respected.

        // Call exactly once after natural completion.
        context.Complete();
    }

    public void Cancel(VirtualScrollAnimationContext context)
    {
        // Stop the animation identified by context.AnimationId.
        // Restore every changed property immediately.
        // Do not call context.Complete() from this method.
    }
}
```

Code-created providers that are not `MonoBehaviour` instances can instead use the runtime-only property:

```csharp
ScrollView.Animation = customAnimation;
ScrollView.AnimateChanges = true;
```

`VirtualScrollAnimationContext` provides:

- `Item`: the materialized `RectTransform` being animated.
- `AnimationType`: `Insert` or `Remove`.
- `Duration`: `ChangeAnimationDuration`, clamped to a positive value.
- `AnimationId`: a unique identifier for matching concurrent playback and cancellation.
- `Complete()`: signals natural completion. Removed views are returned to the pool only after this call.

### Provider lifecycle rules

- `Play` owns timing and presentation. `VirtualScrollView` does not start a coroutine for a custom provider.
- `Cancel` may run when an item scrolls away, is rebound, another animation replaces it, or the scroll view is destroyed.
- `Cancel` must stop external tweens and restore every modified property so the pool never retains scale, opacity, position, rotation, or material state.
- Call `context.Complete()` after natural completion, including when disabling a provider that still owns active animations. Otherwise removed views must remain detached and cannot return to the pool.
- Do not call `Complete()` from `Cancel`; the scroll view has already ended ownership for that animation.
- Stale and duplicate `Complete()` calls are ignored through `AnimationId`, so a late callback cannot recycle a newly rebound item.
- A provider must support multiple simultaneous contexts when a range is inserted or removed.

Animations affect currently materialized items only. Inserting outside the viewport still updates indices and layout, but creates no animation work.

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

For a fixed-size grid, leave `OverrideLayoutItemSize` disabled to use `GridLayoutGroup.cellSize`. Enable it to keep `FixedMainAxisSize`; spacing, padding, lane count, alignment, and cross-axis cell size still come from the GridLayoutGroup.

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
