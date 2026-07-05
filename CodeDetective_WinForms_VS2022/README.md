# CodeDetective WinForms Port

This package is a native C# WinForms port of the uploaded Qt5 CodeDetective project.

## What was ported

- Project folder tree using `TreeView`
- Tabbed source editor using WinForms `TabControl` + `RichTextBox`
- Study view with clickable entries
- Note/Todo/Code records
- Bookmark, back, and forward actions
- Built-in file find using regular expressions
- Built-in grep/search-in-files with file pattern, ignore case, whole word, and regex options
- Ctags-compatible tag-file search
- Internal "Build tags reference" command that creates a simple ctags-like `tags` file for C/C++/C#/Java/Python-style projects
- Save/load study file as JSON, defaulting to `project.study` in the project folder

## Build requirements

- Windows 10 or later
- Visual Studio 2022
- .NET 8 SDK with Windows Desktop/WinForms workload

## Build

Open `CodeDetective.WinForms.sln` in Visual Studio 2022 and build `Release|Any CPU`.

Command line build on Windows:

```bat
dotnet build CodeDetective.WinForms.sln -c Release
```

The executable is created under:

```text
CodeDetective.WinForms\bin\Release\net8.0-windows\CodeDetective.exe
```

## Notes

The original Qt project used FakeVim as the editor component. This port uses WinForms `RichTextBox` to keep the project self-contained and easy to compile. It preserves the main CodeDetective workflows, but it does not attempt to reimplement FakeVim modal editing behavior.

External `ctags` is not required for the included tag builder, but the tag search can also read a normal ctags-compatible `tags` file placed in the project folder.
