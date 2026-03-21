package aspire;

import java.util.Map;

final class AppHost {

    void main() throws Exception {
        var builder = DistributedApplication.CreateBuilder();
        var adminUsername = builder.addParameter("keycloak-admin-user");
        var adminPassword = builder.addParameter("keycloak-admin-password", true);
        var keycloak = builder.addKeycloak("keycloak", new AddKeycloakOptions().port(8080.0));
        keycloak
            .withDataVolume("keycloak-data")
            .withRealmImport(".")
            .withEnabledFeatures(new String[] { "token-exchange", "opentelemetry" })
            .withDisabledFeatures(new String[] { "admin-fine-grained-authz" })
            .withOtlpExporter();
        var keycloak2 = builder.addKeycloak("keycloak2")
            .withDataBindMount(".")
            .withRealmImport(".")
            .withEnabledFeatures(new String[] { "rolling-updates" })
            .withDisabledFeatures(new String[] { "scripts" })
            .withOtlpExporterWithProtocol(OtlpProtocol.HTTP_PROTOBUF);
        var consumer = builder.addContainer("consumer", "nginx");
        consumer.withReference(new IResource(keycloak.getHandle(), keycloak.getClient()));
        consumer.withReference(new IResource(keycloak2.getHandle(), keycloak2.getClient()));
        var _keycloakName = keycloak.name();
        var _keycloakEntrypoint = keycloak.entrypoint();
        var _keycloakShellExecution = keycloak.shellExecution();
        builder.build().run();
    }
}
