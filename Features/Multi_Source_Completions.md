# Multi-Source Completions (0.3.0)

Status: In progress
Canonical design doc: `breitreiter/nb:Features/UglyPrompt_Multi_Source_Completions.md`

## Context

UglyPrompt was just extracted from `breitreiter/nb` into its own repo (April
2026) to ship as an independent NuGet package. History was not preserved —
the library is only ~a week old, no institutional knowledge to carry over.

The first work happening in the standalone repo is a breaking 0.3.0:
generalize the completion-hint system away from the hardcoded `Commands`/`/`
and `Kits`/`+` properties into a single list of `CompletionSource`s with
configurable triggers and anchor rules (line-start vs word-start). This
unblocks `@`-mention file completion in nb without leaking nb concepts
("kits") into the library API.

Read the canonical design doc in nb for the full proposal, API shape,
behavior details, and migration notes. This file is the implementation
tracker — it's deletable once 0.3.0 ships.

## Done

- Repo bootstrapped with import of UglyPrompt 0.2.1 from nb (no history).
- `GridConsole` extracted into `UglyPrompt.Tests/Testing/` with
  `Snapshot()`, `RowAt`, `CellAt`, `Resize`, ANSI-CSI skipping, optional
  cursor marker. Old `FakeConsole` deleted.
- `LineEditor` takes an `IConsoleAdapter` via internal constructor; all
  rendering routes through it. `IConsoleAdapter` gained `WindowWidth` and
  `WriteLine`.
- `Verify.Xunit` added; xunit toolchain bumped to 2.9.x to resolve
  version conflicts.
- Continuation-line history suppression: backslash-continued input no
  longer responds to Up-arrow (would have replaced the second line with a
  past entry, stranding the first).
- Version bumped to 0.2.2 to mark the test-infrastructure work as a
  non-breaking checkpoint before the API change.

## Next

The remaining sequence inside this repo (no NuGet push until everything
below is green):

1. **Sources API.** Add `TriggerAnchor` enum (`LineStart`, `WordStart`),
   `CompletionSource` record (`char Trigger`, anchor, `Func<string,
   IReadOnlyList<CompletionHint>> Lookup`), and `LineEditor.Sources`.
   Remove `Commands` and `Kits` outright — no `[Obsolete]` shims.
2. **`KeyHandler.CursorPosition`.** Expose the cursor offset so
   `LineEditor` can do word-start resolution.
3. **`RefreshHint` rewrite.** Walk back from cursor through the text;
   first trigger char that satisfies its anchor wins. Pass the body
   (substring after the trigger up to the cursor) to `Lookup`. The
   prefix-filter logic that lived inside `RefreshHint` moves into the
   caller-supplied `Lookup`.
4. **Snapshot tests** for: line-start triggers, word-start triggers,
   typing past a candidate, hint clearing on token loss, soft-wrap
   interactions with the hint strip. Use `GridConsole.Snapshot()` +
   `Verify.Xunit`.
5. **`UglyPrompt.Demo` project.** Tiny console app wiring all three
   trigger types pre-configured (`/`, `+`, `@`) for cross-terminal
   smoke-testing and as a runnable README example.
6. **0.3.0 bump + README rewrite** to reflect the new API. Once green
   here and through the manual cross-terminal matrix, `dotnet pack` and
   `dotnet nuget push`.

## Out of scope for 0.3.0

Per the canonical doc:

- Tab-to-accept (active completion). Hints stay display-only.
- Per-source rendering style. All sources share the ambient strip.
- Fuzzy matching. `StartsWith` remains the filter — fuzzy is a `Lookup`
  implementation choice if a consumer wants it.

## Future considerations (post-0.3.0)

`Lookup` is `Func<string, IReadOnlyList<CompletionHint>>` — synchronous,
runs on every keystroke. Cheap for static lists; blocks the keystroke
loop for sources that need real I/O (large-cwd file enumeration, network
backends). The obvious next extension is a non-breaking async overload —
`Func<string, CancellationToken, ValueTask<IReadOnlyList<CompletionHint>>>`,
auto-cancelled when the next keystroke arrives, plus an optional
`PlaceholderText` ("Searching…") rendered while the task is pending.
Sync `Lookup` stays the simple case; consumers opt into async only when
they need it. Flagged here so we don't accidentally close the door in
0.3.0; not designed in detail.

A second pre-existing issue surfaced during 0.3.0 snapshot test work:
when typed input wraps to a new row, the hint strip rendered on the
old row gets partially overwritten by the wrap content (rendering
`moreample` instead of clean `more` + clean hint below). Root cause is
the hint-render optimization skipping re-render when content is
unchanged across cursor-row changes; clean fix requires coordination
between `KeyHandler`'s wrap path and `LineEditor`'s hint state. Out of
scope for 0.3.0; soft-wrap snapshot test deferred until the underlying
fix lands.

## nb's role

nb has been migrated to PackageReference `UglyPrompt 0.2.2` (in-tree
copy deleted). `Program.cs` still uses the 0.2.x `Commands` / `Kits`
API. When 0.3.0 ships, nb's adoption is a version bump in `nb.csproj`
plus a `Program.cs` edit to swap those properties for `AddSource(...)`
registrations.
