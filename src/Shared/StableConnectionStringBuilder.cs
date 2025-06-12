// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text;

namespace Aspire;

/// <summary>
/// Parses and manages connection strings in the form: Key1=Value1;Key2=Value2;...
/// Preserves the exact placement of empty segments and semicolons. It also preserves
/// spaces and case in keys and values.
/// </summary>
/// <remarks>
/// This connection string builder should be used when you need to maintain the exact format of a connection string
/// while adding or removing keys and values. When only parsing/reading connection string it is recommended to use
/// <see cref="System.Data.Common.DbConnectionStringBuilder"/> as it handles escaping too.
/// </remarks>
internal sealed class StableConnectionStringBuilder : IEnumerable<KeyValuePair<string, string>>
{
    private string _connectionString;
    private readonly List<ConnectionStringSegment> _segments;

    /// <summary>
    /// The current connection string, always up-to-date.
    /// </summary>
    public string ConnectionString
    {
        get => _connectionString;
        private set => _connectionString = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StableConnectionStringBuilder"/> class.
    /// </summary>
    /// <param name="connectionString">The connection string to parse.</param>
    public StableConnectionStringBuilder(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
        _segments = ParseSegments(_connectionString);
    }

    /// <summary>
    /// Initializes a new empty instance of the <see cref="StableConnectionStringBuilder"/> class.
    /// </summary>
    public StableConnectionStringBuilder()
    {
        _connectionString = "";
        _segments = [];
    }

    /// <summary>
    /// Tries to parse the given connection string into a <see cref="StableConnectionStringBuilder"/>.
    /// Returns true if parsing succeeds, false otherwise.
    /// </summary>
    public static bool TryParse(string connectionString, out StableConnectionStringBuilder? builder)
    {
        try
        {
            builder = new StableConnectionStringBuilder(connectionString);
            return true;
        }
        catch
        {
            builder = null;
            return false;
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key (case-insensitive).
    /// Returns an empty string if the value is empty, and null if the key does not exist.
    /// Setting a value to null removes the key/value pair and its following semicolon.
    /// </summary>
    public string? this[string key]
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            var idx = FindKeyIndex(key);
            if (idx >= 0)
            {
                return _segments[idx].Value;
            }
            return null;
        }
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            key = key.Trim();

            var idx = FindKeyIndex(key);
            if (idx >= 0)
            {
                if (value is null)
                {
                    RemoveKeyAndSemicolon(idx);
                }
                else
                {
                    UpdateValue(idx, value);
                }
            }
            else
            {
                if (value is null)
                {
                    return;
                }
                AddKey(key, value);
            }
        }
    }

    /// <summary>
    /// Removes the specified key and its following semicolon (if present) from the connection string.
    /// Returns true if the key was found and removed; otherwise, false.
    /// </summary>
    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var idx = FindKeyIndex(key);
        if (idx >= 0)
        {
            RemoveKeyAndSemicolon(idx);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Tries to get the value associated with the specified key (case-insensitive).
    /// </summary>
    public bool TryGetValue(string key, out string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var idx = FindKeyIndex(key);
        if (idx >= 0)
        {
            value = _segments[idx].Value ?? string.Empty;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Returns the connection string in the original order, preserving empty segments and semicolons.
    /// </summary>
    public override string ToString() => ConnectionString;

    IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
    {
        foreach (var seg in _segments)
        {
            if (seg != ConnectionStringSegment.SemiColon)
            {
                yield return new KeyValuePair<string, string>(seg.Key.Trim(), seg.Value ?? string.Empty);
            }
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<string, string>>)this).GetEnumerator();

    private static List<ConnectionStringSegment> ParseSegments(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return [];
        }

        var segments = new List<ConnectionStringSegment>();

        var parts = connectionString.Split(';');

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (i > 0)
            {
                segments.Add(ConnectionStringSegment.SemiColon);
            }

            var keyAndValue = part.Split('=', 2);
            if (keyAndValue.Length > 1)
            {
                var key = keyAndValue[0];
                var value = keyAndValue[1];

                if (string.IsNullOrEmpty(key))
                {
                    // If the key is empty, treat this as an invalid segment
                    throw new ArgumentException($"Invalid segment in connection string: '{part}'", nameof(connectionString));
                }
                else if (segments.Any(s => s.Key != null && KeyEquals(s.Key, key)))
                {
                    // If a key already exists, throw an exception
                    throw new ArgumentException($"Duplicate key in connection string: '{key}'", nameof(connectionString));
                }
                else
                {
                    // Value can be empty
                    segments.Add(new ConnectionStringSegment(key, value ?? string.Empty));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(part))
                {
                    // A segment without an equal sign is considered invalid
                    throw new ArgumentException($"Invalid segment in connection string: '{part}'", nameof(connectionString));
                }
            }
        }

        return segments;
    }

    private static bool KeyEquals(string key1, string key2)
    {
        return key1.Trim().Equals(key2.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateConnectionString()
    {
        var sb = new StringBuilder();
        foreach (var segment in _segments)
        {
            if (segment == ConnectionStringSegment.SemiColon)
            {
                sb.Append(';');
            }
            else
            {
                sb.Append(segment.Key);
                sb.Append('=');
                sb.Append(segment.Value ?? string.Empty);
            }
        }
        _connectionString = sb.ToString();
    }

    private int FindKeyIndex(string key)
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].Key != null && KeyEquals(_segments[i].Key, key))
            {
                return i;
            }
        }
        return -1;
    }

    private void UpdateValue(int idx, string value)
    {
        var seg = _segments[idx];
        seg.Value = value;
        UpdateConnectionString();
    }

    private void RemoveKeyAndSemicolon(int idx)
    {
        _segments.RemoveAt(idx);

        // If there is a following semicolon , remove it as well
        if (idx < _segments.Count)
        {
            if (_segments[idx] == ConnectionStringSegment.SemiColon)
            {
                _segments.RemoveAt(idx);
            }
        }

        UpdateConnectionString();
    }

    private void AddKey(string key, string value)
    {
        if (_segments.Count > 0 && _segments[^1] != ConnectionStringSegment.SemiColon)
        {
            // If the last segment is not a semicolon, add one before adding the new key
            _segments.Add(ConnectionStringSegment.SemiColon);
        }

        _segments.Add(new ConnectionStringSegment(key, value));

        _segments.Add(ConnectionStringSegment.SemiColon);

        UpdateConnectionString();
    }

    private sealed class ConnectionStringSegment(string key, string value)
    {
        public static readonly ConnectionStringSegment SemiColon = new(null!, null!);

        public string Key { get; set; } = key;

        public string Value { get; set; } = value;

        public override string ToString()
        {
            if (this == SemiColon)
            {
                return ";";
            }

            return $"{Key}={Value}";
        }
    }
}
