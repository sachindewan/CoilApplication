﻿//using Microsoft.OpenApi.Models;
//using Swashbuckle.AspNetCore.SwaggerGen;

//namespace Coil.Api.Shared.Filters
//{
//    public class FileUploadOperationFilter : IOperationFilter
//    {
//        public void Apply(OpenApiOperation operation, OperationFilterContext context)
//        {
//            var fileParams = context.MethodInfo.GetParameters()
//                .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(List<IFormFile>))
//                .ToList();

//            if (fileParams.Any())
//            {
//                operation.RequestBody = new OpenApiRequestBody
//                {
//                    Content = new Dictionary<string, OpenApiMediaType>
//                    {
//                        ["multipart/form-data"] = new OpenApiMediaType
//                        {
//                            Schema = new OpenApiSchema
//                            {
//                                Type = "object",
//                                Properties = fileParams.ToDictionary(
//                                    p => p.Name,
//                                    p => new OpenApiSchema { Type = "string", Format = "binary" }
//                                )
//                            }
//                        }
//                    }
//                };
//            }
//        }
//    }

//}
