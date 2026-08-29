namespace Rezilio.Modules.Organization.Application.Services;

public sealed class OrganizationalUnitColumnDefinitionProvider : IImportColumnDefinitionProvider
{
    public EntityType EntityType => EntityType.OrganizationalUnit;

    public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
    [
        new ImportColumnDefinition("Name",        IsRequired: true,  ExampleValue: "IT Osztály",     Description: "A szervezeti egység teljes neve"),
        new ImportColumnDefinition("Code",        IsRequired: true,  ExampleValue: "IT",             Description: "Rövid egyedi azonosító (nagybetűsre konvertálódik)"),
        new ImportColumnDefinition("ParentCode",  IsRequired: false, ExampleValue: "CTO",            Description: "Szülő szervezeti egység kódja"),
        new ImportColumnDefinition("Description", IsRequired: false, ExampleValue: "IT infrastruktúra és fejlesztés", Description: "Leírás"),
    ];
}
