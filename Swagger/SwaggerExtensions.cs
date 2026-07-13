using System.Reflection;
using Microsoft.OpenApi;

namespace MidnightApi.Swagger;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Midnight Family Tree API",
                Version = "v1",
                Description = """
                    Family Tree Management API.

                    **Auth**
                    1. `POST /api/accounts/register` — create admin + one family
                    2. `POST /api/accounts/login` — receive JWT
                    3. Click **Authorize**, paste: `Bearer {token}`

                    FamilyId always comes from the JWT — never from the client.
                    """
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the JWT from /api/accounts/login. Example: Bearer eyJhbGciOi..."
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            options.TagActionsBy(api =>
            {
                var controller = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var name)
                    ? name
                    : "Default";

                return [controller ?? "Default"];
            });

            options.OrderActionsBy(api => api.RelativePath);
            options.OperationFilter<DefaultResponsesOperationFilter>();
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Midnight Family Tree API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Midnight Family Tree API";
            options.DisplayRequestDuration();
            options.EnableTryItOutByDefault();
            options.DefaultModelsExpandDepth(-1);
        });

        return app;
    }
}
