using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PhotosService.Services
{
    public class JwtSecurityTokenModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return context.Metadata.ModelType == typeof(JwtSecurityToken) ? new JwtSecurityTokenModelBinder() : null;
        }
    }
}