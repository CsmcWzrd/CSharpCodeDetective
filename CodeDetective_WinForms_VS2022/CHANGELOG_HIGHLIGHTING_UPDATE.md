# CodeDetective WinForms - Source Editor Update

## Changes

- Reduced line-number gutter flicker during scrolling.
  - Replaced the plain gutter panel with a double-buffered custom painting panel.
  - Suppressed background erase for the gutter.
  - Debounced gutter repaints during scroll, mouse wheel, resize, and text changes.
  - Uses the RichTextBox first-visible-line message for stable line number positioning.

- Added lightweight syntax highlighting for displayed source code.
  - Highlights common language keywords.
  - Highlights strings, comments, numeric literals, preprocessor lines, and PascalCase type names.
  - Supports common C, C++, C#, Java, Python, JavaScript, TypeScript, Go, and Rust style syntax.
  - Uses a delayed re-highlight timer while editing to avoid repainting on every keystroke.
  - Temporarily disables RichTextBox redraw while applying colors to minimize visual flashing.
  - Skips automatic highlighting for very large files over 1,250,000 characters.

## Files changed

- `CodeDetective.WinForms/LineNumberedRichTextBox.cs`
- `CodeDetective.WinForms/MainForm.cs`
