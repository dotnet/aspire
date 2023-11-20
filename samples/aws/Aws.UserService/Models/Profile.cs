// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Aws.UserService.Models;

public class Profile : IValidatableObject
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? ImageName { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            results.Add(new ValidationResult("Name is required", new[] { "Name" }));
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            results.Add(new ValidationResult("Email is required", new[] { "Email" }));
        }

        if (string.IsNullOrWhiteSpace(ImageName))
        {
            results.Add(new ValidationResult("ImageName is required", new[] { "ImageName" }));
        }

        return results;
    }
}
