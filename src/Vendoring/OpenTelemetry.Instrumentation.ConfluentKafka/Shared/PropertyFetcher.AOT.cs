// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// NOTE: This version of PropertyFetcher is AOT-compatible.
// Usages of the non-AOT-compatible version can be moved over to this one when they need to support AOT/trimming.
// Copied from https://github.com/open-telemetry/opentelemetry-dotnet/blob/86a6ba0b7f7ed1f5e84e5a6610e640989cd3ae9f/src/Shared/DiagnosticSourceInstrumentation/PropertyFetcher.cs#L30

#nullable enable

#if NETSTANDARD2_1_0_OR_GREATER || NET
using System.Diagnostics.CodeAnalysis;
#endif
using System.Reflection;

namespace OpenTelemetry.Instrumentation;

/// <summary>
/// PropertyFetcher fetches a property from an object.
/// </summary>
/// <typeparam name="T">The type of the property being fetched.</typeparam>
internal sealed class PropertyFetcher<T>
{
#if NET
    private const string TrimCompatibilityMessage = "PropertyFetcher is used to access properties on objects dynamically by design and cannot be made trim compatible.";
#endif
    private readonly string propertyName;
    private PropertyFetch? innerFetcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyFetcher{T}"/> class.
    /// </summary>
    /// <param name="propertyName">Property name to fetch.</param>
    public PropertyFetcher(string propertyName)
    {
        this.propertyName = propertyName;
    }

    public int NumberOfInnerFetchers => this.innerFetcher == null
        ? 0
        : 1 + this.innerFetcher.NumberOfInnerFetchers;

    /// <summary>
    /// Try to fetch the property from the object.
    /// </summary>
    /// <param name="obj">Object to be fetched.</param>
    /// <param name="value">Fetched value.</param>
    /// <returns><see langword= "true"/> if the property was fetched.</returns>
#if NET
    [RequiresUnreferencedCode(TrimCompatibilityMessage)]
#endif
    public bool TryFetch(
#if NETSTANDARD2_1_0_OR_GREATER || NET
        [NotNullWhen(true)]
#endif
        object? obj,
        out T? value)
    {
        var innerFetcher = this.innerFetcher;
        if (innerFetcher is null)
        {
            return TryFetchRare(obj, this.propertyName, ref this.innerFetcher, out value);
        }

        return innerFetcher.TryFetch(obj, out value);
    }

#if NET
    [RequiresUnreferencedCode(TrimCompatibilityMessage)]
#endif
    private static bool TryFetchRare(object? obj, string propertyName, ref PropertyFetch? destination, out T? value)
    {
        if (obj is null)
        {
            value = default;
            return false;
        }

        var fetcher = PropertyFetch.Create(obj.GetType().GetTypeInfo(), propertyName);

        if (fetcher is null)
        {
            value = default;
            return false;
        }

        destination = fetcher;

        return fetcher.TryFetch(obj, out value);
    }

    // see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs
#if NET
    [RequiresUnreferencedCode(TrimCompatibilityMessage)]
#endif
    private abstract class PropertyFetch
    {
        public abstract int NumberOfInnerFetchers { get; }

        public static PropertyFetch? Create(TypeInfo type, string propertyName)
        {
            var property = type.DeclaredProperties.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase)) ?? type.GetProperty(propertyName);
            return CreateFetcherForProperty(property);

