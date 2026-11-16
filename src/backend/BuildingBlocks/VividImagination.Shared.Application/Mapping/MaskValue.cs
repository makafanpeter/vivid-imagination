using AutoMapper;
using VividImagination.Shared.Infrastructure.Common;

namespace VividImagination.Shared.Application.Mapping
{
    public class MaskValue : IValueConverter<string, string>
    {
        public string Convert(string source, ResolutionContext context)
        {
            if (string.IsNullOrEmpty(source))
            {
                return source;
            }

            return source.Mask(source.Length/2);
        }
           
    }
}