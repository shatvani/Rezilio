using Rezilio.Modules.Organization.Domain;

namespace Rezilio.Modules.Organization.Application.Services;

public sealed class KeyPersonColumnDefinitionProvider : IImportColumnDefinitionProvider
{
    public EntityType EntityType => EntityType.KeyPerson;

    public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
    [
        new ImportColumnDefinition("Name",             IsRequired: true,  ExampleValue: "Kovács János"),
        new ImportColumnDefinition("Title",            IsRequired: false, ExampleValue: "IT Igazgató"),
        new ImportColumnDefinition("Department",       IsRequired: false, ExampleValue: "Informatika"),
        new ImportColumnDefinition("OrgUnitCode",      IsRequired: false, ExampleValue: "IT"),
        new ImportColumnDefinition("Email",            IsRequired: false, ExampleValue: "kovacs.janos@ceg.hu"),
        new ImportColumnDefinition("Phone",            IsRequired: false, ExampleValue: "+36 30 123 4567"),
        new ImportColumnDefinition("BackupPersonName", IsRequired: false, ExampleValue: "Nagy Péter"),
        new ImportColumnDefinition("Description",      IsRequired: false, ExampleValue: "IT infrastruktúra felelős"),
    ];
}
