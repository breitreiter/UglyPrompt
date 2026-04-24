// Vendored from tonerdo/readline (MIT License)
// https://github.com/tonerdo/readline

namespace UglyPrompt;

internal interface IConsoleAdapter
{
    int CursorLeft { get; }
    int CursorTop { get; }
    int BufferWidth { get; }
    int BufferHeight { get; }
    int WindowWidth { get; }
    void SetCursorPosition(int left, int top);
    void Write(string value);
    void WriteLine();
}
