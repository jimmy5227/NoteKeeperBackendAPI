using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Collections.Generic;

namespace HW3NoteKeeper
{
    /// <summary>
    /// Operation filter to enable file uploads using IFormFile in Swagger.
    /// </summary>
    public class SwaggerUploadFileParametersFilter : IOperationFilter
    {
        /// <summary>
        /// Modifies the operation to support file uploads via multipart/form-data.
        /// </summary>
        /// <param name="operation">The Swagger operation to modify.</param>
        /// <param name="context">The operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Identify all parameters of type IFormFile.
            var fileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(IFormFile))
                .ToList();

            if (!fileParams.Any())
                return;

            // Remove IFormFile parameters from query/route parameters.
            foreach (var fp in fileParams)
            {
                var param = operation.Parameters.FirstOrDefault(p => p.Name == fp.Name);
                if (param != null)
                {
                    operation.Parameters.Remove(param);
                }
            }

            // Create a schema for file upload parameters.
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = fileParams.ToDictionary(
                    p => p.Name,
                    p => new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "Upload file"
                    }
                ),
                Required = fileParams.Select(p => p.Name).ToHashSet()
            };

            // Set the request body to use multipart/form-data with the file schema.
            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType { Schema = schema }
                }
            };
        }
    }
}
