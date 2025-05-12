// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using System.Net;

namespace Aspire.Hosting.Azure;

sealed class QueryParameterReference : IValueProvider, IValueWithReferences, IManifestExpressionProvider
{
    public static QueryParameterReference Create(ReferenceExpression reference) => new(reference);

    private readonly ReferenceExpression _reference;

    private QueryParameterReference(ReferenceExpression reference)
    {
        this._reference = reference;
    }

    IEnumerable<object> IValueWithReferences.References => [this._reference];

    public async ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default)
    {
        var value = await _reference.GetValueAsync(cancellationToken).ConfigureAwait(false);

        return WebUtility.UrlEncode(value);
    }

    string IManifestExpressionProvider.ValueExpression => WebUtility.UrlEncode(_reference.ValueExpression);
}
