# Changelog

## [Unreleased]

## [1.1.0] - 2026-07-29

- Changed variable-size indexing to resolve `GetItemSize` lazily near the viewport instead of querying the entire data set during initialization.
- Renamed `FixedItemSize` to `FixedMainAxisSize` with serialized-data migration and a source-compatible obsolete alias.
- Added `EstimatedMainAxisSize` for unmeasured variable items and large-data-set request-count coverage.
- Added explicit reset, offset, anchor, and end-pinned refresh strategies.
- Added incremental insertion, removal, and move notifications that retain visible views.
- Added optional unscaled-time insertion and removal animations.
- Added replaceable `IVirtualScrollAnimation` providers with provider-owned playback and stale-safe completion signaling.
- Added a coroutine-free scale-and-fade sample with directly callable visible insertion and removal actions.
- Added `IVirtualScrollItem` so data sources bind and pool typed item views without repeated component lookups.
- Added a directly callable target-index positioning action to the runtime list sample.
- Added fixed-size multi-lane grids and variable-size masonry layouts.
- Added independent uniform main-axis and cross-axis spacing.
- Added grid and masonry EditMode coverage.
- Added automatic capture and runtime disabling for vertical, horizontal, and grid LayoutGroups.
- Added LayoutGroup padding, alignment, cell size, constraint, corner, and spacing integration.
- Fixed the runtime sample creating mutually exclusive Image and Text components on one GameObject.
- Removed manual sample font setup and use Unity's cached built-in runtime font automatically.
- Extracted LayoutGroup component lifecycle ownership from VirtualScrollView's scrolling responsibilities.

## [1.0.0] - 2026-07-27

- Added fixed-size vertical and horizontal virtualized lists.
- Added variable-size lists backed by a Fenwick tree.
- Added typed item pools and runtime item creation.
- Added item refresh, size notification, scrolling, and anchor preservation APIs.
- Added EditMode tests and a code-driven sample.
