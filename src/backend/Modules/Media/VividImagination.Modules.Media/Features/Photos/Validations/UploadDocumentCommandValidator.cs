using System.Text.RegularExpressions;
using FluentValidation;
using VividImagination.Modules.Media.Features.Photos.Commands;
using VividImagination.Modules.Media.Persistence.Enums;
using VividImagination.Modules.Media.Services.Common;
using VividImagination.Shared.Infrastructure.Validation;

namespace VividImagination.Modules.Media.Features.Photos.Validations
{
    public partial class UploadDocumentCommandValidator : NullReferenceAbstractValidator<UploadDocumentCommand>
    {
        public UploadDocumentCommandValidator()
        {
            RuleFor(x => x.ContentStream).NotNull();
            RuleFor(x => x.ContentType).NotEmpty().Must(ValidateContentType).WithMessage("invalid content Type");
            RuleFor(x => x.Name).NotEmpty().NotNull();

            RuleFor(x => x)
                .Must(x => FileTypeHelper.IsAllowedCategory(x.FileTypes, x.FileExtension, x.ContentType))
                .When(x => x.FileTypes != StorageFileType.All && x.FileTypes != StorageFileType.None)
                .WithMessage(x => $"File '{x.FileName}' with extension '{x.FileExtension}' is not allowed for the specified file types '{x.FileTypes}'.")
                .WithName(nameof(UploadDocumentCommand.FileTypes));

            RuleFor(x => x)
                .Must(x => FileTypeHelper.IsAllowedExtension(x.AllowedFiles, x.FileExtension))
                .When(x => x.AllowedFiles != null && x.AllowedFiles.Length > 0)
                .WithMessage(x => $"File '{x.FileName}' with extension '{x.FileExtension}' is not permitted in the allowed files list.")
                .WithName(nameof(UploadDocumentCommand.AllowedFiles));
        }

        private static bool ValidateContentType(string content)
        {
            var urlMatch = MyRegex();

            var valid = urlMatch.IsMatch(content);
            return valid;
        }

        [GeneratedRegex(@"[a-zA-Z]\w/[a-zA-Z]", RegexOptions.IgnoreCase | RegexOptions.Singleline, "en-GB")]
        private static partial Regex MyRegex();
    }
}
