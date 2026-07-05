# CodeDetective WinForms UI Update

This package updates the WinForms port with the requested study/result UI changes:

- Home tab is the first tab and is not closable.
- All editor/source tabs after Home are closable with an `x` on the tab.
- Source editor now uses `LineNumberedRichTextBox` with a left-side line number gutter.
- Opened/Closed Files section uses a multi-column `ListView` table.
- Find Results section uses a multi-column `ListView` table.
- Grep / Find + Grep Results section uses a multi-column `ListView` table.
- Ctags / Tags Results section uses a multi-column `ListView` table with Tag, Kind, File, Line, and Path columns.
- Table sections automatically resize between 300 px and 800 px high.
- Note, Todo/ToDo, and Code study sections use read-only multiline `TextBox` controls.
