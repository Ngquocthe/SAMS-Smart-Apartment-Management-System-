using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using System.Reflection;

namespace SAMS_BE.Infrastructure.Swagger;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var formParameters = context.MethodInfo.GetParameters()
            .Where(p => p.GetCustomAttributes(typeof(FromFormAttribute), false).Any())
            .ToList();

        if (!formParameters.Any())
            return;

        var hasFile = formParameters.Any(p => p.ParameterType == typeof(IFormFile) ||
                                              (p.ParameterType.IsClass && p.ParameterType != typeof(string) &&
                                               p.ParameterType.GetProperties().Any(prop => prop.PropertyType == typeof(IFormFile))));

        if (hasFile)
        {
            // Xóa parameters cũ và tạo request body mới
            operation.Parameters?.Clear();

            var properties = new Dictionary<string, OpenApiSchema>();

            foreach (var param in formParameters)
            {
                if (param.ParameterType == typeof(IFormFile))
                {
                    properties[param.Name!] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary",
                        Description = "File upload"
                    };
                }
                else if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
                {
                    // Complex type - thêm các properties của nó
                    var typeProperties = param.ParameterType.GetProperties();
                    foreach (var prop in typeProperties)
                    {
                        if (prop.PropertyType == typeof(IFormFile))
                        {
                            properties[prop.Name] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "File upload"
                            };
                        }
                        else
                        {
                            var propSchema = GetSchemaForType(prop.PropertyType);
                            properties[prop.Name] = propSchema;
                        }
                    }
                }
                else
                {
                    var schema = GetSchemaForType(param.ParameterType);
                    properties[param.Name!] = schema;
                }
            }

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = properties
                        }
                    }
                }
            };
        }
    }

    private OpenApiSchema GetSchemaForType(Type type)
    {
        var schema = new OpenApiSchema();

        // Xử lý nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(Guid))
        {
            schema.Type = "string";
            schema.Format = "uuid";
        }
        else if (underlyingType == typeof(string))
        {
            schema.Type = "string";
        }
        else if (underlyingType == typeof(int))
        {
            schema.Type = "integer";
            schema.Format = "int32";
        }
        else if (underlyingType == typeof(long))
        {
            schema.Type = "integer";
            schema.Format = "int64";
        }
        else if (underlyingType == typeof(bool))
        {
            schema.Type = "boolean";
        }
        else if (underlyingType == typeof(float) || underlyingType == typeof(double))
        {
            schema.Type = "number";
        }
        else
        {
            schema.Type = "string";
        }

        return schema;
    }
}