            static PropertyFetch? CreateFetcherForProperty(PropertyInfo? propertyInfo)
            {
                if (propertyInfo == null || !typeof(T).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    // returns null and wait for a valid payload to arrive.
                    return null;
                }

                var declaringType = propertyInfo.DeclaringType;
                if (declaringType!.IsValueType)
                {
                    throw new NotSupportedException(
                        $"Type: {declaringType.FullName} is a value type. PropertyFetcher can only operate on reference payload types.");
                }

                if (declaringType == typeof(object))
                {
                    // TODO: REMOVE this if branch when .NET 7 is out of support.
                    // This branch is never executed and is only needed for .NET 7 AOT-compiler at trimming stage; i.e.,
                    // this is not needed in .NET 8, because the compiler is improved and call into MakeGenericMethod will be AOT-compatible.
                    // It is used to force the AOT compiler to create an instantiation of the method with a reference type.
                    // The code for that instantiation can then be reused at runtime to create instantiation over any other reference.
                    return CreateInstantiated<object>(propertyInfo);
                }
                else
                {
                    return DynamicInstantiationHelper(declaringType, propertyInfo);
                }

                // Separated as a local function to be able to target the suppression to just this call.
                // IL3050 was generated here because of the call to MakeGenericType, which is problematic in AOT if one of the type parameters is a value type;
                // because the compiler might need to generate code specific to that type.
                // If the type parameter is a reference type, there will be no problem; because the generated code can be shared among all reference type instantiations.
#if NET
                [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The code guarantees that all the generic parameters are reference types.")]
#endif
                static PropertyFetch? DynamicInstantiationHelper(Type declaringType, PropertyInfo propertyInfo)
                {
                    return (PropertyFetch?)typeof(PropertyFetch)
                        .GetMethod(nameof(CreateInstantiated), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(declaringType) // This is validated in the earlier call chain to be a reference type.
                        .Invoke(null, new object[] { propertyInfo })!;
                }
            }
        }

        public abstract bool TryFetch(
#if NETSTANDARD2_1_0_OR_GREATER || NET
            [NotNullWhen(true)]
#endif
            object? obj,
            out T? value);

        // Goal: make PropertyFetcher AOT-compatible.
        // AOT compiler can't guarantee correctness when call into MakeGenericType or MakeGenericMethod
        // if one of the generic parameters is a value type (reference types are OK.)
        // For PropertyFetcher, the decision was made to only support reference type payloads, i.e.:
        // the object from which to get the property value MUST be a reference type.
        // Create generics with the declared object type as a generic parameter is OK, but we need the return type
        // of the property to be a value type (on top of reference types.)
        // Normally, we would have a helper class like `PropertyFetchInstantiated` that takes 2 generic parameters,
        // the declared object type, and the type of the property value.
        // But that would mean calling MakeGenericType, with value type parameters which AOT won't support.
        //
        // As a workaround, Generic instantiation was split into:
        // 1. The object type comes from the PropertyFetcher generic parameter.
        //    Compiler supports it even if it is a value type; the type is known statically during compilation
        //    since PropertyFetcher is used with it.
        // 2. Then, the declared object type is passed as a generic parameter to a generic method on PropertyFetcher<T> (or nested type.)
        //    Therefore, calling into MakeGenericMethod will only require specifying one parameter - the declared object type.
        //    The declared object type is guaranteed to be a reference type (throw on value type.) Thus, MakeGenericMethod is AOT compatible.
        private static PropertyFetchInstantiated<TDeclaredObject> CreateInstantiated<TDeclaredObject>(PropertyInfo propertyInfo)
            where TDeclaredObject : class
            => new PropertyFetchInstantiated<TDeclaredObject>(propertyInfo);

#if NET
        [RequiresUnreferencedCode(TrimCompatibilityMessage)]
#endif
        private sealed class PropertyFetchInstantiated<TDeclaredObject> : PropertyFetch
            where TDeclaredObject : class
        {
            private readonly string propertyName;
            private readonly Func<TDeclaredObject, T> propertyFetch;
            private PropertyFetch? innerFetcher;

            public PropertyFetchInstantiated(PropertyInfo property)
            {
                this.propertyName = property.Name;
                this.propertyFetch = (Func<TDeclaredObject, T>)property.GetMethod!.CreateDelegate(typeof(Func<TDeclaredObject, T>));
            }

            public override int NumberOfInnerFetchers => this.innerFetcher == null
                ? 0
                : 1 + this.innerFetcher.NumberOfInnerFetchers;

            public override bool TryFetch(
#if NETSTANDARD2_1_0_OR_GREATER || NET
                [NotNullWhen(true)]
#endif
                object? obj,
                out T? value)
            {
                if (obj is TDeclaredObject o)
                {
                    value = this.propertyFetch(o);
                    return true;
                }

                var innerFetcher = this.innerFetcher;
                if (innerFetcher is null)
                {
                    return TryFetchRare(obj, this.propertyName, ref this.innerFetcher, out value);
                }

                return innerFetcher.TryFetch(obj, out value);
            }
        }
    }
}
