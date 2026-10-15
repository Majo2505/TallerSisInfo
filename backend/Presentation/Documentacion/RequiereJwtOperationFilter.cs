using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Breaku.Presentation.Documentacion;

/// <summary>
/// Marca con el candado de JWT solo las operaciones que llevan [Authorize]
/// (y no [AllowAnonymous]); login, registro y health quedan sin candado.
/// </summary>
public class RequiereJwtOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiereJwt = metadata.OfType<AuthorizeAttribute>().Any()
                          && !metadata.OfType<AllowAnonymousAttribute>().Any();
        if (!requiereJwt) return;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SwaggerExtensions.EsquemaJwt, context.Document)] = []
        });
    }
}
