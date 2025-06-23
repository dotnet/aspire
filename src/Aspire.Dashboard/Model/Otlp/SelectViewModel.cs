// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Model.Otlp;

[DebuggerDisplay(@"Name = {Name}, Id = \{{Id}\}")]
public class SelectViewModel<T> : IEquatable<SelectViewModel<T>>
{
    public required string Name { get; init; }
    public required T? Id { get; init; }

    public bool Equals(SelectViewModel<T>? other)
    {
        if (other == null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Allow the type to implement IEquatable for when comparing SelectViewModel.
        // Complex types can implement the interface so they can match by more than just reference.
        // The is important for when lists check the selected item is a value in the items list.
        // If it isn't then the list clears the selected value.
        return EqualityComparer<T>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is SelectViewModel<T> other)
        {
            return Equals(other);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Id);
    }

    public override string ToString()
    {
        return $"Name = {Name}, Id = {{{Id}}}";
    }
}
