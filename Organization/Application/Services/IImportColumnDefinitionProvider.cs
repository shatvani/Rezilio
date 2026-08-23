using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Application.Services;

/// <summary>
/// EntityType-onként egy provider regisztrálja az oszlopdefiníciókat.
/// ORG.3–ORG.9 story-k adják hozzá a konkrét implementációkat.
/// </summary>
public interface IImportColumnDefinitionProvider
{
    EntityType EntityType { get; }
    IReadOnlyList<ImportColumnDefinition> GetColumns();
}
