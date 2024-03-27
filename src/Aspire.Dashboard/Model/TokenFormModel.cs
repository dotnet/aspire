// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Aspire.Dashboard.Model;

public class TokenFormModel
{
    [Required]
    public string? Token { get; set; }
}
