using DTO.WebApiDTO.Journey;
using FluentValidation;

namespace Presentation.Validators
{
    public sealed class JourneyUnshareRequestDtoApiValidator : AbstractValidator<JourneyUnshareRequestDtoApi>
    {
        public JourneyUnshareRequestDtoApiValidator()
        {
            RuleFor(x => x.UserIds)
                .NotNull().WithMessage("UserIds is required.")
                .Must(list => list is { Count: > 0 })
                    .WithMessage("At least one userId must be provided.")
                .Must(list => list.Distinct().Count() == list.Count)
                    .WithMessage("Duplicate userIds are not allowed.");

            RuleFor(x => x.UserIds.Count)
                .LessThanOrEqualTo(100)
                .WithMessage("You can un-share with at most 100 users per request.");
        }
    }
}
