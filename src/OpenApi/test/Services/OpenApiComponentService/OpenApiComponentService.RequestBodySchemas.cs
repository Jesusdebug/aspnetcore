// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

public partial class OpenApiComponentServiceTests : OpenApiDocumentServiceTestBase
{
    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForPoco()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/", (Todo todo) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;

            Assert.NotNull(requestBody);
            var content = Assert.Single(requestBody.Content);
            Assert.Equal("application/json", content.Key);
            var schema = content.Value.Schema.GetReferencedSchema(document);
            Assert.NotNull(schema);
            Assert.Equal("object", schema.Type);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("id", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("title", property.Key);
                    Assert.Equal("string", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("completed", property.Key);
                    Assert.Equal("boolean", property.Value.Type);
                },
                property =>
                {
                    Assert.Equal("createdAt", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("date-time", property.Value.Format);
                });

        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForFileTypes()
    {
        // Arrange
        var builder = CreateBuilder();
        string[] paths = ["stream", "pipereader"];

        // Act
        builder.MapPost("/stream", ([FromBody] Stream stream) => { });
        builder.MapPost("/pipereader", ([FromBody] PipeReader stream) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            foreach (var path in paths)
            {
                var operation = document.Paths[$"/{path}"].Operations[OperationType.Post];
                var requestBody = operation.RequestBody;

                var schema = requestBody.Content["application/octet-stream"].Schema.GetReferencedSchema(document);
                Assert.NotNull(schema);

                Assert.Equal("string", schema.Type);
                Assert.Equal("binary", schema.Format);
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForFilesInRecursiveType()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/proposal", (Proposal stream) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths[$"/proposal"].Operations[OperationType.Post];
            var requestBody = operation.RequestBody;
            var schema = requestBody.Content["application/json"].Schema.GetReferencedSchema(document);
            Assert.NotNull(schema);
            Assert.Collection(schema.Properties,
                static property =>
                {
                    Assert.Equal("proposalElement", property.Key);
                    Assert.NotNull(property.Value.Reference);
                    Assert.Equal("#\\components\\schemas\\Proposal", property.Value.Reference.Id);
                },
                property =>
                {
                    Assert.Equal("stream", property.Key);
                    Assert.Equal("string", property.Value.Type);
                    Assert.Equal("binary", property.Value.Format);
                });
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_GeneratesSchemaForListOf()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/enumerable-todo", (IEnumerable<Todo> todo) => { });
        builder.MapPost("/array-todo", (Todo[] todo) => { });
        builder.MapGet("/array-parsable", (Guid[] guids) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var enumerableTodo = document.Paths["/enumerable-todo"].Operations[OperationType.Post];
            var arrayTodo = document.Paths["/array-todo"].Operations[OperationType.Post];
            var arrayParsable = document.Paths["/array-parsable"].Operations[OperationType.Get];

            Assert.NotNull(enumerableTodo.RequestBody);
            Assert.NotNull(arrayTodo.RequestBody);
            var parameter = Assert.Single(arrayParsable.Parameters);

            // Assert all types materialize as arrays
            Assert.Equal("array", enumerableTodo.RequestBody.Content["application/json"].Schema.Type);
            Assert.Equal("array", arrayTodo.RequestBody.Content["application/json"].Schema.Type);

            Assert.Equal("array", parameter.Schema.Type);
            Assert.Equal("string", parameter.Schema.Items.Type);
            Assert.Equal("uuid", parameter.Schema.Items.Format);

            // Assert the array items are the same as the Todo schema
            foreach (var element in new[] { enumerableTodo, arrayTodo })
            {
                Assert.Collection(element.RequestBody.Content["application/json"].Schema.Items.Properties,
                    property =>
                    {
                        Assert.Equal("id", property.Key);
                        Assert.Equal("integer", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("title", property.Key);
                        Assert.Equal("string", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("completed", property.Key);
                        Assert.Equal("boolean", property.Value.Type);
                    },
                    property =>
                    {
                        Assert.Equal("createdAt", property.Key);
                        Assert.Equal("string", property.Value.Type);
                        Assert.Equal("date-time", property.Value.Format);
                    });
            }
        });
    }

    [Fact]
    public async Task GetOpenApiRequestBody_HandlesPolymorphicRequestWithoutDiscriminator()
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        builder.MapPost("/api", (Boat boat) => { });

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var operation = document.Paths["/api"].Operations[OperationType.Post];
            Assert.NotNull(operation.RequestBody);
            var requestBody = operation.RequestBody.Content;
            Assert.True(requestBody.TryGetValue("application/json", out var mediaType));
            var schema = mediaType.Schema.GetReferencedSchema(document);
            Assert.NotNull(schema);
            Assert.Equal("object", schema.Type);
            Assert.Empty(schema.AnyOf);
            Assert.Collection(schema.Properties,
                property =>
                {
                    Assert.Equal("length", property.Key);
                    Assert.Equal("number", property.Value.Type);
                    Assert.Equal("double", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("wheels", property.Key);
                    Assert.Equal("integer", property.Value.Type);
                    Assert.Equal("int32", property.Value.Format);
                },
                property =>
                {
                    Assert.Equal("make", property.Key);
                    Assert.Equal("string", property.Value.Type);
                });
        });
    }
}
