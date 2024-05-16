// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OpenApiConstants = Microsoft.AspNetCore.OpenApi.OpenApiConstants;

internal sealed class OpenApiComponentsSchemaTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var documentName = context.DocumentName;
        var schemaService = context.ApplicationServices.GetRequiredKeyedService<OpenApiSchemaService>(documentName);

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();

        foreach (var (schemaId, schema) in schemaService.GetSchemasByRef())
        {
            // schema.Extensions.Remove(OpenApiConstants.SchemaId);
            document.Components.Schemas[schemaId] = schema;
        }

        foreach (var pathItem in document.Paths.Values)
        {
            for (var i = 0; i < DelegateOpenApiDocumentTransformer.OperationTypesCache.Length; i++)
            {
                var operationType = DelegateOpenApiDocumentTransformer.OperationTypesCache[i];
                if (pathItem.Operations.TryGetValue(operationType, out var operation))
                {
                    if (operation.Parameters is not null)
                    {
                        foreach (var parameter in operation.Parameters)
                        {
                            if (parameter.Schema != null && parameter.Reference is { Id: "#" })
                            {
                                parameter.Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "#" } };
                            }

                            if (parameter.Schema != null &&
                                parameter.Schema.Extensions.TryGetValue(OpenApiConstants.SchemaId, out var schemaIdExtension) && schemaIdExtension is OpenApiString { Value: string schemaId })
                            {
                                parameter.Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = schemaId } };
                            }

                            if (parameter.Schema != null && parameter.Schema.AllOf != null)
                            {
                                for (var j = 0; j < parameter.Schema.AllOf.Count; j++)
                                {
                                    if (parameter.Schema.AllOf[j].Extensions.TryGetValue(OpenApiConstants.SchemaId, out var allOfSchemaIdExtension) && allOfSchemaIdExtension is OpenApiString { Value: string allOfSchemaId })
                                    {
                                        parameter.Schema.AllOf[j] = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = allOfSchemaId } };
                                    }
                                }
                            }
                        }
                    }

                    if (operation.RequestBody is not null)
                    {
                        foreach (var content in operation.RequestBody.Content)
                        {
                            if (content.Value.Schema != null)
                            {
                                foreach (var property in content.Value.Schema.Properties)
                                {
                                    if (property.Value.Reference is { Id: "#" })
                                    {
                                        property.Value.Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "#" };
                                    }
                                }
                            }

                            if (content.Value.Schema != null &&
                                content.Value.Schema.Extensions.TryGetValue(OpenApiConstants.SchemaId, out var schemaIdExtension) && schemaIdExtension is OpenApiString { Value: string schemaId })
                            {
                                content.Value.Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = schemaId } };
                            }

                            if (content.Value.Schema != null && content.Value.Schema.AllOf != null)
                            {
                                for (var j = 0; j < content.Value.Schema.AllOf.Count; j++)
                                {
                                    if (content.Value.Schema.AllOf[j].Extensions.TryGetValue(OpenApiConstants.SchemaId, out var allOfSchemaIdExtension) && allOfSchemaIdExtension is OpenApiString { Value: string allOfSchemaId })
                                    {
                                        content.Value.Schema.AllOf[j] = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = allOfSchemaId } };
                                    }
                                }
                            }

                        }
                    }

                    if (operation.Responses is not null)
                    {
                        foreach (var response in operation.Responses.Values)
                        {
                            if (response.Content is not null)
                            {
                                foreach (var content in response.Content)
                                {
                                    if (content.Value.Schema != null &&
                                        content.Value.Schema.Extensions.TryGetValue(OpenApiConstants.SchemaId, out var schemaIdExtension) && schemaIdExtension is OpenApiString { Value: string schemaId })
                                    {
                                        response.Content[content.Key].Schema = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = schemaId } };
                                    }

                                    if (content.Value.Schema != null && content.Value.Schema.AllOf != null)
                                    {
                                        for (var j = 0; j < content.Value.Schema.AllOf.Count; j++)
                                        {
                                            if (content.Value.Schema.AllOf[j].Extensions.TryGetValue(OpenApiConstants.SchemaId, out var allOfSchemaIdExtension) && allOfSchemaIdExtension is OpenApiString { Value: string allOfSchemaId })
                                            {
                                                content.Value.Schema.AllOf[j] = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = allOfSchemaId } };
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return Task.CompletedTask;
    }
}
