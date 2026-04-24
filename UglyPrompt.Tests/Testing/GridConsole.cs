using System.Text;

namespace UglyPrompt.Tests.Testing;

// In-memory 2D grid that mimics a console for layout testing. Handles cursor
// motion, overwrite-on-write, and soft-wrap. Recognized ANSI sequences
// (cursor show/hide, 8-bit color set/reset) are parsed and discarded — they
// don't affect cell contents.
internal class GridConsole : IConsoleAdapter
{
    private char[,] _cells = new char[0, 0];
    private int _width;
    private int _height;

    public int CursorLeft { get; set; }
    public int CursorTop { get; set; }
    public int BufferWidth => _width;
    public int BufferHeight => _height;
    public int WindowWidth => _width;

    public bool ShowCursorMarker { get; set; }

    public GridConsole(int width = 100, int height = 40)
    {
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        _cells = new char[height, width];
        for (int r = 0; r < height; r++)
            for (int c = 0; c < width; c++)
                _cells[r, c] = ' ';
        if (CursorLeft >= width) CursorLeft = width - 1;
        if (CursorTop >= height) CursorTop = height - 1;
    }

    public void SetCursorPosition(int left, int top)
    {
        CursorLeft = left;
        CursorTop = top;
    }

    public void Write(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];

            if (c == '\x1b')
            {
                // Skip a CSI sequence: ESC [ ... final-byte.
                // Final byte is in the range @–~ (0x40–0x7E).
                if (i + 1 < value.Length && value[i + 1] == '[')
                {
                    i += 2;
                    while (i < value.Length && (value[i] < 0x40 || value[i] > 0x7E))
                        i++;
                    // Consume the final byte too (loop will increment past it).
                }
                continue;
            }

            if (c == '\r') { CursorLeft = 0; continue; }

            if (c == '\n')
            {
                CursorTop++;
                CursorLeft = 0;
                continue;
            }

            if (CursorTop >= _height || CursorTop < 0) continue;
            if (CursorLeft >= _width || CursorLeft < 0) continue;

            _cells[CursorTop, CursorLeft] = c;
            CursorLeft++;
            if (CursorLeft >= _width)
            {
                CursorLeft = 0;
                CursorTop++;
            }
        }
    }

    public void WriteLine()
    {
        CursorTop++;
        CursorLeft = 0;
    }

    public char CellAt(int left, int top) => _cells[top, left];

    public string RowAt(int top)
    {
        var buf = new char[_width];
        for (int c = 0; c < _width; c++) buf[c] = _cells[top, c];
        return new string(buf);
    }

    // Returns a multi-line string view of visible cells, with trailing whitespace
    // trimmed per row and trailing empty rows dropped. If ShowCursorMarker is
    // true, renders the cursor cell as '█' so cursor placement is visible in
    // the snapshot.
    public string Snapshot()
    {
        int lastNonEmpty = -1;
        for (int r = 0; r < _height; r++)
        {
            if (RowAt(r).TrimEnd().Length > 0) lastNonEmpty = r;
        }
        if (ShowCursorMarker && CursorTop > lastNonEmpty) lastNonEmpty = CursorTop;

        var sb = new StringBuilder();
        for (int r = 0; r <= lastNonEmpty; r++)
        {
            var row = RowAt(r);
            if (ShowCursorMarker && r == CursorTop)
            {
                var chars = row.ToCharArray();
                if (CursorLeft >= 0 && CursorLeft < _width)
                {
                    chars[CursorLeft] = chars[CursorLeft] == ' ' ? '█' : char.ToUpper(chars[CursorLeft]);
                }
                row = new string(chars);
            }
            sb.AppendLine(row.TrimEnd());
        }
        return sb.ToString();
    }
}
