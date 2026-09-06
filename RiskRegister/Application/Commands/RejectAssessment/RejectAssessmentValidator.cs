namespace Rezilio.Modules.RiskRegister.Application.Commands.RejectAssessment;

public sealed class RejectAssessmentValidator : AbstractValidator<RejectAssessmentCommand>
{
    public RejectAssessmentValidator()
    {
        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("A RejectionReason kötelező.")
            .MaximumLength(2000).WithMessage("A RejectionReason maximum 2000 karakter lehet.");
    }
}
