// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Aspire.Dashboard.Resources;

namespace Aspire.Dashboard.Model;

public class TokenFormModel
{
    [Required(ErrorMessageResourceType = typeof(Login), ErrorMessageResourceName = nameof(Login.TokenRequiredErrorMessage))]
    public string? Token { get; set; }
}
