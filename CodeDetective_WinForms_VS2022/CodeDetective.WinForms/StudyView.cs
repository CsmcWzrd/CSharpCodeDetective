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
    private readonly FlowLayoutPanel _panel = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Dictionary<string, FlowLayoutPanel> _sections = new(StringComparer.OrdinalIgnoreCase);
    public event EventHandler<StudyOpenEntryEventArgs>? OpenEntryRequested;

    public StudyView()
    {
        Controls.Add(_panel);
    }

    public void AddHeader(string title)
    {
        if (_sections.ContainsKey(title)) return;
        var group = new GroupBox { Text = title, AutoSize = true, Width = Math.Max(600, Width - 40), Padding = new Padding(8) };
        var list = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Fill };
        group.Controls.Add(list);
        _panel.Controls.Add(group);
        _sections[title] = list;
    }

    public void AddEntry(string section, StudyEntry entry)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => AddEntry(section, entry))); return; }
        AddHeader(section);
        _sections[section].Controls.Add(CreateEntryControl(entry));
    }

    private Control CreateEntryControl(StudyEntry entry)
    {
        var outer = new Panel { Width = Math.Max(560, Width - 80), AutoSize = true, Padding = new Padding(4), Margin = new Padding(2) };
        var title = new LinkLabel { AutoSize = true, Text = $"{entry.FileName}:{entry.Line} - {entry.Action}", LinkColor = Color.Navy, Tag = entry };
        title.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(entry.FilePath))
                OpenEntryRequested?.Invoke(this, new StudyOpenEntryEventArgs { FilePath = entry.FilePath, Line = Math.Max(1, entry.Line) });
        };
        outer.Controls.Add(title);
        if (!string.IsNullOrWhiteSpace(entry.BodyText))
        {
            var body = new Label { AutoSize = true, MaximumSize = new Size(Math.Max(520, Width - 120), 0), Text = entry.BodyText, Top = title.Bottom + 4, Left = 16 };
            outer.Controls.Add(body);
        }
        return outer;
    }

    public List<StudySection> Export()
    {
        var result = new List<StudySection>();
        foreach (var pair in _sections)
        {
            var section = new StudySection { Title = pair.Key };
            foreach (Panel panel in pair.Value.Controls.OfType<Panel>())
            {
                var link = panel.Controls.OfType<LinkLabel>().FirstOrDefault();
                var label = panel.Controls.OfType<Label>().FirstOrDefault();
                if (link?.Tag is StudyEntry tagged) section.Entries.Add(tagged);
                else section.Entries.Add(new StudyEntry { FileName = link?.Text ?? "", BodyText = label?.Text ?? "" });
            }
            result.Add(section);
        }
        return result;
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
    }
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

    public static StudyEntry Open(string fileName, int line, string action, string filePath) => new() { FileName = fileName, Line = Math.Max(1, line), Action = action, FilePath = filePath };
    public static StudyEntry WithBody(string fileName, int line, string action, string filePath, string text) => new() { FileName = fileName, Line = Math.Max(1, line), Action = action, FilePath = filePath, BodyText = text };
}
