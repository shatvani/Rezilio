using Rezilio.Modules.Organization.Application.Services;
using Rezilio.Modules.Organization.Domain;

namespace Organization.Application.Services;

public sealed class ItSystemColumnDefinitionProvider : IImportColumnDefinitionProvider
{
    public EntityType EntityType => EntityType.ItSystem;

    public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
    [
        new ImportColumnDefinition("Code",                IsRequired: true,  ExampleValue: "ERP-001"),
        new ImportColumnDefinition("Name",                IsRequired: true,  ExampleValue: "SAP ERP"),
        new ImportColumnDefinition("Type",                IsRequired: true,  ExampleValue: "Erp"),
        new ImportColumnDefinition("HostingType",         IsRequired: true,  ExampleValue: "OnPrem"),
        new ImportColumnDefinition("CriticalityLevel",    IsRequired: true,  ExampleValue: "High"),
        new ImportColumnDefinition("Vendor",              IsRequired: false, ExampleValue: "SAP SE"),
        new ImportColumnDefinition("Version",             IsRequired: false, ExampleValue: "S/4HANA 2023"),
        new ImportColumnDefinition("OwnerCode",           IsRequired: false, ExampleValue: "KOVACS.J"),
        new ImportColumnDefinition("SupportedOrgUnitCodes", IsRequired: false, ExampleValue: "IT;FIN;HR"),
    ];
}
