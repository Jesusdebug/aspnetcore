// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;

internal static class TypeExtensions
{
    public static string? GetSchemaReferenceId(this Type type)
    {
        return TypeNameBuilder.ToString(type, TypeNameBuilder.Format.ToString);
    }
}
