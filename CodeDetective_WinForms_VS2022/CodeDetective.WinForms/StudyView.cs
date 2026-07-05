using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace CodeDetective.WinForms;

public sealed class StudyOpenEntryEventArgs : EventArgs
{
    public required string FilePath { get; init; }
    public required int Line { get; init; }
}

public sealed class StudyView : UserControl
{
    private readonly FlowLayoutPanel _panel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false
    };

    private readonly Dictionary<string, SectionState> _sections = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<StudySection> _data = new();

    public event EventHandler<StudyOpenEntryEventArgs>? OpenEntryRequested;

    public StudyView()
    {
        Controls.Add(_panel);
    }

    public void AddHeader(string title)
    {
        EnsureSection(title);
    }

    public void AddEntry(string section, StudyEntry entry)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => AddEntry(section, entry))); return; }

        var state = EnsureSection(section);
        var dataSection = _data.First(s => string.Equals(s.Title, NormalizeSectionTitle(section), StringComparison.OrdinalIgnoreCase));
        dataSection.Entries.Add(entry);
        AddEntryToControl(state, entry);
    }

    private SectionState EnsureSection(string title)
    {
        var normalized = NormalizeSectionTitle(title);
        if (_sections.TryGetValue(normalized, out var existing)) return existing;

        var group = new GroupBox
        {
            Text = normalized,
            Width = Math.Max(760, Width - 40),
            Height = IsTableSection(normalized) ? 330 : 220,
            MinimumSize = new Size(500, IsTableSection(normalized) ? 300 : 140),
            MaximumSize = new Size(4096, IsTableSection(normalized) ? 800 : 800),
            Padding = new Padding(8),
            Margin = new Padding(6)
        };

        Control content;
        if (IsTableSection(normalized))
        {
            var list = CreateResultListView(normalized);
            content = list;
        }
        else if (IsTextBoxSection(normalized))
        {
            var text = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9f)
            };
            content = text;
        }
        else
        {
            var list = CreateResultListView(normalized);
            content = list;
        }

        group.Controls.Add(content);
        _panel.Controls.Add(group);

        var state = new SectionState(normalized, group, content);
        _sections[normalized] = state;
        if (!_data.Any(s => string.Equals(s.Title, normalized, StringComparison.OrdinalIgnoreCase)))
            _data.Add(new StudySection { Title = normalized });
        return state;
    }

    private ListView CreateResultListView(string section)
    {
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
            MultiSelect = false
        };

        if (IsOpenClosedSection(section))
        {
            list.Columns.Add("Action", 90);
            list.Columns.Add("File", 220);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Path", 600);
        }
        else if (IsFindSection(section))
        {
            list.Columns.Add("Result", 90);
            list.Columns.Add("File", 240);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Path", 600);
        }
        else if (IsGrepSection(section))
        {
            list.Columns.Add("File", 220);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Matched Text", 420);
            list.Columns.Add("Path", 600);
        }
        else if (IsTagsSection(section))
        {
            list.Columns.Add("Tag", 220);
            list.Columns.Add("Kind", 180);
            list.Columns.Add("File", 220);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Path", 600);
        }
        else
        {
            list.Columns.Add("Action", 160);
            list.Columns.Add("File", 220);
            list.Columns.Add("Line", 70);
            list.Columns.Add("Details", 420);
            list.Columns.Add("Path", 600);
        }

        list.DoubleClick += (_, _) =>
        {
            if (list.SelectedItems.Count == 0) return;
            if (list.SelectedItems[0].Tag is not StudyEntry entry) return;
            if (!string.IsNullOrWhiteSpace(entry.FilePath))
                OpenEntryRequested?.Invoke(this, new StudyOpenEntryEventArgs { FilePath = entry.FilePath, Line = Math.Max(1, entry.Line) });
        };
        return list;
    }

    private void AddEntryToControl(SectionState state, StudyEntry entry)
    {
        if (state.Content is ListView list)
        {
            ListViewItem item;
            if (IsOpenClosedSection(state.Title))
            {
                item = new ListViewItem(entry.Action);
                item.SubItems.Add(entry.FileName);
                item.SubItems.Add(entry.Line.ToString());
                item.SubItems.Add(entry.FilePath);
            }
            else if (IsFindSection(state.Title))
            {
                item = new ListViewItem(entry.Action);
                item.SubItems.Add(entry.FileName);
                item.SubItems.Add(entry.Line.ToString());
                item.SubItems.Add(entry.FilePath);
            }
            else if (IsGrepSection(state.Title))
            {
                item = new ListViewItem(entry.FileName);
                item.SubItems.Add(entry.Line.ToString());
                item.SubItems.Add(entry.Action);
                item.SubItems.Add(entry.FilePath);
            }
            else if (IsTagsSection(state.Title))
            {
                item = new ListViewItem(entry.Action);
                item.SubItems.Add(entry.BodyText);
                item.SubItems.Add(entry.FileName);
                item.SubItems.Add(entry.Line.ToString());
                item.SubItems.Add(entry.FilePath);
            }
            else
            {
                item = new ListViewItem(entry.Action);
                item.SubItems.Add(entry.FileName);
                item.SubItems.Add(entry.Line.ToString());
                item.SubItems.Add(entry.BodyText);
                item.SubItems.Add(entry.FilePath);
            }

            item.Tag = entry;
            list.Items.Add(item);
            AutoResizeColumns(list);
            ResizeTableSection(state);
        }
        else if (state.Content is TextBox text)
        {
            var header = string.IsNullOrWhiteSpace(entry.FileName)
                ? entry.Action
                : $"{entry.FileName}:{entry.Line} - {entry.Action}";
            if (text.TextLength > 0) text.AppendText(Environment.NewLine + Environment.NewLine);
            text.AppendText(header);
            if (!string.IsNullOrWhiteSpace(entry.BodyText))
                text.AppendText(Environment.NewLine + entry.BodyText);
        }
    }

    private static void AutoResizeColumns(ListView list)
    {
        foreach (ColumnHeader column in list.Columns)
        {
            column.Width = -2;
            if (column.Width < 70) column.Width = 70;
            if (column.Width > 700) column.Width = 700;
        }
    }

    private static void ResizeTableSection(SectionState state)
    {
        if (state.Content is not ListView list) return;
        var desired = 80 + (list.Items.Count * 24);
        state.Group.Height = Math.Max(300, Math.Min(800, desired));
    }

    public List<StudySection> Export()
    {
        return _data.Select(section => new StudySection
        {
            Title = section.Title,
            Entries = section.Entries.Select(e => e.Clone()).ToList()
        }).ToList();
    }

    public void Import(List<StudySection> sections)
    {
        Clear();
        foreach (var section in sections)
            foreach (var entry in section.Entries)
                AddEntry(section.Title, entry);
    }

    public void Clear()
    {
        _panel.Controls.Clear();
        _sections.Clear();
        _data.Clear();
    }

    private static string NormalizeSectionTitle(string title) => title.Trim().TrimEnd(':') + ":";
    private static bool IsOpenClosedSection(string title) => title.StartsWith("Opened/Closed Files", StringComparison.OrdinalIgnoreCase);
    private static bool IsFindSection(string title) => title.StartsWith("Find Results", StringComparison.OrdinalIgnoreCase);
    private static bool IsGrepSection(string title) => title.StartsWith("Grep Results", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Find + Grep Results", StringComparison.OrdinalIgnoreCase);
    private static bool IsTagsSection(string title) => title.StartsWith("Ctags Results", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Tags Results", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Tag Search Results", StringComparison.OrdinalIgnoreCase);
    private static bool IsTextBoxSection(string title) => title.StartsWith("Todo", StringComparison.OrdinalIgnoreCase) || title.StartsWith("ToDo", StringComparison.OrdinalIgnoreCase) || (title.StartsWith("Note", StringComparison.OrdinalIgnoreCase) || title.StartsWith("Notes", StringComparison.OrdinalIgnoreCase)) || title.StartsWith("Code", StringComparison.OrdinalIgnoreCase);
    private static bool IsTableSection(string title) => IsOpenClosedSection(title) || IsFindSection(title) || IsGrepSection(title) || IsTagsSection(title);

    private sealed record SectionState(string Title, GroupBox Group, Control Content);
}

public sealed class StudySection
{
    public string Title { get; set; } = "";
    public List<StudyEntry> Entries { get; set; } = new();
}

public sealed class StudyEntry
{
    public string FileName { get; set; } = "";
    public int Line { get; set; } = 1;
    public string Action { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string BodyText { get; set; } = "";

    public StudyEntry Clone() => new() { FileName = FileName, Line = Line, Action = Action, FilePath = FilePath, BodyText = BodyText };
    public static StudyEntry Open(string fileName, int line, string action, string filePath) => new() { FileName = fileName, Line = Math.Max(1, line), Action = action, FilePath = filePath };
    public static StudyEntry WithBody(string fileName, int line, string action, string filePath, string text) => new() { FileName = fileName, Line = Math.Max(1, line), Action = action, FilePath = filePath, BodyText = text };
}
