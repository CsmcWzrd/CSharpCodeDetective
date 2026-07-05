using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CodeDetective.WinForms;

public sealed class MainForm : Form
{
    private readonly SplitContainer _splitter = new() { Dock = DockStyle.Fill, SplitterDistance = 320 };
    private readonly TabControl _leftTabs = new() { Dock = DockStyle.Fill };
    private readonly TreeView _projectTree = new() { Dock = DockStyle.Fill, HideSelection = false };
    private readonly TabControl _editorTabs = new() { Dock = DockStyle.Fill };
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusText = new("Ready");
    private readonly ToolStripProgressBar _progress = new() { Width = 180, Minimum = 0, Maximum = 100, Value = 0 };
    private readonly StudyView _study = new() { Dock = DockStyle.Fill };
    private readonly TextBox _studyFileName = new() { Dock = DockStyle.Fill, PlaceholderText = "Load/Save Study name. Blank = project.study" };
    private readonly List<Bookmark> _bookmarks = new();
    private readonly List<string> _codeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cpp", ".hpp", ".c++", ".h++", ".cxx", ".hxx", ".c", ".h", ".cs", ".java", ".py", ".js", ".ts", ".go", ".rs"
    };

    private string? _projectPath;
    private string? _currentFile;
    private int _currentLine = 1;

    public MainForm()
    {
        Text = "CodeDetective";
        Width = 1280;
        Height = 800;
        StartPosition = FormStartPosition.CenterScreen;
        Icon = LoadIconSafely();
        BuildMenus();
        BuildLayout();
    }

    private Icon? LoadIconSafely()
    {
        try
        {
            var ico = Path.Combine(AppContext.BaseDirectory, "CodeDetective.ico");
            if (File.Exists(ico)) return new Icon(ico);
        }
        catch { }
        return null;
    }

    private void BuildMenus()
    {
        var menu = new MenuStrip();
        MainMenuStrip = menu;

        var project = new ToolStripMenuItem("Project");
        project.DropDownItems.Add(Item("Open Project", null, (_, _) => OpenProjectDialog()));
        project.DropDownItems.Add(Item("Set as Project Folder", null, (_, _) => SetSelectedFolderAsProject()));
        project.DropDownItems.Add(Item("Close Project", null, (_, _) => CloseProject()));

        var study = new ToolStripMenuItem("Study");
        study.DropDownItems.Add(Item("View Study", Keys.F1, (_, _) => _editorTabs.SelectedTab = _editorTabs.TabPages[0]));
        study.DropDownItems.Add(Item("Add Note/Todo/Code", Keys.F3, (_, _) => AddRecordDialog()));
        study.DropDownItems.Add(Item("Save Open Tabs in Study", null, (_, _) => SaveOpenTabsInStudy()));
        study.DropDownItems.Add(Item("Save Study", null, (_, _) => SaveStudy()));
        study.DropDownItems.Add(Item("Load Study", null, (_, _) => LoadStudy()));
        study.DropDownItems.Add(Item("Clear Study", null, (_, _) => _study.Clear()));

        var bookmarks = new ToolStripMenuItem("Bookmarks");
        bookmarks.DropDownItems.Add(Item("Bookmark", Keys.F2, (_, _) => AddBookmark()));
        bookmarks.DropDownItems.Add(Item("Back", Keys.F4, (_, _) => NavigateBookmark(-1)));
        bookmarks.DropDownItems.Add(Item("Forward", Keys.F5, (_, _) => NavigateBookmark(1)));
        bookmarks.DropDownItems.Add(Item("Clear Bookmarks", null, (_, _) => { _bookmarks.Clear(); SetStatus("Bookmarks cleared"); }));

        var find = new ToolStripMenuItem("Find");
        find.DropDownItems.Add(Item("Files (Find)", Keys.Alt | Keys.F, async (_, _) => await FindFilesDialogAsync()));
        find.DropDownItems.Add(Item("In files (Grep)", Keys.Control | Keys.F, async (_, _) => await GrepDialogAsync()));

        var ctags = new ToolStripMenuItem("Ctags");
        ctags.DropDownItems.Add(Item("Build tags reference", null, async (_, _) => await BuildTagsAsync()));
        ctags.DropDownItems.Add(Item("Find tag", null, async (_, _) => await FindTagDialogAsync()));
        ctags.DropDownItems.Add(Item("Load tags file (or reload)", null, (_, _) => SetStatus(TagsFilePath() is { } t && File.Exists(t) ? $"Loaded {t}" : "No tags file found in project folder")));
        ctags.DropDownItems.Add(Item("Unload tags file", null, (_, _) => { TryDeleteTagsIndexCache(); SetStatus("Tags cache unloaded"); }));

        var help = new ToolStripMenuItem("Help");
        help.DropDownItems.Add(Item("About", null, (_, _) => ShowAbout()));

        menu.Items.AddRange(new ToolStripItem[] { project, study, bookmarks, find, ctags, help });
        Controls.Add(menu);
    }

    private static ToolStripMenuItem Item(string text, Keys? shortcut, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text);
        if (shortcut.HasValue) item.ShortcutKeys = shortcut.Value;
        item.Click += handler;
        return item;
    }

    private void BuildLayout()
    {
        _status.Items.Add(_statusText);
        _status.Items.Add(new ToolStripStatusLabel { Spring = true });
        _status.Items.Add(_progress);
        Controls.Add(_status);
        _status.Dock = DockStyle.Bottom;

        _splitter.Panel1.Controls.Add(_leftTabs);
        _splitter.Panel2.Controls.Add(_editorTabs);
        Controls.Add(_splitter);
        _splitter.BringToFront();
        _splitter.Dock = DockStyle.Fill;

        var projectPage = new TabPage("Project");
        projectPage.Controls.Add(_projectTree);
        _leftTabs.TabPages.Add(projectPage);

        _projectTree.BeforeExpand += (_, e) => PopulateDirectoryNode(e.Node);
        _projectTree.NodeMouseDoubleClick += (_, e) =>
        {
            if (e.Node?.Tag is string path && File.Exists(path)) OpenFileInEditor(path, 1);
        };

        var studyPage = new TabPage("Study");
        var studyPanel = new Panel { Dock = DockStyle.Fill };
        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 34, ColumnCount = 3 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var loadButton = new Button { Text = "Load Study", AutoSize = true };
        var saveButton = new Button { Text = "Save Study", AutoSize = true };
        loadButton.Click += (_, _) => LoadStudy();
        saveButton.Click += (_, _) => SaveStudy();
        top.Controls.Add(_studyFileName, 0, 0);
        top.Controls.Add(loadButton, 1, 0);
        top.Controls.Add(saveButton, 2, 0);
        studyPanel.Controls.Add(_study);
        studyPanel.Controls.Add(top);
        studyPage.Controls.Add(studyPanel);
        _editorTabs.TabPages.Add(studyPage);

        _study.OpenEntryRequested += (_, e) => OpenFileInEditor(e.FilePath, e.Line);
    }

    private void OpenProjectDialog()
    {
        using var dialog = new FolderBrowserDialog { Description = "Open CodeDetective project folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK) OpenProject(dialog.SelectedPath);
    }

    private void SetSelectedFolderAsProject()
    {
        if (_projectTree.SelectedNode?.Tag is string path)
        {
            if (File.Exists(path)) path = Path.GetDirectoryName(path)!;
            if (Directory.Exists(path)) OpenProject(path);
        }
        else OpenProjectDialog();
    }

    private void OpenProject(string path)
    {
        _projectPath = path;
        _projectTree.Nodes.Clear();
        var root = CreateDirectoryNode(path);
        _projectTree.Nodes.Add(root);
        PopulateDirectoryNode(root);
        root.Expand();
        SetStatus($"Project opened: {path}");
    }

    private void CloseProject()
    {
        _projectPath = null;
        _projectTree.Nodes.Clear();
        _editorTabs.TabPages.Cast<TabPage>().Where(p => p.Tag is EditorInfo).ToList().ForEach(p => _editorTabs.TabPages.Remove(p));
        _study.Clear();
        SetStatus("Project closed");
    }

    private TreeNode CreateDirectoryNode(string dir)
    {
        var node = new TreeNode(Path.GetFileName(dir).Length == 0 ? dir : Path.GetFileName(dir)) { Tag = dir };
        node.Nodes.Add(new TreeNode("Loading...") { Tag = null });
        return node;
    }

    private void PopulateDirectoryNode(TreeNode? node)
    {
        if (node?.Tag is not string dir || !Directory.Exists(dir)) return;
        if (node.Nodes.Count != 1 || node.Nodes[0].Tag is not null) return;
        node.Nodes.Clear();
        try
        {
            foreach (var childDir in Directory.EnumerateDirectories(dir).OrderBy(Path.GetFileName))
            {
                var info = new DirectoryInfo(childDir);
                if ((info.Attributes & FileAttributes.Hidden) != 0) continue;
                node.Nodes.Add(CreateDirectoryNode(childDir));
            }
            foreach (var file in Directory.EnumerateFiles(dir).Where(IsCodeLikeFile).OrderBy(Path.GetFileName))
            {
                node.Nodes.Add(new TreeNode(Path.GetFileName(file)) { Tag = file });
            }
        }
        catch (Exception ex) { SetStatus(ex.Message); }
    }

    private bool IsCodeLikeFile(string path)
    {
        var ext = Path.GetExtension(path);
        return _codeExtensions.Contains(ext) || string.Equals(Path.GetFileName(path), "Makefile", StringComparison.OrdinalIgnoreCase);
    }

    private void OpenFileInEditor(string file, int line)
    {
        if (!File.Exists(file)) { SetStatus($"File not found: {file}"); return; }
        var existing = _editorTabs.TabPages.Cast<TabPage>().FirstOrDefault(p => p.Tag is EditorInfo i && string.Equals(i.FilePath, file, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _editorTabs.SelectedTab = existing;
            if (existing.Tag is EditorInfo info) GoToLine(info.Editor, line);
            return;
        }

        var editor = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9f),
            WordWrap = false,
            AcceptsTab = true,
            HideSelection = false,
            DetectUrls = false,
            Text = File.ReadAllText(file)
        };
        editor.SelectionChanged += (_, _) => UpdateCurrentLine(editor, file);
        editor.KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                File.WriteAllText(file, editor.Text);
                e.SuppressKeyPress = true;
                SetStatus($"Saved {file}");
            }
        };

        var page = new TabPage(Path.GetFileName(file));
        page.Tag = new EditorInfo(file, editor);
        page.Controls.Add(editor);
        _editorTabs.TabPages.Add(page);
        _editorTabs.SelectedTab = page;
        _currentFile = file;
        GoToLine(editor, line);
        _study.AddEntry("Opened/Closed Files:", StudyEntry.Open(Path.GetFileName(file), line, "Opened", file));
        SetStatus($"Opened {file}");
    }

    private void UpdateCurrentLine(RichTextBox editor, string file)
    {
        _currentFile = file;
        _currentLine = editor.GetLineFromCharIndex(editor.SelectionStart) + 1;
    }

    private static void GoToLine(RichTextBox editor, int line)
    {
        line = Math.Max(1, line);
        var lines = editor.Lines;
        var offset = 0;
        for (var i = 0; i < Math.Min(line - 1, lines.Length); i++) offset += lines[i].Length + 1;
        editor.Focus();
        editor.SelectionStart = Math.Min(offset, editor.TextLength);
        editor.SelectionLength = 0;
        editor.ScrollToCaret();
    }

    private async Task FindFilesDialogAsync()
    {
        if (!RequireProject()) return;
        using var dialog = new FindDialog("Find files (uses regular expressions)", "Search Term", "\\.(cpp|hpp|h|c|C|H|cxx|hxx|cs|java|py)$", showWholeWord: false, showRegex: false, showFilePattern: false);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunFindAsync(dialog.SearchText, dialog.IgnoreCase);
    }

    private async Task RunFindAsync(string pattern, bool ignoreCase)
    {
        var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
        Regex regex;
        try { regex = new Regex(pattern, options); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Invalid regular expression"); return; }
        var files = Directory.EnumerateFiles(_projectPath!, "*", SearchOption.AllDirectories).ToList();
        _progress.Value = 0;
        _study.AddHeader("Find Results:");
        await Task.Run(() =>
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (regex.IsMatch(Path.GetFileName(file)))
                {
                    BeginInvoke((Action)(() => _study.AddEntry("Find Results:", StudyEntry.Open(Path.GetFileName(file), 1, "Found", file))));
                }
                ReportProgress(i + 1, files.Count);
            }
        });
        SetStatus("Find completed");
    }

    private async Task GrepDialogAsync()
    {
        if (!RequireProject()) return;
        using var dialog = new FindDialog("Find in files (Grep)", "Search Term", "\\.(cpp|hpp|h|c|C|H|cxx|hxx|cs|java|py)$", showWholeWord: true, showRegex: true, showFilePattern: true);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunGrepAsync(dialog.FilePattern, dialog.SearchText, dialog.FileIgnoreCase, dialog.IgnoreCase, dialog.WholeWord, dialog.UseRegex);
    }

    private async Task RunGrepAsync(string filePattern, string searchText, bool fileIgnoreCase, bool ignoreCase, bool wholeWord, bool useRegex)
    {
        Regex fileRegex;
        Regex textRegex;
        try
        {
            fileRegex = new Regex(string.IsNullOrWhiteSpace(filePattern) ? "\\.(cpp|hpp|h|c|C|H|cxx|hxx|cs|java|py)$" : filePattern, fileIgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
            var pattern = useRegex ? searchText : Regex.Escape(searchText);
            if (wholeWord) pattern = $"\\b{pattern}\\b";
            textRegex = new Regex(pattern, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Invalid regular expression"); return; }

        var files = Directory.EnumerateFiles(_projectPath!, "*", SearchOption.AllDirectories)
            .Where(f => fileRegex.IsMatch(Path.GetFileName(f)))
            .Where(f => !f.EndsWith(".study", StringComparison.OrdinalIgnoreCase))
            .ToList();
        _progress.Value = 0;
        _study.AddHeader("Grep Results:");
        await Task.Run(() =>
        {
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                try
                {
                    var lineNo = 0;
                    foreach (var line in File.ReadLines(file))
                    {
                        lineNo++;
                        if (textRegex.IsMatch(line))
                        {
                            var entry = StudyEntry.Open(Path.GetFileName(file), lineNo, line.Trim(), file);
                            BeginInvoke((Action)(() => _study.AddEntry("Grep Results:", entry)));
                        }
                    }
                }
                catch { }
                ReportProgress(i + 1, files.Count);
            }
        });
        SetStatus("Grep completed");
    }

    private async Task BuildTagsAsync()
    {
        if (!RequireProject()) return;
        var files = Directory.EnumerateFiles(_projectPath!, "*", SearchOption.AllDirectories).Where(IsCodeLikeFile).ToList();
        var tagsPath = TagsFilePath()!;
        var sb = new StringBuilder();
        var patterns = new[]
        {
            (Kind:"c", Regex:new Regex(@"\b(class|struct)\s+([A-Za-z_][A-Za-z0-9_]*)")),
            (Kind:"f", Regex:new Regex(@"\b([A-Za-z_][A-Za-z0-9_:<>~*&\s]+)\s+([A-Za-z_][A-Za-z0-9_:~]*)\s*\([^;]*\)\s*(\{|$)")),
            (Kind:"d", Regex:new Regex(@"^\s*#\s*define\s+([A-Za-z_][A-Za-z0-9_]*)"))
        };
        _progress.Value = 0;
        await Task.Run(() =>
        {
            for (var i = 0; i < files.Count; i++)
            {
                var rel = Path.GetRelativePath(_projectPath!, files[i]).Replace('\\', '/');
                var lineNo = 0;
                foreach (var line in SafeReadLines(files[i]))
                {
                    lineNo++;
                    foreach (var p in patterns)
                    {
                        var m = p.Regex.Match(line);
                        if (!m.Success) continue;
                        var name = p.Kind == "f" ? m.Groups[2].Value : m.Groups[^1].Value;
                        if (string.IsNullOrWhiteSpace(name) || name is "if" or "for" or "while" or "switch") continue;
                        lock (sb) sb.Append(name).Append('\t').Append(rel).Append('\t').Append(lineNo).Append(";\"\t").Append(p.Kind).AppendLine();
                    }
                }
                ReportProgress(i + 1, files.Count);
            }
        });
        File.WriteAllText(tagsPath, sb.ToString());
        SetStatus($"Tags reference built: {tagsPath}");
    }

    private async Task FindTagDialogAsync()
    {
        if (!RequireProject()) return;
        using var dialog = new FindDialog("Ctags Search", "Enter the tag name here", "", showWholeWord: true, showRegex: true, showFilePattern: false);
        dialog.WholeWordLabel = "Match exact tag";
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await RunTagSearchAsync(dialog.SearchText, dialog.IgnoreCase, dialog.WholeWord, dialog.UseRegex);
    }

    private async Task RunTagSearchAsync(string search, bool ignoreCase, bool exact, bool useRegex)
    {
        var tagsPath = TagsFilePath();
        if (tagsPath == null || !File.Exists(tagsPath)) { MessageBox.Show(this, "No tags file exists. Use Ctags > Build tags reference first, or place a ctags-compatible tags file in the project folder."); return; }
        Regex? regex = null;
        if (!exact)
        {
            try { regex = new Regex(useRegex ? search : Regex.Escape(search), ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Invalid regular expression"); return; }
        }
        var lines = File.ReadAllLines(tagsPath).Where(l => !l.StartsWith("!", StringComparison.Ordinal)).ToList();
        _progress.Value = 0;
        _study.AddHeader("Ctags Results:");
        await Task.Run(() =>
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var fields = lines[i].Split('\t');
                if (fields.Length >= 4)
                {
                    var tag = fields[0];
                    var match = exact ? string.Equals(tag, search, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) : regex!.IsMatch(tag);
                    if (match)
                    {
                        var lineText = fields[2].Replace(";\"", "");
                        _ = int.TryParse(lineText, out var lineNo);
                        var file = Path.Combine(_projectPath!, fields[1].Replace('/', Path.DirectorySeparatorChar));
                        var type = CtagsKindName(fields[3]);
                        BeginInvoke((Action)(() => _study.AddEntry("Ctags Results:", StudyEntry.Open(Path.GetFileName(file), Math.Max(1, lineNo), $"{tag} is {type}", file))));
                    }
                }
                ReportProgress(i + 1, lines.Count);
            }
        });
        SetStatus("Ctags search completed");
    }

    private string? TagsFilePath() => _projectPath == null ? null : Path.Combine(_projectPath, "tags");
    private void TryDeleteTagsIndexCache() { }

    private static string CtagsKindName(string type) => type.Trim() switch
    {
        "c" => "Class Name", "d" => "Macro Definition", "e" => "Enumerator", "f" => "Function/Method", "F" => "File", "g" => "Enumeration Name", "m" => "Member Name", "p" => "Function Prototype", "s" => "Structure Name", "t" => "Typedef Name", "u" => "Union Name", _ => "Variable"
    };

    private static IEnumerable<string> SafeReadLines(string file)
    {
        try { return File.ReadLines(file); } catch { return Array.Empty<string>(); }
    }

    private void AddRecordDialog()
    {
        using var dialog = new NoteDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var type = dialog.RecordType;
        var text = dialog.RecordText;
        var file = _currentFile ?? "";
        var name = string.IsNullOrEmpty(file) ? "" : Path.GetFileName(file);
        _study.AddEntry(type + ":", StudyEntry.WithBody(name, _currentLine, type, file, text));
    }

    private void SaveOpenTabsInStudy()
    {
        foreach (var info in _editorTabs.TabPages.Cast<TabPage>().Select(p => p.Tag).OfType<EditorInfo>())
            _study.AddEntry("Opened/Closed Files:", StudyEntry.Open(Path.GetFileName(info.FilePath), 1, "Opened", info.FilePath));
        SetStatus("Open tabs saved in study");
    }

    private void AddBookmark()
    {
        if (_currentFile == null) return;
        _bookmarks.Add(new Bookmark(_currentFile, _currentLine));
        _study.AddEntry("Bookmarks:", StudyEntry.Open(Path.GetFileName(_currentFile), _currentLine, "Bookmark", _currentFile));
        SetStatus($"Bookmark added at line {_currentLine}");
    }

    private void NavigateBookmark(int direction)
    {
        if (_bookmarks.Count == 0) return;
        var currentIndex = _bookmarks.FindIndex(b => string.Equals(b.FilePath, _currentFile, StringComparison.OrdinalIgnoreCase) && b.Line == _currentLine);
        var next = currentIndex < 0 ? (direction > 0 ? 0 : _bookmarks.Count - 1) : (currentIndex + direction + _bookmarks.Count) % _bookmarks.Count;
        var bm = _bookmarks[next];
        OpenFileInEditor(bm.FilePath, bm.Line);
    }

    private void SaveStudy()
    {
        if (!RequireProject()) return;
        var path = StudyPath();
        File.WriteAllText(path, JsonSerializer.Serialize(_study.Export(), new JsonSerializerOptions { WriteIndented = true }));
        SetStatus($"Study saved: {path}");
    }

    private void LoadStudy()
    {
        if (!RequireProject()) return;
        var path = StudyPath();
        if (!File.Exists(path)) { SetStatus($"Study file not found: {path}"); return; }
        var data = JsonSerializer.Deserialize<List<StudySection>>(File.ReadAllText(path)) ?? new List<StudySection>();
        _study.Import(data);
        SetStatus($"Study loaded: {path}");
    }

    private string StudyPath()
    {
        var name = string.IsNullOrWhiteSpace(_studyFileName.Text) ? "project.study" : _studyFileName.Text.Trim();
        if (Path.IsPathRooted(name)) return name;
        return Path.Combine(_projectPath!, name);
    }

    private bool RequireProject()
    {
        if (!string.IsNullOrWhiteSpace(_projectPath) && Directory.Exists(_projectPath)) return true;
        MessageBox.Show(this, "Open or set a project folder first.", "CodeDetective");
        return false;
    }

    private void ReportProgress(int done, int total)
    {
        var value = total <= 0 ? 100 : Math.Min(100, Math.Max(0, (int)(done * 100.0 / total)));
        if (!IsDisposed) BeginInvoke((Action)(() => _progress.Value = value));
    }

    private void SetStatus(string text)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => SetStatus(text))); return; }
        _statusText.Text = text;
    }

    private void ShowAbout()
    {
        MessageBox.Show(this,
            "CodeDetective WinForms Port\n\nOriginal author: Anoop Kumar Narayanan\nOriginal version: 0.0.6\nPort: native C# WinForms for Windows\n\nIncludes project tree, tabbed editor, study view, notes, find, grep, and ctags-compatible tag search.",
            "About CodeDetective", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private sealed record EditorInfo(string FilePath, RichTextBox Editor);
    private sealed record Bookmark(string FilePath, int Line);
}
