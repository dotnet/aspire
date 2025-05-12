// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

static class DurableTaskConstants
{
    public static class Scheduler
    {
        public static class Dashboard
        {
            public static readonly Uri Endpoint = new Uri("https://dashboard.durabletask.io");
        }

        public static class Emulator
        {
            public static class Container
            {
                /// <remarks>mcr.microsoft.com/dts</remarks>
                public const string Registry = "mcr.microsoft.com/dts";

                /// <remarks>dts-emulator</remarks>
                public const string Image = "dts-emulator";

                /// <remarks>latest</remarks>
                public static string Tag => "latest";
            }

            public static class Endpoints
            {
                public const string Worker = "worker";
                public const string Dashboard = "dashboard";
            }
        }

        public static class TaskHub
        {
            public const string DefaultName = "default";
        }
    }
}
