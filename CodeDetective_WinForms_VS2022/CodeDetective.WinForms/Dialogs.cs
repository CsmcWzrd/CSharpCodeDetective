using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeDetective.WinForms;

public sealed class FindDialog : Form
{
    private readonly TextBox _search = new() { Dock = DockStyle.Fill };
    private readonly TextBox _filePattern = new() { Dock = DockStyle.Fill };
    private readonly CheckBox _ignoreCase = new() { Text = "Ignore case", AutoSize = true };
    private readonly CheckBox _fileIgnoreCase = new() { Text = "Ignore case", AutoSize = true };
    private readonly CheckBox _wholeWord = new() { Text = "Match whole words", AutoSize = true };
    private readonly CheckBox _useRegex = new() { Text = "Use regular expressions", AutoSize = true };

    public string SearchText => _search.Text;
    public string FilePattern => _filePattern.Text;
    public bool IgnoreCase => _ignoreCase.Checked;
    public bool FileIgnoreCase => _fileIgnoreCase.Checked;
    public bool WholeWord => _wholeWord.Checked;
    public bool UseRegex => _useRegex.Checked;
    public string WholeWordLabel { set => _wholeWord.Text = value; }

    public FindDialog(string title, string prompt, string defaultFilePattern, bool showWholeWord, bool showRegex, bool showFilePattern)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        Width = 520;
        Height = showFilePattern ? 260 : 190;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 6 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var row = 0;
        root.Controls.Add(new Label { Text = prompt, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        root.Controls.Add(_search, 1, row++);
        if (showFilePattern)
        {
            _filePattern.Text = defaultFilePattern;
            root.Controls.Add(new Label { Text = "Find Files (Uses RegExp)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            root.Controls.Add(_filePattern, 1, row++);
            root.Controls.Add(new Label { Text = "File options", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            root.Controls.Add(_fileIgnoreCase, 1, row++);
        }
        root.Controls.Add(new Label { Text = "Search options", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        options.Controls.Add(_ignoreCase);
        if (showWholeWord) options.Controls.Add(_wholeWord);
        if (showRegex) options.Controls.Add(_useRegex);
        root.Controls.Add(options, 1, row++);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.SetColumnSpan(buttons, 2);
        root.Controls.Add(buttons, 0, row);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

public sealed class NoteDialog : Form
{
    private readonly ComboBox _type = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly TextBox _text = new() { Multiline = true, Dock = DockStyle.Fill, ScrollBars = ScrollBars.Both, AcceptsReturn = true, AcceptsTab = true };

    public string RecordType => _type.SelectedItem?.ToString() ?? "Note";
    public string RecordText => _text.Text;

    public NoteDialog()
    {
        Text = "Add Note/Todo/Code";
        StartPosition = FormStartPosition.CenterParent;
        Width = 600;
        Height = 420;
        _type.Items.AddRange(new object[] { "Note", "Todo", "Code" });
        _type.SelectedIndex = 0;
        Controls.Add(_text);
        Controls.Add(_type);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(8) };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
