using System.Text;

namespace XinSoft.Services;

/// <summary>终端单元格：字符 + 前景/背景色 + 粗体。</summary>
public class TerminalCell
{
    public char Char { get; set; }
    public Color Fore { get; set; }
    public Color Back { get; set; }
    public bool Bold { get; set; }

    public TerminalCell(char ch, Color fore, Color back, bool bold)
    {
        Char = ch;
        Fore = fore;
        Back = back;
        Bold = bold;
    }

    public TerminalCell Clone() => new(Char, Fore, Back, Bold);
}

/// <summary>
/// 轻量终端模拟器：网格缓冲区 + ANSI SGR 颜色与常用光标序列解析。
/// 仅覆盖演示所需子集，不处理完整 VT100/ANSI 协议。
/// </summary>
public class TerminalEmulator
{
    private static readonly Color[] Ansi =
    {
        Color.Black,
        Color.FromArgb(0xCC, 0x00, 0x00),
        Color.FromArgb(0x00, 0xCC, 0x00),
        Color.FromArgb(0xCC, 0xAA, 0x00),
        Color.FromArgb(0x00, 0x00, 0xCC),
        Color.FromArgb(0xCC, 0x00, 0xCC),
        Color.FromArgb(0x00, 0xCC, 0xCC),
        Color.FromArgb(0xCC, 0xCC, 0xCC)
    };

    private static readonly Color[] Bright =
    {
        Color.FromArgb(0x80, 0x80, 0x80),
        Color.FromArgb(0xFF, 0x55, 0x55),
        Color.FromArgb(0x55, 0xFF, 0x55),
        Color.FromArgb(0xFF, 0xFF, 0x55),
        Color.FromArgb(0x55, 0x55, 0xFF),
        Color.FromArgb(0xFF, 0x55, 0xFF),
        Color.FromArgb(0x55, 0xFF, 0xFF),
        Color.White
    };

    public int Cols { get; private set; }
    public int Rows { get; private set; }

    private List<List<TerminalCell>> _grid = new();
    private int _curRow;
    private int _curCol;
    private int _saveRow;
    private int _saveCol;

    private Color _fore;
    private Color _back;
    private bool _bold;

    public Color DefaultFore { get; }
    public Color DefaultBack { get; }

    public TerminalEmulator(int cols, int rows)
    {
        DefaultFore = Color.FromArgb(0xBE, 0xE6, 0xA0);
        DefaultBack = Color.Black;
        _fore = DefaultFore;
        _back = DefaultBack;
        Resize(cols, rows);
    }

    public IReadOnlyList<IReadOnlyList<TerminalCell>> Grid => _grid;

    public void Resize(int cols, int rows)
    {
        cols = Math.Max(1, cols);
        rows = Math.Max(1, rows);

        var old = _grid;
        var newGrid = new List<List<TerminalCell>>(rows);
        for (int r = 0; r < rows; r++)
        {
            var row = new List<TerminalCell>(cols);
            for (int c = 0; c < cols; c++)
            {
                if (old.Count > r && old[r].Count > c)
                    row.Add(old[r][c].Clone());
                else
                    row.Add(EmptyCell());
            }
            newGrid.Add(row);
        }

        _grid = newGrid;
        Cols = cols;
        Rows = rows;
        _curRow = Math.Min(_curRow, rows - 1);
        _curCol = Math.Min(_curCol, cols - 1);
    }

