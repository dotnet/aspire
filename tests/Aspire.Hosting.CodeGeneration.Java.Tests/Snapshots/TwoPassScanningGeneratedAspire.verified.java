// Aspire.java - Capability-based Aspire SDK
// GENERATED CODE - DO NOT EDIT

package aspire;

import java.util.*;
import java.util.function.*;

// ============================================================================
// Enums
// ============================================================================

/** ContainerLifetime enum. */
enum ContainerLifetime {
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
enum ImagePullPolicy {
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
enum DistributedApplicationOperation {
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

/** ProtocolType enum. */
enum ProtocolType {
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

/** EndpointProperty enum. */
enum EndpointProperty {
    URL("Url"),
    HOST("Host"),
    IPV4_HOST("IPV4Host"),
    PORT("Port"),
    SCHEME("Scheme"),
    TARGET_PORT("TargetPort"),
    HOST_AND_PORT("HostAndPort");

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

/** IconVariant enum. */
enum IconVariant {
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

/** UrlDisplayLocation enum. */
enum UrlDisplayLocation {
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

/** TestPersistenceMode enum. */
enum TestPersistenceMode {
    NONE("None"),
    VOLUME("Volume"),
    BIND("Bind");

    private final String value;

    TestPersistenceMode(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static TestPersistenceMode fromValue(String value) {
        for (TestPersistenceMode e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** TestResourceStatus enum. */
enum TestResourceStatus {
    PENDING("Pending"),
    RUNNING("Running"),
    STOPPED("Stopped"),
    FAILED("Failed");

    private final String value;

    TestResourceStatus(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static TestResourceStatus fromValue(String value) {
        for (TestResourceStatus e : values()) {
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

/** TestConfigDto DTO. */
class TestConfigDto {
    private String name;
    private double port;
    private boolean enabled;
    private String optionalField;

    public String getName() { return name; }
    public void setName(String value) { this.name = value; }
    public double getPort() { return port; }
    public void setPort(double value) { this.port = value; }
    public boolean getEnabled() { return enabled; }
    public void setEnabled(boolean value) { this.enabled = value; }
    public String getOptionalField() { return optionalField; }
    public void setOptionalField(String value) { this.optionalField = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Name", AspireClient.serializeValue(name));
        map.put("Port", AspireClient.serializeValue(port));
        map.put("Enabled", AspireClient.serializeValue(enabled));
        map.put("OptionalField", AspireClient.serializeValue(optionalField));
        return map;
    }
}

/** TestNestedDto DTO. */
class TestNestedDto {
    private String id;
    private TestConfigDto config;
    private AspireList<String> tags;
    private AspireDict<String, double> counts;

    public String getId() { return id; }
    public void setId(String value) { this.id = value; }
    public TestConfigDto getConfig() { return config; }
    public void setConfig(TestConfigDto value) { this.config = value; }
    public AspireList<String> getTags() { return tags; }
    public void setTags(AspireList<String> value) { this.tags = value; }
    public AspireDict<String, double> getCounts() { return counts; }
    public void setCounts(AspireDict<String, double> value) { this.counts = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Id", AspireClient.serializeValue(id));
        map.put("Config", AspireClient.serializeValue(config));
        map.put("Tags", AspireClient.serializeValue(tags));
        map.put("Counts", AspireClient.serializeValue(counts));
        return map;
    }
}

/** TestDeeplyNestedDto DTO. */
class TestDeeplyNestedDto {
    private AspireDict<String, AspireList<TestConfigDto>> nestedData;
    private AspireDict<String, String>[] metadataArray;

    public AspireDict<String, AspireList<TestConfigDto>> getNestedData() { return nestedData; }
    public void setNestedData(AspireDict<String, AspireList<TestConfigDto>> value) { this.nestedData = value; }
    public AspireDict<String, String>[] getMetadataArray() { return metadataArray; }
    public void setMetadataArray(AspireDict<String, String>[] value) { this.metadataArray = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("NestedData", AspireClient.serializeValue(nestedData));
        map.put("MetadataArray", AspireClient.serializeValue(metadataArray));
        return map;
    }
}

// ============================================================================
// Handle Wrappers
// ============================================================================

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

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource. */
class ContainerResource extends ResourceBuilderBase {
    ContainerResource(Handle handle, AspireClient client) {
        super(handle, client);
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
    public IResourceWithEnvironment withEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResourceWithConnectionString source, String connectionName, Boolean optional) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a service discovery reference to another resource */
    public IResourceWithEnvironment withServiceReference(IResourceWithServiceDiscovery source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withServiceReference", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
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
    public IResourceWithEndpoints withHttpEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResourceWithEndpoints withHttpsEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
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

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(String path, Double statusCode, String endpointName) {
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplication. */
class DistributedApplication extends HandleWrapperBase {
    DistributedApplication(Handle handle, AspireClient client) {
        super(handle, client);
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

/** Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription. */
class DistributedApplicationResourceEventSubscription extends HandleWrapperBase {
    DistributedApplicationResourceEventSubscription(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference. */
class EndpointReference extends HandleWrapperBase {
    EndpointReference(Handle handle, AspireClient client) {
        super(handle, client);
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

    /** Gets the URL of the endpoint asynchronously */
    public String getValueAsync(CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/getValueAsync", reqArgs);
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
    private AspireDict<String, Object> environmentVariablesField;
    public AspireDict<String, Object> environmentVariables() {
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
    public IResourceWithEnvironment withEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResourceWithConnectionString source, String connectionName, Boolean optional) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a service discovery reference to another resource */
    public IResourceWithEnvironment withServiceReference(IResourceWithServiceDiscovery source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withServiceReference", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
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
    public IResourceWithEndpoints withHttpEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResourceWithEndpoints withHttpsEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
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

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(String path, Double statusCode, String endpointName) {
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext. */
class ExecuteCommandContext extends HandleWrapperBase {
    ExecuteCommandContext(Handle handle, AspireClient client) {
        super(handle, client);
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

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder. */
class IDistributedApplicationBuilder extends HandleWrapperBase {
    IDistributedApplicationBuilder(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Adds a container resource */
    public ContainerResource addContainer(String name, String image) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("image", AspireClient.serializeValue(image));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/addContainer", reqArgs);
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

    /** Gets the AppHostDirectory property */
    public String appHostDirectory() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory", reqArgs);
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

    /** Builds the distributed application */
    public DistributedApplication build() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplication) getClient().invokeCapability("Aspire.Hosting/build", reqArgs);
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

    /** Adds a test Redis resource */
    public TestRedisResource addTestRedis(String name, Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/addTestRedis", reqArgs);
    }

    /** Adds a test vault resource */
    public TestVaultResource addTestVault(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (TestVaultResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/addTestVault", reqArgs);
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

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource. */
class IResource extends ResourceBuilderBase {
    IResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs. */
class IResourceWithArgs extends HandleWrapperBase {
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

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints. */
class IResourceWithEndpoints extends HandleWrapperBase {
    IResourceWithEndpoints(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment. */
class IResourceWithEnvironment extends HandleWrapperBase {
    IResourceWithEnvironment(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery. */
class IResourceWithServiceDiscovery extends ResourceBuilderBase {
    IResourceWithServiceDiscovery(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport. */
class IResourceWithWaitSupport extends HandleWrapperBase {
    IResourceWithWaitSupport(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource. */
class ITestVaultResource extends ResourceBuilderBase {
    ITestVaultResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource. */
class ParameterResource extends ResourceBuilderBase {
    ParameterResource(Handle handle, AspireClient client) {
        super(handle, client);
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

    /** Customizes displayed URLs via callback */
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource. */
class ProjectResource extends ResourceBuilderBase {
    ProjectResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Sets the number of replicas */
    public ProjectResource withReplicas(double replicas) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("replicas", AspireClient.serializeValue(replicas));
        return (ProjectResource) getClient().invokeCapability("Aspire.Hosting/withReplicas", reqArgs);
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
    public IResourceWithEnvironment withEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResourceWithConnectionString source, String connectionName, Boolean optional) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a service discovery reference to another resource */
    public IResourceWithEnvironment withServiceReference(IResourceWithServiceDiscovery source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withServiceReference", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
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
    public IResourceWithEndpoints withHttpEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResourceWithEndpoints withHttpsEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
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

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(String path, Double statusCode, String endpointName) {
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Gets the resource name */
    public String getResourceName() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("resource", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting/getResourceName", reqArgs);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext. */
class ResourceUrlsCallbackContext extends HandleWrapperBase {
    ResourceUrlsCallbackContext(Handle handle, AspireClient client) {
        super(handle, client);
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

    /** Gets the ExecutionContext property */
    public DistributedApplicationExecutionContext executionContext() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (DistributedApplicationExecutionContext) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext. */
class TestCallbackContext extends HandleWrapperBase {
    TestCallbackContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name", reqArgs);
    }

    /** Sets the Name property */
    public TestCallbackContext setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestCallbackContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName", reqArgs);
    }

    /** Gets the Value property */
    public double value() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value", reqArgs);
    }

    /** Sets the Value property */
    public TestCallbackContext setValue(double value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestCallbackContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue", reqArgs);
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken", reqArgs);
    }

    /** Sets the CancellationToken property */
    public TestCallbackContext setCancellationToken(CancellationToken value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", getClient().registerCancellation(value));
        }
        return (TestCallbackContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext. */
class TestCollectionContext extends HandleWrapperBase {
    TestCollectionContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Items property */
    private AspireList<String> itemsField;
    public AspireList<String> items() {
        if (itemsField == null) {
            itemsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items");
        }
        return itemsField;
    }

    /** Gets the Metadata property */
    private AspireDict<String, String> metadataField;
    public AspireDict<String, String> metadata() {
        if (metadataField == null) {
            metadataField = new AspireDict<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata");
        }
        return metadataField;
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource. */
class TestDatabaseResource extends ResourceBuilderBase {
    TestDatabaseResource(Handle handle, AspireClient client) {
        super(handle, client);
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

    /** Sets the container name */
    public ContainerResource withContainerName(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withContainerName", reqArgs);
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
    public IResourceWithEnvironment withEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResourceWithConnectionString source, String connectionName, Boolean optional) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a service discovery reference to another resource */
    public IResourceWithEnvironment withServiceReference(IResourceWithServiceDiscovery source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withServiceReference", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
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
    public IResourceWithEndpoints withHttpEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResourceWithEndpoints withHttpsEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
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

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(String path, Double statusCode, String endpointName) {
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Adds a volume */
    public ContainerResource withVolume(String target, String name, Boolean isReadOnly) {
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

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext. */
class TestEnvironmentContext extends HandleWrapperBase {
    TestEnvironmentContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name", reqArgs);
    }

    /** Sets the Name property */
    public TestEnvironmentContext setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestEnvironmentContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName", reqArgs);
    }

    /** Gets the Description property */
    public String description() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description", reqArgs);
    }

    /** Sets the Description property */
    public TestEnvironmentContext setDescription(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestEnvironmentContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription", reqArgs);
    }

    /** Gets the Priority property */
    public double priority() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority", reqArgs);
    }

    /** Sets the Priority property */
    public TestEnvironmentContext setPriority(double value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestEnvironmentContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource. */
class TestRedisResource extends ResourceBuilderBase {
    TestRedisResource(Handle handle, AspireClient client) {
        super(handle, client);
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

    /** Sets the container name */
    public ContainerResource withContainerName(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withContainerName", reqArgs);
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
    public IResourceWithEnvironment withEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResourceWithConnectionString source, String connectionName, Boolean optional) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a service discovery reference to another resource */
    public IResourceWithEnvironment withServiceReference(IResourceWithServiceDiscovery source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withServiceReference", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
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
    public IResourceWithEndpoints withHttpEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResourceWithEndpoints withHttpsEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
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

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(String path, Double statusCode, String endpointName) {
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Adds a volume */
    public ContainerResource withVolume(String target, String name, Boolean isReadOnly) {
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

    /** Adds a child database to a test Redis resource */
    public TestDatabaseResource addTestChildDatabase(String name, String databaseName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (databaseName != null) {
            reqArgs.put("databaseName", AspireClient.serializeValue(databaseName));
        }
        return (TestDatabaseResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/addTestChildDatabase", reqArgs);
    }

    /** Configures the Redis resource with persistence */
    public TestRedisResource withPersistence(TestPersistenceMode mode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (mode != null) {
            reqArgs.put("mode", AspireClient.serializeValue(mode));
        }
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withPersistence", reqArgs);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Gets the tags for the resource */
    private AspireList<String> getTagsField;
    public AspireList<String> getTags() {
        if (getTagsField == null) {
            getTagsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.Java.Tests/getTags");
        }
        return getTagsField;
    }

    /** Gets the metadata for the resource */
    private AspireDict<String, String> getMetadataField;
    public AspireDict<String, String> getMetadata() {
        if (getMetadataField == null) {
            getMetadataField = new AspireDict<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.Java.Tests/getMetadata");
        }
        return getMetadataField;
    }

    /** Sets the connection string using a reference expression */
    public IResourceWithConnectionString withConnectionString(ReferenceExpression connectionString) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("connectionString", AspireClient.serializeValue(connectionString));
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConnectionString", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Gets the endpoints */
    public String[] getEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (String[]) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/getEndpoints", reqArgs);
    }

    /** Sets connection string using direct interface target */
    public IResourceWithConnectionString withConnectionStringDirect(String connectionString) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("connectionString", AspireClient.serializeValue(connectionString));
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConnectionStringDirect", reqArgs);
    }

    /** Redis-specific configuration */
    public TestRedisResource withRedisSpecific(String option) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("option", AspireClient.serializeValue(option));
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withRedisSpecific", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Gets the status of the resource asynchronously */
    public String getStatusAsync(CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/getStatusAsync", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

    /** Waits for the resource to be ready */
    public boolean waitForReadyAsync(double timeout, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("timeout", AspireClient.serializeValue(timeout));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (boolean) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/waitForReadyAsync", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext. */
class TestResourceContext extends HandleWrapperBase {
    TestResourceContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name", reqArgs);
    }

    /** Sets the Name property */
    public TestResourceContext setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestResourceContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName", reqArgs);
    }

    /** Gets the Value property */
    public double value() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value", reqArgs);
    }

    /** Sets the Value property */
    public TestResourceContext setValue(double value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestResourceContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue", reqArgs);
    }

    /** Invokes the GetValueAsync method */
    public String getValueAsync() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync", reqArgs);
    }

    /** Invokes the SetValueAsync method */
    public void setValueAsync(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync", reqArgs);
    }

    /** Invokes the ValidateAsync method */
    public boolean validateAsync() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource. */
class TestVaultResource extends ResourceBuilderBase {
    TestVaultResource(Handle handle, AspireClient client) {
        super(handle, client);
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

    /** Sets the container name */
    public ContainerResource withContainerName(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (ContainerResource) getClient().invokeCapability("Aspire.Hosting/withContainerName", reqArgs);
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
    public IResourceWithEnvironment withEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs);
    }

    /** Sets environment variables via async callback */
    public IResourceWithEnvironment withEnvironmentCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs);
    }

    /** Adds arguments */
    public IResourceWithArgs withArgs(String[] args) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("args", AspireClient.serializeValue(args));
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgs", reqArgs);
    }

    /** Sets command-line arguments via callback */
    public IResourceWithArgs withArgsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallback", reqArgs);
    }

    /** Sets command-line arguments via async callback */
    public IResourceWithArgs withArgsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithArgs) getClient().invokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs);
    }

    /** Adds a reference to another resource */
    public IResourceWithEnvironment withReference(IResourceWithConnectionString source, String connectionName, Boolean optional) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (connectionName != null) {
            reqArgs.put("connectionName", AspireClient.serializeValue(connectionName));
        }
        if (optional != null) {
            reqArgs.put("optional", AspireClient.serializeValue(optional));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withReference", reqArgs);
    }

    /** Adds a service discovery reference to another resource */
    public IResourceWithEnvironment withServiceReference(IResourceWithServiceDiscovery source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting/withServiceReference", reqArgs);
    }

    /** Adds a network endpoint */
    public IResourceWithEndpoints withEndpoint(Double port, Double targetPort, String scheme, String name, String env, Boolean isProxied, Boolean isExternal, ProtocolType protocol) {
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
    public IResourceWithEndpoints withHttpEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResourceWithEndpoints withHttpsEndpoint(Double port, Double targetPort, String name, String env, Boolean isProxied) {
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
    public IResource withUrlsCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs);
    }

    /** Customizes displayed URLs via async callback */
    public IResource withUrlsCallbackAsync(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs);
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
    public IResource withUrlForEndpoint(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs);
    }

    /** Adds a URL for a specific endpoint via factory callback */
    public IResourceWithEndpoints withUrlForEndpointFactory(String endpointName, Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEndpoints) getClient().invokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs);
    }

    /** Waits for another resource to be ready */
    public IResourceWithWaitSupport waitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResourceWithWaitSupport) getClient().invokeCapability("Aspire.Hosting/waitFor", reqArgs);
    }

    /** Prevents resource from starting automatically */
    public IResource withExplicitStart() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withExplicitStart", reqArgs);
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

    /** Adds a health check by key */
    public IResource withHealthCheck(String key) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("key", AspireClient.serializeValue(key));
        return (IResource) getClient().invokeCapability("Aspire.Hosting/withHealthCheck", reqArgs);
    }

    /** Adds an HTTP health check */
    public IResourceWithEndpoints withHttpHealthCheck(String path, Double statusCode, String endpointName) {
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

    /** Adds a resource command */
    public IResource withCommand(String name, String displayName, Function<Object[], Object> executeCommand, CommandOptions commandOptions) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        reqArgs.put("displayName", AspireClient.serializeValue(displayName));
        if (executeCommand != null) {
            reqArgs.put("executeCommand", getClient().registerCallback(executeCommand));
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

    /** Adds a volume */
    public ContainerResource withVolume(String target, String name, Boolean isReadOnly) {
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

    /** Adds an optional string parameter */
    public IResource withOptionalString(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(Function<Object[], Object> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (callback != null) {
            reqArgs.put("callback", getClient().registerCallback(callback));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(Function<Object[], Object> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (validator != null) {
            reqArgs.put("validator", getClient().registerCallback(validator));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(Function<Object[], Object> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (operation != null) {
            reqArgs.put("operation", getClient().registerCallback(operation));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

    /** Configures vault using direct interface target */
    public ITestVaultResource withVaultDirect(String option) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("option", AspireClient.serializeValue(option));
        return (ITestVaultResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withVaultDirect", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext. */
class UpdateCommandStateContext extends HandleWrapperBase {
    UpdateCommandStateContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

// ============================================================================
// Handle wrapper registrations
// ============================================================================

/** Static initializer to register handle wrappers. */
class AspireRegistrations {
    static {
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplication", (h, c) -> new DistributedApplication(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext", (h, c) -> new DistributedApplicationExecutionContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions", (h, c) -> new DistributedApplicationExecutionContextOptions(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", (h, c) -> new IDistributedApplicationBuilder(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription", (h, c) -> new DistributedApplicationEventSubscription(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription", (h, c) -> new DistributedApplicationResourceEventSubscription(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent", (h, c) -> new IDistributedApplicationEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent", (h, c) -> new IDistributedApplicationResourceEvent(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing", (h, c) -> new IDistributedApplicationEventing(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext", (h, c) -> new CommandLineArgsCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference", (h, c) -> new EndpointReference(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression", (h, c) -> new EndpointReferenceExpression(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext", (h, c) -> new EnvironmentCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext", (h, c) -> new UpdateCommandStateContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext", (h, c) -> new ExecuteCommandContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext", (h, c) -> new ResourceUrlsCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource", (h, c) -> new ContainerResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource", (h, c) -> new ExecutableResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource", (h, c) -> new ParameterResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString", (h, c) -> new IResourceWithConnectionString(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource", (h, c) -> new ProjectResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery", (h, c) -> new IResourceWithServiceDiscovery(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource", (h, c) -> new IResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", (h, c) -> new TestCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", (h, c) -> new TestResourceContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", (h, c) -> new TestEnvironmentContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", (h, c) -> new TestCollectionContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", (h, c) -> new TestRedisResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource", (h, c) -> new TestDatabaseResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource", (h, c) -> new TestVaultResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource", (h, c) -> new ITestVaultResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment", (h, c) -> new IResourceWithEnvironment(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs", (h, c) -> new IResourceWithArgs(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints", (h, c) -> new IResourceWithEndpoints(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport", (h, c) -> new IResourceWithWaitSupport(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Dict<string,any>", (h, c) -> new AspireDict(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<any>", (h, c) -> new AspireList(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Dict<string,string|Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression>", (h, c) -> new AspireDict(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlAnnotation>", (h, c) -> new AspireList(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<string>", (h, c) -> new AspireList(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Dict<string,string>", (h, c) -> new AspireDict(h, c));
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

