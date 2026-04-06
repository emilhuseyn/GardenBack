using App.Business.DTOs.Divisions;
using App.Business.DTOs.Payments;
using App.Business.DTOs.Attendance;
using App.Business.DTOs.Cashboxes;
using FluentValidation;

namespace App.Business.Validators
{
    public class CreateDivisionValidator : AbstractValidator<CreateDivisionRequest>
    {
        public CreateDivisionValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Bölmə adı tələb olunur.").MaximumLength(200);
            RuleFor(x => x.Language).NotEmpty().WithMessage("Dil tələb olunur.").MaximumLength(100);
            RuleFor(x => x.Description).MaximumLength(500);
        }
    }

    public class RecordPaymentValidator : AbstractValidator<RecordPaymentRequest>
    {
        public RecordPaymentValidator()
        {
            RuleFor(x => x.ChildId).GreaterThan(0).WithMessage("Uşaq tələb olunur.");
            RuleFor(x => x.Month).InclusiveBetween(1, 12).WithMessage("Ay 1-12 arasında olmalıdır.");
            RuleFor(x => x.Year).InclusiveBetween(2020, 2100).WithMessage("İl 2020-2100 arasında olmalıdır.");
            RuleFor(x => x.CashboxId).GreaterThan(0).WithMessage("Kassa seçimi tələb olunur.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Məbləğ 0-dan böyük olmalıdır.");
        }
    }

    public class MarkAttendanceValidator : AbstractValidator<MarkAttendanceRequest>
    {
        public MarkAttendanceValidator()
        {
            RuleFor(x => x.ChildId).GreaterThan(0).WithMessage("Uşaq tələb olunur.");
            RuleFor(x => x.Date).NotEmpty().WithMessage("Tarix tələb olunur.");
        }
    }

    public class DiscountRequestValidator : AbstractValidator<DiscountRequest>
    {
        public DiscountRequestValidator()
        {
            RuleFor(x => x.DiscountType).IsInEnum().WithMessage("Yanlış endirim növü.");
            RuleFor(x => x.DiscountValue).GreaterThanOrEqualTo(0).WithMessage("Endirim dəyəri 0-dan az ola bilməz.");
        }
    }

    public class CreateCashboxValidator : AbstractValidator<CreateCashboxRequest>
    {
        public CreateCashboxValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Kassa adı tələb olunur.")
                .MaximumLength(150);

            RuleFor(x => x.Type)
                .NotEmpty().WithMessage("Kassa tipi tələb olunur.");

            RuleFor(x => x.AccountNumber)
                .MaximumLength(100)
                .When(x => x.AccountNumber != null);
        }
    }

    public class UpdateCashboxValidator : AbstractValidator<UpdateCashboxRequest>
    {
        public UpdateCashboxValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(150)
                .When(x => x.Name != null);

            RuleFor(x => x.Type)
                .NotEmpty().When(x => x.Type != null);

            RuleFor(x => x.AccountNumber)
                .MaximumLength(100)
                .When(x => x.AccountNumber != null);
        }
    }

    public class CashboxOperationValidator : AbstractValidator<CashboxOperationRequest>
    {
        public CashboxOperationValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Məbləğ 0-dan böyük olmalıdır.");

            RuleFor(x => x.Note)
                .MaximumLength(500)
                .When(x => x.Note != null);

            RuleFor(x => x.OperationDate)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.OperationDate.HasValue)
                .WithMessage("Əməliyyat tarixi gələcək tarix ola bilməz.");
        }
    }
}
