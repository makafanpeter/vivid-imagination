using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VividImagination.Modules.Media.Persistence.Enums;

namespace VividImagination.Modules.Media.Features.Photos.ViewModels;

public class UploadFile
{
    [Display(Name = "Name")]
    [StringLength(50, MinimumLength = 0)]
    public string Name { get; set; }

    [Display(Name = "Description")]
    [StringLength(50, MinimumLength = 0)]
    public string Description { get; set; }

    [Required]
    [Display(Name = "File")]
    public IFormFile FormFile { get; set; }
    [Display(Name = "Container")]
    public string Container { get; set; }
    
    
}