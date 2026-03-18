using App.Business.DTOs.Commons;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Business.Validators.Commons
{
    public class BaseEntityValidator<T> : AbstractValidator<T> where T : BaseEntityDTO
    {
        public BaseEntityValidator()
        {
            RuleFor(entity => entity.Id)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Id tələb olunur.")
                .GreaterThan(0).WithMessage("Id 0-dan böyük olmalıdır.");
        }
    }
}
