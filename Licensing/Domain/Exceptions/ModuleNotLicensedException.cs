namespace Rezilio.Modules.Licensing.Domain.Exceptions;

public sealed class ModuleNotLicensedException : Exception
{
    public ModuleType Module { get; }

    public ModuleNotLicensedException(ModuleType module)
        : base($"A(z) '{module}' modul nem aktív ennél a tenantnél.")
    {
        Module = module;
    }
}
