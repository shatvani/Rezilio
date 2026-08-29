namespace Rezilio.Modules.Organization.Application.Services;

public sealed class CustomerColumnDefinitionProvider : IImportColumnDefinitionProvider
{
    public EntityType EntityType => EntityType.Customer;

    public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
    [
        new ImportColumnDefinition("Name",         IsRequired: true,  ExampleValue: "Minta Kft."),
        new ImportColumnDefinition("Code",         IsRequired: true,  ExampleValue: "MINTA"),
        new ImportColumnDefinition("Industry",     IsRequired: false, ExampleValue: "Kereskedelem"),
        new ImportColumnDefinition("Country",      IsRequired: false, ExampleValue: "Magyarország"),
        new ImportColumnDefinition("ContactEmail", IsRequired: false, ExampleValue: "kapcsolat@minta.hu"),
        new ImportColumnDefinition("ContactPhone", IsRequired: false, ExampleValue: "+36 1 234 5678"),
        new ImportColumnDefinition("Description",  IsRequired: false, ExampleValue: "Fő viszonteladó partner"),
    ];
}
