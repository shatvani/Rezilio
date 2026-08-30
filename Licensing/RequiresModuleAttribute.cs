namespace Rezilio.Modules.Licensing;

/// <summary>
/// Egy command/query osztályra téve jelzi, hogy a végrehajtásához az adott tenantnek
/// aktív licensszel kell rendelkeznie a megadott modulra. A //see cref="Rezilio.Api.Middleware.ModuleAccessBehavior"//
/// olvassa ki reflection-nel és ellenőrzi a Wolverine pipeline-ban.
///
/// Csak azokra a modulokra kell feltenni, amik ténylegesen licenszkötelesek (pl. a jövőbeli
/// Risk/Assessment/Treatment stb. modulok parancsaira). Az Organization modul parancsain
/// szándékosan NINCS ilyen attribútum, mert az alapadat-kezelés minden tenant számára elérhető.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequiresModuleAttribute(ModuleType module) : Attribute
{
    public ModuleType Module { get; } = module;
}
