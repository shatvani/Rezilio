using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Application.Services;

public interface IExcelTemplateGenerator
{
    /// <summary>Letölthető .xlsx template byte tömbként.</summary>
    byte[] GenerateTemplate(EntityType entityType);

    IReadOnlyList<ImportColumnDefinition> GetColumns(EntityType entityType);
}
