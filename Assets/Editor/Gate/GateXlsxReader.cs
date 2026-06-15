using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

/// <summary>
/// 의존성 없이 .xlsx(Open XML)를 읽습니다. 시트 경로는 xl/ 접두사로 정규화합니다.
/// 시트명·컬럼명이 '#'으로 시작하면 호출 측에서 스킵합니다.
/// </summary>
public static class GateXlsxReader
{
    public sealed class SheetTable
    {
        public string SheetName;
        public List<string> Headers = new();
        public List<Dictionary<string, string>> Rows = new();
    }

    public static List<SheetTable> ReadAllSheets(string xlsxPath)
    {
        using var stream = File.OpenRead(xlsxPath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var sharedStrings = LoadSharedStrings(archive);
        var sheetEntries = LoadSheetEntries(archive);

        var tables = new List<SheetTable>();
        foreach (var entry in sheetEntries)
            tables.Add(ReadSheet(entry.name, entry.stream, sharedStrings));

        return tables;
    }

    private static List<string> LoadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return new List<string>();

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        var list = new List<string>();
        foreach (var si in doc.Descendants(ns + "si"))
        {
            var text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
            list.Add(text);
        }

        return list;
    }

    private sealed class SheetEntry
    {
        public string name;
        public Stream stream;
    }

    private static List<SheetEntry> LoadSheetEntries(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml")
                          ?? throw new InvalidOperationException("workbook.xml not found");

        using var workbookStream = workbookEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        XNamespace mainNs = workbook.Root?.Name.Namespace ?? XNamespace.None;
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
                      ?? throw new InvalidOperationException("workbook.xml.rels not found");

        using var relsStream = relsEntry.Open();
        var relsDoc = XDocument.Load(relsStream);
        XNamespace relsNs = relsDoc.Root?.Name.Namespace ?? XNamespace.None;

        var relMap = relsDoc.Descendants(relsNs + "Relationship")
            .ToDictionary(
                r => r.Attribute("Id")?.Value ?? string.Empty,
                r => r.Attribute("Target")?.Value ?? string.Empty);

        var sheets = new List<SheetEntry>();
        foreach (var sheet in workbook.Descendants(mainNs + "sheet"))
        {
            var name = sheet.Attribute("name")?.Value ?? string.Empty;
            var relId = sheet.Attribute(relNs + "id")?.Value;
            if (string.IsNullOrEmpty(relId) || !relMap.TryGetValue(relId, out var target))
                continue;

            var path = target.Replace('\\', '/').TrimStart('/');
            if (path.StartsWith("/", StringComparison.Ordinal))
                path = path.TrimStart('/');

            if (!path.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                path = "xl/" + path;

            var sheetEntry = archive.GetEntry(path)
                          ?? archive.GetEntry(path.Replace('/', '\\'));
            if (sheetEntry == null)
                continue;

            sheets.Add(new SheetEntry
            {
                name = name,
                stream = sheetEntry.Open()
            });
        }

        return sheets;
    }

    private static SheetTable ReadSheet(string sheetName, Stream sheetStream, List<string> sharedStrings)
    {
        var table = new SheetTable { SheetName = sheetName };

        using (sheetStream)
        {
            var doc = XDocument.Load(sheetStream);
            XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var cellMap = new SortedDictionary<int, SortedDictionary<int, string>>();
            foreach (var cell in doc.Descendants(ns + "c"))
            {
                var reference = cell.Attribute("r")?.Value;
                if (string.IsNullOrEmpty(reference))
                    continue;

                if (!TryParseCellReference(reference, out var col, out var row))
                    continue;

                var value = ReadCellValue(cell, ns, sharedStrings);
                if (!cellMap.TryGetValue(row, out var rowCells))
                {
                    rowCells = new SortedDictionary<int, string>();
                    cellMap[row] = rowCells;
                }

                rowCells[col] = value;
            }

            if (cellMap.Count == 0)
                return table;

            var headerRowIndex = cellMap.Keys.Min();
            if (!cellMap.TryGetValue(headerRowIndex, out var headerCells))
                return table;

            var colIndexToHeader = new SortedDictionary<int, string>();
            foreach (var pair in headerCells)
                colIndexToHeader[pair.Key] = pair.Value?.Trim() ?? string.Empty;

            table.Headers.AddRange(colIndexToHeader.Values);

            foreach (var rowIndex in cellMap.Keys.Where(r => r != headerRowIndex))
            {
                if (!cellMap.TryGetValue(rowIndex, out var rowCells))
                    continue;

                var rowDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var colPair in colIndexToHeader)
                {
                    var header = colPair.Value;
                    if (string.IsNullOrEmpty(header))
                        continue;

                    rowCells.TryGetValue(colPair.Key, out var cellValue);
                    rowDict[header] = cellValue?.Trim() ?? string.Empty;
                }

                if (rowDict.Values.All(string.IsNullOrWhiteSpace))
                    continue;

                table.Rows.Add(rowDict);
            }
        }

        return table;
    }

    private static string ReadCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;

        if (type == "inlineStr")
        {
            var inline = cell.Element(ns + "is")?.Element(ns + "t");
            return inline?.Value ?? string.Empty;
        }

        var valueElement = cell.Element(ns + "v");
        if (valueElement == null)
            return string.Empty;

        var raw = valueElement.Value;
        if (type == "s" && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            return sharedStrings[sharedIndex];

        return raw;
    }

    private static bool TryParseCellReference(string reference, out int columnIndex, out int rowIndex)
    {
        columnIndex = 0;
        rowIndex = 0;

        var i = 0;
        while (i < reference.Length && char.IsLetter(reference[i]))
        {
            columnIndex = (columnIndex * 26) + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
            i++;
        }

        if (i == 0 || i >= reference.Length)
            return false;

        if (!int.TryParse(reference.Substring(i), out rowIndex))
            return false;

        columnIndex -= 1;
        return true;
    }
}