    public void Clear()
    {
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
                _grid[r][c] = EmptyCell();
        }
        _curRow = 0;
        _curCol = 0;
    }

    /// <summary>喂入一段远端输出（可能被拆成多次到达，状态在缓冲区中保持）。</summary>
    public void Feed(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        int i = 0;
        while (i < text.Length)
        {
            char ch = text[i];

            if (ch == '\u001B')
            {
                i = ParseEscape(text, i);
                continue;
            }

            switch (ch)
            {
                case '\r':
                    _curCol = 0;
                    break;
                case '\n':
                    _curRow = Math.Min(Rows - 1, _curRow + 1);
                    break;
                case '\b':
                    _curCol = Math.Max(0, _curCol - 1);
                    break;
                case '\t':
                    _curCol = (_curCol / 8 + 1) * 8;
                    if (_curCol >= Cols) { _curCol = 0; _curRow = Math.Min(Rows - 1, _curRow + 1); }
                    break;
                default:
                    if (!char.IsControl(ch)) PutChar(ch);
                    break;
            }
            i++;
        }
    }

    private int ParseEscape(string text, int start)
    {
        int i = start + 1;
        if (i >= text.Length) return i;

        char next = text[i];

        if (next == '[')
        {
            // CSI：收集参数直到终止字母
            i++;
            var param = new StringBuilder();
            while (i < text.Length)
            {
                char c = text[i];
                if (c >= '@' && c <= '~')
                {
                    ApplyCsi(param.ToString(), c);
                    return i + 1;
                }
                param.Append(c);
                i++;
            }
            return i;
        }

        if (next == ']')
        {
            // OSC：忽略直到 BEL 或 ESC\\
            while (i < text.Length)
            {
                if (text[i] == '\u0007') return i + 1;
                if (text[i] == '\u001B' && i + 1 < text.Length && text[i + 1] == '\\') return i + 2;
                i++;
            }
            return i;
        }

        // 其它两字符转义（如 ESC(M)）直接跳过第二个字符
        return i + 1;
    }

    private void ApplyCsi(string param, char final)
    {
        var parts = param.Split(';', StringSplitOptions.RemoveEmptyEntries);
        int[] nums = parts.Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        switch (final)
        {
            case 'm':
                ApplySgr(nums);
                break;
            case 'H':
            case 'f':
                CursorPosition(nums);
                break;
            case 'A': MoveUp(Num(nums, 0, 1)); break;
            case 'B': MoveDown(Num(nums, 0, 1)); break;
            case 'C': MoveRight(Num(nums, 0, 1)); break;
            case 'D': MoveLeft(Num(nums, 0, 1)); break;
            case 'G': SetColumn(Num(nums, 0, 1)); break;
            case 'd': SetRow(Num(nums, 0, 1)); break;
            case 'J':
                if (nums.Length > 0 && nums[0] == 2) Clear();
                break;
            case 'K': EraseLine(Num(nums, 0, 0)); break;
            case 's':
                _saveRow = _curRow;
                _saveCol = _curCol;
                break;
            case 'u':
                _curRow = _saveRow;
                _curCol = _saveCol;
                break;
        }
    }

    private void ApplySgr(int[] nums)
    {
        if (nums.Length == 0) nums = new[] { 0 };

        foreach (var code in nums)
        {
            switch (code)
            {
                case 0:
                    _fore = DefaultFore;
                    _back = DefaultBack;
                    _bold = false;
                    break;
                case 1: _bold = true; break;
                case 22: _bold = false; break;
                case 30: case 31: case 32: case 33: case 34: case 35: case 36: case 37:
                    _fore = Ansi[code - 30];
                    break;
                case 90: case 91: case 92: case 93: case 94: case 95: case 96: case 97:
                    _fore = Bright[code - 90];
                    break;
                case 40: case 41: case 42: case 43: case 44: case 45: case 46: case 47:
                    _back = Ansi[code - 40];
                    break;
                case 100: case 101: case 102: case 103: case 104: case 105: case 106: case 107:
                    _back = Bright[code - 100];
                    break;
                case 39: _fore = DefaultFore; break;
                case 49: _back = DefaultBack; break;
            }
        }
    }

    private void CursorPosition(int[] nums)
    {
        int row = Num(nums, 0, 1) - 1;
        int col = Num(nums, 1, 1) - 1;
        _curRow = ClampRow(row);
        _curCol = ClampCol(col);
    }

    private void MoveUp(int n) => _curRow = ClampRow(_curRow - n);
    private void MoveDown(int n) => _curRow = ClampRow(_curRow + n);
    private void MoveRight(int n) => _curCol = ClampCol(_curCol + n);
    private void MoveLeft(int n) => _curCol = ClampCol(_curCol - n);
    private void SetColumn(int n) => _curCol = ClampCol(n - 1);
    private void SetRow(int n) => _curRow = ClampRow(n - 1);

    private void EraseLine(int mode)
    {
        var row = _grid[_curRow];
        switch (mode)
        {
            case 0: // 从光标到行尾
                for (int c = _curCol; c < Cols; c++) row[c] = EmptyCell();
                break;
            case 1: // 从行首到光标
                for (int c = 0; c <= _curCol; c++) row[c] = EmptyCell();
                break;
            case 2: // 整行
                for (int c = 0; c < Cols; c++) row[c] = EmptyCell();
                break;
        }
    }

    private void PutChar(char ch)
    {
        EnsureRow(_curRow);
        var row = _grid[_curRow];
        EnsureCol(row);
        row[_curCol] = new TerminalCell(ch, _fore, _back, _bold);

        _curCol++;
        if (_curCol >= Cols)
        {
            _curCol = 0;
            _curRow = Math.Min(Rows - 1, _curRow + 1);
        }
    }

    private TerminalCell EmptyCell() => new(' ', DefaultFore, DefaultBack, false);

    private void EnsureRow(int row)
    {
        while (_grid.Count <= row)
        {
            _grid.Add(Enumerable.Range(0, Cols).Select(_ => EmptyCell()).ToList());
        }
    }

    private void EnsureCol(List<TerminalCell> row)
    {
        while (row.Count < 1) row.Add(EmptyCell());
    }

    private int ClampRow(int r) => Math.Max(0, Math.Min(Rows - 1, r));
    private int ClampCol(int c) => Math.Max(0, Math.Min(Cols - 1, c));

    private static int Num(int[] nums, int idx, int fallback) =>
        nums.Length > idx ? nums[idx] : fallback;
}
