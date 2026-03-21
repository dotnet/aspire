// Aspire.java - Capability-based Aspire SDK
// GENERATED CODE - DO NOT EDIT

package aspire;

import java.util.*;
import java.util.function.*;

// ============================================================================
// Enums
// ============================================================================

/** ContainerLifetime enum. */
enum ContainerLifetime implements WireValueEnum {
    SESSION("Session"),
    PERSISTENT("Persistent");

    private final String value;

    ContainerLifetime(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static ContainerLifetime fromValue(String value) {
        for (ContainerLifetime e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** ImagePullPolicy enum. */
enum ImagePullPolicy implements WireValueEnum {
    DEFAULT("Default"),
    ALWAYS("Always"),
    MISSING("Missing"),
    NEVER("Never");

    private final String value;

    ImagePullPolicy(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static ImagePullPolicy fromValue(String value) {
        for (ImagePullPolicy e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** DistributedApplicationOperation enum. */
enum DistributedApplicationOperation implements WireValueEnum {
    RUN("Run"),
    PUBLISH("Publish");

    private final String value;

    DistributedApplicationOperation(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static DistributedApplicationOperation fromValue(String value) {
        for (DistributedApplicationOperation e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** OtlpProtocol enum. */
enum OtlpProtocol implements WireValueEnum {
    GRPC("Grpc"),
    HTTP_PROTOBUF("HttpProtobuf"),
    HTTP_JSON("HttpJson");

    private final String value;

    OtlpProtocol(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static OtlpProtocol fromValue(String value) {
        for (OtlpProtocol e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** ProtocolType enum. */
enum ProtocolType implements WireValueEnum {
    IP("IP"),
    IPV6_HOP_BY_HOP_OPTIONS("IPv6HopByHopOptions"),
    UNSPECIFIED("Unspecified"),
    ICMP("Icmp"),
    IGMP("Igmp"),
    GGP("Ggp"),
    IPV4("IPv4"),
    TCP("Tcp"),
    PUP("Pup"),
    UDP("Udp"),
    IDP("Idp"),
    IPV6("IPv6"),
    IPV6_ROUTING_HEADER("IPv6RoutingHeader"),
    IPV6_FRAGMENT_HEADER("IPv6FragmentHeader"),
    IPSEC_ENCAPSULATING_SECURITY_PAYLOAD("IPSecEncapsulatingSecurityPayload"),
    IPSEC_AUTHENTICATION_HEADER("IPSecAuthenticationHeader"),
    ICMP_V6("IcmpV6"),
    IPV6_NO_NEXT_HEADER("IPv6NoNextHeader"),
    IPV6_DESTINATION_OPTIONS("IPv6DestinationOptions"),
    ND("ND"),
    RAW("Raw"),
    IPX("Ipx"),
    SPX("Spx"),
    SPX_II("SpxII"),
    UNKNOWN("Unknown");

    private final String value;

    ProtocolType(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static ProtocolType fromValue(String value) {
        for (ProtocolType e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** WaitBehavior enum. */
enum WaitBehavior implements WireValueEnum {
    WAIT_ON_RESOURCE_UNAVAILABLE("WaitOnResourceUnavailable"),
    STOP_ON_RESOURCE_UNAVAILABLE("StopOnResourceUnavailable");

    private final String value;

    WaitBehavior(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static WaitBehavior fromValue(String value) {
        for (WaitBehavior e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** CertificateTrustScope enum. */
enum CertificateTrustScope implements WireValueEnum {
    NONE("None"),
    APPEND("Append"),
    OVERRIDE("Override"),
    SYSTEM("System");

    private final String value;

    CertificateTrustScope(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static CertificateTrustScope fromValue(String value) {
        for (CertificateTrustScope e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** IconVariant enum. */
enum IconVariant implements WireValueEnum {
    REGULAR("Regular"),
    FILLED("Filled");

    private final String value;

    IconVariant(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static IconVariant fromValue(String value) {
        for (IconVariant e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** ProbeType enum. */
enum ProbeType implements WireValueEnum {
    STARTUP("Startup"),
    READINESS("Readiness"),
    LIVENESS("Liveness");

    private final String value;

    ProbeType(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static ProbeType fromValue(String value) {
        for (ProbeType e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** EndpointProperty enum. */
enum EndpointProperty implements WireValueEnum {
    URL("Url"),
    HOST("Host"),
    IPV4_HOST("IPV4Host"),
    PORT("Port"),
    SCHEME("Scheme"),
    TARGET_PORT("TargetPort"),
    HOST_AND_PORT("HostAndPort"),
    TLS_ENABLED("TlsEnabled");

    private final String value;

    EndpointProperty(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static EndpointProperty fromValue(String value) {
        for (EndpointProperty e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** UrlDisplayLocation enum. */
enum UrlDisplayLocation implements WireValueEnum {
    SUMMARY_AND_DETAILS("SummaryAndDetails"),
    DETAILS_ONLY("DetailsOnly");

    private final String value;

    UrlDisplayLocation(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static UrlDisplayLocation fromValue(String value) {
        for (UrlDisplayLocation e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** EntrypointType enum. */
enum EntrypointType implements WireValueEnum {
    EXECUTABLE("Executable"),
    SCRIPT("Script"),
    MODULE("Module");

    private final String value;

    EntrypointType(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static EntrypointType fromValue(String value) {
        for (EntrypointType e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

// ============================================================================
// DTOs
// ============================================================================

/** CreateBuilderOptions DTO. */
class CreateBuilderOptions {
    private String[] args;
    private String projectDirectory;
    private String appHostFilePath;
    private String containerRegistryOverride;
    private boolean disableDashboard;
    private String dashboardApplicationName;
    private boolean allowUnsecuredTransport;
    private boolean enableResourceLogging;

    public String[] getArgs() { return args; }
    public void setArgs(String[] value) { this.args = value; }
    public String getProjectDirectory() { return projectDirectory; }
    public void setProjectDirectory(String value) { this.projectDirectory = value; }
    public String getAppHostFilePath() { return appHostFilePath; }
    public void setAppHostFilePath(String value) { this.appHostFilePath = value; }
    public String getContainerRegistryOverride() { return containerRegistryOverride; }
    public void setContainerRegistryOverride(String value) { this.containerRegistryOverride = value; }
    public boolean getDisableDashboard() { return disableDashboard; }
    public void setDisableDashboard(boolean value) { this.disableDashboard = value; }
    public String getDashboardApplicationName() { return dashboardApplicationName; }
    public void setDashboardApplicationName(String value) { this.dashboardApplicationName = value; }
    public boolean getAllowUnsecuredTransport() { return allowUnsecuredTransport; }
    public void setAllowUnsecuredTransport(boolean value) { this.allowUnsecuredTransport = value; }
    public boolean getEnableResourceLogging() { return enableResourceLogging; }
    public void setEnableResourceLogging(boolean value) { this.enableResourceLogging = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Args", AspireClient.serializeValue(args));
        map.put("ProjectDirectory", AspireClient.serializeValue(projectDirectory));
        map.put("AppHostFilePath", AspireClient.serializeValue(appHostFilePath));
        map.put("ContainerRegistryOverride", AspireClient.serializeValue(containerRegistryOverride));
        map.put("DisableDashboard", AspireClient.serializeValue(disableDashboard));
        map.put("DashboardApplicationName", AspireClient.serializeValue(dashboardApplicationName));
        map.put("AllowUnsecuredTransport", AspireClient.serializeValue(allowUnsecuredTransport));
        map.put("EnableResourceLogging", AspireClient.serializeValue(enableResourceLogging));
        return map;
    }
}

/** ResourceEventDto DTO. */
class ResourceEventDto {
    private String resourceName;
    private String resourceId;
    private String state;
    private String stateStyle;
    private String healthStatus;
    private double exitCode;

    public String getResourceName() { return resourceName; }
    public void setResourceName(String value) { this.resourceName = value; }
    public String getResourceId() { return resourceId; }
    public void setResourceId(String value) { this.resourceId = value; }
    public String getState() { return state; }
    public void setState(String value) { this.state = value; }
    public String getStateStyle() { return stateStyle; }
    public void setStateStyle(String value) { this.stateStyle = value; }
    public String getHealthStatus() { return healthStatus; }
    public void setHealthStatus(String value) { this.healthStatus = value; }
    public double getExitCode() { return exitCode; }
    public void setExitCode(double value) { this.exitCode = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("ResourceName", AspireClient.serializeValue(resourceName));
        map.put("ResourceId", AspireClient.serializeValue(resourceId));
        map.put("State", AspireClient.serializeValue(state));
        map.put("StateStyle", AspireClient.serializeValue(stateStyle));
        map.put("HealthStatus", AspireClient.serializeValue(healthStatus));
        map.put("ExitCode", AspireClient.serializeValue(exitCode));
        return map;
    }
}

/** CommandOptions DTO. */
class CommandOptions {
    private String description;
    private Object parameter;
    private String confirmationMessage;
    private String iconName;
    private IconVariant iconVariant;
    private boolean isHighlighted;
    private Object updateState;

    public String getDescription() { return description; }
    public void setDescription(String value) { this.description = value; }
    public Object getParameter() { return parameter; }
    public void setParameter(Object value) { this.parameter = value; }
    public String getConfirmationMessage() { return confirmationMessage; }
    public void setConfirmationMessage(String value) { this.confirmationMessage = value; }
    public String getIconName() { return iconName; }
    public void setIconName(String value) { this.iconName = value; }
    public IconVariant getIconVariant() { return iconVariant; }
    public void setIconVariant(IconVariant value) { this.iconVariant = value; }
    public boolean getIsHighlighted() { return isHighlighted; }
    public void setIsHighlighted(boolean value) { this.isHighlighted = value; }
    public Object getUpdateState() { return updateState; }
    public void setUpdateState(Object value) { this.updateState = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Description", AspireClient.serializeValue(description));
        map.put("Parameter", AspireClient.serializeValue(parameter));
        map.put("ConfirmationMessage", AspireClient.serializeValue(confirmationMessage));
        map.put("IconName", AspireClient.serializeValue(iconName));
        map.put("IconVariant", AspireClient.serializeValue(iconVariant));
        map.put("IsHighlighted", AspireClient.serializeValue(isHighlighted));
        map.put("UpdateState", AspireClient.serializeValue(updateState));
        return map;
    }
}

/** ExecuteCommandResult DTO. */
class ExecuteCommandResult {
    private boolean success;
    private boolean canceled;
    private String errorMessage;

    public boolean getSuccess() { return success; }
    public void setSuccess(boolean value) { this.success = value; }
    public boolean getCanceled() { return canceled; }
    public void setCanceled(boolean value) { this.canceled = value; }
    public String getErrorMessage() { return errorMessage; }
    public void setErrorMessage(String value) { this.errorMessage = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Success", AspireClient.serializeValue(success));
        map.put("Canceled", AspireClient.serializeValue(canceled));
        map.put("ErrorMessage", AspireClient.serializeValue(errorMessage));
        return map;
    }
}

/** ResourceUrlAnnotation DTO. */
class ResourceUrlAnnotation {
    private String url;
    private String displayText;
    private EndpointReference endpoint;
    private UrlDisplayLocation displayLocation;

    public String getUrl() { return url; }
    public void setUrl(String value) { this.url = value; }
    public String getDisplayText() { return displayText; }
    public void setDisplayText(String value) { this.displayText = value; }
    public EndpointReference getEndpoint() { return endpoint; }
    public void setEndpoint(EndpointReference value) { this.endpoint = value; }
    public UrlDisplayLocation getDisplayLocation() { return displayLocation; }
    public void setDisplayLocation(UrlDisplayLocation value) { this.displayLocation = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Url", AspireClient.serializeValue(url));
        map.put("DisplayText", AspireClient.serializeValue(displayText));
        map.put("Endpoint", AspireClient.serializeValue(endpoint));
        map.put("DisplayLocation", AspireClient.serializeValue(displayLocation));
        return map;
    }
}

// ============================================================================
// Options Types
// ============================================================================

/** Options for AddDockerfile. */
final class AddDockerfileOptions {
    private String dockerfilePath;
    private String stage;

    public String getDockerfilePath() { return dockerfilePath; }
    public AddDockerfileOptions dockerfilePath(String value) {
        this.dockerfilePath = value;
        return this;
    }

    public String getStage() { return stage; }
    public AddDockerfileOptions stage(String value) {
        this.stage = value;
        return this;
    }

}

/** Options for AddParameterWithValue. */
final class AddParameterWithValueOptions {
    private Boolean publishValueAsDefault;
    private Boolean secret;

    public Boolean getPublishValueAsDefault() { return publishValueAsDefault; }
    public AddParameterWithValueOptions publishValueAsDefault(Boolean value) {
        this.publishValueAsDefault = value;
        return this;
    }

    public Boolean getSecret() { return secret; }
    public AddParameterWithValueOptions secret(Boolean value) {
        this.secret = value;
        return this;
    }

}

/** Options for CompleteStepMarkdown. */
final class CompleteStepMarkdownOptions {
    private String completionState;
    private CancellationToken cancellationToken;

    public String getCompletionState() { return completionState; }
    public CompleteStepMarkdownOptions completionState(String value) {
        this.completionState = value;
        return this;
    }

    public CancellationToken getCancellationToken() { return cancellationToken; }
    public CompleteStepMarkdownOptions cancellationToken(CancellationToken value) {
        this.cancellationToken = value;
        return this;
    }

}

/** Options for CompleteStep. */
final class CompleteStepOptions {
    private String completionState;
    private CancellationToken cancellationToken;

    public String getCompletionState() { return completionState; }
    public CompleteStepOptions completionState(String value) {
        this.completionState = value;
        return this;
    }

    public CancellationToken getCancellationToken() { return cancellationToken; }
    public CompleteStepOptions cancellationToken(CancellationToken value) {
        this.cancellationToken = value;
        return this;
    }

}

/** Options for CompleteTaskMarkdown. */
final class CompleteTaskMarkdownOptions {
    private String completionState;
    private CancellationToken cancellationToken;

    public String getCompletionState() { return completionState; }
    public CompleteTaskMarkdownOptions completionState(String value) {
        this.completionState = value;
        return this;
    }

    public CancellationToken getCancellationToken() { return cancellationToken; }
    public CompleteTaskMarkdownOptions cancellationToken(CancellationToken value) {
        this.cancellationToken = value;
        return this;
    }

}

/** Options for CompleteTask. */
final class CompleteTaskOptions {
    private String completionMessage;
    private String completionState;
    private CancellationToken cancellationToken;

    public String getCompletionMessage() { return completionMessage; }
    public CompleteTaskOptions completionMessage(String value) {
        this.completionMessage = value;
        return this;
    }

    public String getCompletionState() { return completionState; }
    public CompleteTaskOptions completionState(String value) {
        this.completionState = value;
        return this;
    }

    public CancellationToken getCancellationToken() { return cancellationToken; }
    public CompleteTaskOptions cancellationToken(CancellationToken value) {
        this.cancellationToken = value;
        return this;
    }

}

/** Options for PublishResourceUpdate. */
final class PublishResourceUpdateOptions {
    private String state;
    private String stateStyle;

    public String getState() { return state; }
    public PublishResourceUpdateOptions state(String value) {
        this.state = value;
        return this;
    }

    public String getStateStyle() { return stateStyle; }
    public PublishResourceUpdateOptions stateStyle(String value) {
        this.stateStyle = value;
        return this;
    }

}

/** Options for WithDockerfileBaseImage. */
final class WithDockerfileBaseImageOptions {
    private String buildImage;
    private String runtimeImage;

    public String getBuildImage() { return buildImage; }
    public WithDockerfileBaseImageOptions buildImage(String value) {
        this.buildImage = value;
        return this;
    }

    public String getRuntimeImage() { return runtimeImage; }
    public WithDockerfileBaseImageOptions runtimeImage(String value) {
        this.runtimeImage = value;
        return this;
    }

}

/** Options for WithDockerfile. */
final class WithDockerfileOptions {
    private String dockerfilePath;
    private String stage;

    public String getDockerfilePath() { return dockerfilePath; }
    public WithDockerfileOptions dockerfilePath(String value) {
        this.dockerfilePath = value;
        return this;
    }

    public String getStage() { return stage; }
    public WithDockerfileOptions stage(String value) {
        this.stage = value;
        return this;
    }

}

/** Options for WithEndpoint. */
final class WithEndpointOptions {
    private Double port;
    private Double targetPort;
    private String scheme;
    private String name;
    private String env;
    private Boolean isProxied;
    private Boolean isExternal;
    private ProtocolType protocol;

    public Double getPort() { return port; }
    public WithEndpointOptions port(Double value) {
        this.port = value;
        return this;
    }

    public Double getTargetPort() { return targetPort; }
    public WithEndpointOptions targetPort(Double value) {
        this.targetPort = value;
        return this;
    }

    public String getScheme() { return scheme; }
    public WithEndpointOptions scheme(String value) {
        this.scheme = value;
        return this;
    }

    public String getName() { return name; }
    public WithEndpointOptions name(String value) {
        this.name = value;
        return this;
    }

    public String getEnv() { return env; }
    public WithEndpointOptions env(String value) {
        this.env = value;
        return this;
    }

    public Boolean isProxied() { return isProxied; }
    public WithEndpointOptions isProxied(Boolean value) {
        this.isProxied = value;
        return this;
    }

    public Boolean isExternal() { return isExternal; }
    public WithEndpointOptions isExternal(Boolean value) {
        this.isExternal = value;
        return this;
    }

    public ProtocolType getProtocol() { return protocol; }
    public WithEndpointOptions protocol(ProtocolType value) {
        this.protocol = value;
        return this;
    }

}

/** Options for WithExternalServiceHttpHealthCheck. */
final class WithExternalServiceHttpHealthCheckOptions {
    private String path;
    private Double statusCode;

    public String getPath() { return path; }
    public WithExternalServiceHttpHealthCheckOptions path(String value) {
        this.path = value;
        return this;
    }

    public Double getStatusCode() { return statusCode; }
    public WithExternalServiceHttpHealthCheckOptions statusCode(Double value) {
        this.statusCode = value;
        return this;
    }

}

/** Options for WithHttpEndpoint. */
final class WithHttpEndpointOptions {
    private Double port;
    private Double targetPort;
    private String name;
    private String env;
    private Boolean isProxied;

    public Double getPort() { return port; }
    public WithHttpEndpointOptions port(Double value) {
        this.port = value;
        return this;
    }

    public Double getTargetPort() { return targetPort; }
    public WithHttpEndpointOptions targetPort(Double value) {
        this.targetPort = value;
        return this;
    }

    public String getName() { return name; }
    public WithHttpEndpointOptions name(String value) {
        this.name = value;
        return this;
    }

    public String getEnv() { return env; }
    public WithHttpEndpointOptions env(String value) {
        this.env = value;
        return this;
    }

    public Boolean isProxied() { return isProxied; }
    public WithHttpEndpointOptions isProxied(Boolean value) {
        this.isProxied = value;
        return this;
    }

}

/** Options for WithHttpHealthCheck. */
final class WithHttpHealthCheckOptions {
    private String path;
    private Double statusCode;
    private String endpointName;

    public String getPath() { return path; }
    public WithHttpHealthCheckOptions path(String value) {
        this.path = value;
        return this;
    }

    public Double getStatusCode() { return statusCode; }
    public WithHttpHealthCheckOptions statusCode(Double value) {
        this.statusCode = value;
        return this;
    }

    public String getEndpointName() { return endpointName; }
    public WithHttpHealthCheckOptions endpointName(String value) {
        this.endpointName = value;
        return this;
    }

}

/** Options for WithHttpProbe. */
final class WithHttpProbeOptions {
    private String path;
    private Double initialDelaySeconds;
    private Double periodSeconds;
    private Double timeoutSeconds;
    private Double failureThreshold;
    private Double successThreshold;
    private String endpointName;

    public String getPath() { return path; }
    public WithHttpProbeOptions path(String value) {
        this.path = value;
        return this;
    }

    public Double getInitialDelaySeconds() { return initialDelaySeconds; }
    public WithHttpProbeOptions initialDelaySeconds(Double value) {
        this.initialDelaySeconds = value;
        return this;
    }

    public Double getPeriodSeconds() { return periodSeconds; }
    public WithHttpProbeOptions periodSeconds(Double value) {
        this.periodSeconds = value;
        return this;
    }

    public Double getTimeoutSeconds() { return timeoutSeconds; }
    public WithHttpProbeOptions timeoutSeconds(Double value) {
        this.timeoutSeconds = value;
        return this;
    }

    public Double getFailureThreshold() { return failureThreshold; }
    public WithHttpProbeOptions failureThreshold(Double value) {
        this.failureThreshold = value;
        return this;
    }

    public Double getSuccessThreshold() { return successThreshold; }
    public WithHttpProbeOptions successThreshold(Double value) {
        this.successThreshold = value;
        return this;
    }

    public String getEndpointName() { return endpointName; }
    public WithHttpProbeOptions endpointName(String value) {
        this.endpointName = value;
        return this;
    }

}

/** Options for WithHttpsEndpoint. */
final class WithHttpsEndpointOptions {
    private Double port;
    private Double targetPort;
    private String name;
    private String env;
    private Boolean isProxied;

    public Double getPort() { return port; }
    public WithHttpsEndpointOptions port(Double value) {
        this.port = value;
        return this;
    }

    public Double getTargetPort() { return targetPort; }
    public WithHttpsEndpointOptions targetPort(Double value) {
        this.targetPort = value;
        return this;
    }

    public String getName() { return name; }
    public WithHttpsEndpointOptions name(String value) {
        this.name = value;
        return this;
    }

    public String getEnv() { return env; }
    public WithHttpsEndpointOptions env(String value) {
        this.env = value;
        return this;
    }

    public Boolean isProxied() { return isProxied; }
    public WithHttpsEndpointOptions isProxied(Boolean value) {
        this.isProxied = value;
        return this;
    }

}

/** Options for WithMcpServer. */
final class WithMcpServerOptions {
    private String path;
    private String endpointName;

    public String getPath() { return path; }
    public WithMcpServerOptions path(String value) {
        this.path = value;
        return this;
    }

    public String getEndpointName() { return endpointName; }
    public WithMcpServerOptions endpointName(String value) {
        this.endpointName = value;
        return this;
    }

}

/** Options for WithPip. */
final class WithPipOptions {
    private Boolean install;
    private String[] installArgs;

    public Boolean getInstall() { return install; }
    public WithPipOptions install(Boolean value) {
        this.install = value;
        return this;
    }

    public String[] getInstallArgs() { return installArgs; }
    public WithPipOptions installArgs(String[] value) {
        this.installArgs = value;
        return this;
    }

}

/** Options for WithPipelineStepFactory. */
final class WithPipelineStepFactoryOptions {
    private String[] dependsOn;
    private String[] requiredBy;
    private String[] tags;
    private String description;

    public String[] getDependsOn() { return dependsOn; }
    public WithPipelineStepFactoryOptions dependsOn(String[] value) {
        this.dependsOn = value;
        return this;
    }

    public String[] getRequiredBy() { return requiredBy; }
    public WithPipelineStepFactoryOptions requiredBy(String[] value) {
        this.requiredBy = value;
        return this;
    }

    public String[] getTags() { return tags; }
    public WithPipelineStepFactoryOptions tags(String[] value) {
        this.tags = value;
        return this;
    }

    public String getDescription() { return description; }
    public WithPipelineStepFactoryOptions description(String value) {
        this.description = value;
        return this;
    }

}

/** Options for WithReference. */
final class WithReferenceOptions {
    private String connectionName;
    private Boolean optional;
    private String name;

    public String getConnectionName() { return connectionName; }
    public WithReferenceOptions connectionName(String value) {
        this.connectionName = value;
        return this;
    }

    public Boolean getOptional() { return optional; }
    public WithReferenceOptions optional(Boolean value) {
        this.optional = value;
        return this;
    }

    public String getName() { return name; }
    public WithReferenceOptions name(String value) {
        this.name = value;
        return this;
    }

}

/** Options for WithUv. */
final class WithUvOptions {
    private Boolean install;
    private String[] args;

    public Boolean getInstall() { return install; }
    public WithUvOptions install(Boolean value) {
        this.install = value;
        return this;
    }

    public String[] getArgs() { return args; }
    public WithUvOptions args(String[] value) {
        this.args = value;
        return this;
    }

}

/** Options for WithVolume. */
final class WithVolumeOptions {
    private String name;
    private Boolean isReadOnly;

    public String getName() { return name; }
    public WithVolumeOptions name(String value) {
        this.name = value;
        return this;
    }

    public Boolean isReadOnly() { return isReadOnly; }
    public WithVolumeOptions isReadOnly(Boolean value) {
        this.isReadOnly = value;
        return this;
    }

}

// ============================================================================
// Handle Wrappers
// ============================================================================

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.AfterResourcesCreatedEvent. */
class AfterResourcesCreatedEvent extends HandleWrapperBase {
    AfterResourcesCreatedEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/AfterResourcesCreatedEvent.services", reqArgs);
    }

    /** Gets the Model property */
    public DistributedApplicationModel model() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationModel) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/AfterResourcesCreatedEvent.model", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.BeforeResourceStartedEvent. */
class BeforeResourceStartedEvent extends HandleWrapperBase {
    BeforeResourceStartedEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/BeforeResourceStartedEvent.resource", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/BeforeResourceStartedEvent.services", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.BeforeStartEvent. */
class BeforeStartEvent extends HandleWrapperBase {
    BeforeStartEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/BeforeStartEvent.services", reqArgs);
    }

    /** Gets the Model property */
    public DistributedApplicationModel model() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationModel) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/BeforeStartEvent.model", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.CSharpAppResource. */
class CSharpAppResource extends ResourceBuilderBase {
    CSharpAppResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    /** Sets the number of replicas */
    public ProjectResource withReplicas(double replicas) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("replicas", AspireClient.serializeValue(replicas));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/withReplicas", reqArgs);
    }

    /** Disables forwarded headers for the project */
    public ProjectResource disableForwardedHeaders() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/disableForwardedHeaders", reqArgs);
    }

    public ProjectResource publishAsDockerFile() {
        return publishAsDockerFile(null);
    }

    /** Publishes a project as a Docker file with optional container configuration */
    public ProjectResource publishAsDockerFile(AspireAction1<ContainerResource> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureId = configure == null ? null : getClient().registerCallback(args -> {
            var obj = (ContainerResource) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/publishProjectAsDockerFileWithConfigure", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    public IContainerFilesDestinationResource publishWithContainerFiles(IResourceWithContainerFiles source, String destinationPath) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        reqArgs.put("destinationPath", AspireClient.serializeValue(destinationPath));
        return (IContainerFilesDestinationResource) getClient().invokeCapability("Aspire.Hosting/publishWithContainerFiles", reqArgs);
    }

    public IContainerFilesDestinationResource publishWithContainerFiles(ResourceBuilderBase source, String destinationPath) {
        return publishWithContainerFiles(new IResourceWithContainerFiles(source.getHandle(), source.getClient()), destinationPath);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext. */
class CommandLineArgsCallbackContext extends HandleWrapperBase {
    CommandLineArgsCallbackContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Args property */
    private AspireList<Object> argsField;
    public AspireList<Object> args() {
        if (argsField == null) {
            argsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args");
        }
        return argsField;
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.cancellationToken", reqArgs);
    }

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.executionContext", reqArgs);
    }

    /** Sets the ExecutionContext property */
    public CommandLineArgsCallbackContext setExecutionContext(DistributedApplicationExecutionContext value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (CommandLineArgsCallbackContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.setExecutionContext", reqArgs);
    }

    /** Gets the Logger property */
    public ILogger logger() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.logger", reqArgs);
    }

    /** Sets the Logger property */
    public CommandLineArgsCallbackContext setLogger(ILogger value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (CommandLineArgsCallbackContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.setLogger", reqArgs);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.resource", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ConnectionStringAvailableEvent. */
class ConnectionStringAvailableEvent extends HandleWrapperBase {
    ConnectionStringAvailableEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ConnectionStringAvailableEvent.resource", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ConnectionStringAvailableEvent.services", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ConnectionStringResource. */
class ConnectionStringResource extends ResourceBuilderBase {
    ConnectionStringResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Adds a connection property with a reference expression */
    public IResourceWithConnectionString withConnectionProperty(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting/withConnectionProperty", reqArgs);
    }

    /** Adds a connection property with a string value */
    public IResourceWithConnectionString withConnectionPropertyValue(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting/withConnectionPropertyValue", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the ConnectionStringAvailable event */
    public IResourceWithConnectionString onConnectionStringAvailable(AspireAction1<ConnectionStringAvailableEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ConnectionStringAvailableEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting/onConnectionStringAvailable", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerRegistryResource. */
class ContainerRegistryResource extends ResourceBuilderBase {
    ContainerRegistryResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource. */
class ContainerResource extends ResourceBuilderBase {
    ContainerResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    public ContainerResource withBindMount(String source, String target) {
        return withBindMount(source, target, null);
    }

    /** Adds a bind mount */
    public ContainerResource withBindMount(String source, String target, Boolean isReadOnly) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        reqArgs.put("target", AspireClient.serializeValue(target));
        if (isReadOnly != null) {
            reqArgs.put("isReadOnly", AspireClient.serializeValue(isReadOnly));
        }
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withBindMount", reqArgs);
    }

    /** Sets the container entrypoint */
    public ContainerResource withEntrypoint(String entrypoint) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("entrypoint", AspireClient.serializeValue(entrypoint));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withEntrypoint", reqArgs);
    }

    /** Sets the container image tag */
    public ContainerResource withImageTag(String tag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("tag", AspireClient.serializeValue(tag));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withImageTag", reqArgs);
    }

    /** Sets the container image registry */
    public ContainerResource withImageRegistry(String registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withImageRegistry", reqArgs);
    }

    public ContainerResource withImage(String image) {
        return withImage(image, null);
    }

    /** Sets the container image */
    public ContainerResource withImage(String image, String tag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("image", AspireClient.serializeValue(image));
        if (tag != null) {
            reqArgs.put("tag", AspireClient.serializeValue(tag));
        }
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withImage", reqArgs);
    }

    /** Sets the image SHA256 digest */
    public ContainerResource withImageSHA256(String sha256) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("sha256", AspireClient.serializeValue(sha256));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withImageSHA256", reqArgs);
    }

    /** Adds runtime arguments for the container */
    public ContainerResource withContainerRuntimeArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withContainerRuntimeArgs", reqArgs);
    }

    /** Sets the lifetime behavior of the container resource */
    public ContainerResource withLifetime(ContainerLifetime lifetime) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("lifetime", AspireClient.serializeValue(lifetime));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withLifetime", reqArgs);
    }

    /** Sets the container image pull policy */
    public ContainerResource withImagePullPolicy(ImagePullPolicy pullPolicy) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("pullPolicy", AspireClient.serializeValue(pullPolicy));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withImagePullPolicy", reqArgs);
    }

    /** Configures the resource to be published as a container */
    public ContainerResource publishAsContainer() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/publishAsContainer", reqArgs);
    }

    /** Configures the resource to use a Dockerfile */
    public ContainerResource withDockerfile(String contextPath, WithDockerfileOptions options) {
        var dockerfilePath = options == null ? null : options.getDockerfilePath();
        var stage = options == null ? null : options.getStage();
        return withDockerfileImpl(contextPath, dockerfilePath, stage);
    }

    public ContainerResource withDockerfile(String contextPath) {
        return withDockerfile(contextPath, null);
    }

    /** Configures the resource to use a Dockerfile */
    private ContainerResource withDockerfileImpl(String contextPath, String dockerfilePath, String stage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("contextPath", AspireClient.serializeValue(contextPath));
        if (dockerfilePath != null) {
            reqArgs.put("dockerfilePath", AspireClient.serializeValue(dockerfilePath));
        }
        if (stage != null) {
            reqArgs.put("stage", AspireClient.serializeValue(stage));
        }
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withDockerfile", reqArgs);
    }

    /** Sets the container name */
    public ContainerResource withContainerName(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withContainerName", reqArgs);
    }

    /** Adds a build argument from a parameter resource */
    public ContainerResource withBuildArg(String name, ParameterResource value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withBuildArg", reqArgs);
    }

    /** Adds a build secret from a parameter resource */
    public ContainerResource withBuildSecret(String name, ParameterResource value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withBuildSecret", reqArgs);
    }

    /** Configures endpoint proxy support */
    public ContainerResource withEndpointProxySupport(boolean proxyEnabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("proxyEnabled", AspireClient.serializeValue(proxyEnabled));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withEndpointProxySupport", reqArgs);
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Adds a network alias for the container */
    public ContainerResource withContainerNetworkAlias(String alias) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("alias", AspireClient.serializeValue(alias));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withContainerNetworkAlias", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    /** Publishes the resource as a connection string */
    public ContainerResource publishAsConnectionString() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/publishAsConnectionString", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Adds a volume */
    public ContainerResource withVolume(String target, WithVolumeOptions options) {
        var name = options == null ? null : options.getName();
        var isReadOnly = options == null ? null : options.isReadOnly();
        return withVolumeImpl(target, name, isReadOnly);
    }

    public ContainerResource withVolume(String target) {
        return withVolume(target, null);
    }

    /** Adds a volume */
    private ContainerResource withVolumeImpl(String target, String name, Boolean isReadOnly) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        reqArgs.put("target", AspireClient.serializeValue(target));
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (isReadOnly != null) {
            reqArgs.put("isReadOnly", AspireClient.serializeValue(isReadOnly));
        }
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withVolume", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplication. */
class DistributedApplication extends HandleWrapperBase {
    DistributedApplication(Handle handle, AspireClient client) {
        super(handle, client);
    }

    public void run() {
        run(null);
    }

    /** Runs the distributed application */
    public void run(CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/run", reqArgs);
    }

    /** Create a new distributed application builder. */
    public static IDistributedApplicationBuilder CreateBuilder() throws Exception {
        return CreateBuilder((String[]) null);
    }

    /** Create a new distributed application builder. */
    public static IDistributedApplicationBuilder CreateBuilder(String[] args) throws Exception {
        CreateBuilderOptions options = new CreateBuilderOptions();
        if (args != null) {
            options.setArgs(args);
        }
        return CreateBuilder(options);
    }

    /** Create a new distributed application builder. */
    public static IDistributedApplicationBuilder CreateBuilder(CreateBuilderOptions options) throws Exception {
        return Aspire.createBuilder(options);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription. */
class DistributedApplicationEventSubscription extends HandleWrapperBase {
    DistributedApplicationEventSubscription(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext. */
class DistributedApplicationExecutionContext extends HandleWrapperBase {
    DistributedApplicationExecutionContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the PublisherName property */
    public String publisherName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.publisherName", reqArgs);
    }

    /** Sets the PublisherName property */
    public DistributedApplicationExecutionContext setPublisherName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName", reqArgs);
    }

    /** Gets the Operation property */
    public DistributedApplicationOperation operation() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationOperation) getClient().invokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.operation", reqArgs);
    }

    /** Gets the ServiceProvider property */
    public IServiceProvider serviceProvider() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.serviceProvider", reqArgs);
    }

    /** Gets the IsPublishMode property */
    public boolean isPublishMode() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode", reqArgs);
    }

    /** Gets the IsRunMode property */
    public boolean isRunMode() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions. */
class DistributedApplicationExecutionContextOptions extends HandleWrapperBase {
    DistributedApplicationExecutionContextOptions(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.DistributedApplicationModel. */
class DistributedApplicationModel extends HandleWrapperBase {
    DistributedApplicationModel(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets resources from the distributed application model */
    public IResource[] getResources() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("model", AspireClient.serializeValue(getHandle()));
        return (IResource[]) getClient().invokeCapability("Aspire.Hosting/getResources", reqArgs);
    }

    /** Finds a resource by name */
    public IResource findResourceByName(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("model", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/findResourceByName", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription. */
class DistributedApplicationResourceEventSubscription extends HandleWrapperBase {
    DistributedApplicationResourceEventSubscription(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.DotnetToolResource. */
class DotnetToolResource extends ResourceBuilderBase {
    DotnetToolResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Sets the tool package ID */
    public DotnetToolResource withToolPackage(String packageId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("packageId", AspireClient.serializeValue(packageId));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/withToolPackage", reqArgs);
    }

    /** Sets the tool version */
    public DotnetToolResource withToolVersion(String version) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("version", AspireClient.serializeValue(version));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/withToolVersion", reqArgs);
    }

    /** Allows prerelease tool versions */
    public DotnetToolResource withToolPrerelease() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/withToolPrerelease", reqArgs);
    }

    /** Adds a NuGet source for the tool */
    public DotnetToolResource withToolSource(String source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/withToolSource", reqArgs);
    }

    /** Ignores existing NuGet feeds */
    public DotnetToolResource withToolIgnoreExistingFeeds() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/withToolIgnoreExistingFeeds", reqArgs);
    }

    /** Ignores failed NuGet sources */
    public DotnetToolResource withToolIgnoreFailedSources() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/withToolIgnoreFailedSources", reqArgs);
    }

    /** Publishes the executable as a Docker container */
    public ExecutableResource publishAsDockerFile() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFile", reqArgs);
    }

    /** Publishes an executable as a Docker file with optional container configuration */
    public ExecutableResource publishAsDockerFileWithConfigure(AspireAction1<ContainerResource> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureId = getClient().registerCallback(args -> {
            var obj = (ContainerResource) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFileWithConfigure", reqArgs);
    }

    /** Sets the executable command */
    public ExecutableResource withExecutableCommand(String command) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withExecutableCommand", reqArgs);
    }

    /** Sets the executable working directory */
    public ExecutableResource withWorkingDirectory(String workingDirectory) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("workingDirectory", AspireClient.serializeValue(workingDirectory));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withWorkingDirectory", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference. */
class EndpointReference extends HandleWrapperBase {
    EndpointReference(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResourceWithEndpoints resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.resource", reqArgs);
    }

    /** Gets the EndpointName property */
    public String endpointName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.endpointName", reqArgs);
    }

    /** Gets the ErrorMessage property */
    public String errorMessage() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage", reqArgs);
    }

    /** Sets the ErrorMessage property */
    public EndpointReference setErrorMessage(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage", reqArgs);
    }

    /** Gets the IsAllocated property */
    public boolean isAllocated() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated", reqArgs);
    }

    /** Gets the Exists property */
    public boolean exists() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.exists", reqArgs);
    }

    /** Gets the IsHttp property */
    public boolean isHttp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttp", reqArgs);
    }

    /** Gets the IsHttps property */
    public boolean isHttps() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttps", reqArgs);
    }

    /** Gets the TlsEnabled property */
    public boolean tlsEnabled() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.tlsEnabled", reqArgs);
    }

    /** Gets the Port property */
    public double port() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.port", reqArgs);
    }

    /** Gets the TargetPort property */
    public double targetPort() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.targetPort", reqArgs);
    }

    /** Gets the Host property */
    public String host() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.host", reqArgs);
    }

    /** Gets the Scheme property */
    public String scheme() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.scheme", reqArgs);
    }

    /** Gets the Url property */
    public String url() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.url", reqArgs);
    }

    public String getValueAsync() {
        return getValueAsync(null);
    }

    /** Gets the URL of the endpoint asynchronously */
    public String getValueAsync(CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/getValueAsync", reqArgs);
    }

    /** Gets a conditional expression that resolves to the enabledValue when TLS is enabled on the endpoint, or to the disabledValue otherwise. */
    public ReferenceExpression getTlsValue(ReferenceExpression enabledValue, ReferenceExpression disabledValue) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("enabledValue", AspireClient.serializeValue(enabledValue));
        reqArgs.put("disabledValue", AspireClient.serializeValue(disabledValue));
        return (ReferenceExpression) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.getTlsValue", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression. */
class EndpointReferenceExpression extends HandleWrapperBase {
    EndpointReferenceExpression(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Endpoint property */
    public EndpointReference endpoint() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.endpoint", reqArgs);
    }

    /** Gets the Property property */
    public EndpointProperty property() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (EndpointProperty) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.property", reqArgs);
    }

    /** Gets the ValueExpression property */
    public String valueExpression() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.valueExpression", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext. */
class EnvironmentCallbackContext extends HandleWrapperBase {
    EnvironmentCallbackContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the EnvironmentVariables property */
    private AspireDict<String, AspireUnion> environmentVariablesField;
    public AspireDict<String, AspireUnion> environmentVariables() {
        if (environmentVariablesField == null) {
            environmentVariablesField = new AspireDict<>(getHandle(), getClient(), "Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables");
        }
        return environmentVariablesField;
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken", reqArgs);
    }

    /** Gets the Logger property */
    public ILogger logger() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.logger", reqArgs);
    }

    /** Sets the Logger property */
    public EnvironmentCallbackContext setLogger(ILogger value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (EnvironmentCallbackContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.setLogger", reqArgs);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.resource", reqArgs);
    }

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource. */
class ExecutableResource extends ResourceBuilderBase {
    ExecutableResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Publishes the executable as a Docker container */
    public ExecutableResource publishAsDockerFile() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFile", reqArgs);
    }

    /** Publishes an executable as a Docker file with optional container configuration */
    public ExecutableResource publishAsDockerFileWithConfigure(AspireAction1<ContainerResource> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureId = getClient().registerCallback(args -> {
            var obj = (ContainerResource) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFileWithConfigure", reqArgs);
    }

    /** Sets the executable command */
    public ExecutableResource withExecutableCommand(String command) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withExecutableCommand", reqArgs);
    }

    /** Sets the executable working directory */
    public ExecutableResource withWorkingDirectory(String workingDirectory) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("workingDirectory", AspireClient.serializeValue(workingDirectory));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withWorkingDirectory", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext. */
class ExecuteCommandContext extends HandleWrapperBase {
    ExecuteCommandContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the ServiceProvider property */
    public IServiceProvider serviceProvider() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.serviceProvider", reqArgs);
    }

    /** Sets the ServiceProvider property */
    public ExecuteCommandContext setServiceProvider(IServiceProvider value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ExecuteCommandContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setServiceProvider", reqArgs);
    }

    /** Gets the ResourceName property */
    public String resourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.resourceName", reqArgs);
    }

    /** Sets the ResourceName property */
    public ExecuteCommandContext setResourceName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ExecuteCommandContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setResourceName", reqArgs);
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.cancellationToken", reqArgs);
    }

    /** Sets the CancellationToken property */
    public ExecuteCommandContext setCancellationToken(CancellationToken value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", getClient().registerCancellation(value));
        }
        return (ExecuteCommandContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setCancellationToken", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ExternalServiceResource. */
class ExternalServiceResource extends ResourceBuilderBase {
    ExternalServiceResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Adds an HTTP health check to an external service */
    public ExternalServiceResource withExternalServiceHttpHealthCheck(WithExternalServiceHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        return withExternalServiceHttpHealthCheckImpl(path, statusCode);
    }

    public ExternalServiceResource withExternalServiceHttpHealthCheck() {
        return withExternalServiceHttpHealthCheck(null);
    }

    /** Adds an HTTP health check to an external service */
    private ExternalServiceResource withExternalServiceHttpHealthCheckImpl(String path, Double statusCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        return (ExternalServiceResource) getClient().invokeCapability("Aspire.Hosting/withExternalServiceHttpHealthCheck", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IComputeResource. */
class IComputeResource extends HandleWrapperBase {
    IComputeResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfiguration. */
class IConfiguration extends HandleWrapperBase {
    IConfiguration(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets a configuration value by key */
    public String getConfigValue(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("configuration", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (String) getClient().invokeCapability("Aspire.Hosting/getConfigValue", reqArgs);
    }

    /** Gets a connection string by name */
    public String getConnectionString(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("configuration", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (String) getClient().invokeCapability("Aspire.Hosting/getConnectionString", reqArgs);
    }

    /** Gets a configuration section by key */
    public IConfigurationSection getSection(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("configuration", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IConfigurationSection) getClient().invokeCapability("Aspire.Hosting/getSection", reqArgs);
    }

    /** Gets child configuration sections */
    public IConfigurationSection[] getChildren() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("configuration", AspireClient.serializeValue(getHandle()));
        return (IConfigurationSection[]) getClient().invokeCapability("Aspire.Hosting/getChildren", reqArgs);
    }

    /** Checks whether a configuration section exists */
    public boolean exists(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("configuration", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/exists", reqArgs);
    }

}

/** Wrapper for Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfigurationSection. */
class IConfigurationSection extends HandleWrapperBase {
    IConfigurationSection(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IContainerFilesDestinationResource. */
class IContainerFilesDestinationResource extends HandleWrapperBase {
    IContainerFilesDestinationResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder. */
class IDistributedApplicationBuilder extends HandleWrapperBase {
    IDistributedApplicationBuilder(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Adds a connection string with a reference expression */
    public ConnectionStringResource addConnectionStringExpression(String name, ReferenceExpression connectionStringExpression) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("connectionStringExpression", AspireClient.serializeValue(connectionStringExpression));
        return (ConnectionStringResource) getClient().invokeCapability("Aspire.Hosting/addConnectionStringExpression", reqArgs);
    }

    /** Adds a connection string with a builder callback */
    public ConnectionStringResource addConnectionStringBuilder(String name, AspireAction1<ReferenceExpressionBuilder> connectionStringBuilder) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        var connectionStringBuilderId = getClient().registerCallback(args -> {
            var obj = (ReferenceExpressionBuilder) args[0];
            connectionStringBuilder.invoke(obj);
            return null;
        });
        if (connectionStringBuilderId != null) {
            reqArgs.put("connectionStringBuilder", connectionStringBuilderId);
        }
        return (ConnectionStringResource) getClient().invokeCapability("Aspire.Hosting/addConnectionStringBuilder", reqArgs);
    }

    public ContainerRegistryResource addContainerRegistry(String name, ParameterResource endpoint) {
        return addContainerRegistry(name, endpoint, null);
    }

    /** Adds a container registry resource */
    public ContainerRegistryResource addContainerRegistry(String name, ParameterResource endpoint, ParameterResource repository) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpoint", AspireClient.serializeValue(endpoint));
        if (repository != null) {
            reqArgs.put("repository", AspireClient.serializeValue(repository));
        }
        return (ContainerRegistryResource) getClient().invokeCapability("Aspire.Hosting/addContainerRegistry", reqArgs);
    }

    public ContainerRegistryResource addContainerRegistryFromString(String name, String endpoint) {
        return addContainerRegistryFromString(name, endpoint, null);
    }

    /** Adds a container registry with string endpoint */
    public ContainerRegistryResource addContainerRegistryFromString(String name, String endpoint, String repository) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpoint", AspireClient.serializeValue(endpoint));
        if (repository != null) {
            reqArgs.put("repository", AspireClient.serializeValue(repository));
        }
        return (ContainerRegistryResource) getClient().invokeCapability("Aspire.Hosting/addContainerRegistryFromString", reqArgs);
    }

    /** Adds a container resource */
    public ContainerResource addContainer(String name, String image) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("image", AspireClient.serializeValue(image));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/addContainer", reqArgs);
    }

    /** Adds a container resource built from a Dockerfile */
    public ContainerResource addDockerfile(String name, String contextPath, AddDockerfileOptions options) {
        var dockerfilePath = options == null ? null : options.getDockerfilePath();
        var stage = options == null ? null : options.getStage();
        return addDockerfileImpl(name, contextPath, dockerfilePath, stage);
    }

    public ContainerResource addDockerfile(String name, String contextPath) {
        return addDockerfile(name, contextPath, null);
    }

    /** Adds a container resource built from a Dockerfile */
    private ContainerResource addDockerfileImpl(String name, String contextPath, String dockerfilePath, String stage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("contextPath", AspireClient.serializeValue(contextPath));
        if (dockerfilePath != null) {
            reqArgs.put("dockerfilePath", AspireClient.serializeValue(dockerfilePath));
        }
        if (stage != null) {
            reqArgs.put("stage", AspireClient.serializeValue(stage));
        }
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/addDockerfile", reqArgs);
    }

    /** Adds a .NET tool resource */
    public DotnetToolResource addDotnetTool(String name, String packageId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("packageId", AspireClient.serializeValue(packageId));
        return (DotnetToolResource) getClient().invokeCapability("Aspire.Hosting/addDotnetTool", reqArgs);
    }

    /** Adds an executable resource */
    public ExecutableResource addExecutable(String name, String command, String workingDirectory, String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("command", AspireClient.serializeValue(command));
        reqArgs.put("workingDirectory", AspireClient.serializeValue(workingDirectory));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/addExecutable", reqArgs);
    }

    /** Adds an external service resource */
    public ExternalServiceResource addExternalService(String name, String url) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("url", AspireClient.serializeValue(url));
        return (ExternalServiceResource) getClient().invokeCapability("Aspire.Hosting/addExternalService", reqArgs);
    }

    /** Adds an external service with a URI */
    public ExternalServiceResource addExternalServiceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (ExternalServiceResource) getClient().invokeCapability("Aspire.Hosting/addExternalServiceUri", reqArgs);
    }

    /** Adds an external service with a parameter URL */
    public ExternalServiceResource addExternalServiceParameter(String name, ParameterResource urlParameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("urlParameter", AspireClient.serializeValue(urlParameter));
        return (ExternalServiceResource) getClient().invokeCapability("Aspire.Hosting/addExternalServiceParameter", reqArgs);
    }

    /** Gets the AppHostDirectory property */
    public String appHostDirectory() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory", reqArgs);
    }

    /** Gets the Environment property */
    public IHostEnvironment environment() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IHostEnvironment) getClient().invokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.environment", reqArgs);
    }

    /** Gets the Eventing property */
    public IDistributedApplicationEventing eventing() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IDistributedApplicationEventing) getClient().invokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.eventing", reqArgs);
    }

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.executionContext", reqArgs);
    }

    /** Gets the UserSecretsManager property */
    public IUserSecretsManager userSecretsManager() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IUserSecretsManager) getClient().invokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.userSecretsManager", reqArgs);
    }

    /** Builds the distributed application */
    public DistributedApplication build() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplication) getClient().invokeCapability("Aspire.Hosting/build", reqArgs);
    }

    public ParameterResource addParameter(String name) {
        return addParameter(name, null);
    }

    /** Adds a parameter resource */
    public ParameterResource addParameter(String name, Boolean secret) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (secret != null) {
            reqArgs.put("secret", AspireClient.serializeValue(secret));
        }
        return (ParameterResource) getClient().invokeCapability("Aspire.Hosting/addParameter", reqArgs);
    }

    /** Adds a parameter with a default value */
    public ParameterResource addParameterWithValue(String name, String value, AddParameterWithValueOptions options) {
        var publishValueAsDefault = options == null ? null : options.getPublishValueAsDefault();
        var secret = options == null ? null : options.getSecret();
        return addParameterWithValueImpl(name, value, publishValueAsDefault, secret);
    }

    public ParameterResource addParameterWithValue(String name, String value) {
        return addParameterWithValue(name, value, null);
    }

    /** Adds a parameter with a default value */
    private ParameterResource addParameterWithValueImpl(String name, String value, Boolean publishValueAsDefault, Boolean secret) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        if (publishValueAsDefault != null) {
            reqArgs.put("publishValueAsDefault", AspireClient.serializeValue(publishValueAsDefault));
        }
        if (secret != null) {
            reqArgs.put("secret", AspireClient.serializeValue(secret));
        }
        return (ParameterResource) getClient().invokeCapability("Aspire.Hosting/addParameterWithValue", reqArgs);
    }

    public ParameterResource addParameterFromConfiguration(String name, String configurationKey) {
        return addParameterFromConfiguration(name, configurationKey, null);
    }

    /** Adds a parameter sourced from configuration */
    public ParameterResource addParameterFromConfiguration(String name, String configurationKey, Boolean secret) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("configurationKey", AspireClient.serializeValue(configurationKey));
        if (secret != null) {
            reqArgs.put("secret", AspireClient.serializeValue(secret));
        }
        return (ParameterResource) getClient().invokeCapability("Aspire.Hosting/addParameterFromConfiguration", reqArgs);
    }

    public IResourceWithConnectionString addConnectionString(String name) {
        return addConnectionString(name, null);
    }

    /** Adds a connection string resource */
    public IResourceWithConnectionString addConnectionString(String name, String environmentVariableName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (environmentVariableName != null) {
            reqArgs.put("environmentVariableName", AspireClient.serializeValue(environmentVariableName));
        }
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting/addConnectionString", reqArgs);
    }

    /** Adds a .NET project resource */
    public ProjectResource addProject(String name, String projectPath, String launchProfileName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("projectPath", AspireClient.serializeValue(projectPath));
        reqArgs.put("launchProfileName", AspireClient.serializeValue(launchProfileName));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/addProject", reqArgs);
    }

    /** Adds a project resource with configuration options */
    public ProjectResource addProjectWithOptions(String name, String projectPath, AspireAction1<ProjectResourceOptions> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("projectPath", AspireClient.serializeValue(projectPath));
        var configureId = getClient().registerCallback(args -> {
            var obj = (ProjectResourceOptions) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/addProjectWithOptions", reqArgs);
    }

    /** Adds a C# application resource */
    public ProjectResource addCSharpApp(String name, String path) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("path", AspireClient.serializeValue(path));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/addCSharpApp", reqArgs);
    }

    /** Adds a C# application resource with configuration options */
    public CSharpAppResource addCSharpAppWithOptions(String name, String path, AspireAction1<ProjectResourceOptions> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("path", AspireClient.serializeValue(path));
        var configureId = getClient().registerCallback(args -> {
            var obj = (ProjectResourceOptions) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (CSharpAppResource) getClient().invokeCapability("Aspire.Hosting/addCSharpAppWithOptions", reqArgs);
    }

    /** Gets the application configuration */
    public IConfiguration getConfiguration() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IConfiguration) getClient().invokeCapability("Aspire.Hosting/getConfiguration", reqArgs);
    }

    /** Subscribes to the BeforeStart event */
    public DistributedApplicationEventSubscription subscribeBeforeStart(AspireAction1<BeforeStartEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeStartEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (DistributedApplicationEventSubscription) getClient().invokeCapability("Aspire.Hosting/subscribeBeforeStart", reqArgs);
    }

    /** Subscribes to the AfterResourcesCreated event */
    public DistributedApplicationEventSubscription subscribeAfterResourcesCreated(AspireAction1<AfterResourcesCreatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (AfterResourcesCreatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (DistributedApplicationEventSubscription) getClient().invokeCapability("Aspire.Hosting/subscribeAfterResourcesCreated", reqArgs);
    }

    /** Adds a Python script application resource */
    public PythonAppResource addPythonApp(String name, String appDirectory, String scriptPath) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("appDirectory", AspireClient.serializeValue(appDirectory));
        reqArgs.put("scriptPath", AspireClient.serializeValue(scriptPath));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/addPythonApp", reqArgs);
    }

    /** Adds a Python module application resource */
    public PythonAppResource addPythonModule(String name, String appDirectory, String moduleName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("appDirectory", AspireClient.serializeValue(appDirectory));
        reqArgs.put("moduleName", AspireClient.serializeValue(moduleName));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/addPythonModule", reqArgs);
    }

    /** Adds a Python executable application resource */
    public PythonAppResource addPythonExecutable(String name, String appDirectory, String executableName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("appDirectory", AspireClient.serializeValue(appDirectory));
        reqArgs.put("executableName", AspireClient.serializeValue(executableName));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/addPythonExecutable", reqArgs);
    }

    /** Adds a Uvicorn-based Python application resource */
    public UvicornAppResource addUvicornApp(String name, String appDirectory, String app) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("appDirectory", AspireClient.serializeValue(appDirectory));
        reqArgs.put("app", AspireClient.serializeValue(app));
        return (UvicornAppResource) getClient().invokeCapability("Aspire.Hosting.Python/addUvicornApp", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent. */
class IDistributedApplicationEvent extends HandleWrapperBase {
    IDistributedApplicationEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing. */
class IDistributedApplicationEventing extends HandleWrapperBase {
    IDistributedApplicationEventing(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Invokes the Unsubscribe method */
    public void unsubscribe(DistributedApplicationEventSubscription subscription) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("subscription", AspireClient.serializeValue(subscription));
        getClient().invokeCapability("Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent. */
class IDistributedApplicationResourceEvent extends HandleWrapperBase {
    IDistributedApplicationResourceEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Microsoft.Extensions.Hosting.Abstractions/Microsoft.Extensions.Hosting.IHostEnvironment. */
class IHostEnvironment extends HandleWrapperBase {
    IHostEnvironment(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Checks if running in Development environment */
    public boolean isDevelopment() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("environment", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/isDevelopment", reqArgs);
    }

    /** Checks if running in Production environment */
    public boolean isProduction() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("environment", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/isProduction", reqArgs);
    }

    /** Checks if running in Staging environment */
    public boolean isStaging() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("environment", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/isStaging", reqArgs);
    }

    /** Checks if the environment matches the specified name */
    public boolean isEnvironment(String environmentName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("environment", AspireClient.serializeValue(getHandle()));
        reqArgs.put("environmentName", AspireClient.serializeValue(environmentName));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/isEnvironment", reqArgs);
    }

}

/** Wrapper for Microsoft.Extensions.Logging.Abstractions/Microsoft.Extensions.Logging.ILogger. */
class ILogger extends HandleWrapperBase {
    ILogger(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Logs an information message */
    public void logInformation(String message) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("logger", AspireClient.serializeValue(getHandle()));
        reqArgs.put("message", AspireClient.serializeValue(message));
        getClient().invokeCapability("Aspire.Hosting/logInformation", reqArgs);
    }

    /** Logs a warning message */
    public void logWarning(String message) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("logger", AspireClient.serializeValue(getHandle()));
        reqArgs.put("message", AspireClient.serializeValue(message));
        getClient().invokeCapability("Aspire.Hosting/logWarning", reqArgs);
    }

    /** Logs an error message */
    public void logError(String message) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("logger", AspireClient.serializeValue(getHandle()));
        reqArgs.put("message", AspireClient.serializeValue(message));
        getClient().invokeCapability("Aspire.Hosting/logError", reqArgs);
    }

    /** Logs a debug message */
    public void logDebug(String message) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("logger", AspireClient.serializeValue(getHandle()));
        reqArgs.put("message", AspireClient.serializeValue(message));
        getClient().invokeCapability("Aspire.Hosting/logDebug", reqArgs);
    }

    /** Logs a message with specified level */
    public void log(String level, String message) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("logger", AspireClient.serializeValue(getHandle()));
        reqArgs.put("level", AspireClient.serializeValue(level));
        reqArgs.put("message", AspireClient.serializeValue(message));
        getClient().invokeCapability("Aspire.Hosting/log", reqArgs);
    }

}

/** Wrapper for Microsoft.Extensions.Logging.Abstractions/Microsoft.Extensions.Logging.ILoggerFactory. */
class ILoggerFactory extends HandleWrapperBase {
    ILoggerFactory(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Creates a logger for a category */
    public ILogger createLogger(String categoryName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("loggerFactory", AspireClient.serializeValue(getHandle()));
        reqArgs.put("categoryName", AspireClient.serializeValue(categoryName));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting/createLogger", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.IReportingStep. */
class IReportingStep extends HandleWrapperBase {
    IReportingStep(Handle handle, AspireClient client) {
        super(handle, client);
    }

    public IReportingTask createTask(String statusText) {
        return createTask(statusText, null);
    }

    /** Creates a reporting task with plain-text status text */
    public IReportingTask createTask(String statusText, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingStep", AspireClient.serializeValue(getHandle()));
        reqArgs.put("statusText", AspireClient.serializeValue(statusText));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (IReportingTask) getClient().invokeCapability("Aspire.Hosting/createTask", reqArgs);
    }

    public IReportingTask createMarkdownTask(String markdownString) {
        return createMarkdownTask(markdownString, null);
    }

    /** Creates a reporting task with Markdown-formatted status text */
    public IReportingTask createMarkdownTask(String markdownString, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingStep", AspireClient.serializeValue(getHandle()));
        reqArgs.put("markdownString", AspireClient.serializeValue(markdownString));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (IReportingTask) getClient().invokeCapability("Aspire.Hosting/createMarkdownTask", reqArgs);
    }

    /** Logs a plain-text message for the reporting step */
    public void logStep(String level, String message) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingStep", AspireClient.serializeValue(getHandle()));
        reqArgs.put("level", AspireClient.serializeValue(level));
        reqArgs.put("message", AspireClient.serializeValue(message));
        getClient().invokeCapability("Aspire.Hosting/logStep", reqArgs);
    }

    /** Logs a Markdown-formatted message for the reporting step */
    public void logStepMarkdown(String level, String markdownString) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingStep", AspireClient.serializeValue(getHandle()));
        reqArgs.put("level", AspireClient.serializeValue(level));
        reqArgs.put("markdownString", AspireClient.serializeValue(markdownString));
        getClient().invokeCapability("Aspire.Hosting/logStepMarkdown", reqArgs);
    }

    /** Completes the reporting step with plain-text completion text */
    public void completeStep(String completionText, CompleteStepOptions options) {
        var completionState = options == null ? null : options.getCompletionState();
        var cancellationToken = options == null ? null : options.getCancellationToken();
        completeStepImpl(completionText, completionState, cancellationToken);
    }

    public void completeStep(String completionText) {
        completeStep(completionText, null);
    }

    /** Completes the reporting step with plain-text completion text */
    private void completeStepImpl(String completionText, String completionState, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingStep", AspireClient.serializeValue(getHandle()));
        reqArgs.put("completionText", AspireClient.serializeValue(completionText));
        if (completionState != null) {
            reqArgs.put("completionState", AspireClient.serializeValue(completionState));
        }
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/completeStep", reqArgs);
    }

    /** Completes the reporting step with Markdown-formatted completion text */
    public void completeStepMarkdown(String markdownString, CompleteStepMarkdownOptions options) {
        var completionState = options == null ? null : options.getCompletionState();
        var cancellationToken = options == null ? null : options.getCancellationToken();
        completeStepMarkdownImpl(markdownString, completionState, cancellationToken);
    }

    public void completeStepMarkdown(String markdownString) {
        completeStepMarkdown(markdownString, null);
    }

    /** Completes the reporting step with Markdown-formatted completion text */
    private void completeStepMarkdownImpl(String markdownString, String completionState, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingStep", AspireClient.serializeValue(getHandle()));
        reqArgs.put("markdownString", AspireClient.serializeValue(markdownString));
        if (completionState != null) {
            reqArgs.put("completionState", AspireClient.serializeValue(completionState));
        }
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/completeStepMarkdown", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.IReportingTask. */
class IReportingTask extends HandleWrapperBase {
    IReportingTask(Handle handle, AspireClient client) {
        super(handle, client);
    }

    public void updateTask(String statusText) {
        updateTask(statusText, null);
    }

    /** Updates the reporting task with plain-text status text */
    public void updateTask(String statusText, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingTask", AspireClient.serializeValue(getHandle()));
        reqArgs.put("statusText", AspireClient.serializeValue(statusText));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/updateTask", reqArgs);
    }

    public void updateTaskMarkdown(String markdownString) {
        updateTaskMarkdown(markdownString, null);
    }

    /** Updates the reporting task with Markdown-formatted status text */
    public void updateTaskMarkdown(String markdownString, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingTask", AspireClient.serializeValue(getHandle()));
        reqArgs.put("markdownString", AspireClient.serializeValue(markdownString));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/updateTaskMarkdown", reqArgs);
    }

    /** Completes the reporting task with plain-text completion text */
    public void completeTask(CompleteTaskOptions options) {
        var completionMessage = options == null ? null : options.getCompletionMessage();
        var completionState = options == null ? null : options.getCompletionState();
        var cancellationToken = options == null ? null : options.getCancellationToken();
        completeTaskImpl(completionMessage, completionState, cancellationToken);
    }

    public void completeTask() {
        completeTask(null);
    }

    /** Completes the reporting task with plain-text completion text */
    private void completeTaskImpl(String completionMessage, String completionState, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingTask", AspireClient.serializeValue(getHandle()));
        if (completionMessage != null) {
            reqArgs.put("completionMessage", AspireClient.serializeValue(completionMessage));
        }
        if (completionState != null) {
            reqArgs.put("completionState", AspireClient.serializeValue(completionState));
        }
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/completeTask", reqArgs);
    }

    /** Completes the reporting task with Markdown-formatted completion text */
    public void completeTaskMarkdown(String markdownString, CompleteTaskMarkdownOptions options) {
        var completionState = options == null ? null : options.getCompletionState();
        var cancellationToken = options == null ? null : options.getCancellationToken();
        completeTaskMarkdownImpl(markdownString, completionState, cancellationToken);
    }

    public void completeTaskMarkdown(String markdownString) {
        completeTaskMarkdown(markdownString, null);
    }

    /** Completes the reporting task with Markdown-formatted completion text */
    private void completeTaskMarkdownImpl(String markdownString, String completionState, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("reportingTask", AspireClient.serializeValue(getHandle()));
        reqArgs.put("markdownString", AspireClient.serializeValue(markdownString));
        if (completionState != null) {
            reqArgs.put("completionState", AspireClient.serializeValue(completionState));
        }
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/completeTaskMarkdown", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource. */
class IResource extends ResourceBuilderBase {
    IResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs. */
class IResourceWithArgs extends ResourceBuilderBase {
    IResourceWithArgs(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString. */
class IResourceWithConnectionString extends ResourceBuilderBase {
    IResourceWithConnectionString(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IResourceWithContainerFiles. */
class IResourceWithContainerFiles extends ResourceBuilderBase {
    IResourceWithContainerFiles(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Sets the source directory for container files */
    public IResourceWithContainerFiles withContainerFilesSource(String sourcePath) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("sourcePath", AspireClient.serializeValue(sourcePath));
        return (IResourceWithContainerFiles) getClient().invokeCapability("Aspire.Hosting/withContainerFilesSource", reqArgs);
    }

    /** Clears all container file sources */
    public IResourceWithContainerFiles clearContainerFilesSources() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithContainerFiles) getClient().invokeCapability("Aspire.Hosting/clearContainerFilesSources", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints. */
class IResourceWithEndpoints extends ResourceBuilderBase {
    IResourceWithEndpoints(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment. */
class IResourceWithEnvironment extends ResourceBuilderBase {
    IResourceWithEnvironment(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithParent. */
class IResourceWithParent extends ResourceBuilderBase {
    IResourceWithParent(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport. */
class IResourceWithWaitSupport extends ResourceBuilderBase {
    IResourceWithWaitSupport(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for System.ComponentModel/System.IServiceProvider. */
class IServiceProvider extends HandleWrapperBase {
    IServiceProvider(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the distributed application eventing service from the service provider */
    public IDistributedApplicationEventing getEventing() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("serviceProvider", AspireClient.serializeValue(getHandle()));
        return (IDistributedApplicationEventing) getClient().invokeCapability("Aspire.Hosting/getEventing", reqArgs);
    }

    /** Gets the logger factory from the service provider */
    public ILoggerFactory getLoggerFactory() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("serviceProvider", AspireClient.serializeValue(getHandle()));
        return (ILoggerFactory) getClient().invokeCapability("Aspire.Hosting/getLoggerFactory", reqArgs);
    }

    /** Gets the resource logger service from the service provider */
    public ResourceLoggerService getResourceLoggerService() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("serviceProvider", AspireClient.serializeValue(getHandle()));
        return (ResourceLoggerService) getClient().invokeCapability("Aspire.Hosting/getResourceLoggerService", reqArgs);
    }

    /** Gets the distributed application model from the service provider */
    public DistributedApplicationModel getDistributedApplicationModel() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("serviceProvider", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationModel) getClient().invokeCapability("Aspire.Hosting/getDistributedApplicationModel", reqArgs);
    }

    /** Gets the resource notification service from the service provider */
    public ResourceNotificationService getResourceNotificationService() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("serviceProvider", AspireClient.serializeValue(getHandle()));
        return (ResourceNotificationService) getClient().invokeCapability("Aspire.Hosting/getResourceNotificationService", reqArgs);
    }

    /** Gets the user secrets manager from the service provider */
    public IUserSecretsManager getUserSecretsManager() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("serviceProvider", AspireClient.serializeValue(getHandle()));
        return (IUserSecretsManager) getClient().invokeCapability("Aspire.Hosting/getUserSecretsManager", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IUserSecretsManager. */
class IUserSecretsManager extends HandleWrapperBase {
    IUserSecretsManager(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the IsAvailable property */
    public boolean isAvailable() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/IUserSecretsManager.isAvailable", reqArgs);
    }

    /** Gets the FilePath property */
    public String filePath() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/IUserSecretsManager.filePath", reqArgs);
    }

    /** Attempts to set a user secret value */
    public boolean trySetSecret(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/IUserSecretsManager.trySetSecret", reqArgs);
    }

    public void saveStateJson(String json) {
        saveStateJson(json, null);
    }

    /** Saves state to user secrets from a JSON string */
    public void saveStateJson(String json, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("userSecretsManager", AspireClient.serializeValue(getHandle()));
        reqArgs.put("json", AspireClient.serializeValue(json));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        getClient().invokeCapability("Aspire.Hosting/saveStateJson", reqArgs);
    }

    /** Gets a secret value if it exists, or sets it to the provided value if it does not */
    public void getOrSetSecret(IResource resourceBuilder, String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("userSecretsManager", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceBuilder", AspireClient.serializeValue(resourceBuilder));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        getClient().invokeCapability("Aspire.Hosting/getOrSetSecret", reqArgs);
    }

    public void getOrSetSecret(ResourceBuilderBase resourceBuilder, String name, String value) {
        getOrSetSecret(new IResource(resourceBuilder.getHandle(), resourceBuilder.getClient()), name, value);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.InitializeResourceEvent. */
class InitializeResourceEvent extends HandleWrapperBase {
    InitializeResourceEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/InitializeResourceEvent.resource", reqArgs);
    }

    /** Gets the Eventing property */
    public IDistributedApplicationEventing eventing() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IDistributedApplicationEventing) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/InitializeResourceEvent.eventing", reqArgs);
    }

    /** Gets the Logger property */
    public ILogger logger() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/InitializeResourceEvent.logger", reqArgs);
    }

    /** Gets the Notifications property */
    public ResourceNotificationService notifications() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ResourceNotificationService) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/InitializeResourceEvent.notifications", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/InitializeResourceEvent.services", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource. */
class ParameterResource extends ResourceBuilderBase {
    ParameterResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    public ParameterResource withDescription(String description) {
        return withDescription(description, null);
    }

    /** Sets a parameter description */
    public ParameterResource withDescription(String description, Boolean enableMarkdown) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("description", AspireClient.serializeValue(description));
        if (enableMarkdown != null) {
            reqArgs.put("enableMarkdown", AspireClient.serializeValue(enableMarkdown));
        }
        return (ParameterResource) getClient().invokeCapability("Aspire.Hosting/withDescription", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineConfigurationContext. */
class PipelineConfigurationContext extends HandleWrapperBase {
    PipelineConfigurationContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineConfigurationContext.services", reqArgs);
    }

    /** Sets the Services property */
    public PipelineConfigurationContext setServices(IServiceProvider value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineConfigurationContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineConfigurationContext.setServices", reqArgs);
    }

    /** Gets the Steps property */
    public PipelineStep[] steps() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (PipelineStep[]) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineConfigurationContext.steps", reqArgs);
    }

    /** Sets the Steps property */
    public PipelineConfigurationContext setSteps(PipelineStep[] value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineConfigurationContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineConfigurationContext.setSteps", reqArgs);
    }

    /** Gets the Model property */
    public DistributedApplicationModel model() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationModel) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineConfigurationContext.model", reqArgs);
    }

    /** Sets the Model property */
    public PipelineConfigurationContext setModel(DistributedApplicationModel value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineConfigurationContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineConfigurationContext.setModel", reqArgs);
    }

    /** Gets pipeline steps with the specified tag */
    public PipelineStep[] getStepsByTag(String tag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("tag", AspireClient.serializeValue(tag));
        return (PipelineStep[]) getClient().invokeCapability("Aspire.Hosting.Pipelines/getStepsByTag", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineContext. */
class PipelineContext extends HandleWrapperBase {
    PipelineContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Model property */
    public DistributedApplicationModel model() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationModel) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.model", reqArgs);
    }

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.executionContext", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.services", reqArgs);
    }

    /** Gets the Logger property */
    public ILogger logger() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.logger", reqArgs);
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.cancellationToken", reqArgs);
    }

    /** Sets the CancellationToken property */
    public PipelineContext setCancellationToken(CancellationToken value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", getClient().registerCancellation(value));
        }
        return (PipelineContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.setCancellationToken", reqArgs);
    }

    /** Gets the Summary property */
    public PipelineSummary summary() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (PipelineSummary) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineContext.summary", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineStep. */
class PipelineStep extends HandleWrapperBase {
    PipelineStep(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.name", reqArgs);
    }

    /** Sets the Name property */
    public PipelineStep setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.setName", reqArgs);
    }

    /** Gets the Description property */
    public String description() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.description", reqArgs);
    }

    /** Sets the Description property */
    public PipelineStep setDescription(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.setDescription", reqArgs);
    }

    /** Gets the DependsOnSteps property */
    private AspireList<String> dependsOnStepsField;
    public AspireList<String> dependsOnSteps() {
        if (dependsOnStepsField == null) {
            dependsOnStepsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.Pipelines/PipelineStep.dependsOnSteps");
        }
        return dependsOnStepsField;
    }

    /** Sets the DependsOnSteps property */
    public PipelineStep setDependsOnSteps(AspireList<String> value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.setDependsOnSteps", reqArgs);
    }

    /** Gets the RequiredBySteps property */
    private AspireList<String> requiredByStepsField;
    public AspireList<String> requiredBySteps() {
        if (requiredByStepsField == null) {
            requiredByStepsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.Pipelines/PipelineStep.requiredBySteps");
        }
        return requiredByStepsField;
    }

    /** Sets the RequiredBySteps property */
    public PipelineStep setRequiredBySteps(AspireList<String> value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.setRequiredBySteps", reqArgs);
    }

    /** Gets the Tags property */
    private AspireList<String> tagsField;
    public AspireList<String> tags() {
        if (tagsField == null) {
            tagsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.Pipelines/PipelineStep.tags");
        }
        return tagsField;
    }

    /** Sets the Tags property */
    public PipelineStep setTags(AspireList<String> value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.setTags", reqArgs);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.resource", reqArgs);
    }

    /** Sets the Resource property */
    public PipelineStep setResource(IResource value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStep.setResource", reqArgs);
    }

    public PipelineStep setResource(ResourceBuilderBase value) {
        return setResource(new IResource(value.getHandle(), value.getClient()));
    }

    /** Adds a dependency on another step by name */
    public void dependsOn(String stepName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        getClient().invokeCapability("Aspire.Hosting.Pipelines/dependsOn", reqArgs);
    }

    /** Specifies that another step requires this step by name */
    public void requiredBy(String stepName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        getClient().invokeCapability("Aspire.Hosting.Pipelines/requiredBy", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineStepContext. */
class PipelineStepContext extends HandleWrapperBase {
    PipelineStepContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the PipelineContext property */
    public PipelineContext pipelineContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (PipelineContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.pipelineContext", reqArgs);
    }

    /** Sets the PipelineContext property */
    public PipelineStepContext setPipelineContext(PipelineContext value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStepContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.setPipelineContext", reqArgs);
    }

    /** Gets the ReportingStep property */
    public IReportingStep reportingStep() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IReportingStep) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.reportingStep", reqArgs);
    }

    /** Sets the ReportingStep property */
    public PipelineStepContext setReportingStep(IReportingStep value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStepContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.setReportingStep", reqArgs);
    }

    /** Gets the Model property */
    public DistributedApplicationModel model() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationModel) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.model", reqArgs);
    }

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.executionContext", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.services", reqArgs);
    }

    /** Gets the Logger property */
    public ILogger logger() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.logger", reqArgs);
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.cancellationToken", reqArgs);
    }

    /** Gets the Summary property */
    public PipelineSummary summary() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (PipelineSummary) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepContext.summary", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineStepFactoryContext. */
class PipelineStepFactoryContext extends HandleWrapperBase {
    PipelineStepFactoryContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the PipelineContext property */
    public PipelineContext pipelineContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (PipelineContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepFactoryContext.pipelineContext", reqArgs);
    }

    /** Sets the PipelineContext property */
    public PipelineStepFactoryContext setPipelineContext(PipelineContext value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStepFactoryContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepFactoryContext.setPipelineContext", reqArgs);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepFactoryContext.resource", reqArgs);
    }

    /** Sets the Resource property */
    public PipelineStepFactoryContext setResource(IResource value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (PipelineStepFactoryContext) getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineStepFactoryContext.setResource", reqArgs);
    }

    public PipelineStepFactoryContext setResource(ResourceBuilderBase value) {
        return setResource(new IResource(value.getHandle(), value.getClient()));
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineSummary. */
class PipelineSummary extends HandleWrapperBase {
    PipelineSummary(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Invokes the Add method */
    public void add(String key, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        reqArgs.put("value", AspireClient.serializeValue(value));
        getClient().invokeCapability("Aspire.Hosting.Pipelines/PipelineSummary.add", reqArgs);
    }

    /** Adds a Markdown-formatted value to the pipeline summary */
    public void addMarkdown(String key, String markdownString) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("summary", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        reqArgs.put("markdownString", AspireClient.serializeValue(markdownString));
        getClient().invokeCapability("Aspire.Hosting/addMarkdown", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource. */
class ProjectResource extends ResourceBuilderBase {
    ProjectResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    /** Sets the number of replicas */
    public ProjectResource withReplicas(double replicas) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("replicas", AspireClient.serializeValue(replicas));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/withReplicas", reqArgs);
    }

    /** Disables forwarded headers for the project */
    public ProjectResource disableForwardedHeaders() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/disableForwardedHeaders", reqArgs);
    }

    public ProjectResource publishAsDockerFile() {
        return publishAsDockerFile(null);
    }

    /** Publishes a project as a Docker file with optional container configuration */
    public ProjectResource publishAsDockerFile(AspireAction1<ContainerResource> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureId = configure == null ? null : getClient().registerCallback(args -> {
            var obj = (ContainerResource) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/publishProjectAsDockerFileWithConfigure", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    public IContainerFilesDestinationResource publishWithContainerFiles(IResourceWithContainerFiles source, String destinationPath) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        reqArgs.put("destinationPath", AspireClient.serializeValue(destinationPath));
        return (IContainerFilesDestinationResource) getClient().invokeCapability("Aspire.Hosting/publishWithContainerFiles", reqArgs);
    }

    public IContainerFilesDestinationResource publishWithContainerFiles(ResourceBuilderBase source, String destinationPath) {
        return publishWithContainerFiles(new IResourceWithContainerFiles(source.getHandle(), source.getClient()), destinationPath);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ProjectResourceOptions. */
class ProjectResourceOptions extends HandleWrapperBase {
    ProjectResourceOptions(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the LaunchProfileName property */
    public String launchProfileName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/ProjectResourceOptions.launchProfileName", reqArgs);
    }

    /** Sets the LaunchProfileName property */
    public ProjectResourceOptions setLaunchProfileName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ProjectResourceOptions) getClient().invokeCapability("Aspire.Hosting/ProjectResourceOptions.setLaunchProfileName", reqArgs);
    }

    /** Gets the ExcludeLaunchProfile property */
    public boolean excludeLaunchProfile() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/ProjectResourceOptions.excludeLaunchProfile", reqArgs);
    }

    /** Sets the ExcludeLaunchProfile property */
    public ProjectResourceOptions setExcludeLaunchProfile(boolean value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ProjectResourceOptions) getClient().invokeCapability("Aspire.Hosting/ProjectResourceOptions.setExcludeLaunchProfile", reqArgs);
    }

    /** Gets the ExcludeKestrelEndpoints property */
    public boolean excludeKestrelEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting/ProjectResourceOptions.excludeKestrelEndpoints", reqArgs);
    }

    /** Sets the ExcludeKestrelEndpoints property */
    public ProjectResourceOptions setExcludeKestrelEndpoints(boolean value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ProjectResourceOptions) getClient().invokeCapability("Aspire.Hosting/ProjectResourceOptions.setExcludeKestrelEndpoints", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.Python/Aspire.Hosting.Python.PythonAppResource. */
class PythonAppResource extends ResourceBuilderBase {
    PythonAppResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Publishes the executable as a Docker container */
    public ExecutableResource publishAsDockerFile() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFile", reqArgs);
    }

    /** Publishes an executable as a Docker file with optional container configuration */
    public ExecutableResource publishAsDockerFileWithConfigure(AspireAction1<ContainerResource> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureId = getClient().registerCallback(args -> {
            var obj = (ContainerResource) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFileWithConfigure", reqArgs);
    }

    /** Sets the executable command */
    public ExecutableResource withExecutableCommand(String command) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withExecutableCommand", reqArgs);
    }

    /** Sets the executable working directory */
    public ExecutableResource withWorkingDirectory(String workingDirectory) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("workingDirectory", AspireClient.serializeValue(workingDirectory));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withWorkingDirectory", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    public IContainerFilesDestinationResource publishWithContainerFiles(IResourceWithContainerFiles source, String destinationPath) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        reqArgs.put("destinationPath", AspireClient.serializeValue(destinationPath));
        return (IContainerFilesDestinationResource) getClient().invokeCapability("Aspire.Hosting/publishWithContainerFiles", reqArgs);
    }

    public IContainerFilesDestinationResource publishWithContainerFiles(ResourceBuilderBase source, String destinationPath) {
        return publishWithContainerFiles(new IResourceWithContainerFiles(source.getHandle(), source.getClient()), destinationPath);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

    public PythonAppResource withVirtualEnvironment(String virtualEnvironmentPath) {
        return withVirtualEnvironment(virtualEnvironmentPath, null);
    }

    /** Configures the virtual environment for a Python application */
    public PythonAppResource withVirtualEnvironment(String virtualEnvironmentPath, Boolean createIfNotExists) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("virtualEnvironmentPath", AspireClient.serializeValue(virtualEnvironmentPath));
        if (createIfNotExists != null) {
            reqArgs.put("createIfNotExists", AspireClient.serializeValue(createIfNotExists));
        }
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withVirtualEnvironment", reqArgs);
    }

    /** Enables debugging support for a Python application */
    public PythonAppResource withDebugging() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withDebugging", reqArgs);
    }

    /** Configures the entrypoint for a Python application */
    public PythonAppResource withEntrypoint(EntrypointType entrypointType, String entrypoint) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("entrypointType", AspireClient.serializeValue(entrypointType));
        reqArgs.put("entrypoint", AspireClient.serializeValue(entrypoint));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withEntrypoint", reqArgs);
    }

    /** Configures pip package installation for a Python application */
    public PythonAppResource withPip(WithPipOptions options) {
        var install = options == null ? null : options.getInstall();
        var installArgs = options == null ? null : options.getInstallArgs();
        return withPipImpl(install, installArgs);
    }

    public PythonAppResource withPip() {
        return withPip(null);
    }

    /** Configures pip package installation for a Python application */
    private PythonAppResource withPipImpl(Boolean install, String[] installArgs) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (install != null) {
            reqArgs.put("install", AspireClient.serializeValue(install));
        }
        if (installArgs != null) {
            reqArgs.put("installArgs", AspireClient.serializeValue(installArgs));
        }
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withPip", reqArgs);
    }

    /** Configures uv package management for a Python application */
    public PythonAppResource withUv(WithUvOptions options) {
        var install = options == null ? null : options.getInstall();
        var args = options == null ? null : options.getArgs();
        return withUvImpl(install, args);
    }

    public PythonAppResource withUv() {
        return withUv(null);
    }

    /** Configures uv package management for a Python application */
    private PythonAppResource withUvImpl(Boolean install, String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (install != null) {
            reqArgs.put("install", AspireClient.serializeValue(install));
        }
        if (args != null) {
            reqArgs.put("args", AspireClient.serializeValue(args));
        }
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withUv", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpressionBuilder. */
class ReferenceExpressionBuilder extends HandleWrapperBase {
    ReferenceExpressionBuilder(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the IsEmpty property */
    public boolean isEmpty() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ReferenceExpressionBuilder.isEmpty", reqArgs);
    }

    /** Appends a literal string to the reference expression */
    public void appendLiteral(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        getClient().invokeCapability("Aspire.Hosting.ApplicationModel/appendLiteral", reqArgs);
    }

    public void appendFormatted(String value) {
        appendFormatted(value, null);
    }

    /** Appends a formatted string value to the reference expression */
    public void appendFormatted(String value, String format) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        if (format != null) {
            reqArgs.put("format", AspireClient.serializeValue(format));
        }
        getClient().invokeCapability("Aspire.Hosting.ApplicationModel/appendFormatted", reqArgs);
    }

    public void appendValueProvider(Object valueProvider) {
        appendValueProvider(valueProvider, null);
    }

    /** Appends a value provider to the reference expression */
    public void appendValueProvider(Object valueProvider, String format) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("valueProvider", AspireClient.serializeValue(valueProvider));
        if (format != null) {
            reqArgs.put("format", AspireClient.serializeValue(format));
        }
        getClient().invokeCapability("Aspire.Hosting.ApplicationModel/appendValueProvider", reqArgs);
    }

    /** Builds the reference expression */
    public ReferenceExpression build() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ReferenceExpression) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/build", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceEndpointsAllocatedEvent. */
class ResourceEndpointsAllocatedEvent extends HandleWrapperBase {
    ResourceEndpointsAllocatedEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceEndpointsAllocatedEvent.resource", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceEndpointsAllocatedEvent.services", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceLoggerService. */
class ResourceLoggerService extends HandleWrapperBase {
    ResourceLoggerService(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Completes the log stream for a resource */
    public void completeLog(IResource resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("loggerService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        getClient().invokeCapability("Aspire.Hosting/completeLog", reqArgs);
    }

    public void completeLog(ResourceBuilderBase resource) {
        completeLog(new IResource(resource.getHandle(), resource.getClient()));
    }

    /** Completes the log stream by resource name */
    public void completeLogByName(String resourceName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("loggerService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        getClient().invokeCapability("Aspire.Hosting/completeLogByName", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceNotificationService. */
class ResourceNotificationService extends HandleWrapperBase {
    ResourceNotificationService(Handle handle, AspireClient client) {
        super(handle, client);
    }

    public void waitForResourceState(String resourceName) {
        waitForResourceState(resourceName, null);
    }

    /** Waits for a resource to reach a specified state */
    public void waitForResourceState(String resourceName, String targetState) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("notificationService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        if (targetState != null) {
            reqArgs.put("targetState", AspireClient.serializeValue(targetState));
        }
        getClient().invokeCapability("Aspire.Hosting/waitForResourceState", reqArgs);
    }

    /** Waits for a resource to reach one of the specified states */
    public String waitForResourceStates(String resourceName, String[] targetStates) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("notificationService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        reqArgs.put("targetStates", AspireClient.serializeValue(targetStates));
        return (String) getClient().invokeCapability("Aspire.Hosting/waitForResourceStates", reqArgs);
    }

    /** Waits for a resource to become healthy */
    public ResourceEventDto waitForResourceHealthy(String resourceName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("notificationService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        return (ResourceEventDto) getClient().invokeCapability("Aspire.Hosting/waitForResourceHealthy", reqArgs);
    }

    /** Waits for all dependencies of a resource to be ready */
    public void waitForDependencies(IResource resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("notificationService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        getClient().invokeCapability("Aspire.Hosting/waitForDependencies", reqArgs);
    }

    public void waitForDependencies(ResourceBuilderBase resource) {
        waitForDependencies(new IResource(resource.getHandle(), resource.getClient()));
    }

    /** Tries to get the current state of a resource */
    public ResourceEventDto tryGetResourceState(String resourceName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("notificationService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        return (ResourceEventDto) getClient().invokeCapability("Aspire.Hosting/tryGetResourceState", reqArgs);
    }

    /** Publishes an update for a resource's state */
    public void publishResourceUpdate(IResource resource, PublishResourceUpdateOptions options) {
        var state = options == null ? null : options.getState();
        var stateStyle = options == null ? null : options.getStateStyle();
        publishResourceUpdateImpl(resource, state, stateStyle);
    }

    public void publishResourceUpdate(ResourceBuilderBase resource, PublishResourceUpdateOptions options) {
        publishResourceUpdate(new IResource(resource.getHandle(), resource.getClient()), options);
    }

    public void publishResourceUpdate(IResource resource) {
        publishResourceUpdate(resource, null);
    }

    public void publishResourceUpdate(ResourceBuilderBase resource) {
        publishResourceUpdate(new IResource(resource.getHandle(), resource.getClient()));
    }

    /** Publishes an update for a resource's state */
    private void publishResourceUpdateImpl(IResource resource, String state, String stateStyle) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("notificationService", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        if (state != null) {
            reqArgs.put("state", AspireClient.serializeValue(state));
        }
        if (stateStyle != null) {
            reqArgs.put("stateStyle", AspireClient.serializeValue(stateStyle));
        }
        getClient().invokeCapability("Aspire.Hosting/publishResourceUpdate", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceReadyEvent. */
class ResourceReadyEvent extends HandleWrapperBase {
    ResourceReadyEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceReadyEvent.resource", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceReadyEvent.services", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceStoppedEvent. */
class ResourceStoppedEvent extends HandleWrapperBase {
    ResourceStoppedEvent(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceStoppedEvent.resource", reqArgs);
    }

    /** Gets the Services property */
    public IServiceProvider services() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceStoppedEvent.services", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext. */
class ResourceUrlsCallbackContext extends HandleWrapperBase {
    ResourceUrlsCallbackContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Resource property */
    public IResource resource() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.resource", reqArgs);
    }

    /** Gets the Urls property */
    private AspireList<ResourceUrlAnnotation> urlsField;
    public AspireList<ResourceUrlAnnotation> urls() {
        if (urlsField == null) {
            urlsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls");
        }
        return urlsField;
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.cancellationToken", reqArgs);
    }

    /** Gets the Logger property */
    public ILogger logger() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ILogger) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.logger", reqArgs);
    }

    /** Sets the Logger property */
    public ResourceUrlsCallbackContext setLogger(ILogger value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (ResourceUrlsCallbackContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.setLogger", reqArgs);
    }

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext. */
class UpdateCommandStateContext extends HandleWrapperBase {
    UpdateCommandStateContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the ServiceProvider property */
    public IServiceProvider serviceProvider() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (IServiceProvider) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/UpdateCommandStateContext.serviceProvider", reqArgs);
    }

    /** Sets the ServiceProvider property */
    public UpdateCommandStateContext setServiceProvider(IServiceProvider value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (UpdateCommandStateContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/UpdateCommandStateContext.setServiceProvider", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.Python/Aspire.Hosting.Python.UvicornAppResource. */
class UvicornAppResource extends ResourceBuilderBase {
    UvicornAppResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Configures a resource to use a container registry */
    public IResource withContainerRegistry(IResource registry) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("registry", AspireClient.serializeValue(registry));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withContainerRegistry", reqArgs);
    }

    public IResource withContainerRegistry(ResourceBuilderBase registry) {
        return withContainerRegistry(new IResource(registry.getHandle(), registry.getClient()));
    }

    /** Sets the base image for a Dockerfile build */
    public IResource withDockerfileBaseImage(WithDockerfileBaseImageOptions options) {
        var buildImage = options == null ? null : options.getBuildImage();
        var runtimeImage = options == null ? null : options.getRuntimeImage();
        return withDockerfileBaseImageImpl(buildImage, runtimeImage);
    }

    public IResource withDockerfileBaseImage() {
        return withDockerfileBaseImage(null);
    }

    /** Sets the base image for a Dockerfile build */
    private IResource withDockerfileBaseImageImpl(String buildImage, String runtimeImage) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (buildImage != null) {
            reqArgs.put("buildImage", AspireClient.serializeValue(buildImage));
        }
        if (runtimeImage != null) {
            reqArgs.put("runtimeImage", AspireClient.serializeValue(runtimeImage));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withDockerfileBaseImage", reqArgs);
    }

    /** Publishes the executable as a Docker container */
    public ExecutableResource publishAsDockerFile() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFile", reqArgs);
    }

    /** Publishes an executable as a Docker file with optional container configuration */
    public ExecutableResource publishAsDockerFileWithConfigure(AspireAction1<ContainerResource> configure) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureId = getClient().registerCallback(args -> {
            var obj = (ContainerResource) args[0];
            configure.invoke(obj);
            return null;
        });
        if (configureId != null) {
            reqArgs.put("configure", configureId);
        }
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/publishAsDockerFileWithConfigure", reqArgs);
    }

    /** Sets the executable command */
    public ExecutableResource withExecutableCommand(String command) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withExecutableCommand", reqArgs);
    }

    /** Sets the executable working directory */
    public ExecutableResource withWorkingDirectory(String workingDirectory) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("workingDirectory", AspireClient.serializeValue(workingDirectory));
        return (ExecutableResource) getClient().invokeCapability("Aspire.Hosting/withWorkingDirectory", reqArgs);
    }

    /** Configures an MCP server endpoint on the resource */
    public IResourceWithEndpoints withMcpServer(WithMcpServerOptions options) {
        var path = options == null ? null : options.getPath();
        var endpointName = options == null ? null : options.getEndpointName();
        return withMcpServerImpl(path, endpointName);
    }

    public IResourceWithEndpoints withMcpServer() {
        return withMcpServer(null);
    }

    /** Configures an MCP server endpoint on the resource */
    private IResourceWithEndpoints withMcpServerImpl(String path, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withMcpServer", reqArgs);
    }

    /** Configures OTLP telemetry export */
    public IResourceWithEnvironment withOtlpExporter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporter", reqArgs);
    }

    /** Configures OTLP telemetry export with specific protocol */
    public IResourceWithEnvironment withOtlpExporterProtocol(OtlpProtocol protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withOtlpExporterProtocol", reqArgs);
    }

    public IResource withRequiredCommand(String command) {
        return withRequiredCommand(command, null);
    }

    /** Adds a required command dependency */
    public IResource withRequiredCommand(String command, String helpLink) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("command", AspireClient.serializeValue(command));
        if (helpLink != null) {
            reqArgs.put("helpLink", AspireClient.serializeValue(helpLink));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withRequiredCommand", reqArgs);
    }

    /** Sets an environment variable */
    public IResourceWithEnvironment withEnvironment(String name, String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironment", reqArgs);
    }

    /** Adds an environment variable with a reference expression */
    public IResourceWithEnvironment withEnvironmentExpression(String name, ReferenceExpression value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs);
    }

    /** Sets environment variables via callback */
    public IResourceWithEnvironment withEnvironmentCallback(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (EnvironmentCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(AspireAction1<EnvironmentCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EnvironmentCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Sets an environment variable from an endpoint reference */
    public IResourceWithEnvironment withEnvironmentEndpoint(String name, EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentEndpoint", reqArgs);
    }

    /** Sets an environment variable from a parameter resource */
    public IResourceWithEnvironment withEnvironmentParameter(String name, ParameterResource parameter) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("parameter", AspireClient.serializeValue(parameter));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentParameter", reqArgs);
    }

    /** Sets an environment variable from a connection string resource */
    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, IResourceWithConnectionString resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("envVarName", AspireClient.serializeValue(envVarName));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentConnectionString", reqArgs);
    }

    public IResourceWithEnvironment withEnvironmentConnectionString(String envVarName, ResourceBuilderBase resource) {
        return withEnvironmentConnectionString(envVarName, new IResourceWithConnectionString(resource.getHandle(), resource.getClient()));
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(AspireAction1<CommandLineArgsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (CommandLineArgsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResource source, WithReferenceOptions options) {
        var connectionName = options == null ? null : options.getConnectionName();
        var optional = options == null ? null : options.getOptional();
        var name = options == null ? null : options.getName();
        return withReferenceImpl(source, connectionName, optional, name);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source, WithReferenceOptions options) {
        return withReference(new IResource(source.getHandle(), source.getClient()), options);
    }

    public IResourceWithEnvironment withReference(IResource source) {
        return withReference(source, null);
    }

    public IResourceWithEnvironment withReference(ResourceBuilderBase source) {
        return withReference(new IResource(source.getHandle(), source.getClient()));
    }

    /** Adds a reference to another resource */
    private IResourceWithEnvironment withReferenceImpl(IResource source, String connectionName, Boolean optional, String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a reference to a URI */
    public IResourceWithEnvironment withReferenceUri(String name, String uri) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("uri", AspireClient.serializeValue(uri));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceUri", reqArgs);
    }

    /** Adds a reference to an external service */
    public IResourceWithEnvironment withReferenceExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceExternalService", reqArgs);
    }

    /** Adds a reference to an endpoint */
    public IResourceWithEnvironment withReferenceEndpoint(EndpointReference endpointReference) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointReference", AspireClient.serializeValue(endpointReference));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReferenceEndpoint", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(WithEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var scheme = options == null ? null : options.getScheme();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        var isExternal = options == null ? null : options.isExternal();
        var protocol = options == null ? null : options.getProtocol();
        return withEndpointImpl(port, targetPort, scheme, name, env, isProxied, isExternal, protocol);
    }

    public IResourceWithEndpoints withEndpoint() {
        return withEndpoint(null);
    }

    /** Adds a network endpoint */
    private IResourceWithEndpoints withEndpointImpl(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (scheme != null) {
            reqArgs.put("scheme", AspireClient.serializeValue(scheme));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        if (isExternal != null) {
            reqArgs.put("isExternal", AspireClient.serializeValue(isExternal));
        }
        if (protocol != null) {
            reqArgs.put("protocol", AspireClient.serializeValue(protocol));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withEndpoint", reqArgs);
    }

    /** Adds an HTTP endpoint */
    public IResourceWithEndpoints withHttpEndpoint(WithHttpEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpEndpoint() {
        return withHttpEndpoint(null);
    }

    /** Adds an HTTP endpoint */
    private IResourceWithEndpoints withHttpEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs);
    }

    /** Adds an HTTPS endpoint */
    public IResourceWithEndpoints withHttpsEndpoint(WithHttpsEndpointOptions options) {
        var port = options == null ? null : options.getPort();
        var targetPort = options == null ? null : options.getTargetPort();
        var name = options == null ? null : options.getName();
        var env = options == null ? null : options.getEnv();
        var isProxied = options == null ? null : options.isProxied();
        return withHttpsEndpointImpl(port, targetPort, name, env, isProxied);
    }

    public IResourceWithEndpoints withHttpsEndpoint() {
        return withHttpsEndpoint(null);
    }

    /** Adds an HTTPS endpoint */
    private IResourceWithEndpoints withHttpsEndpointImpl(Double port, Double targetPort, String name, String env, Boolean isProxied) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (targetPort != null) {
            reqArgs.put("targetPort", AspireClient.serializeValue(targetPort));
        }
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (env != null) {
            reqArgs.put("env", AspireClient.serializeValue(env));
        }
        if (isProxied != null) {
            reqArgs.put("isProxied", AspireClient.serializeValue(isProxied));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs);
    }

    /** Makes HTTP endpoints externally accessible */
    public IResourceWithEndpoints withExternalHttpEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs);
    }

    /** Gets an endpoint reference */
    public EndpointReference getEndpoint(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting/getEndpoint", reqArgs);
    }

    /** Configures resource for HTTP/2 */
    public IResourceWithEndpoints asHttp2Service() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/asHttp2Service", reqArgs);
    }

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(AspireAction1<ResourceUrlsCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceUrlsCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
    }

    public IResource withUrl(String url) {
        return withUrl(url, null);
    }

    /** Adds or modifies displayed URLs */
    public IResource withUrl(String url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrl", reqArgs);
    }

    public IResource withUrlExpression(ReferenceExpression url) {
        return withUrlExpression(url, null);
    }

    /** Adds a URL using a reference expression */
    public IResource withUrlExpression(ReferenceExpression url, String displayText) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("url", AspireClient.serializeValue(url));
        if (displayText != null) {
            reqArgs.put("displayText", AspireClient.serializeValue(displayText));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlExpression", reqArgs);
    }

    /** Customizes the URL for a specific endpoint via callback */
    public IResource withUrlForEndpoint(String endpointName, AspireAction1<ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (ResourceUrlAnnotation) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, AspireFunc1<EndpointReference, ResourceUrlAnnotation> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (EndpointReference) args[0];
            return AspireClient.awaitValue(callback.invoke(arg));
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Configures the resource to copy container files from the specified source during publishing */
    public IContainerFilesDestinationResource publishWithContainerFiles(IResourceWithContainerFiles source, String destinationPath) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        reqArgs.put("destinationPath", AspireClient.serializeValue(destinationPath));
        return (IContainerFilesDestinationResource) getClient().invokeCapability("Aspire.Hosting/publishWithContainerFiles", reqArgs);
    }

    public IContainerFilesDestinationResource publishWithContainerFiles(ResourceBuilderBase source, String destinationPath) {
        return publishWithContainerFiles(new IResourceWithContainerFiles(source.getHandle(), source.getClient()), destinationPath);
    }

    /** Excludes the resource from the deployment manifest */
    public IResource excludeFromManifest() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromManifest", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    public IResourceWithWaitSupport waitFor(ResourceBuilderBase dependency) {
        return waitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource with specific behavior */
    public IResourceWithWaitSupport waitForWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Waits for another resource to start */
    public IResourceWithWaitSupport waitForStart(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForStart(ResourceBuilderBase dependency) {
        return waitForStart(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for another resource to start with specific behavior */
    public IResourceWithWaitSupport waitForStartWithBehavior(IResource dependency, WaitBehavior waitBehavior) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        reqArgs.put("waitBehavior", AspireClient.serializeValue(waitBehavior));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForStartWithBehavior", reqArgs);
    }

    public IResourceWithWaitSupport waitForStartWithBehavior(ResourceBuilderBase dependency, WaitBehavior waitBehavior) {
        return waitForStartWithBehavior(new IResource(dependency.getHandle(), dependency.getClient()), waitBehavior);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(IResource dependency) {
        return waitForCompletion(dependency, null);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Waits for resource completion */
    public IResourceWithWaitSupport waitForCompletion(IResource dependency, Double exitCode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        if (exitCode != null) {
            reqArgs.put("exitCode", AspireClient.serializeValue(exitCode));
        }
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitForCompletion", reqArgs);
    }

    public IResourceWithWaitSupport waitForCompletion(ResourceBuilderBase dependency, Double exitCode) {
        return waitForCompletion(new IResource(dependency.getHandle(), dependency.getClient()), exitCode);
    }

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(WithHttpHealthCheckOptions options) {
        var path = options == null ? null : options.getPath();
        var statusCode = options == null ? null : options.getStatusCode();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpHealthCheckImpl(path, statusCode, endpointName);
    }

    public IResourceWithEndpoints withHttpHealthCheck() {
        return withHttpHealthCheck(null);
    }

    /** Adds an HTTP health check */
    private IResourceWithEndpoints withHttpHealthCheckImpl(String path, Double statusCode, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (statusCode != null) {
            reqArgs.put("statusCode", AspireClient.serializeValue(statusCode));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs);
    }

    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand) {
        return withCommand(name, displayName, executeCommand, null);
    }

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, AspireFunc1<ExecuteCommandContext, ExecuteCommandResult> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        var executeCommandId = getClient().registerCallback(args -> {
            var arg = (ExecuteCommandContext) args[0];
            return AspireClient.awaitValue(executeCommand.invoke(arg));
        });
        if (executeCommandId != null) {
            reqArgs.put("executeCommand", executeCommandId);
        }
        if (commandOptions != null) {
            reqArgs.put("commandOptions", AspireClient.serializeValue(commandOptions));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withCommand", reqArgs);
    }

    /** Configures developer certificate trust */
    public IResourceWithEnvironment withDeveloperCertificateTrust(boolean trust) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("trust", AspireClient.serializeValue(trust));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withDeveloperCertificateTrust", reqArgs);
    }

    /** Sets the certificate trust scope */
    public IResourceWithEnvironment withCertificateTrustScope(CertificateTrustScope scope) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("scope", AspireClient.serializeValue(scope));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withCertificateTrustScope", reqArgs);
    }

    public IResourceWithEnvironment withHttpsDeveloperCertificate() {
        return withHttpsDeveloperCertificate(null);
    }

    /** Configures HTTPS with a developer certificate */
    public IResourceWithEnvironment withHttpsDeveloperCertificate(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withHttpsDeveloperCertificate", reqArgs);
    }

    /** Removes HTTPS certificate configuration */
    public IResourceWithEnvironment withoutHttpsCertificate() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withoutHttpsCertificate", reqArgs);
    }

    /** Sets the parent relationship */
    public IResource withParentRelationship(IResource parent) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("parent", AspireClient.serializeValue(parent));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withParentRelationship", reqArgs);
    }

    public IResource withParentRelationship(ResourceBuilderBase parent) {
        return withParentRelationship(new IResource(parent.getHandle(), parent.getClient()));
    }

    /** Sets a child relationship */
    public IResource withChildRelationship(IResource child) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("child", AspireClient.serializeValue(child));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withChildRelationship", reqArgs);
    }

    public IResource withChildRelationship(ResourceBuilderBase child) {
        return withChildRelationship(new IResource(child.getHandle(), child.getClient()));
    }

    public IResource withIconName(String iconName) {
        return withIconName(iconName, null);
    }

    /** Sets the icon for the resource */
    public IResource withIconName(String iconName, IconVariant iconVariant) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("iconName", AspireClient.serializeValue(iconName));
        if (iconVariant != null) {
            reqArgs.put("iconVariant", AspireClient.serializeValue(iconVariant));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withIconName", reqArgs);
    }

    /** Adds an HTTP health probe to the resource */
    public IResourceWithEndpoints withHttpProbe(ProbeType probeType, WithHttpProbeOptions options) {
        var path = options == null ? null : options.getPath();
        var initialDelaySeconds = options == null ? null : options.getInitialDelaySeconds();
        var periodSeconds = options == null ? null : options.getPeriodSeconds();
        var timeoutSeconds = options == null ? null : options.getTimeoutSeconds();
        var failureThreshold = options == null ? null : options.getFailureThreshold();
        var successThreshold = options == null ? null : options.getSuccessThreshold();
        var endpointName = options == null ? null : options.getEndpointName();
        return withHttpProbeImpl(probeType, path, initialDelaySeconds, periodSeconds, timeoutSeconds, failureThreshold, successThreshold, endpointName);
    }

    public IResourceWithEndpoints withHttpProbe(ProbeType probeType) {
        return withHttpProbe(probeType, null);
    }

    /** Adds an HTTP health probe to the resource */
    private IResourceWithEndpoints withHttpProbeImpl(ProbeType probeType, String path, Double initialDelaySeconds, Double periodSeconds, Double timeoutSeconds, Double failureThreshold, Double successThreshold, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("probeType", AspireClient.serializeValue(probeType));
        if (path != null) {
            reqArgs.put("path", AspireClient.serializeValue(path));
        }
        if (initialDelaySeconds != null) {
            reqArgs.put("initialDelaySeconds", AspireClient.serializeValue(initialDelaySeconds));
        }
        if (periodSeconds != null) {
            reqArgs.put("periodSeconds", AspireClient.serializeValue(periodSeconds));
        }
        if (timeoutSeconds != null) {
            reqArgs.put("timeoutSeconds", AspireClient.serializeValue(timeoutSeconds));
        }
        if (failureThreshold != null) {
            reqArgs.put("failureThreshold", AspireClient.serializeValue(failureThreshold));
        }
        if (successThreshold != null) {
            reqArgs.put("successThreshold", AspireClient.serializeValue(successThreshold));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withHttpProbe", reqArgs);
    }

    /** Excludes the resource from MCP server exposure */
    public IResource excludeFromMcp() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/excludeFromMcp", reqArgs);
    }

    /** Sets the remote image name for publishing */
    public IComputeResource withRemoteImageName(String remoteImageName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageName", AspireClient.serializeValue(remoteImageName));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageName", reqArgs);
    }

    /** Sets the remote image tag for publishing */
    public IComputeResource withRemoteImageTag(String remoteImageTag) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("remoteImageTag", AspireClient.serializeValue(remoteImageTag));
        return (IComputeResource) getClient().invokeCapability("Aspire.Hosting/withRemoteImageTag", reqArgs);
    }

    /** Adds a pipeline step to the resource */
    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback, WithPipelineStepFactoryOptions options) {
        var dependsOn = options == null ? null : options.getDependsOn();
        var requiredBy = options == null ? null : options.getRequiredBy();
        var tags = options == null ? null : options.getTags();
        var description = options == null ? null : options.getDescription();
        return withPipelineStepFactoryImpl(stepName, callback, dependsOn, requiredBy, tags, description);
    }

    public IResource withPipelineStepFactory(String stepName, AspireAction1<PipelineStepContext> callback) {
        return withPipelineStepFactory(stepName, callback, null);
    }

    /** Adds a pipeline step to the resource */
    private IResource withPipelineStepFactoryImpl(String stepName, AspireAction1<PipelineStepContext> callback, String[] dependsOn, String[] requiredBy, String[] tags, String description) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("stepName", AspireClient.serializeValue(stepName));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineStepContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        if (dependsOn != null) {
            reqArgs.put("dependsOn", AspireClient.serializeValue(dependsOn));
        }
        if (requiredBy != null) {
            reqArgs.put("requiredBy", AspireClient.serializeValue(requiredBy));
        }
        if (tags != null) {
            reqArgs.put("tags", AspireClient.serializeValue(tags));
        }
        if (description != null) {
            reqArgs.put("description", AspireClient.serializeValue(description));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineStepFactory", reqArgs);
    }

    /** Configures pipeline step dependencies via an async callback */
    public IResource withPipelineConfigurationAsync(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (PipelineConfigurationContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfigurationAsync", reqArgs);
    }

    /** Configures pipeline step dependencies via a callback */
    public IResource withPipelineConfiguration(AspireAction1<PipelineConfigurationContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var obj = (PipelineConfigurationContext) args[0];
            callback.invoke(obj);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withPipelineConfiguration", reqArgs);
    }

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Subscribes to the BeforeResourceStarted event */
    public IResource onBeforeResourceStarted(AspireAction1<BeforeResourceStartedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (BeforeResourceStartedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onBeforeResourceStarted", reqArgs);
    }

    /** Subscribes to the ResourceStopped event */
    public IResource onResourceStopped(AspireAction1<ResourceStoppedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceStoppedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceStopped", reqArgs);
    }

    /** Subscribes to the InitializeResource event */
    public IResource onInitializeResource(AspireAction1<InitializeResourceEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (InitializeResourceEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onInitializeResource", reqArgs);
    }

    /** Subscribes to the ResourceEndpointsAllocated event */
    public IResourceWithEndpoints onResourceEndpointsAllocated(AspireAction1<ResourceEndpointsAllocatedEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceEndpointsAllocatedEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/onResourceEndpointsAllocated", reqArgs);
    }

    /** Subscribes to the ResourceReady event */
    public IResource onResourceReady(AspireAction1<ResourceReadyEvent> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (ResourceReadyEvent) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/onResourceReady", reqArgs);
    }

    public PythonAppResource withVirtualEnvironment(String virtualEnvironmentPath) {
        return withVirtualEnvironment(virtualEnvironmentPath, null);
    }

    /** Configures the virtual environment for a Python application */
    public PythonAppResource withVirtualEnvironment(String virtualEnvironmentPath, Boolean createIfNotExists) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("virtualEnvironmentPath", AspireClient.serializeValue(virtualEnvironmentPath));
        if (createIfNotExists != null) {
            reqArgs.put("createIfNotExists", AspireClient.serializeValue(createIfNotExists));
        }
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withVirtualEnvironment", reqArgs);
    }

    /** Enables debugging support for a Python application */
    public PythonAppResource withDebugging() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withDebugging", reqArgs);
    }

    /** Configures the entrypoint for a Python application */
    public PythonAppResource withEntrypoint(EntrypointType entrypointType, String entrypoint) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("entrypointType", AspireClient.serializeValue(entrypointType));
        reqArgs.put("entrypoint", AspireClient.serializeValue(entrypoint));
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withEntrypoint", reqArgs);
    }

    /** Configures pip package installation for a Python application */
    public PythonAppResource withPip(WithPipOptions options) {
        var install = options == null ? null : options.getInstall();
        var installArgs = options == null ? null : options.getInstallArgs();
        return withPipImpl(install, installArgs);
    }

    public PythonAppResource withPip() {
        return withPip(null);
    }

    /** Configures pip package installation for a Python application */
    private PythonAppResource withPipImpl(Boolean install, String[] installArgs) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (install != null) {
            reqArgs.put("install", AspireClient.serializeValue(install));
        }
        if (installArgs != null) {
            reqArgs.put("installArgs", AspireClient.serializeValue(installArgs));
        }
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withPip", reqArgs);
    }

    /** Configures uv package management for a Python application */
    public PythonAppResource withUv(WithUvOptions options) {
        var install = options == null ? null : options.getInstall();
        var args = options == null ? null : options.getArgs();
        return withUvImpl(install, args);
    }

    public PythonAppResource withUv() {
        return withUv(null);
    }

    /** Configures uv package management for a Python application */
    private PythonAppResource withUvImpl(Boolean install, String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (install != null) {
            reqArgs.put("install", AspireClient.serializeValue(install));
        }
        if (args != null) {
            reqArgs.put("args", AspireClient.serializeValue(args));
        }
        return (PythonAppResource) getClient().invokeCapability("Aspire.Hosting.Python/withUv", reqArgs);
    }

}

// ============================================================================
// Handle wrapper registrations
// ============================================================================

/** Static initializer to register handle wrappers. */
class AspireRegistrations {
    static {
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", (h, c) -> new IDistributedApplicationBuilder(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplication", (h, c) -> new DistributedApplication(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference", (h, c) -> new EndpointReference(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource", (h, c) -> new IResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment", (h, c) -> new IResourceWithEnvironment(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints", (h, c) -> new IResourceWithEndpoints(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs", (h, c) -> new IResourceWithArgs(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString", (h, c) -> new IResourceWithConnectionString(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport", (h, c) -> new IResourceWithWaitSupport(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithParent", (h, c) -> new IResourceWithParent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource", (h, c) -> new ContainerResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource", (h, c) -> new ExecutableResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource", (h, c) -> new ProjectResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource", (h, c) -> new ParameterResource(h, c));
        AspireClient.registerHandleWrapper("System.ComponentModel/System.IServiceProvider", (h, c) -> new IServiceProvider(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceNotificationService", (h, c) -> new ResourceNotificationService(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceLoggerService", (h, c) -> new ResourceLoggerService(h, c));
        AspireClient.registerHandleWrapper("Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfiguration", (h, c) -> new IConfiguration(h, c));
        AspireClient.registerHandleWrapper("Microsoft.Extensions.Configuration.Abstractions/Microsoft.Extensions.Configuration.IConfigurationSection", (h, c) -> new IConfigurationSection(h, c));
        AspireClient.registerHandleWrapper("Microsoft.Extensions.Hosting.Abstractions/Microsoft.Extensions.Hosting.IHostEnvironment", (h, c) -> new IHostEnvironment(h, c));
        AspireClient.registerHandleWrapper("Microsoft.Extensions.Logging.Abstractions/Microsoft.Extensions.Logging.ILogger", (h, c) -> new ILogger(h, c));
        AspireClient.registerHandleWrapper("Microsoft.Extensions.Logging.Abstractions/Microsoft.Extensions.Logging.ILoggerFactory", (h, c) -> new ILoggerFactory(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.IReportingStep", (h, c) -> new IReportingStep(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.IReportingTask", (h, c) -> new IReportingTask(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription", (h, c) -> new DistributedApplicationEventSubscription(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext", (h, c) -> new DistributedApplicationExecutionContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions", (h, c) -> new DistributedApplicationExecutionContextOptions(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ProjectResourceOptions", (h, c) -> new ProjectResourceOptions(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IUserSecretsManager", (h, c) -> new IUserSecretsManager(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineConfigurationContext", (h, c) -> new PipelineConfigurationContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineContext", (h, c) -> new PipelineContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineStep", (h, c) -> new PipelineStep(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineStepContext", (h, c) -> new PipelineStepContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineStepFactoryContext", (h, c) -> new PipelineStepFactoryContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Pipelines.PipelineSummary", (h, c) -> new PipelineSummary(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription", (h, c) -> new DistributedApplicationResourceEventSubscription(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent", (h, c) -> new IDistributedApplicationEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent", (h, c) -> new IDistributedApplicationResourceEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing", (h, c) -> new IDistributedApplicationEventing(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.AfterResourcesCreatedEvent", (h, c) -> new AfterResourcesCreatedEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.BeforeResourceStartedEvent", (h, c) -> new BeforeResourceStartedEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.BeforeStartEvent", (h, c) -> new BeforeStartEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext", (h, c) -> new CommandLineArgsCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ConnectionStringAvailableEvent", (h, c) -> new ConnectionStringAvailableEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.DistributedApplicationModel", (h, c) -> new DistributedApplicationModel(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression", (h, c) -> new EndpointReferenceExpression(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext", (h, c) -> new EnvironmentCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.InitializeResourceEvent", (h, c) -> new InitializeResourceEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpressionBuilder", (h, c) -> new ReferenceExpressionBuilder(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext", (h, c) -> new UpdateCommandStateContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext", (h, c) -> new ExecuteCommandContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceEndpointsAllocatedEvent", (h, c) -> new ResourceEndpointsAllocatedEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceReadyEvent", (h, c) -> new ResourceReadyEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceStoppedEvent", (h, c) -> new ResourceStoppedEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext", (h, c) -> new ResourceUrlsCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ConnectionStringResource", (h, c) -> new ConnectionStringResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerRegistryResource", (h, c) -> new ContainerRegistryResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.DotnetToolResource", (h, c) -> new DotnetToolResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ExternalServiceResource", (h, c) -> new ExternalServiceResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.CSharpAppResource", (h, c) -> new CSharpAppResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IResourceWithContainerFiles", (h, c) -> new IResourceWithContainerFiles(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Python/Aspire.Hosting.Python.PythonAppResource", (h, c) -> new PythonAppResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Python/Aspire.Hosting.Python.UvicornAppResource", (h, c) -> new UvicornAppResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IContainerFilesDestinationResource", (h, c) -> new IContainerFilesDestinationResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IComputeResource", (h, c) -> new IComputeResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<string>", (h, c) -> new AspireList(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Dict<string,any>", (h, c) -> new AspireDict(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<any>", (h, c) -> new AspireList(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Dict<string,string|Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression>", (h, c) -> new AspireDict(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlAnnotation>", (h, c) -> new AspireList(h, c));
    }

    static void ensureRegistered() {
        // Called to trigger static initializer
    }
}

// ============================================================================
// Connection Helpers
// ============================================================================

/** Main entry point for Aspire SDK. */
public class Aspire {
    /** Connect to the AppHost server. */
    public static AspireClient connect() throws Exception {
        BaseRegistrations.ensureRegistered();
        AspireRegistrations.ensureRegistered();
        String socketPath = System.getenv("REMOTE_APP_HOST_SOCKET_PATH");
        if (socketPath == null || socketPath.isEmpty()) {
            throw new RuntimeException("REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Run this application using `aspire run`.");
        }
        AspireClient client = new AspireClient(socketPath);
        client.connect();
        client.onDisconnect(() -> System.exit(1));
        return client;
    }

    /** Create a new distributed application builder. */
    public static IDistributedApplicationBuilder createBuilder(CreateBuilderOptions options) throws Exception {
        AspireClient client = connect();
        Map<String, Object> resolvedOptions = new HashMap<>();
        if (options != null) {
            resolvedOptions.putAll(options.toMap());
        }
        if (!resolvedOptions.containsKey("Args")) {
            // Note: Java doesn't have easy access to command line args from here
            resolvedOptions.put("Args", new String[0]);
        }
        if (!resolvedOptions.containsKey("ProjectDirectory")) {
            resolvedOptions.put("ProjectDirectory", System.getProperty("user.dir"));
        }
        Map<String, Object> args = new HashMap<>();
        args.put("options", resolvedOptions);
        return (IDistributedApplicationBuilder) client.invokeCapability("Aspire.Hosting/createBuilderWithOptions", args);
    }
}

