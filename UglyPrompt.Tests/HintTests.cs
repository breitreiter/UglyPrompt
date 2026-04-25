using UglyPrompt;
using UglyPrompt.Tests.Testing;
using VerifyXunit;
using static VerifyXunit.Verifier;

namespace UglyPrompt.Tests;

public class HintTests
{
    // --- helpers ---

    private static ConsoleKeyInfo Char(char c) =>
        new(c, ConsoleKey.NoName, false, false, false);

    private static ConsoleKeyInfo Key(ConsoleKey key) =>
        new('\0', key, false, false, false);

    private static (LineEditor editor, KeyHandler handler, GridConsole grid) Setup(
        int width = 40, int height = 8, string prompt = "> ")
    {
        var grid = new GridConsole(width, height);
        grid.Write(prompt);
        var editor = new LineEditor(grid);
        var handler = new KeyHandler(grid, new List<string>());
        return (editor, handler, grid);
    }

    private static void Type(LineEditor editor, KeyHandler handler, string text)
    {
        foreach (var c in text) editor.DriveKey(handler, Char(c));
    }

    private static IReadOnlyList<CompletionHint> StartsWithFilter(
        IEnumerable<CompletionHint> all, string prefix) =>
        all.Where(h => h.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

    private static CompletionSource SlashSource() => new('/', TriggerAnchor.LineStart,
        body => StartsWithFilter(
            new[] { new CompletionHint("/help", ""), new CompletionHint("/clear", ""), new CompletionHint("/quit", "") },
            "/" + body));

    private static CompletionSource PlusSource() => new('+', TriggerAnchor.LineStart,
        body => StartsWithFilter(
            new[] { new CompletionHint("+coder", ""), new CompletionHint("+writer", "") },
            "+" + body));

    private static CompletionSource AtSource() => new('@', TriggerAnchor.WordStart,
        body => StartsWithFilter(
            new[] { new CompletionHint("README.md", ""), new CompletionHint("readme-old.txt", ""), new CompletionHint("LICENSE", "") },
            body));

    // --- tests ---

    [Fact]
    public Task LineStart_trigger_renders_filtered_hint()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(SlashSource());

        Type(editor, handler, "/he");

        return Verify(grid.Snapshot());
    }

    [Fact]
    public Task LineStart_trigger_at_position_zero_with_empty_body()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(SlashSource());

        Type(editor, handler, "/");

        return Verify(grid.Snapshot());
    }

    [Fact]
    public Task WordStart_trigger_after_whitespace_renders_hint()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(AtSource());

        Type(editor, handler, "edit @rea");

        return Verify(grid.Snapshot());
    }

    [Fact]
    public Task WordStart_trigger_at_line_start_renders_hint()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(AtSource());

        Type(editor, handler, "@LIC");

        return Verify(grid.Snapshot());
    }

    [Fact]
    public Task Typing_past_candidate_clears_hint_content()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(SlashSource());

        Type(editor, handler, "/helpx");

        return Verify(grid.Snapshot());
    }

    [Fact]
    public Task Backspacing_past_trigger_clears_hint_strip()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(SlashSource());

        Type(editor, handler, "/h");
        editor.DriveKey(handler, Key(ConsoleKey.Backspace));
        editor.DriveKey(handler, Key(ConsoleKey.Backspace));

        return Verify(grid.Snapshot());
    }

    [Fact]
    public Task Closest_trigger_to_cursor_wins()
    {
        var (editor, handler, grid) = Setup();
        editor.AddSource(SlashSource());
        editor.AddSource(AtSource());

        // Slash source registered first; @ trigger is closer to cursor and
        // should win regardless of registration order.
        Type(editor, handler, "/foo @rea");

        return Verify(grid.Snapshot());
    }

    // Registration must reject duplicate triggers; this is a contract
    // assertion, not a layout one, so no Verify needed.
    [Fact]
    public void Duplicate_trigger_registration_throws()
    {
        var editor = new LineEditor();
        editor.AddSource(SlashSource());

        var ex = Assert.Throws<ArgumentException>(() => editor.AddSource(SlashSource()));
        Assert.Contains("already registered", ex.Message);
    }
}
