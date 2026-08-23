using ClosedXML.Excel;
using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Infrastructure.Excel;

public sealed class ExcelImportParser : IExcelImportParser
{
    private readonly IExcelTemplateGenerator _templateGenerator;

    public ExcelImportParser(IExcelTemplateGenerator templateGenerator)
    {
        _templateGenerator = templateGenerator;
    }

    public IReadOnlyList<ParsedRow> Parse(byte[] fileContent, EntityType entityType)
    {
        var columns = _templateGenerator.GetColumns(entityType);
        var results = new List<ParsedRow>();

        using var ms = new MemoryStream(fileContent);
        using var workbook = new XLWorkbook(ms);
        var sheet = workbook.Worksheets.First();

        // Fejléc leolvasása (1. sor) → oszlop index map
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int c = 1; c <= lastCol; c++)
        {
            var header = sheet.Cell(1, c).GetString().TrimEnd(' ', '*').Trim();
            if (!string.IsNullOrWhiteSpace(header))
            {
                headerMap[header] = c;
            }
        }

        // Adatsorok (2. sortól, ha az 1. példasor → 3. sortól)
        // Konvenció: 2. sor = példasor (template-ből), 3. sortól = adat
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 3; row <= lastRow; row++)
        {
            if (IsRowEmpty(sheet, row, lastCol)) { continue; }

            var values = new Dictionary<string, string?>();
            var errors = new List<string>();

            foreach (var colDef in columns)
            {
                // Fejlécből keresünk — tolerálja a "Name *" formátumot is
                if (!headerMap.TryGetValue(colDef.Name, out int colIndex))
                {
                    errors.Add($"Hiányzó oszlop: {colDef.Name}");
                    values[colDef.Name] = null;
                    continue;
                }

                var value = sheet.Cell(row, colIndex).GetString()?.Trim();
                values[colDef.Name] = string.IsNullOrWhiteSpace(value) ? null : value;

                if (colDef.IsRequired && string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"{colDef.Name}: kötelező mező hiányzik");
                }
            }

            results.Add(new ParsedRow(
                RowNumber: row,
                IsValid: errors.Count == 0,
                Values: values,
                Errors: errors));
        }

        return results;
    }

    private static bool IsRowEmpty(IXLWorksheet sheet, int row, int lastCol)
    {
        for (int c = 1; c <= lastCol; c++)
        {
            if (!string.IsNullOrWhiteSpace(sheet.Cell(row, c).GetString()))
            {
                return false;
            }
        }
        return true;
    }
}
