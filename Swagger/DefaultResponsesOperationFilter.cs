using Microsoft.OpenApi;
using MidnightApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MidnightApi.Swagger;

public class DefaultResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();

        operation.Responses.TryAdd("500", new OpenApiResponse
        {
            Description = "Unexpected server error",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ApiResponse<object?>), context.SchemaRepository)
                }
            }
        });
    }
}
