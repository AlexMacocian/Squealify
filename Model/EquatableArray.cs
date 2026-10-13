using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Squealify.Model;

/// <summary>
/// Immutable array with value equality, so incremental generator outputs can be cached.
/// </summary>
internal readonly struct EquatableArray<T>(ImmutableArray<T> items) : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> items = items;

    public ImmutableArray<T> Items => this.items.IsDefault ? ImmutableArray<T>.Empty : this.items;

    public int Length => this.Items.Length;

    public T this[int index] => this.Items[index];

    public bool Equals(EquatableArray<T> other) => this.Items.SequenceEqual(other.Items);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && this.Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in this.Items)
            {
                hash = (hash * 31) + (item?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)this.Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}

internal static class EquatableArray
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> items)
        where T : IEquatable<T> => new(items.ToImmutableArray());
}
