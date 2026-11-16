using AutoMapper;
using VividImagination.Shared.Application.Mapping;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Shared.Application.Contracts;

public class ValidationErrorResponse : ErrorResponse, IMapFrom<ValidationException>
{
    public IDictionary<string, string[]> Errors { get; set; }

    public new void Mapping(Profile profile)
    {
        profile.CreateMap<ValidationException, ValidationErrorResponse>()
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => src.Failures));
    }
}
