# UglyPrompt

A no-frills readline-style console line editor for .NET. Backslash continuation, ambient completion hints, bracketed paste, and history — with no external dependencies. A permissively-licensed alternative to [PrettyPrompt](https://github.com/waf/PrettyPrompt).

The full API reference lives in [`UglyPrompt/README.md`](UglyPrompt/README.md) (it's also the NuGet package readme). This file is a developer-oriented overview of the repo.

## Repo layout

| Path | Purpose |
|------|---------|
| `UglyPrompt/` | The library. Targets `net8.0`. Published to NuGet as `UglyPrompt`. |
| `UglyPrompt.Demo/` | Runnable workbench wiring `/`, `+`, and `@` completion sources. |
| `UglyPrompt.Tests/` | xUnit + Verify snapshot tests. |
| `Features/` | Design notes for in-flight features. |

## Build and run

```
dotnet build
dotnet test
dotnet run --project UglyPrompt.Demo
```

The demo starts a REPL. Type `/`, `+`, or `@` at the start of a line to see hint dispatch in action. Submit `/quit` (or an empty line) to exit.

## Quickstart

```csharp
using UglyPrompt;

var editor = new LineEditor();

editor.AddSource(new CompletionSource('/', TriggerAnchor.LineStart,
    body => MyCommands
        .Where(c => c.Name.StartsWith("/" + body, StringComparison.OrdinalIgnoreCase))
        .ToList()));

while (true)
{
    var line = editor.ReadLine("> ");
    if (line == null) continue;
    if (line.Trim() == "/quit") break;
    // ... handle line
}
```

## Gotchas

A few things that surprise people on first contact:

- **`ReadLine` returns `null` on whitespace-only input.** It does not return `""`. If you want a sentinel for "user pressed Enter," check for `null` — don't compare against the empty string.
- **Backslash continuation is built in.** A line ending in `\` opens a continuation prompt; the value returned to you is a single string with embedded `\n`s. There is no opt-out for this at the moment.
- **`Lookup` runs on every keystroke.** Cheap for static lists; gate heavy I/O behind a body-length threshold or a cache. The callback is synchronous — long lookups block the editor.
- **One source per trigger char.** `AddSource` throws on duplicate trigger registration. To overload a trigger (e.g. multiple kinds of `/`-prefixed thing), merge candidates inside a single `Lookup`.
- **`TriggerAnchor.LineStart` means column 0 only.** `WordStart` fires after whitespace too. Pick the one that matches your trigger's natural placement.
- **The hint renders on the line below the prompt.** That line is reserved while the editor is active and cleared on submit. Avoid writing your own output there mid-edit.
- **Bracketed paste is auto-detected.** Most modern terminals send the sequence; Windows Terminal works, plain `cmd.exe` does not. On terminals without bracketed paste, pasted newlines arrive as Enter keys and are reassembled best-effort.
- **History stores a flattened display form.** Multi-line entries appear in history as `first \ second \ third`. If the user recalls one and submits unchanged, the original embedded newlines are restored before `ReadLine` returns.

## Testing

Tests use [Verify](https://github.com/VerifyTests/Verify) for snapshot assertions of rendered hint output. To accept a snapshot diff after intentionally changing rendering, follow the Verify workflow (the diff tool will be invoked or `*.received.txt` files written next to the `.verified.txt` baselines).

## License

MIT
