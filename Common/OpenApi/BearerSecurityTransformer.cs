using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Logistics.Api.Common.OpenApi;

internal sealed class BearerSecurityTransformer(IAuthenticationSchemeProvider schemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument openApiDocument, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        if(!schemes.Any(s => s.Name == JwtBearerDefaults.AuthenticationScheme)) return;

        openApiDocument.Components ??= new OpenApiComponents();
        openApiDocument.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
            }
        };

        foreach (var operation in openApiDocument.Paths.Values.SelectMany(path => path.Operations!))
        {
            operation.Value.Security ??= [];
            operation.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", openApiDocument)] = []
            });
        }
    }
    
}