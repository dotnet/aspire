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

/** ForwardedTransformActions enum. */
enum ForwardedTransformActions implements WireValueEnum {
    OFF("Off"),
    SET("Set"),
    APPEND("Append"),
    REMOVE("Remove");

    private final String value;

    ForwardedTransformActions(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static ForwardedTransformActions fromValue(String value) {
        for (ForwardedTransformActions e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** NodeFormat enum. */
enum NodeFormat implements WireValueEnum {
    NONE("None"),
    RANDOM("Random"),
    RANDOM_AND_PORT("RandomAndPort"),
    RANDOM_AND_RANDOM_PORT("RandomAndRandomPort"),
    UNKNOWN("Unknown"),
    UNKNOWN_AND_PORT("UnknownAndPort"),
    UNKNOWN_AND_RANDOM_PORT("UnknownAndRandomPort"),
    IP("Ip"),
    IP_AND_PORT("IpAndPort"),
    IP_AND_RANDOM_PORT("IpAndRandomPort");

    private final String value;

    NodeFormat(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static NodeFormat fromValue(String value) {
        for (NodeFormat e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** ResponseCondition enum. */
enum ResponseCondition implements WireValueEnum {
    ALWAYS("Always"),
    SUCCESS("Success"),
    FAILURE("Failure");

    private final String value;

    ResponseCondition(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static ResponseCondition fromValue(String value) {
        for (ResponseCondition e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** HttpVersionPolicy enum. */
enum HttpVersionPolicy implements WireValueEnum {
    REQUEST_VERSION_OR_LOWER("RequestVersionOrLower"),
    REQUEST_VERSION_OR_HIGHER("RequestVersionOrHigher"),
    REQUEST_VERSION_EXACT("RequestVersionExact");

    private final String value;

    HttpVersionPolicy(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static HttpVersionPolicy fromValue(String value) {
        for (HttpVersionPolicy e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** YarpSslProtocol enum. */
enum YarpSslProtocol implements WireValueEnum {
    NONE("None"),
    TLS12("Tls12"),
    TLS13("Tls13");

    private final String value;

    YarpSslProtocol(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static YarpSslProtocol fromValue(String value) {
        for (YarpSslProtocol e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** SameSiteMode enum. */
enum SameSiteMode implements WireValueEnum {
    NONE("None"),
    LAX("Lax"),
    STRICT("Strict"),
    UNSPECIFIED("Unspecified");

    private final String value;

    SameSiteMode(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static SameSiteMode fromValue(String value) {
        for (SameSiteMode e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** CookieSecurePolicy enum. */
enum CookieSecurePolicy implements WireValueEnum {
    SAME_AS_REQUEST("SameAsRequest"),
    ALWAYS("Always"),
    NONE("None");

    private final String value;

    CookieSecurePolicy(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static CookieSecurePolicy fromValue(String value) {
        for (CookieSecurePolicy e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** HeaderMatchMode enum. */
enum HeaderMatchMode implements WireValueEnum {
    EXACT_HEADER("ExactHeader"),
    HEADER_PREFIX("HeaderPrefix"),
    CONTAINS("Contains"),
    NOT_CONTAINS("NotContains"),
    EXISTS("Exists"),
    NOT_EXISTS("NotExists");

    private final String value;

    HeaderMatchMode(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static HeaderMatchMode fromValue(String value) {
        for (HeaderMatchMode e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** QueryParameterMatchMode enum. */
enum QueryParameterMatchMode implements WireValueEnum {
    EXACT("Exact"),
    CONTAINS("Contains"),
    NOT_CONTAINS("NotContains"),
    PREFIX("Prefix"),
    EXISTS("Exists");

    private final String value;

    QueryParameterMatchMode(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static QueryParameterMatchMode fromValue(String value) {
        for (QueryParameterMatchMode e : values()) {
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

/** YarpForwarderRequestConfig DTO. */
class YarpForwarderRequestConfig {
    private double activityTimeout;
    private boolean allowResponseBuffering;
    private String version;
    private HttpVersionPolicy versionPolicy;

    public double getActivityTimeout() { return activityTimeout; }
    public void setActivityTimeout(double value) { this.activityTimeout = value; }
    public boolean getAllowResponseBuffering() { return allowResponseBuffering; }
    public void setAllowResponseBuffering(boolean value) { this.allowResponseBuffering = value; }
    public String getVersion() { return version; }
    public void setVersion(String value) { this.version = value; }
    public HttpVersionPolicy getVersionPolicy() { return versionPolicy; }
    public void setVersionPolicy(HttpVersionPolicy value) { this.versionPolicy = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("ActivityTimeout", AspireClient.serializeValue(activityTimeout));
        map.put("AllowResponseBuffering", AspireClient.serializeValue(allowResponseBuffering));
        map.put("Version", AspireClient.serializeValue(version));
        map.put("VersionPolicy", AspireClient.serializeValue(versionPolicy));
        return map;
    }
}

/** YarpHttpClientConfig DTO. */
class YarpHttpClientConfig {
    private boolean dangerousAcceptAnyServerCertificate;
    private boolean enableMultipleHttp2Connections;
    private double maxConnectionsPerServer;
    private String requestHeaderEncoding;
    private String responseHeaderEncoding;
    private YarpSslProtocol[] sslProtocols;
    private YarpWebProxyConfig webProxy;

    public boolean getDangerousAcceptAnyServerCertificate() { return dangerousAcceptAnyServerCertificate; }
    public void setDangerousAcceptAnyServerCertificate(boolean value) { this.dangerousAcceptAnyServerCertificate = value; }
    public boolean getEnableMultipleHttp2Connections() { return enableMultipleHttp2Connections; }
    public void setEnableMultipleHttp2Connections(boolean value) { this.enableMultipleHttp2Connections = value; }
    public double getMaxConnectionsPerServer() { return maxConnectionsPerServer; }
    public void setMaxConnectionsPerServer(double value) { this.maxConnectionsPerServer = value; }
    public String getRequestHeaderEncoding() { return requestHeaderEncoding; }
    public void setRequestHeaderEncoding(String value) { this.requestHeaderEncoding = value; }
    public String getResponseHeaderEncoding() { return responseHeaderEncoding; }
    public void setResponseHeaderEncoding(String value) { this.responseHeaderEncoding = value; }
    public YarpSslProtocol[] getSslProtocols() { return sslProtocols; }
    public void setSslProtocols(YarpSslProtocol[] value) { this.sslProtocols = value; }
    public YarpWebProxyConfig getWebProxy() { return webProxy; }
    public void setWebProxy(YarpWebProxyConfig value) { this.webProxy = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("DangerousAcceptAnyServerCertificate", AspireClient.serializeValue(dangerousAcceptAnyServerCertificate));
        map.put("EnableMultipleHttp2Connections", AspireClient.serializeValue(enableMultipleHttp2Connections));
        map.put("MaxConnectionsPerServer", AspireClient.serializeValue(maxConnectionsPerServer));
        map.put("RequestHeaderEncoding", AspireClient.serializeValue(requestHeaderEncoding));
        map.put("ResponseHeaderEncoding", AspireClient.serializeValue(responseHeaderEncoding));
        map.put("SslProtocols", AspireClient.serializeValue(sslProtocols));
        map.put("WebProxy", AspireClient.serializeValue(webProxy));
        return map;
    }
}

/** YarpWebProxyConfig DTO. */
class YarpWebProxyConfig {
    private String address;
    private boolean bypassOnLocal;
    private boolean useDefaultCredentials;

    public String getAddress() { return address; }
    public void setAddress(String value) { this.address = value; }
    public boolean getBypassOnLocal() { return bypassOnLocal; }
    public void setBypassOnLocal(boolean value) { this.bypassOnLocal = value; }
    public boolean getUseDefaultCredentials() { return useDefaultCredentials; }
    public void setUseDefaultCredentials(boolean value) { this.useDefaultCredentials = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Address", AspireClient.serializeValue(address));
        map.put("BypassOnLocal", AspireClient.serializeValue(bypassOnLocal));
        map.put("UseDefaultCredentials", AspireClient.serializeValue(useDefaultCredentials));
        return map;
    }
}

/** YarpSessionAffinityConfig DTO. */
class YarpSessionAffinityConfig {
    private String affinityKeyName;
    private YarpSessionAffinityCookieConfig cookie;
    private boolean enabled;
    private String failurePolicy;
    private String policy;

    public String getAffinityKeyName() { return affinityKeyName; }
    public void setAffinityKeyName(String value) { this.affinityKeyName = value; }
    public YarpSessionAffinityCookieConfig getCookie() { return cookie; }
    public void setCookie(YarpSessionAffinityCookieConfig value) { this.cookie = value; }
    public boolean getEnabled() { return enabled; }
    public void setEnabled(boolean value) { this.enabled = value; }
    public String getFailurePolicy() { return failurePolicy; }
    public void setFailurePolicy(String value) { this.failurePolicy = value; }
    public String getPolicy() { return policy; }
    public void setPolicy(String value) { this.policy = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("AffinityKeyName", AspireClient.serializeValue(affinityKeyName));
        map.put("Cookie", AspireClient.serializeValue(cookie));
        map.put("Enabled", AspireClient.serializeValue(enabled));
        map.put("FailurePolicy", AspireClient.serializeValue(failurePolicy));
        map.put("Policy", AspireClient.serializeValue(policy));
        return map;
    }
}

/** YarpSessionAffinityCookieConfig DTO. */
class YarpSessionAffinityCookieConfig {
    private String domain;
    private double expiration;
    private boolean httpOnly;
    private boolean isEssential;
    private double maxAge;
    private String path;
    private SameSiteMode sameSite;
    private CookieSecurePolicy securePolicy;

    public String getDomain() { return domain; }
    public void setDomain(String value) { this.domain = value; }
    public double getExpiration() { return expiration; }
    public void setExpiration(double value) { this.expiration = value; }
    public boolean getHttpOnly() { return httpOnly; }
    public void setHttpOnly(boolean value) { this.httpOnly = value; }
    public boolean getIsEssential() { return isEssential; }
    public void setIsEssential(boolean value) { this.isEssential = value; }
    public double getMaxAge() { return maxAge; }
    public void setMaxAge(double value) { this.maxAge = value; }
    public String getPath() { return path; }
    public void setPath(String value) { this.path = value; }
    public SameSiteMode getSameSite() { return sameSite; }
    public void setSameSite(SameSiteMode value) { this.sameSite = value; }
    public CookieSecurePolicy getSecurePolicy() { return securePolicy; }
    public void setSecurePolicy(CookieSecurePolicy value) { this.securePolicy = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Domain", AspireClient.serializeValue(domain));
        map.put("Expiration", AspireClient.serializeValue(expiration));
        map.put("HttpOnly", AspireClient.serializeValue(httpOnly));
        map.put("IsEssential", AspireClient.serializeValue(isEssential));
        map.put("MaxAge", AspireClient.serializeValue(maxAge));
        map.put("Path", AspireClient.serializeValue(path));
        map.put("SameSite", AspireClient.serializeValue(sameSite));
        map.put("SecurePolicy", AspireClient.serializeValue(securePolicy));
        return map;
    }
}

/** YarpHealthCheckConfig DTO. */
class YarpHealthCheckConfig {
    private YarpActiveHealthCheckConfig active;
    private String availableDestinationsPolicy;
    private YarpPassiveHealthCheckConfig passive;

    public YarpActiveHealthCheckConfig getActive() { return active; }
    public void setActive(YarpActiveHealthCheckConfig value) { this.active = value; }
    public String getAvailableDestinationsPolicy() { return availableDestinationsPolicy; }
    public void setAvailableDestinationsPolicy(String value) { this.availableDestinationsPolicy = value; }
    public YarpPassiveHealthCheckConfig getPassive() { return passive; }
    public void setPassive(YarpPassiveHealthCheckConfig value) { this.passive = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Active", AspireClient.serializeValue(active));
        map.put("AvailableDestinationsPolicy", AspireClient.serializeValue(availableDestinationsPolicy));
        map.put("Passive", AspireClient.serializeValue(passive));
        return map;
    }
}

/** YarpActiveHealthCheckConfig DTO. */
class YarpActiveHealthCheckConfig {
    private boolean enabled;
    private double interval;
    private String path;
    private String policy;
    private String query;
    private double timeout;

    public boolean getEnabled() { return enabled; }
    public void setEnabled(boolean value) { this.enabled = value; }
    public double getInterval() { return interval; }
    public void setInterval(double value) { this.interval = value; }
    public String getPath() { return path; }
    public void setPath(String value) { this.path = value; }
    public String getPolicy() { return policy; }
    public void setPolicy(String value) { this.policy = value; }
    public String getQuery() { return query; }
    public void setQuery(String value) { this.query = value; }
    public double getTimeout() { return timeout; }
    public void setTimeout(double value) { this.timeout = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Enabled", AspireClient.serializeValue(enabled));
        map.put("Interval", AspireClient.serializeValue(interval));
        map.put("Path", AspireClient.serializeValue(path));
        map.put("Policy", AspireClient.serializeValue(policy));
        map.put("Query", AspireClient.serializeValue(query));
        map.put("Timeout", AspireClient.serializeValue(timeout));
        return map;
    }
}

/** YarpPassiveHealthCheckConfig DTO. */
class YarpPassiveHealthCheckConfig {
    private boolean enabled;
    private String policy;
    private double reactivationPeriod;

    public boolean getEnabled() { return enabled; }
    public void setEnabled(boolean value) { this.enabled = value; }
    public String getPolicy() { return policy; }
    public void setPolicy(String value) { this.policy = value; }
    public double getReactivationPeriod() { return reactivationPeriod; }
    public void setReactivationPeriod(double value) { this.reactivationPeriod = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Enabled", AspireClient.serializeValue(enabled));
        map.put("Policy", AspireClient.serializeValue(policy));
        map.put("ReactivationPeriod", AspireClient.serializeValue(reactivationPeriod));
        return map;
    }
}

/** YarpRouteHeaderMatch DTO. */
class YarpRouteHeaderMatch {
    private String name;
    private String[] values;
    private boolean isCaseSensitive;
    private HeaderMatchMode mode;

    public String getName() { return name; }
    public void setName(String value) { this.name = value; }
    public String[] getValues() { return values; }
    public void setValues(String[] value) { this.values = value; }
    public boolean getIsCaseSensitive() { return isCaseSensitive; }
    public void setIsCaseSensitive(boolean value) { this.isCaseSensitive = value; }
    public HeaderMatchMode getMode() { return mode; }
    public void setMode(HeaderMatchMode value) { this.mode = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Name", AspireClient.serializeValue(name));
        map.put("Values", AspireClient.serializeValue(values));
        map.put("IsCaseSensitive", AspireClient.serializeValue(isCaseSensitive));
        map.put("Mode", AspireClient.serializeValue(mode));
        return map;
    }
}

/** YarpRouteMatch DTO. */
class YarpRouteMatch {
    private String path;
    private String[] methods;
    private String[] hosts;
    private YarpRouteHeaderMatch[] headers;
    private YarpRouteQueryParameterMatch[] queryParameters;

    public String getPath() { return path; }
    public void setPath(String value) { this.path = value; }
    public String[] getMethods() { return methods; }
    public void setMethods(String[] value) { this.methods = value; }
    public String[] getHosts() { return hosts; }
    public void setHosts(String[] value) { this.hosts = value; }
    public YarpRouteHeaderMatch[] getHeaders() { return headers; }
    public void setHeaders(YarpRouteHeaderMatch[] value) { this.headers = value; }
    public YarpRouteQueryParameterMatch[] getQueryParameters() { return queryParameters; }
    public void setQueryParameters(YarpRouteQueryParameterMatch[] value) { this.queryParameters = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Path", AspireClient.serializeValue(path));
        map.put("Methods", AspireClient.serializeValue(methods));
        map.put("Hosts", AspireClient.serializeValue(hosts));
        map.put("Headers", AspireClient.serializeValue(headers));
        map.put("QueryParameters", AspireClient.serializeValue(queryParameters));
        return map;
    }
}

/** YarpRouteQueryParameterMatch DTO. */
class YarpRouteQueryParameterMatch {
    private String name;
    private String[] values;
    private boolean isCaseSensitive;
    private QueryParameterMatchMode mode;

    public String getName() { return name; }
    public void setName(String value) { this.name = value; }
    public String[] getValues() { return values; }
    public void setValues(String[] value) { this.values = value; }
    public boolean getIsCaseSensitive() { return isCaseSensitive; }
    public void setIsCaseSensitive(boolean value) { this.isCaseSensitive = value; }
    public QueryParameterMatchMode getMode() { return mode; }
    public void setMode(QueryParameterMatchMode value) { this.mode = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Name", AspireClient.serializeValue(name));
        map.put("Values", AspireClient.serializeValue(values));
        map.put("IsCaseSensitive", AspireClient.serializeValue(isCaseSensitive));
        map.put("Mode", AspireClient.serializeValue(mode));
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

/** Options for AddRedis. */
final class AddRedisOptions {
    private Double port;
    private ParameterResource password;

    public Double getPort() { return port; }
    public AddRedisOptions port(Double value) {
        this.port = value;
        return this;
    }

    public ParameterResource getPassword() { return password; }
    public AddRedisOptions password(ParameterResource value) {
        this.password = value;
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

/** Options for GetEndpointForNetwork. */
final class GetEndpointForNetworkOptions {
    private String networkIdentifier;
    private String endpointName;

    public String getNetworkIdentifier() { return networkIdentifier; }
    public GetEndpointForNetworkOptions networkIdentifier(String value) {
        this.networkIdentifier = value;
        return this;
    }

    public String getEndpointName() { return endpointName; }
    public GetEndpointForNetworkOptions endpointName(String value) {
        this.endpointName = value;
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

/** Options for WithDataVolume. */
final class WithDataVolumeOptions {
    private String name;
    private Boolean isReadOnly;

    public String getName() { return name; }
    public WithDataVolumeOptions name(String value) {
        this.name = value;
        return this;
    }

    public Boolean isReadOnly() { return isReadOnly; }
    public WithDataVolumeOptions isReadOnly(Boolean value) {
        this.isReadOnly = value;
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

/** Options for WithPersistence. */
final class WithPersistenceOptions {
    private Double interval;
    private Double keysChangedThreshold;

    public Double getInterval() { return interval; }
    public WithPersistenceOptions interval(Double value) {
        this.interval = value;
        return this;
    }

    public Double getKeysChangedThreshold() { return keysChangedThreshold; }
    public WithPersistenceOptions keysChangedThreshold(Double value) {
        this.keysChangedThreshold = value;
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

/** Options for WithRedisCommander. */
final class WithRedisCommanderOptions {
    private AspireAction1<RedisCommanderResource> configureContainer;
    private String containerName;

    public AspireAction1<RedisCommanderResource> getConfigureContainer() { return configureContainer; }
    public WithRedisCommanderOptions configureContainer(AspireAction1<RedisCommanderResource> value) {
        this.configureContainer = value;
        return this;
    }

    public String getContainerName() { return containerName; }
    public WithRedisCommanderOptions containerName(String value) {
        this.containerName = value;
        return this;
    }

}

/** Options for WithRedisInsight. */
final class WithRedisInsightOptions {
    private AspireAction1<RedisInsightResource> configureContainer;
    private String containerName;

    public AspireAction1<RedisInsightResource> getConfigureContainer() { return configureContainer; }
    public WithRedisInsightOptions configureContainer(AspireAction1<RedisInsightResource> value) {
        this.configureContainer = value;
        return this;
    }

    public String getContainerName() { return containerName; }
    public WithRedisInsightOptions containerName(String value) {
        this.containerName = value;
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

/** Options for WithTransformForwarded. */
final class WithTransformForwardedOptions {
    private Boolean useHost;
    private Boolean useProto;
    private NodeFormat forFormat;
    private NodeFormat byFormat;
    private ForwardedTransformActions action;

    public Boolean getUseHost() { return useHost; }
    public WithTransformForwardedOptions useHost(Boolean value) {
        this.useHost = value;
        return this;
    }

    public Boolean getUseProto() { return useProto; }
    public WithTransformForwardedOptions useProto(Boolean value) {
        this.useProto = value;
        return this;
    }

    public NodeFormat getForFormat() { return forFormat; }
    public WithTransformForwardedOptions forFormat(NodeFormat value) {
        this.forFormat = value;
        return this;
    }

    public NodeFormat getByFormat() { return byFormat; }
    public WithTransformForwardedOptions byFormat(NodeFormat value) {
        this.byFormat = value;
        return this;
    }

    public ForwardedTransformActions getAction() { return action; }
    public WithTransformForwardedOptions action(ForwardedTransformActions value) {
        this.action = value;
        return this;
    }

}

/** Options for WithTransformResponseHeader. */
final class WithTransformResponseHeaderOptions {
    private Boolean append;
    private ResponseCondition condition;

    public Boolean getAppend() { return append; }
    public WithTransformResponseHeaderOptions append(Boolean value) {
        this.append = value;
        return this;
    }

    public ResponseCondition getCondition() { return condition; }
    public WithTransformResponseHeaderOptions condition(ResponseCondition value) {
        this.condition = value;
        return this;
    }

}

/** Options for WithTransformResponseTrailer. */
final class WithTransformResponseTrailerOptions {
    private Boolean append;
    private ResponseCondition condition;

    public Boolean getAppend() { return append; }
    public WithTransformResponseTrailerOptions append(Boolean value) {
        this.append = value;
        return this;
    }

    public ResponseCondition getCondition() { return condition; }
    public WithTransformResponseTrailerOptions condition(ResponseCondition value) {
        this.condition = value;
        return this;
    }

}

/** Options for WithTransformXForwarded. */
final class WithTransformXForwardedOptions {
    private String headerPrefix;
    private ForwardedTransformActions xDefault;
    private ForwardedTransformActions xFor;
    private ForwardedTransformActions xHost;
    private ForwardedTransformActions xProto;
    private ForwardedTransformActions xPrefix;

    public String getHeaderPrefix() { return headerPrefix; }
    public WithTransformXForwardedOptions headerPrefix(String value) {
        this.headerPrefix = value;
        return this;
    }

    public ForwardedTransformActions getXDefault() { return xDefault; }
    public WithTransformXForwardedOptions xDefault(ForwardedTransformActions value) {
        this.xDefault = value;
        return this;
    }

    public ForwardedTransformActions getXFor() { return xFor; }
    public WithTransformXForwardedOptions xFor(ForwardedTransformActions value) {
        this.xFor = value;
        return this;
    }

    public ForwardedTransformActions getXHost() { return xHost; }
    public WithTransformXForwardedOptions xHost(ForwardedTransformActions value) {
        this.xHost = value;
        return this;
    }

    public ForwardedTransformActions getXProto() { return xProto; }
    public WithTransformXForwardedOptions xProto(ForwardedTransformActions value) {
        this.xProto = value;
        return this;
    }

    public ForwardedTransformActions getXPrefix() { return xPrefix; }
    public WithTransformXForwardedOptions xPrefix(ForwardedTransformActions value) {
        this.xPrefix = value;
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

    /** Gets the connection string for the specified resource. */
    public String getConnectionString(String resourceName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("app", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        return (String) getClient().invokeCapability("Aspire.Hosting.Testing/getConnectionString", reqArgs);
    }

    public String getEndpoint(String resourceName) {
        return getEndpoint(resourceName, null);
    }

    /** Gets the endpoint for the specified resource. */
    public String getEndpoint(String resourceName, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("app", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (String) getClient().invokeCapability("Aspire.Hosting.Testing/getEndpoint", reqArgs);
    }

    /** Gets the endpoint for the specified resource in the specified network context. */
    public String getEndpointForNetwork(String resourceName, GetEndpointForNetworkOptions options) {
        var networkIdentifier = options == null ? null : options.getNetworkIdentifier();
        var endpointName = options == null ? null : options.getEndpointName();
        return getEndpointForNetworkImpl(resourceName, networkIdentifier, endpointName);
    }

    public String getEndpointForNetwork(String resourceName) {
        return getEndpointForNetwork(resourceName, null);
    }

    /** Gets the endpoint for the specified resource in the specified network context. */
    private String getEndpointForNetworkImpl(String resourceName, String networkIdentifier, String endpointName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("app", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceName", AspireClient.serializeValue(resourceName));
        if (networkIdentifier != null) {
            reqArgs.put("networkIdentifier", AspireClient.serializeValue(networkIdentifier));
        }
        if (endpointName != null) {
            reqArgs.put("endpointName", AspireClient.serializeValue(endpointName));
        }
        return (String) getClient().invokeCapability("Aspire.Hosting.Testing/getEndpointForNetwork", reqArgs);
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

    /** Adds a YARP container to the application model. */
    public YarpResource addYarp(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (YarpResource) getClient().invokeCapability("Aspire.Hosting.Yarp/addYarp", reqArgs);
    }

    public RedisResource addRedisWithPort(String name) {
        return addRedisWithPort(name, null);
    }

    /** Adds a Redis container resource with specific port */
    public RedisResource addRedisWithPort(String name, Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/addRedisWithPort", reqArgs);
    }

    /** Adds a Redis container resource */
    public RedisResource addRedis(String name, AddRedisOptions options) {
        var port = options == null ? null : options.getPort();
        var password = options == null ? null : options.getPassword();
        return addRedisImpl(name, port, password);
    }

    public RedisResource addRedis(String name) {
        return addRedis(name, null);
    }

    /** Adds a Redis container resource */
    private RedisResource addRedisImpl(String name, Double port, ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        if (password != null) {
            reqArgs.put("password", AspireClient.serializeValue(password));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/addRedis", reqArgs);
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

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery. */
class IResourceWithServiceDiscovery extends ResourceBuilderBase {
    IResourceWithServiceDiscovery(Handle handle, AspireClient client) {
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

/** Wrapper for Aspire.Hosting.Yarp/Aspire.Hosting.IYarpConfigurationBuilder. */
class IYarpConfigurationBuilder extends HandleWrapperBase {
    IYarpConfigurationBuilder(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Invokes the AddRoute method */
    public YarpRoute addRoute(String path, YarpCluster cluster) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("path", AspireClient.serializeValue(path));
        reqArgs.put("cluster", AspireClient.serializeValue(cluster));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting/IYarpConfigurationBuilder.addRoute", reqArgs);
    }

    /** Adds a YARP cluster for an endpoint reference. */
    public YarpCluster addClusterFromEndpoint(EndpointReference endpoint) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoint", AspireClient.serializeValue(endpoint));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/addClusterFromEndpoint", reqArgs);
    }

    /** Adds a YARP cluster for a resource that supports service discovery. */
    public YarpCluster addClusterFromResource(IResourceWithServiceDiscovery resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/addClusterFromResource", reqArgs);
    }

    public YarpCluster addClusterFromResource(ResourceBuilderBase resource) {
        return addClusterFromResource(new IResourceWithServiceDiscovery(resource.getHandle(), resource.getClient()));
    }

    /** Adds a YARP cluster for an external service resource. */
    public YarpCluster addClusterFromExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/addClusterFromExternalService", reqArgs);
    }

    /** Adds a YARP cluster with multiple destinations. */
    public YarpCluster addClusterWithDestinations(String clusterName, Object[] destinations) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("clusterName", AspireClient.serializeValue(clusterName));
        reqArgs.put("destinations", AspireClient.serializeValue(destinations));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/addClusterWithDestinations", reqArgs);
    }

    /** Adds a YARP cluster with a single destination. */
    public YarpCluster addClusterWithDestination(String clusterName, Object destination) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("clusterName", AspireClient.serializeValue(clusterName));
        reqArgs.put("destination", AspireClient.serializeValue(destination));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/addClusterWithDestination", reqArgs);
    }

    /** Adds a YARP catch-all route for an existing cluster. */
    public YarpRoute addCatchAllRoute(YarpCluster cluster) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("cluster", AspireClient.serializeValue(cluster));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addCatchAllRoute", reqArgs);
    }

    /** Adds a YARP catch-all route for an endpoint reference. */
    public YarpRoute addCatchAllRouteFromEndpoint(EndpointReference endpoint) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoint", AspireClient.serializeValue(endpoint));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addCatchAllRouteFromEndpoint", reqArgs);
    }

    /** Adds a YARP catch-all route for a resource that supports service discovery. */
    public YarpRoute addCatchAllRouteFromResource(IResourceWithServiceDiscovery resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addCatchAllRouteFromResource", reqArgs);
    }

    public YarpRoute addCatchAllRouteFromResource(ResourceBuilderBase resource) {
        return addCatchAllRouteFromResource(new IResourceWithServiceDiscovery(resource.getHandle(), resource.getClient()));
    }

    /** Adds a YARP route for an endpoint reference. */
    public YarpRoute addRouteFromEndpoint(String path, EndpointReference endpoint) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("path", AspireClient.serializeValue(path));
        reqArgs.put("endpoint", AspireClient.serializeValue(endpoint));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addRouteFromEndpoint", reqArgs);
    }

    /** Adds a YARP route for a resource that supports service discovery. */
    public YarpRoute addRouteFromResource(String path, IResourceWithServiceDiscovery resource) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("path", AspireClient.serializeValue(path));
        reqArgs.put("resource", AspireClient.serializeValue(resource));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addRouteFromResource", reqArgs);
    }

    public YarpRoute addRouteFromResource(String path, ResourceBuilderBase resource) {
        return addRouteFromResource(path, new IResourceWithServiceDiscovery(resource.getHandle(), resource.getClient()));
    }

    /** Adds a YARP route for an external service resource. */
    public YarpRoute addRouteFromExternalService(String path, ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("path", AspireClient.serializeValue(path));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addRouteFromExternalService", reqArgs);
    }

    /** Adds a YARP catch-all route for an external service resource. */
    public YarpRoute addCatchAllRouteFromExternalService(ExternalServiceResource externalService) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("externalService", AspireClient.serializeValue(externalService));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/addCatchAllRouteFromExternalService", reqArgs);
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

/** Wrapper for Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisCommanderResource. */
class RedisCommanderResource extends ResourceBuilderBase {
    RedisCommanderResource(Handle handle, AspireClient client) {
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

    public RedisCommanderResource withHostPort() {
        return withHostPort(null);
    }

    /** Sets the host port for Redis Commander */
    public RedisCommanderResource withHostPort(Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (RedisCommanderResource) getClient().invokeCapability("Aspire.Hosting.Redis/withRedisCommanderHostPort", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisInsightResource. */
class RedisInsightResource extends ResourceBuilderBase {
    RedisInsightResource(Handle handle, AspireClient client) {
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

    public RedisInsightResource withHostPort() {
        return withHostPort(null);
    }

    /** Sets the host port for Redis Insight */
    public RedisInsightResource withHostPort(Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (RedisInsightResource) getClient().invokeCapability("Aspire.Hosting.Redis/withRedisInsightHostPort", reqArgs);
    }

    public RedisInsightResource withDataVolume() {
        return withDataVolume(null);
    }

    /** Adds a data volume for Redis Insight */
    public RedisInsightResource withDataVolume(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (RedisInsightResource) getClient().invokeCapability("Aspire.Hosting.Redis/withRedisInsightDataVolume", reqArgs);
    }

    /** Adds a data bind mount for Redis Insight */
    public RedisInsightResource withDataBindMount(String source) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        return (RedisInsightResource) getClient().invokeCapability("Aspire.Hosting.Redis/withRedisInsightDataBindMount", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource. */
class RedisResource extends ResourceBuilderBase {
    RedisResource(Handle handle, AspireClient client) {
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

    /** Adds Redis Commander management UI */
    public RedisResource withRedisCommander(WithRedisCommanderOptions options) {
        var configureContainer = options == null ? null : options.getConfigureContainer();
        var containerName = options == null ? null : options.getContainerName();
        return withRedisCommanderImpl(configureContainer, containerName);
    }

    public RedisResource withRedisCommander() {
        return withRedisCommander(null);
    }

    /** Adds Redis Commander management UI */
    private RedisResource withRedisCommanderImpl(AspireAction1<RedisCommanderResource> configureContainer, String containerName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureContainerId = configureContainer == null ? null : getClient().registerCallback(args -> {
            var obj = (RedisCommanderResource) args[0];
            configureContainer.invoke(obj);
            return null;
        });
        if (configureContainerId != null) {
            reqArgs.put("configureContainer", configureContainerId);
        }
        if (containerName != null) {
            reqArgs.put("containerName", AspireClient.serializeValue(containerName));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withRedisCommander", reqArgs);
    }

    /** Adds Redis Insight management UI */
    public RedisResource withRedisInsight(WithRedisInsightOptions options) {
        var configureContainer = options == null ? null : options.getConfigureContainer();
        var containerName = options == null ? null : options.getContainerName();
        return withRedisInsightImpl(configureContainer, containerName);
    }

    public RedisResource withRedisInsight() {
        return withRedisInsight(null);
    }

    /** Adds Redis Insight management UI */
    private RedisResource withRedisInsightImpl(AspireAction1<RedisInsightResource> configureContainer, String containerName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configureContainerId = configureContainer == null ? null : getClient().registerCallback(args -> {
            var obj = (RedisInsightResource) args[0];
            configureContainer.invoke(obj);
            return null;
        });
        if (configureContainerId != null) {
            reqArgs.put("configureContainer", configureContainerId);
        }
        if (containerName != null) {
            reqArgs.put("containerName", AspireClient.serializeValue(containerName));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withRedisInsight", reqArgs);
    }

    /** Adds a data volume with persistence */
    public RedisResource withDataVolume(WithDataVolumeOptions options) {
        var name = options == null ? null : options.getName();
        var isReadOnly = options == null ? null : options.isReadOnly();
        return withDataVolumeImpl(name, isReadOnly);
    }

    public RedisResource withDataVolume() {
        return withDataVolume(null);
    }

    /** Adds a data volume with persistence */
    private RedisResource withDataVolumeImpl(String name, Boolean isReadOnly) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (isReadOnly != null) {
            reqArgs.put("isReadOnly", AspireClient.serializeValue(isReadOnly));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withDataVolume", reqArgs);
    }

    public RedisResource withDataBindMount(String source) {
        return withDataBindMount(source, null);
    }

    /** Adds a data bind mount with persistence */
    public RedisResource withDataBindMount(String source, Boolean isReadOnly) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("source", AspireClient.serializeValue(source));
        if (isReadOnly != null) {
            reqArgs.put("isReadOnly", AspireClient.serializeValue(isReadOnly));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withDataBindMount", reqArgs);
    }

    /** Configures Redis persistence */
    public RedisResource withPersistence(WithPersistenceOptions options) {
        var interval = options == null ? null : options.getInterval();
        var keysChangedThreshold = options == null ? null : options.getKeysChangedThreshold();
        return withPersistenceImpl(interval, keysChangedThreshold);
    }

    public RedisResource withPersistence() {
        return withPersistence(null);
    }

    /** Configures Redis persistence */
    private RedisResource withPersistenceImpl(Double interval, Double keysChangedThreshold) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (interval != null) {
            reqArgs.put("interval", AspireClient.serializeValue(interval));
        }
        if (keysChangedThreshold != null) {
            reqArgs.put("keysChangedThreshold", AspireClient.serializeValue(keysChangedThreshold));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withPersistence", reqArgs);
    }

    /** Configures the password for Redis */
    public RedisResource withPassword(ParameterResource password) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("password", AspireClient.serializeValue(password));
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withPassword", reqArgs);
    }

    public RedisResource withHostPort() {
        return withHostPort(null);
    }

    /** Sets the host port for Redis */
    public RedisResource withHostPort(Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.Redis/withHostPort", reqArgs);
    }

    /** Gets the PrimaryEndpoint property */
    public EndpointReference primaryEndpoint() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (EndpointReference) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.primaryEndpoint", reqArgs);
    }

    /** Gets the Host property */
    public EndpointReferenceExpression host() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (EndpointReferenceExpression) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.host", reqArgs);
    }

    /** Gets the Port property */
    public EndpointReferenceExpression port() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (EndpointReferenceExpression) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.port", reqArgs);
    }

    /** Gets the PasswordParameter property */
    public ParameterResource passwordParameter() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ParameterResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.passwordParameter", reqArgs);
    }

    /** Sets the PasswordParameter property */
    public RedisResource setPasswordParameter(ParameterResource value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.setPasswordParameter", reqArgs);
    }

    /** Gets the TlsEnabled property */
    public boolean tlsEnabled() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.tlsEnabled", reqArgs);
    }

    /** Gets the ConnectionStringExpression property */
    public ReferenceExpression connectionStringExpression() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ReferenceExpression) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.connectionStringExpression", reqArgs);
    }

    /** Gets the UriExpression property */
    public ReferenceExpression uriExpression() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (ReferenceExpression) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.uriExpression", reqArgs);
    }

    /** Gets the Entrypoint property */
    public String entrypoint() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.entrypoint", reqArgs);
    }

    /** Sets the Entrypoint property */
    public RedisResource setEntrypoint(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.setEntrypoint", reqArgs);
    }

    /** Gets the ShellExecution property */
    public boolean shellExecution() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.shellExecution", reqArgs);
    }

    /** Sets the ShellExecution property */
    public RedisResource setShellExecution(boolean value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (RedisResource) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.setShellExecution", reqArgs);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.ApplicationModel/RedisResource.name", reqArgs);
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

/** Wrapper for Aspire.Hosting.Yarp/Aspire.Hosting.Yarp.YarpCluster. */
class YarpCluster extends HandleWrapperBase {
    YarpCluster(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Sets the forwarder request configuration for the cluster. */
    public YarpCluster withForwarderRequestConfig(YarpForwarderRequestConfig config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("cluster", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/withForwarderRequestConfig", reqArgs);
    }

    /** Sets the HTTP client configuration for the cluster. */
    public YarpCluster withHttpClientConfig(YarpHttpClientConfig config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("cluster", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/withHttpClientConfig", reqArgs);
    }

    /** Sets the session affinity configuration for the cluster. */
    public YarpCluster withSessionAffinityConfig(YarpSessionAffinityConfig config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("cluster", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/withSessionAffinityConfig", reqArgs);
    }

    /** Sets the health check configuration for the cluster. */
    public YarpCluster withHealthCheckConfig(YarpHealthCheckConfig config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("cluster", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/withHealthCheckConfig", reqArgs);
    }

    /** Sets the load balancing policy for the cluster. */
    public YarpCluster withLoadBalancingPolicy(String policy) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("cluster", AspireClient.serializeValue(getHandle()));
        reqArgs.put("policy", AspireClient.serializeValue(policy));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/withLoadBalancingPolicy", reqArgs);
    }

    /** Sets metadata for the cluster. */
    public YarpCluster withMetadata(Map<String, String> metadata) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("cluster", AspireClient.serializeValue(getHandle()));
        reqArgs.put("metadata", AspireClient.serializeValue(metadata));
        return (YarpCluster) getClient().invokeCapability("Aspire.Hosting.Yarp/withClusterMetadata", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.Yarp/Aspire.Hosting.Yarp.YarpResource. */
class YarpResource extends ResourceBuilderBase {
    YarpResource(Handle handle, AspireClient client) {
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

    /** Configure the YARP resource. */
    public YarpResource withConfiguration(AspireAction1<IYarpConfigurationBuilder> configurationBuilder) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var configurationBuilderId = getClient().registerCallback(args -> {
            var obj = (IYarpConfigurationBuilder) args[0];
            configurationBuilder.invoke(obj);
            return null;
        });
        if (configurationBuilderId != null) {
            reqArgs.put("configurationBuilder", configurationBuilderId);
        }
        return (YarpResource) getClient().invokeCapability("Aspire.Hosting.Yarp/withConfiguration", reqArgs);
    }

    public YarpResource withHostPort() {
        return withHostPort(null);
    }

    /** Configures the host port that the YARP resource is exposed on instead of using randomly assigned port. */
    public YarpResource withHostPort(Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (YarpResource) getClient().invokeCapability("Aspire.Hosting.Yarp/withHostPort", reqArgs);
    }

    public YarpResource withHostHttpsPort() {
        return withHostHttpsPort(null);
    }

    /** Configures the host HTTPS port that the YARP resource is exposed on instead of using randomly assigned port. */
    public YarpResource withHostHttpsPort(Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (YarpResource) getClient().invokeCapability("Aspire.Hosting.Yarp/withHostHttpsPort", reqArgs);
    }

    /** Enables static file serving in the YARP resource. Static files are served from the wwwroot folder. */
    public YarpResource withStaticFiles() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (YarpResource) getClient().invokeCapability("Aspire.Hosting.Yarp/withStaticFiles1", reqArgs);
    }

    /** In publish mode, generates a Dockerfile that copies static files from the specified resource into /app/wwwroot. */
    public YarpResource publishWithStaticFiles(IResourceWithContainerFiles resourceWithFiles) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("resourceWithFiles", AspireClient.serializeValue(resourceWithFiles));
        return (YarpResource) getClient().invokeCapability("Aspire.Hosting.Yarp/publishWithStaticFiles", reqArgs);
    }

    public YarpResource publishWithStaticFiles(ResourceBuilderBase resourceWithFiles) {
        return publishWithStaticFiles(new IResourceWithContainerFiles(resourceWithFiles.getHandle(), resourceWithFiles.getClient()));
    }

}

/** Wrapper for Aspire.Hosting.Yarp/Aspire.Hosting.Yarp.YarpRoute. */
class YarpRoute extends HandleWrapperBase {
    YarpRoute(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Sets the route match criteria. */
    public YarpRoute withMatch(YarpRouteMatch match) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("match", AspireClient.serializeValue(match));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMatch", reqArgs);
    }

    /** Matches requests with the specified path pattern. */
    public YarpRoute withMatchPath(String path) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("path", AspireClient.serializeValue(path));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMatchPath", reqArgs);
    }

    /** Matches requests that use the specified HTTP methods. */
    public YarpRoute withMatchMethods(String[] methods) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("methods", AspireClient.serializeValue(methods));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMatchMethods", reqArgs);
    }

    /** Matches requests that contain the specified headers. */
    public YarpRoute withMatchHeaders(YarpRouteHeaderMatch[] headers) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headers", AspireClient.serializeValue(headers));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMatchHeaders", reqArgs);
    }

    /** Matches requests that contain the specified host headers. */
    public YarpRoute withMatchHosts(String[] hosts) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("hosts", AspireClient.serializeValue(hosts));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMatchHosts", reqArgs);
    }

    /** Matches requests that contain the specified query parameters. */
    public YarpRoute withMatchRouteQueryParameter(YarpRouteQueryParameterMatch[] queryParameters) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("queryParameters", AspireClient.serializeValue(queryParameters));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMatchRouteQueryParameter", reqArgs);
    }

    public YarpRoute withOrder() {
        return withOrder(null);
    }

    /** Sets the route order. */
    public YarpRoute withOrder(Double order) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (order != null) {
            reqArgs.put("order", AspireClient.serializeValue(order));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withOrder", reqArgs);
    }

    /** Sets the maximum request body size for the route. */
    public YarpRoute withMaxRequestBodySize(double maxRequestBodySize) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("maxRequestBodySize", AspireClient.serializeValue(maxRequestBodySize));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withMaxRequestBodySize", reqArgs);
    }

    /** Sets metadata for the route. */
    public YarpRoute withMetadata(Map<String, String> metadata) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("metadata", AspireClient.serializeValue(metadata));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withRouteMetadata", reqArgs);
    }

    /** Sets the transforms for the route. */
    public YarpRoute withTransforms(Map<String, String>[] transforms) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("transforms", AspireClient.serializeValue(transforms));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransforms", reqArgs);
    }

    /** Adds a transform to the route. */
    public YarpRoute withTransform(Map<String, String> transform) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("transform", AspireClient.serializeValue(transform));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransform", reqArgs);
    }

    /** Adds the transform which will add X-Forwarded-* headers. */
    public YarpRoute withTransformXForwarded(WithTransformXForwardedOptions options) {
        var headerPrefix = options == null ? null : options.getHeaderPrefix();
        var xDefault = options == null ? null : options.getXDefault();
        var xFor = options == null ? null : options.getXFor();
        var xHost = options == null ? null : options.getXHost();
        var xProto = options == null ? null : options.getXProto();
        var xPrefix = options == null ? null : options.getXPrefix();
        return withTransformXForwardedImpl(headerPrefix, xDefault, xFor, xHost, xProto, xPrefix);
    }

    public YarpRoute withTransformXForwarded() {
        return withTransformXForwarded(null);
    }

    /** Adds the transform which will add X-Forwarded-* headers. */
    private YarpRoute withTransformXForwardedImpl(String headerPrefix, ForwardedTransformActions xDefault, ForwardedTransformActions xFor, ForwardedTransformActions xHost, ForwardedTransformActions xProto, ForwardedTransformActions xPrefix) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (headerPrefix != null) {
            reqArgs.put("headerPrefix", AspireClient.serializeValue(headerPrefix));
        }
        if (xDefault != null) {
            reqArgs.put("xDefault", AspireClient.serializeValue(xDefault));
        }
        if (xFor != null) {
            reqArgs.put("xFor", AspireClient.serializeValue(xFor));
        }
        if (xHost != null) {
            reqArgs.put("xHost", AspireClient.serializeValue(xHost));
        }
        if (xProto != null) {
            reqArgs.put("xProto", AspireClient.serializeValue(xProto));
        }
        if (xPrefix != null) {
            reqArgs.put("xPrefix", AspireClient.serializeValue(xPrefix));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformXForwarded", reqArgs);
    }

    /** Adds the transform which will add the Forwarded header as defined by [RFC 7239](https://tools.ietf.org/html/rfc7239). */
    public YarpRoute withTransformForwarded(WithTransformForwardedOptions options) {
        var useHost = options == null ? null : options.getUseHost();
        var useProto = options == null ? null : options.getUseProto();
        var forFormat = options == null ? null : options.getForFormat();
        var byFormat = options == null ? null : options.getByFormat();
        var action = options == null ? null : options.getAction();
        return withTransformForwardedImpl(useHost, useProto, forFormat, byFormat, action);
    }

    public YarpRoute withTransformForwarded() {
        return withTransformForwarded(null);
    }

    /** Adds the transform which will add the Forwarded header as defined by [RFC 7239](https://tools.ietf.org/html/rfc7239). */
    private YarpRoute withTransformForwardedImpl(Boolean useHost, Boolean useProto, NodeFormat forFormat, NodeFormat byFormat, ForwardedTransformActions action) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (useHost != null) {
            reqArgs.put("useHost", AspireClient.serializeValue(useHost));
        }
        if (useProto != null) {
            reqArgs.put("useProto", AspireClient.serializeValue(useProto));
        }
        if (forFormat != null) {
            reqArgs.put("forFormat", AspireClient.serializeValue(forFormat));
        }
        if (byFormat != null) {
            reqArgs.put("byFormat", AspireClient.serializeValue(byFormat));
        }
        if (action != null) {
            reqArgs.put("action", AspireClient.serializeValue(action));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformForwarded", reqArgs);
    }

    /** Adds the transform which will set the given header with the Base64 encoded client certificate. */
    public YarpRoute withTransformClientCertHeader(String headerName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformClientCertHeader", reqArgs);
    }

    /** Adds the transform that will replace the HTTP method if it matches. */
    public YarpRoute withTransformHttpMethodChange(String fromHttpMethod, String toHttpMethod) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("fromHttpMethod", AspireClient.serializeValue(fromHttpMethod));
        reqArgs.put("toHttpMethod", AspireClient.serializeValue(toHttpMethod));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformHttpMethodChange", reqArgs);
    }

    /** Adds the transform which sets the request path with the given value. */
    public YarpRoute withTransformPathSet(String path) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("path", AspireClient.serializeValue(path));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformPathSet", reqArgs);
    }

    /** Adds the transform which will prefix the request path with the given value. */
    public YarpRoute withTransformPathPrefix(String prefix) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("prefix", AspireClient.serializeValue(prefix));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformPathPrefix", reqArgs);
    }

    /** Adds the transform which will remove the matching prefix from the request path. */
    public YarpRoute withTransformPathRemovePrefix(String prefix) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("prefix", AspireClient.serializeValue(prefix));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformPathRemovePrefix", reqArgs);
    }

    /** Adds the transform which will set the request path with route values. */
    public YarpRoute withTransformPathRouteValues(String pattern) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("pattern", AspireClient.serializeValue(pattern));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformPathRouteValues", reqArgs);
    }

    public YarpRoute withTransformQueryValue(String queryKey, String value) {
        return withTransformQueryValue(queryKey, value, null);
    }

    /** Adds the transform that will append or set the query parameter from the given value. */
    public YarpRoute withTransformQueryValue(String queryKey, String value, Boolean append) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("queryKey", AspireClient.serializeValue(queryKey));
        reqArgs.put("value", AspireClient.serializeValue(value));
        if (append != null) {
            reqArgs.put("append", AspireClient.serializeValue(append));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformQueryValue", reqArgs);
    }

    public YarpRoute withTransformQueryRouteValue(String queryKey, String routeValueKey) {
        return withTransformQueryRouteValue(queryKey, routeValueKey, null);
    }

    /** Adds the transform that will append or set the query parameter from a route value. */
    public YarpRoute withTransformQueryRouteValue(String queryKey, String routeValueKey, Boolean append) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("queryKey", AspireClient.serializeValue(queryKey));
        reqArgs.put("routeValueKey", AspireClient.serializeValue(routeValueKey));
        if (append != null) {
            reqArgs.put("append", AspireClient.serializeValue(append));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformQueryRouteValue", reqArgs);
    }

    /** Adds the transform that will remove the given query key. */
    public YarpRoute withTransformQueryRemoveKey(String queryKey) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("queryKey", AspireClient.serializeValue(queryKey));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformQueryRemoveKey", reqArgs);
    }

    public YarpRoute withTransformCopyRequestHeaders() {
        return withTransformCopyRequestHeaders(null);
    }

    /** Adds the transform which will enable or suppress copying request headers to the proxy request. */
    public YarpRoute withTransformCopyRequestHeaders(Boolean copy) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (copy != null) {
            reqArgs.put("copy", AspireClient.serializeValue(copy));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformCopyRequestHeaders", reqArgs);
    }

    public YarpRoute withTransformUseOriginalHostHeader() {
        return withTransformUseOriginalHostHeader(null);
    }

    /** Adds the transform which will copy the incoming request Host header to the proxy request. */
    public YarpRoute withTransformUseOriginalHostHeader(Boolean useOriginal) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (useOriginal != null) {
            reqArgs.put("useOriginal", AspireClient.serializeValue(useOriginal));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformUseOriginalHostHeader", reqArgs);
    }

    public YarpRoute withTransformRequestHeader(String headerName, String value) {
        return withTransformRequestHeader(headerName, value, null);
    }

    /** Adds the transform which will append or set the request header. */
    public YarpRoute withTransformRequestHeader(String headerName, String value, Boolean append) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        reqArgs.put("value", AspireClient.serializeValue(value));
        if (append != null) {
            reqArgs.put("append", AspireClient.serializeValue(append));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformRequestHeader", reqArgs);
    }

    public YarpRoute withTransformRequestHeaderRouteValue(String headerName, String routeValueKey) {
        return withTransformRequestHeaderRouteValue(headerName, routeValueKey, null);
    }

    /** Adds the transform which will append or set the request header from a route value. */
    public YarpRoute withTransformRequestHeaderRouteValue(String headerName, String routeValueKey, Boolean append) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        reqArgs.put("routeValueKey", AspireClient.serializeValue(routeValueKey));
        if (append != null) {
            reqArgs.put("append", AspireClient.serializeValue(append));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformRequestHeaderRouteValue", reqArgs);
    }

    /** Adds the transform which will remove the request header. */
    public YarpRoute withTransformRequestHeaderRemove(String headerName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformRequestHeaderRemove", reqArgs);
    }

    /** Adds the transform which will only copy the allowed request headers. Other transforms */
    public YarpRoute withTransformRequestHeadersAllowed(String[] allowedHeaders) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("allowedHeaders", AspireClient.serializeValue(allowedHeaders));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformRequestHeadersAllowed", reqArgs);
    }

    public YarpRoute withTransformCopyResponseHeaders() {
        return withTransformCopyResponseHeaders(null);
    }

    /** Adds the transform which will enable or suppress copying response headers to the client response. */
    public YarpRoute withTransformCopyResponseHeaders(Boolean copy) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (copy != null) {
            reqArgs.put("copy", AspireClient.serializeValue(copy));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformCopyResponseHeaders", reqArgs);
    }

    public YarpRoute withTransformCopyResponseTrailers() {
        return withTransformCopyResponseTrailers(null);
    }

    /** Adds the transform which will enable or suppress copying response trailers to the client response. */
    public YarpRoute withTransformCopyResponseTrailers(Boolean copy) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        if (copy != null) {
            reqArgs.put("copy", AspireClient.serializeValue(copy));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformCopyResponseTrailers", reqArgs);
    }

    /** Adds the transform which will append or set the response header. */
    public YarpRoute withTransformResponseHeader(String headerName, String value, WithTransformResponseHeaderOptions options) {
        var append = options == null ? null : options.getAppend();
        var condition = options == null ? null : options.getCondition();
        return withTransformResponseHeaderImpl(headerName, value, append, condition);
    }

    public YarpRoute withTransformResponseHeader(String headerName, String value) {
        return withTransformResponseHeader(headerName, value, null);
    }

    /** Adds the transform which will append or set the response header. */
    private YarpRoute withTransformResponseHeaderImpl(String headerName, String value, Boolean append, ResponseCondition condition) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        reqArgs.put("value", AspireClient.serializeValue(value));
        if (append != null) {
            reqArgs.put("append", AspireClient.serializeValue(append));
        }
        if (condition != null) {
            reqArgs.put("condition", AspireClient.serializeValue(condition));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformResponseHeader", reqArgs);
    }

    public YarpRoute withTransformResponseHeaderRemove(String headerName) {
        return withTransformResponseHeaderRemove(headerName, null);
    }

    /** Adds the transform which will remove the response header. */
    public YarpRoute withTransformResponseHeaderRemove(String headerName, ResponseCondition condition) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        if (condition != null) {
            reqArgs.put("condition", AspireClient.serializeValue(condition));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformResponseHeaderRemove", reqArgs);
    }

    /** Adds the transform which will only copy the allowed response headers. Other transforms */
    public YarpRoute withTransformResponseHeadersAllowed(String[] allowedHeaders) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("allowedHeaders", AspireClient.serializeValue(allowedHeaders));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformResponseHeadersAllowed", reqArgs);
    }

    /** Adds the transform which will append or set the response trailer. */
    public YarpRoute withTransformResponseTrailer(String headerName, String value, WithTransformResponseTrailerOptions options) {
        var append = options == null ? null : options.getAppend();
        var condition = options == null ? null : options.getCondition();
        return withTransformResponseTrailerImpl(headerName, value, append, condition);
    }

    public YarpRoute withTransformResponseTrailer(String headerName, String value) {
        return withTransformResponseTrailer(headerName, value, null);
    }

    /** Adds the transform which will append or set the response trailer. */
    private YarpRoute withTransformResponseTrailerImpl(String headerName, String value, Boolean append, ResponseCondition condition) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        reqArgs.put("value", AspireClient.serializeValue(value));
        if (append != null) {
            reqArgs.put("append", AspireClient.serializeValue(append));
        }
        if (condition != null) {
            reqArgs.put("condition", AspireClient.serializeValue(condition));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformResponseTrailer", reqArgs);
    }

    public YarpRoute withTransformResponseTrailerRemove(String headerName) {
        return withTransformResponseTrailerRemove(headerName, null);
    }

    /** Adds the transform which will remove the response trailer. */
    public YarpRoute withTransformResponseTrailerRemove(String headerName, ResponseCondition condition) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("headerName", AspireClient.serializeValue(headerName));
        if (condition != null) {
            reqArgs.put("condition", AspireClient.serializeValue(condition));
        }
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformResponseTrailerRemove", reqArgs);
    }

    /** Adds the transform which will only copy the allowed response trailers. Other transforms */
    public YarpRoute withTransformResponseTrailersAllowed(String[] allowedHeaders) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("route", AspireClient.serializeValue(getHandle()));
        reqArgs.put("allowedHeaders", AspireClient.serializeValue(allowedHeaders));
        return (YarpRoute) getClient().invokeCapability("Aspire.Hosting.Yarp/withTransformResponseTrailersAllowed", reqArgs);
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
        AspireClient.registerHandleWrapper("Aspire.Hosting.Yarp/Aspire.Hosting.IYarpConfigurationBuilder", (h, c) -> new IYarpConfigurationBuilder(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Yarp/Aspire.Hosting.Yarp.YarpCluster", (h, c) -> new YarpCluster(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Yarp/Aspire.Hosting.Yarp.YarpRoute", (h, c) -> new YarpRoute(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery", (h, c) -> new IResourceWithServiceDiscovery(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Yarp/Aspire.Hosting.Yarp.YarpResource", (h, c) -> new YarpResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Redis/Aspire.Hosting.ApplicationModel.RedisResource", (h, c) -> new RedisResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisCommanderResource", (h, c) -> new RedisCommanderResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.Redis/Aspire.Hosting.Redis.RedisInsightResource", (h, c) -> new RedisInsightResource(h, c));
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

