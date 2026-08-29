namespace Rezilio.Modules.Organization.Application.Services;

public sealed class BusinessProcessColumnDefinitionProvider : IImportColumnDefinitionProvider
{
    public EntityType EntityType => EntityType.BusinessProcess;

    public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
    [
        new ImportColumnDefinition("Code",                       IsRequired: true,  ExampleValue: "BP-001"),
        new ImportColumnDefinition("Name",                       IsRequired: true,  ExampleValue: "Számlázási folyamat"),
        new ImportColumnDefinition("Category",                   IsRequired: true,  ExampleValue: "Finance"),
        new ImportColumnDefinition("CriticalityLevel",           IsRequired: true,  ExampleValue: "High"),
        new ImportColumnDefinition("OwnerCode",                  IsRequired: false, ExampleValue: "KOVACS.J"),
        new ImportColumnDefinition("OrgUnitCode",                IsRequired: false, ExampleValue: "FIN"),
        new ImportColumnDefinition("MaxTolerableDowntimeMinutes",IsRequired: false, ExampleValue: "480"),
        new ImportColumnDefinition("RecoveryTimeObjectiveMinutes",IsRequired: false, ExampleValue: "240"),
        new ImportColumnDefinition("DependsOnSystemCodes",       IsRequired: false, ExampleValue: "ERP-001;CRM-002"),
    ];
}
