# Editor File Workflow Update

This update turns source tabs into editable document tabs.

## Added

- File > New Text Window for an unnamed empty editor tab.
- File > Open File for opening any text/source file directly into the editor.
- File > Save for saving the selected editor tab.
- File > Save As for saving the selected editor tab under a different name.
- File > Save Backup Copy for writing a timestamped backup file.
- Keyboard shortcuts:
  - Ctrl+N: New Text Window
  - Ctrl+O: Open File
  - Ctrl+S: Save
  - Ctrl+Shift+S: Save As
  - Ctrl+W: Close Current Tab

## Backup filename format

Backups use the local system time and this suffix pattern:

```text
_bkp_Year_Month_Date__Hour_min_sec
```

For example:

```text
source_bkp_2026_07_05__17_20_41.cpp
```

## Improved editor behavior

- Edited tabs show an asterisk prefix, such as `*main.cpp`.
- Closing a dirty editor tab asks whether to save changes.
- Closing the project or application prompts for unsaved changes.
- Newly opened untitled text windows can be saved as new files.
