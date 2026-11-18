using AutoMapper;
using VividImagination.Shared.Application.Contracts;
using VividImagination.Shared.Infrastructure.Exceptions;

namespace VividImagination.Shared.Application.Mapping;

public class ExceptionMappingProfile : Profile
{
    public ExceptionMappingProfile()
    {


        /* ---------- FALL‑BACK MAP ---------- */
        // Anything not matched by a more specific map
        CreateMap<System.Exception, ErrorResponse>()
            .ForMember(d => d.Code,
                o => o.MapFrom(_ => "SYSTEM_ERROR"))
            .ForMember(d => d.Message,
                o => o.MapFrom(_ => 
                    "Unexpected error occured please try again or confirm current operation status"));

        /* ---------- BASE MAP FOR ALL BUSINESS EXCEPTIONS ---------- */
        CreateMap<AppException, ErrorResponse>()
            .ForMember(d => d.Code,     o => o.MapFrom(s => s.Code))
            .ForMember(d => d.Message,  o => o.MapFrom(s => s.Message))
            // Derived exceptions that need their own map go here
            .Include<ValidationException,      ValidationErrorResponse>()  
            .Include<BadRequestException,      ErrorResponse>()
            .Include<ConflictException,        ErrorResponse>()
            .Include<ForbiddenException,       ErrorResponse>()
            .Include<NotFoundException,        ErrorResponse>()
            .Include<ServiceUnavailableException, ErrorResponse>()
            .Include<SystemErrorException,     ErrorResponse>()
            .Include<UnAuthorizedException,    ErrorResponse>();

        /* ---------- SPECIAL CASE: VALIDATION ---------- */
        CreateMap<ValidationException, ValidationErrorResponse>()
            .ForMember(d => d.Code,    o => o.MapFrom(s => s.Code))
            .ForMember(d => d.Errors,  o => o.MapFrom(s => s.Failures))
            .ForMember(d => d.Message, o => o.MapFrom(s =>
                $"{s.Message} " +
                string.Join(" ", s.Failures.SelectMany(kvp => kvp.Value))));

        /* ---------- OTHER BUSINESS EXCEPTIONS (inherit base map) ---------- */
        CreateMap<BadRequestException,         ErrorResponse>().IncludeBase<AppException, ErrorResponse>();
        CreateMap<ConflictException,           ErrorResponse>().IncludeBase<AppException, ErrorResponse>();
        CreateMap<ForbiddenException,          ErrorResponse>().IncludeBase<AppException, ErrorResponse>();
        CreateMap<NotFoundException,           ErrorResponse>().IncludeBase<AppException, ErrorResponse>();
        CreateMap<ServiceUnavailableException, ErrorResponse>().IncludeBase<AppException, ErrorResponse>();
        CreateMap<SystemErrorException,        ErrorResponse>().IncludeBase<AppException, ErrorResponse>();
        CreateMap<UnAuthorizedException,       ErrorResponse>().IncludeBase<AppException, ErrorResponse>();

        /* ---------- ARGUMENT EXCEPTION (framework) ---------- */
        CreateMap<ArgumentException, ErrorResponse>()
            .ForMember(d => d.Code,    o => o.MapFrom(_ => "InvalidRequest"))
            .ForMember(d => d.Message, o => o.MapFrom(s => s.Message));
        /*CreateMap<ApiException, ErrorResponse>()
            .IncludeBase<System.Exception, ErrorResponse>()
            .ForMember(d => d.Code, opt => opt.MapFrom(s => s.ResponseStatusCode));*/
    }
}