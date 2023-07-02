// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This was copied from https://github.com/dotnet/runtime/blob/1db4357891752cc028620710fe924425235b8f89/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/CallerArgumentExpressionAttribute.cs
// and updated to have the scope of the attributes be internal.

namespace System.Runtime.CompilerServices;

#if !NETCOREAPP3_0_OR_GREATER
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute : Attribute
{
    public CallerArgumentExpressionAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }

    public string ParameterName { get; }
}
#endif