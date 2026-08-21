using Rezilio.Modules.Licensing.Domain;

namespace Rezilio.Modules.Licensing.Application.Commands.StartTrial;

public sealed record StartTrialCommand(Guid TenantId, ModuleType Module);
