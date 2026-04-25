# UglyPrompt

A no-frills readline-style console line editor for .NET. Backslash continuation, ambient completion hints, bracketed paste, and history — with no external dependencies.

A permissively-licensed alternative to [PrettyPrompt](https://github.com/waf/PrettyPrompt).

## Installation

```
dotnet add package UglyPrompt
```

## Usage

```csharp
var editor = new LineEditor();

while (true)
{
    string? input = editor.ReadLine(">> ");
    if (input == null) continue; // whitespace-only input
    Console.WriteLine(input);
}
```

`ReadLine` returns `null` on whitespace-only input. It handles backslash continuation internally — the returned string is the fully-joined multi-line value.

## Completion Hints

Register one or more `CompletionSource`s to enable ambient hint dispatch. Each source carries a trigger character, an anchor rule (`LineStart` or `WordStart`), and a `Lookup` callback that returns matching candidates for the body typed after the trigger:

```csharp
var commands = new[]
{
    new CompletionHint("/help",  "Show help"),
    new CompletionHint("/clear", "Clear the screen"),
};

var editor = new LineEditor();

editor.AddSource(new CompletionSource('/', TriggerAnchor.LineStart,
    body => commands
        .Where(c => c.Name.StartsWith("/" + body, StringComparison.OrdinalIgnoreCase))
        .ToList()));

editor.AddSource(new CompletionSource('@', TriggerAnchor.WordStart,
    body => EnumerateMatchingFiles(body)));
```

When the cursor is preceded by a registered trigger satisfying its anchor, a terse comma-separated list of matching names from `Lookup` is shown on an ephemeral line below the input. Closer-to-cursor triggers win when multiple are present. The hint is display-only — the user still types the full input and presses Enter to submit. Editing (arrows, history, Ctrl+U/W, etc.) works normally throughout.

`AddSource` rejects duplicate trigger chars; if you want to overload a trigger, combine candidates inside a single `Lookup` callback.

`Lookup` runs synchronously on every keystroke. Cheap for static lists; if your source needs heavy I/O, gate it behind a body-length threshold (`body.Length < 3 ? [] : Search(body)`) or pre-cache.

See [`UglyPrompt.Demo/Program.cs`](https://github.com/breitreiter/UglyPrompt/blob/main/UglyPrompt.Demo/Program.cs) for a runnable example wiring `/`, `+`, and `@` together.

## Features

- **History** — Up/Down arrows or Ctrl+P/N to navigate previous inputs
- **Backslash continuation** — Lines ending with `\` prompt for more input; the full value is returned joined with newlines
- **Bracketed paste** — Handles terminal paste sequences including embedded newlines
- **Standard line editing** — Home/End, Ctrl+A/E, Ctrl+U/K/W, Ctrl+T, and more (see below)
- **No dependencies** — Only the .NET standard library; targets .NET 8.0

## Keyboard Shortcuts

| Keys | Action |
|------|--------|
| Left / Ctrl+B | Move cursor left |
| Right / Ctrl+F | Move cursor right |
| Home / Ctrl+A | Move to start of line |
| End / Ctrl+E | Move to end of line |
| Backspace / Ctrl+H | Delete character before cursor |
| Delete / Ctrl+D | Delete character at cursor |
| Ctrl+U | Delete to start of line |
| Ctrl+K | Delete to end of line |
| Ctrl+W | Delete word before cursor |
| Ctrl+T | Transpose characters |
| Ctrl+L / Escape | Clear line |
| Up / Ctrl+P | Previous history entry |
| Down / Ctrl+N | Next history entry |

## License

MIT
