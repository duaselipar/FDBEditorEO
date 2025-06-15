# 🗂️ FDB Editor EO

A lightweight, modern **FDB table editor** for Eudemons Online private server files.  
Supports massive files, instant sorting, batch edit, CSV import/export, and more — without needing Excel!

[![GitHub stars](https://img.shields.io/github/stars/duaselipar/FDBEditorEO.svg?style=social)](https://github.com/duaselipar/FDBEditorEO)
[![Facebook](https://img.shields.io/badge/Facebook-Profile-blue)](https://www.facebook.com/profile.php?id=61554036273018)

---

## 🚀 Features

- **Super Fast:** Can handle huge .fdb files with tens of thousands of rows (optimized RAM usage)
- **Instant Table Editing:** 
  - Edit, sort, search, multi-select, and delete rows in real time
  - Manual or shortcut-based row editing (add, copy, paste, delete)
- **Custom Column Sorting:** Click column header to sort ascending/descending
- **Multi-row Selection:** Ctrl/Shift+Click for group operations
- **Right-click Context Menu:**  
  - Add New Row
  - Copy / Paste Row(s) (can copy multiple at once)
  - Delete Selected Row(s)
  - Export Selected Row(s) to CSV
- **Full CSV Export & Import:**  
  - Export all or selected rows as clean CSV
  - Import CSV (auto-mapping by column name, skip duplicates by first column/key)
  - Error reporting for malformed or duplicate rows
- **Search:**  
  - Find any value, or limit to specific column (dropdown)
  - "Find Next" wraps around
- **Column Auto-fit:**  
  - Auto-fit columns to content for easier viewing

---

## 📸 Screenshots

> ![FDBEditorEO screenshot](https://raw.githubusercontent.com/duaselipar/FDBEditorEO/main/screenshots/main.png)

---

## 📄 How To Use

1. **Load FDB File:**  
   Click **Load FDB** button and select your `.fdb` file.

2. **Edit Data:**  
   - Click to edit any cell (auto-type based on field)
   - Right-click row for copy/paste/delete/export
   - Use column header to sort

3. **Import/Export CSV:**  
   - **Export:** Click *Export* or right-click → *Export Selected Row(s)*
   - **Import:** Click *Import*, select your CSV file. Duplicates (based on first column) are skipped.

4. **Search:**  
   - Type in search box, choose column (or All), and hit Enter / click Search
   - Keep pressing Search to find next result

5. **Save:**  
   Click **Save** to write back to `.fdb` (new file, doesn’t overwrite original by default).

---

## 📋 CSV Format

- **Header required:** First row must contain column names.
- **Column mapping:** Only columns matching FDB field names are used.
- **Unique key:** First column in CSV must be unique per row (e.g. `uID`).

---

## 💡 Tips & Shortcuts

- **Ctrl+C**: Copy row(s) to clipboard (can paste to Notepad as CSV)
- **Ctrl+V**: Paste row(s) back (auto adds new rows as needed)
- **Ctrl+N**: Add new row
- **Del**: Delete selected rows
- **Ctrl+E**: Export selected rows as CSV
- **Ctrl+F**: Focus search box

---
## 🗨️ Support & Contact

- **Facebook:** [duaselipar](https://www.facebook.com/profile.php?id=61554036273018)

Feel free to report bugs on Facebook!
