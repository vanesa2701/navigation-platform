using DTO.WebApiDTO.User;
using FluentValidation;

namespace Presentation.Validators
{
    public sealed class AdminChangeUserStatusRequestDtoApiValidator : AbstractValidator<AdminChangeUserStatusRequestDtoApi>
    {
        private static readonly string[] Allowed = new[] { "Active", "Suspended", "Deactivated" };

        public AdminChangeUserStatusRequestDtoApiValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .Must(s => Allowed.Contains(s))
                .WithMessage($"Status must be one of: {string.Join(", ", Allowed)}");
        }
    }
}
