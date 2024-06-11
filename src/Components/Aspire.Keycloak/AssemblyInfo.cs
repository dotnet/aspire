// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire;
using Aspire.Keycloak;

[assembly: ConfigurationSchema("Aspire:Keycloak", typeof(KeycloakSettings))]

[assembly: LoggingCategories("Keycloak")]
