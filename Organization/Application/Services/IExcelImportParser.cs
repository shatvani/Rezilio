namespace Rezilio.Modules.Organization.Application.Services;

public interface IExcelImportParser
{
    /// <summary>
    /// Beolvassa az Excel fájlt, validálja az oszlopokat a column definitions alapján.
    /// Nem dob exception-t — minden hibát ParsedRow.Errors-ban gyűjt.
    /// </summary>
    IReadOnlyList<ParsedRow> Parse(byte[] fileContent, EntityType entityType);
}
