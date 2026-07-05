# Porting notes

The uploaded Qt5 project was inspected and translated into a native WinForms implementation.

## Qt to WinForms mapping

| Qt5 component/workflow | WinForms port |
|---|---|
| `QMainWindow` | `MainForm : Form` |
| `QSplitter` | `SplitContainer` |
| `QTreeView` + `QFileSystemModel` | `TreeView` with lazy directory population |
| `QTabWidget` | `TabControl` |
| FakeVim editor widget | `RichTextBox` editor tabs |
| `CodeDetectiveStudyNative` | `StudyView : UserControl` with clickable entries |
| `CodeDetectiveNote` | `NoteDialog` |
| `CodeDetectiveFind` | `FindDialog` + async regex file search |
| `CodeDetectiveGrep` | `FindDialog` + async search-in-files |
| `CodeDetectiveCtags` | ctags-compatible tag parser and simple built-in tag generator |
| Qt signals/slots | C# events and event handlers |

## Intentional differences

- FakeVim modal editing was not reimplemented. The WinForms version uses the native `RichTextBox` editor to keep the port self-contained.
- The built-in ctags builder is lightweight and regex-based. It is enough for navigation, but a full Universal Ctags integration can be added later.
- Study files are JSON instead of the original internal Qt/native control serialization format.

## Main files

- `CodeDetective.WinForms/MainForm.cs` - main shell and workflows
- `CodeDetective.WinForms/StudyView.cs` - study control and saved study model
- `CodeDetective.WinForms/Dialogs.cs` - find, grep, ctags, and note dialogs
- `CodeDetective.WinForms/Program.cs` - WinForms app entry point
