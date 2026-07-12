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
                    REST API for managing family trees, member profiles, and user accounts.

                    **Controllers**
                    - **Families** — create and manage family groups
                    - **Members** — members, tree views, spouses, children, and sub-resources
                    - **Accounts** — login accounts linked to members
                    """
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
