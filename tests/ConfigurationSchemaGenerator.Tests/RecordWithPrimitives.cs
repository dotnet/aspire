// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire;
using ConfigurationSchemaGenerator.Tests;

[assembly: ConfigurationSchema("RecordWithPrimitives", typeof(RecordWithPrimitives))]

namespace ConfigurationSchemaGenerator.Tests;

public record RecordWithPrimitives
{
    /// <summary>
    /// Prop0 is a bool.
    /// </summary>
    public bool Prop0 { get; set; }
    public byte Prop1 { get; set; }
    public sbyte Prop2 { get; set; }
    public char Prop3 { get; set; }
    public double Prop4 { get; set; }
    public string? Prop5 { get; set; }
    public int Prop6 { get; set; }
    public short Prop8 { get; set; }
    public long Prop9 { get; set; }
    /// <summary>
    /// Prop10 is a float.
    /// </summary>
    public float Prop10 { get; set; }
    public ushort Prop13 { get; set; }
    public uint Prop14 { get; set; }
    public ulong Prop15 { get; set; }
    public object? Prop16 { get; set; }
    public CultureInfo? Prop17 { get; set; }
    public DateTime Prop19 { get; set; }
    public DateTimeOffset Prop20 { get; set; }
    public decimal Prop21 { get; set; }
    public TimeSpan Prop23 { get; set; }
    public Guid Prop24 { get; set; }
    public Uri? Prop25 { get; set; }
    public Version? Prop26 { get; set; }
    public DayOfWeek Prop27 { get; set; }
#if NETCOREAPP
    public Int128 Prop7 { get; set; }
    public Half Prop11 { get; set; }
    public UInt128 Prop12 { get; set; }
    public DateOnly Prop18 { get; set; }
    public TimeOnly Prop22 { get; set; }
#endif
}
