using FluentValidation;
using VividImagination.Modules.Media.Features.Photos.Commands;
using VividImagination.Shared.Infrastructure.Validation;

namespace VividImagination.Modules.Media.Features.Photos.Validations
{
    public class DeleteDocumentCommandValidator : NullReferenceAbstractValidator<DeleteDocumentCommand>
    {
        public DeleteDocumentCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().NotNull();

        }

    }
}