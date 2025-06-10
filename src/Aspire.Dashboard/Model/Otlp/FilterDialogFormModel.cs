// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Aspire.Dashboard.Resources;

namespace Aspire.Dashboard.Model.Otlp;

public class FilterDialogFormModel
{
    [Required(ErrorMessageResourceType = typeof(Dialogs), ErrorMessageResourceName = nameof(Dialogs.FieldRequired))]
    public SelectViewModel<string>? Parameter { get; set; }

    [Required(ErrorMessageResourceType = typeof(Dialogs), ErrorMessageResourceName = nameof(Dialogs.FieldRequired))]
    public SelectViewModel<FilterCondition>? Condition { get; set; }

    // Set a max length on value because it will be added to the query string.
    // Max length is protection against accidently building a query string that exceeds limits because of a very long value.
    [Required(ErrorMessageResourceType = typeof(Dialogs), ErrorMessageResourceName = nameof(Dialogs.FieldRequired))]
    [MaxLength(1024, ErrorMessageResourceType = typeof(Dialogs), ErrorMessageResourceName = nameof(Dialogs.FieldTooLong))]
    public string? Value { get; set; }
}
