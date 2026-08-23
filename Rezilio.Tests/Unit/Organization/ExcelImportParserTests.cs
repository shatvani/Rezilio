using ClosedXML.Excel;
using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Domain;
using Rezilio.Modules.Organization.Infrastructure.Excel;
using Xunit;

namespace Rezilio.Tests.Unit.Organization;

public class ExcelImportParserTests
{
    // --- Stub provider teszteléshez ---
    private static ExcelTemplateGenerator BuildGenerator()
    {
        var provider = new StubOrganizationalUnitProvider();
        return new ExcelTemplateGenerator([provider]);
    }

    // --- Template generálás ---

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateTemplate_ReturnsNonEmptyBytes()
    {
        var generator = BuildGenerator();
        byte[] result = generator.GenerateTemplate(EntityType.OrganizationalUnit);
        Assert.NotEmpty(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GenerateTemplate_ContainsHeaderRow()
    {
        var generator = BuildGenerator();
        byte[] result = generator.GenerateTemplate(EntityType.OrganizationalUnit);

        using var ms = new MemoryStream(result);
        using var wb = new XLWorkbook(ms);
        var sheet = wb.Worksheets.First();

        // "Name *" – kötelező mező fejléc
        string header = sheet.Cell(1, 1).GetString();
        Assert.Contains("Name", header);
    }

    // --- Parser: boldog út ---

    [Fact]
    [Trait("Category", "Unit")]
    public void Parse_ValidFile_ReturnsSuccessRows()
    {
        var generator = BuildGenerator();
        byte[] template = generator.GenerateTemplate(EntityType.OrganizationalUnit);

        // Adatsor hozzáadása a template-hez (3. sor = első adatsor)
        byte[] fileWithData = AddDataRow(template, row: 3, values: ["IT Osztály", "IT"]);

        var parser = new ExcelImportParser(generator);
        IReadOnlyList<ParsedRow> rows = parser.Parse(fileWithData, EntityType.OrganizationalUnit);

        Assert.Single(rows);
        Assert.True(rows[0].IsValid);
    }

    // --- Parser: kötelező mező hiányzik ---

    [Fact]
    [Trait("Category", "Unit")]
    public void Parse_MissingRequiredField_ReturnsInvalidRow()
    {
        var generator = BuildGenerator();
        byte[] template = generator.GenerateTemplate(EntityType.OrganizationalUnit);

        // Name hiányzik (üres)
        byte[] fileWithData = AddDataRow(template, row: 3, values: ["", "IT"]);

        var parser = new ExcelImportParser(generator);
        IReadOnlyList<ParsedRow> rows = parser.Parse(fileWithData, EntityType.OrganizationalUnit);

        Assert.Single(rows);
        Assert.False(rows[0].IsValid);
        Assert.Contains(rows[0].Errors, e => e.Contains("Name"));
    }

    // --- Parser: üres fájl (nincs adatsor) ---

    [Fact]
    [Trait("Category", "Unit")]
    public void Parse_EmptyFile_ReturnsEmptyList()
    {
        var generator = BuildGenerator();
        byte[] template = generator.GenerateTemplate(EntityType.OrganizationalUnit);

        var parser = new ExcelImportParser(generator);
        IReadOnlyList<ParsedRow> rows = parser.Parse(template, EntityType.OrganizationalUnit);

        Assert.Empty(rows);
    }

    // --- Segédmetódus: adatsor beírása a template-be ---
    private static byte[] AddDataRow(byte[] template, int row, string[] values)
    {
        using var ms = new MemoryStream(template);
        using var wb = new XLWorkbook(ms);
        var sheet = wb.Worksheets.First();

        for (int i = 0; i < values.Length; i++)
        {
            sheet.Cell(row, i + 1).Value = values[i];
        }

        using var outMs = new MemoryStream();
        wb.SaveAs(outMs);
        return outMs.ToArray();
    }

    // --- Stub provider ---
    private sealed class StubOrganizationalUnitProvider : IImportColumnDefinitionProvider
    {
        public EntityType EntityType => EntityType.OrganizationalUnit;

        public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
        [
            new ImportColumnDefinition("Name", IsRequired: true, ExampleValue: "IT Osztály"),
            new ImportColumnDefinition("Code", IsRequired: false, ExampleValue: "IT"),
        ];
    }
}
