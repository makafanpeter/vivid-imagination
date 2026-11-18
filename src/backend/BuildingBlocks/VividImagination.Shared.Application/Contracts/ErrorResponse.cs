using AutoMapper;
using VividImagination.Shared.Application.Mapping;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Shared.Application.Contracts;

public class ErrorResponse : IMapFrom<AppException>, IAppResponse
{
    public string Message { get; set; }
    public string Code { get; set; }

    public void Mapping(Profile profile)
    {
        profile.CreateMap<AppException, ErrorResponse>()
            .Include<ValidationException, ValidationErrorResponse>()
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code));
    }
}
