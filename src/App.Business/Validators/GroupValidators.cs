using App.Business.DTOs.Groups;
using FluentValidation;

namespace App.Business.Validators
{
    public class CreateGroupValidator : AbstractValidator<CreateGroupRequest>
    {
        public CreateGroupValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Qrup adı tələb olunur.").MaximumLength(200);
            RuleFor(x => x.DivisionId).GreaterThan(0).WithMessage("Bölmə tələb olunur.");
            RuleFor(x => x.TeacherId).NotEmpty().WithMessage("Müəllim tələb olunur.");
            RuleFor(x => x.MaxChildCount).GreaterThan(0).WithMessage("Maksimum uşaq sayı 0-dan böyük olmalıdır.").LessThanOrEqualTo(50);
            RuleFor(x => x.AgeCategory).NotEmpty().WithMessage("Yaş kateqoriyası tələb olunur.").MaximumLength(50);
            RuleFor(x => x.Language).NotEmpty().WithMessage("Dil tələb olunur.").MaximumLength(100);
        }
    }

    public class UpdateGroupValidator : AbstractValidator<UpdateGroupRequest>
    {
        public UpdateGroupValidator()
        {
            RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name != null);
            RuleFor(x => x.MaxChildCount).GreaterThan(0).LessThanOrEqualTo(50).When(x => x.MaxChildCount.HasValue);
        }
    }
}
