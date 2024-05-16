// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;

internal static class TestHelpers
{
    public static OpenApiSchema GetReferencedSchema(this OpenApiSchema schema, OpenApiDocument document)
    {
        if (schema.Reference is null)
        {
            return schema;
        }

        var schemaId = schema.Reference.Id;
        return document.Components.Schemas[schemaId];
    }
}
