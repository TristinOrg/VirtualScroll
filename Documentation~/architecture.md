# Architecture

`VirtualScrollView` separates three responsibilities:

- `IVirtualScrollDataSource` owns data binding and `IVirtualScrollItem` creation.
- `IVirtualSizeIndex` maps indices to offsets.
- `VirtualScrollView` owns viewport range calculation, positioning, and typed pools.

Fixed-size lists and grids use direct arithmetic. Variable-size single-lane lists use a Fenwick tree so distant jumps and measured-size changes do not scan the full collection. Variable-size multi-lane lists keep lane-local sorted indices so visibility lookup remains proportional to visible items and lane count.

The scrolling hot path first calculates the desired visible range. If that range has not changed, it returns immediately. When it changes, only views outside the range are recycled and only missing indices are materialized.

Collection notifications remap active slot indices before rebuilding layout metadata. This preserves views representing unchanged logical items. Anchor refresh mode stores the first visible logical item and its viewport-relative offset, then restores both after insertion, removal, or movement.

Collection animation interruption and removal pooling remain owned by `VirtualScrollView`. An optional `IVirtualScrollAnimation` component fully owns playback through an immutable context and signals natural completion without package-created closures or coroutines. Unique animation identifiers reject stale completion calls while leaving the normal scrolling hot path unchanged. The built-in animation uses a reusable `LateUpdate` list and unscaled time.

Supported uGUI LayoutGroups are authoring inputs rather than runtime layout engines. Their parameters are captured once, the components are disabled, and the virtual layout applies equivalent padding, spacing, lanes, cell dimensions, alignment, and cross-axis corner ordering without invoking Unity's per-child layout rebuild path.

## Item ownership

The data source creates `IVirtualScrollItem` instances, while the scroll view owns their active and pooled lifetime. Each item exposes one stable root `RectTransform`; the scroll view caches that transform when materializing the item and uses the interface itself for binding and typed pooling. `BindItem` can cast directly to the data source's known view type without repeating `GetComponent`. It must fully derive presentation from the supplied index. `UnbindItem` must remove transient listeners, cancel item-owned asynchronous work, and reset state that should not survive reuse.

## Canvas guidance

Avoid `LayoutGroup` and `ContentSizeFitter` on the virtualized content. Place frequently changing virtual lists on a suitable Canvas boundary when the surrounding screen is expensive to rebuild.
