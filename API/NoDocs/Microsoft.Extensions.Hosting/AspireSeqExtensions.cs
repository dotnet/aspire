// Assembly 'Aspire.Seq'

using System;
using Aspire.Seq;

namespace Microsoft.Extensions.Hosting;

public static class AspireSeqExtensions
{
    public static void AddSeqEndpoint(this IHostApplicationBuilder builder, string connectionName, Action<SeqSettings>? configureSettings = null);
}
