// UglyPrompt — a no-frills readline-style line editor for .NET console apps
// Wraps vendored tonerdo/readline KeyHandler (MIT License)
// Adds: backslash continuation, history, ambient completion hints for / and +

namespace UglyPrompt;

public record CompletionHint(string Name, string Description);

public class LineEditor
{
    private readonly List<string> _history = new();
    private readonly IConsoleAdapter _console;
    private bool _hintActive;
    private string? _lastHintContent;

    public List<CompletionHint> Commands { get; set; } = new();
    public List<CompletionHint> Kits { get; set; } = new();

    public LineEditor() : this(new ConsoleAdapter()) { }

    internal LineEditor(IConsoleAdapter console)
    {
        _console = console;
    }

    public string? ReadLine(string prompt)
    {
        var line = ReadSingleLine(prompt);
        if (line == null) return null;

        // Backslash continuation
        if (line.EndsWith('\\'))
        {
            var lines = new List<string> { line[..^1] };
            while (true)
            {
                var continuation = ReadSingleLine("  ", enableGuards: false);
                if (continuation == null)
                {
                    lines.Add("");
                    break;
                }
                if (continuation.EndsWith('\\'))
                    lines.Add(continuation[..^1]);
                else
                {
                    lines.Add(continuation);
                    break;
                }
            }
            line = string.Join("\n", lines);
        }

        if (string.IsNullOrWhiteSpace(line)) return null;

        _history.Add(line.Contains('\n') ? line.Split('\n')[0] + "..." : line);
        return line;
    }

    private string? ReadSingleLine(string prompt, bool enableGuards = true)
    {
        _console.Write(prompt);

        // Reserve a line below the prompt for ambient hints. Without this,
        // when the prompt lands on the last row the hint renders below the fold.
        if (enableGuards && _console.CursorTop == _console.BufferHeight - 1)
        {
            var left = _console.CursorLeft;
            _console.WriteLine();
            _console.SetCursorPosition(left, _console.CursorTop - 1);
        }

        var handler = new KeyHandler(_console, _history);
        var keyInfo = Console.ReadKey(true);

        while (true)
        {
            // Bracketed paste: ESC [ 2 0 0 ~ ... content ... ESC [ 2 0 1 ~
            if (keyInfo.Key == ConsoleKey.Escape && Console.KeyAvailable)
            {
                var pasted = TryReadBracketedPaste();
                if (pasted != null)
                {
                    handler.InsertText(pasted);
                    RefreshHint(handler, enableGuards);
                    keyInfo = Console.ReadKey(true);
                    continue;
                }
                // Not a paste sequence — fall through to handler (clears line)
            }

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                // On Windows without bracketed paste, pasted newlines arrive as Enter keys.
                // If more input is buffered immediately after Enter, it's a paste — keep reading.
                if (Console.KeyAvailable)
                {
                    handler.InsertText("\n");
                    keyInfo = Console.ReadKey(true);
                    continue;
                }
                break;
            }

            handler.Handle(keyInfo);
            RefreshHint(handler, enableGuards);
            keyInfo = Console.ReadKey(true);
        }

        ClearHintLine();
        _console.WriteLine();
        var text = handler.Text;
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    // Try to read a bracketed paste sequence. Call after consuming \x1b.
    // Returns the paste content if \x1b[200~ was present, null otherwise.
    private static string? TryReadBracketedPaste()
    {
        var prefix = new char[5];
        for (int i = 0; i < 5; i++)
        {
            if (!Console.KeyAvailable) return null;
            prefix[i] = Console.ReadKey(true).KeyChar;
        }
        if (new string(prefix) != "[200~") return null;

        var content = new System.Text.StringBuilder();
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Escape)
            {
                var end = new char[5];
                for (int i = 0; i < 5; i++)
                    end[i] = Console.ReadKey(true).KeyChar;
                if (new string(end) == "[201~") break;
                content.Append('\x1b');
                content.Append(new string(end));
            }
            else if (k.Key == ConsoleKey.Enter)
            {
                content.Append('\n');
            }
            else
            {
                content.Append(k.KeyChar);
            }
        }
        return content.ToString();
    }

    private void RefreshHint(KeyHandler handler, bool enabled)
    {
        if (!enabled) return;

        var text = handler.Text;
        string? prefix = null;
        List<CompletionHint>? items = null;

        if (text.Length > 0)
        {
            if (text[0] == '/' && Commands.Count > 0) { prefix = "/"; items = Commands; }
            else if (text[0] == '+' && Kits.Count > 0) { prefix = "+"; items = Kits; }
        }

        // No guard character — skip all cursor work unless we need to clear a stale hint.
        if (prefix == null) { ClearHintLine(); return; }

        var matches = items!
            .Where(h => h.Name.StartsWith(prefix + text[1..], StringComparison.OrdinalIgnoreCase))
            .Select(h => h.Name)
            .ToList();
        var content = matches.Count > 0 ? string.Join(", ", matches) : "";

        if (_hintActive && content == _lastHintContent) return;

        RenderHintLine(content);
        _hintActive = true;
        _lastHintContent = content;
    }

    private void RenderHintLine(string content)
    {
        var savedLeft = _console.CursorLeft;
        var savedTop = _console.CursorTop;

        if (savedTop + 1 >= _console.BufferHeight) return;

        var width = _console.WindowWidth;
        _console.Write("\u001b[?25l");
        _console.SetCursorPosition(0, savedTop + 1);
        _console.Write(new string(' ', width));

        if (content.Length > 0)
        {
            _console.SetCursorPosition(0, savedTop + 1);
            var maxLen = Math.Max(0, width - 4);
            if (content.Length > maxLen)
                content = content[..Math.Max(0, maxLen - 1)] + "…";
            _console.Write($"\u001b[90m  {content}\u001b[0m");
        }

        _console.SetCursorPosition(savedLeft, savedTop);
        _console.Write("\u001b[?25h");
    }

    private void ClearHintLine()
    {
        if (!_hintActive) return;

        var savedLeft = _console.CursorLeft;
        var savedTop = _console.CursorTop;

        if (savedTop + 1 < _console.BufferHeight)
        {
            _console.Write("\u001b[?25l");
            _console.SetCursorPosition(0, savedTop + 1);
            _console.Write(new string(' ', _console.WindowWidth));
            _console.SetCursorPosition(savedLeft, savedTop);
            _console.Write("\u001b[?25h");
        }

        _hintActive = false;
        _lastHintContent = null;
    }
}
