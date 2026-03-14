using System.Collections;
using System.Collections.Generic;

namespace Semver.Comparers;

/// <summary>
/// An interface that combines equality and order comparison for the <see cref="SemVersion"/>
/// class.
/// </summary>
/// <remarks>
/// <para>This interface provides a type for the <see cref="SemVersion.PrecedenceComparer"/> and
/// <see cref="SemVersion.SortOrderComparer"/> so that separate properties aren't needed for the
/// <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/> of <see cref="SemVersion"/>.
/// </para>
/// <para>Consumers of this library should not implement this interface. Doing so may expose your
/// code to breaking changes in a future release.</para>
/// </remarks>
internal interface ISemVersionComparer : IEqualityComparer<SemVersion?>, IEqualityComparer, IComparer<SemVersion?>, IComparer
{
}
