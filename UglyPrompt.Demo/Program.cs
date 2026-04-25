// UglyPrompt.Demo — runnable workbench for the multi-source completion API.
// Wires three completion sources against demo data so the hint dispatch can
// be exercised across terminal emulators without standing up a real client.

using UglyPrompt;

var commands = new[]
{
    new CompletionHint("/help",  "show available commands"),
    new CompletionHint("/clear", "clear screen"),
    new CompletionHint("/quit",  "exit"),
};

var kits = new[]
{
    new CompletionHint("+coder",  "coding kit"),
    new CompletionHint("+writer", "writing kit"),
};

var editor = new LineEditor();

editor.AddSource(new CompletionSource('/', TriggerAnchor.LineStart,
    body => commands
        .Where(c => c.Name.StartsWith("/" + body, StringComparison.OrdinalIgnoreCase))
        .ToList()));

editor.AddSource(new CompletionSource('+', TriggerAnchor.LineStart,
    body => kits
        .Where(k => k.Name.StartsWith("+" + body, StringComparison.OrdinalIgnoreCase))
        .ToList()));

editor.AddSource(new CompletionSource('@', TriggerAnchor.WordStart,
    body => Directory.EnumerateFileSystemEntries(".")
        .Select(p => Path.GetFileName(p)!)
        .Where(n => n.StartsWith(body, StringComparison.OrdinalIgnoreCase))
        .Take(10)
        .Select(n => new CompletionHint(n, ""))
        .ToList()));

Console.WriteLine("UglyPrompt demo. Try /, +, or @ to see hints. Empty line to exit.");

while (true)
{
    var line = editor.ReadLine("> ");
    if (line == null) break;
    Console.WriteLine($"  echo: {line}");
}
