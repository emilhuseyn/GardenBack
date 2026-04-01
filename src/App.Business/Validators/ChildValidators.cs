using App.Business.DTOs.Children;
using FluentValidation;

namespace App.Business.Validators
{
    public class CreateChildValidator : AbstractValidator<CreateChildRequest>
    {
        public CreateChildValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad tələb olunur.")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad tələb olunur.")
                .MaximumLength(100);

            RuleFor(x => x.DateOfBirth)
                .NotEmpty().WithMessage("Doğum tarixi tələb olunur.")
                .LessThan(DateTime.UtcNow).WithMessage("Doğum tarixi keçmiş tarixdə olmalıdır.")
                .Must(dob => DateTime.UtcNow.Year - dob.Year is >= 1 and <= 7)
                .WithMessage("Uşaq 1-7 yaş arasında olmalıdır.");

            RuleFor(x => x.GroupId)
                .GreaterThan(0).WithMessage("Qrup tələb olunur.");

            RuleFor(x => x.MonthlyFee)
                .GreaterThan(0).WithMessage("Aylıq ödəniş 0-dan böyük olmalıdır.");

            RuleFor(x => x.PaymentDay)
                .InclusiveBetween(1, 28).WithMessage("Ödəniş günü 1 ilə 28 arasında olmalıdır.");

            RuleFor(x => x.ParentFullName)
                .NotEmpty().WithMessage("Valideynin tam adı tələb olunur.")
                .MaximumLength(200);

            RuleFor(x => x.ParentPhone)
                .NotEmpty().WithMessage("Valideynin telefon nömrəsi tələb olunur.")
                .Matches(@"^\+994\d{9}$").WithMessage("Telefon Azərbaycan formatında olmalıdır (+994XXXXXXXXX).");

            RuleFor(x => x.ParentEmail)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.ParentEmail))
                .WithMessage("E-poçt formatı yanlışdır.");

            RuleFor(x => x.ScheduleType)
                .IsInEnum().WithMessage("Yanlış qrafik növü.");
        }
    }

    public class UpdateChildValidator : AbstractValidator<UpdateChildRequest>
    {
        public UpdateChildValidator()
        {
            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .When(x => x.FirstName != null);

            RuleFor(x => x.LastName)
                .MaximumLength(100)
                .When(x => x.LastName != null);

            RuleFor(x => x.MonthlyFee)
                .GreaterThan(0).When(x => x.MonthlyFee.HasValue)
                .WithMessage("Aylıq ödəniş 0-dan böyük olmalıdır.");

            RuleFor(x => x.PaymentDay)
                .InclusiveBetween(1, 28).When(x => x.PaymentDay.HasValue)
                .WithMessage("Ödəniş günü 1 ilə 28 arasında olmalıdır.");

            RuleFor(x => x.ParentPhone)
                .Matches(@"^\+994\d{9}$").When(x => x.ParentPhone != null)
                .WithMessage("Telefon Azərbaycan formatında olmalıdır (+994XXXXXXXXX).");

            RuleFor(x => x.ParentEmail)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.ParentEmail));
        }
    }
}
