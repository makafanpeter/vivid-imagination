using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VividImagination.Modules.Media.Features.Photos.ViewModels;

public class UploadFile
{
    [Display(Name = "Name")]
    [StringLength(50, MinimumLength = 0)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(50, MinimumLength = 0)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "File")]
    public IFormFile FormFile { get; set; } = default!;

    [Display(Name = "Container")]
    public string Container { get; set; } = string.Empty;
}
