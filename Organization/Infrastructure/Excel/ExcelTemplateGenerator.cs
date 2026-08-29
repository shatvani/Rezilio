using ClosedXML.Excel;
using Rezilio.Modules.Organization.Application.Services;

namespace Rezilio.Modules.Organization.Infrastructure.Excel;

public sealed class ExcelTemplateGenerator : IExcelTemplateGenerator
{
    private readonly IReadOnlyDictionary<EntityType, IImportColumnDefinitionProvider> _providers;

    public ExcelTemplateGenerator(IEnumerable<IImportColumnDefinitionProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.EntityType);
    }

    public IReadOnlyList<ImportColumnDefinition> GetColumns(EntityType entityType)
    {
        if (!_providers.TryGetValue(entityType, out var provider))
        {
            throw new NotSupportedException($"No column definition provider for EntityType: {entityType}");
        }

        return provider.GetColumns();
    }

    public byte[] GenerateTemplate(EntityType entityType)
    {
        var columns = GetColumns(entityType);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(entityType.ToString());

        // --- Fejlécsor (1. sor) ---
        for (int i = 0; i < columns.Count; i++)
        {
            var col = columns[i];
            var cell = sheet.Cell(1, i + 1);
            cell.Value = col.IsRequired ? $"{col.Name} *" : col.Name;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = col.IsRequired
                ? XLColor.FromHtml("#D9534F")   // piros – kötelező
                : XLColor.FromHtml("#5B9BD5");  // kék – opcionális
            cell.Style.Font.FontColor = XLColor.White;
        }

        // --- Példasor (2. sor) ---
        for (int i = 0; i < columns.Count; i++)
        {
            sheet.Cell(2, i + 1).Value = columns[i].ExampleValue;
        }

        // --- Oszlopszélesség auto ---
        sheet.Columns().AdjustToContents();

        // --- Fejléc rögzítése ---
        sheet.SheetView.FreezeRows(1);

        // --- Jelmagyarázat (utolsó oszlop után) ---
        int legendCol = columns.Count + 2;
        sheet.Cell(1, legendCol).Value = "* = kötelező mező";
        sheet.Cell(1, legendCol).Style.Font.Italic = true;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
