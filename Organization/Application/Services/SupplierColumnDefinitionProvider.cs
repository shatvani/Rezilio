namespace Rezilio.Modules.Organization.Application.Services;

public sealed class SupplierColumnDefinitionProvider : IImportColumnDefinitionProvider
{
    public EntityType EntityType => EntityType.Supplier;

    public IReadOnlyList<ImportColumnDefinition> GetColumns() =>
    [
        new ImportColumnDefinition("Name",         IsRequired: true,  ExampleValue: "Minta Szállító Kft."),
        new ImportColumnDefinition("Code",         IsRequired: true,  ExampleValue: "MSZALL"),
        new ImportColumnDefinition("Industry",     IsRequired: false, ExampleValue: "Logisztika"),
        new ImportColumnDefinition("Country",      IsRequired: false, ExampleValue: "Magyarország"),
        new ImportColumnDefinition("ContactEmail", IsRequired: false, ExampleValue: "info@mintaszallito.hu"),
        new ImportColumnDefinition("ContactPhone", IsRequired: false, ExampleValue: "+36 1 234 5678"),
        new ImportColumnDefinition("Description",  IsRequired: false, ExampleValue: "Fő alapanyag szállító"),
    ];
}
