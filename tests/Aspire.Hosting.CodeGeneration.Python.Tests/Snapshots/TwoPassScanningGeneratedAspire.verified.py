# aspire.py - Capability-based Aspire SDK
# GENERATED CODE - DO NOT EDIT

from __future__ import annotations

import os
import sys
from dataclasses import dataclass
from enum import Enum
from typing import Any, Callable, Dict, List

from transport import AspireClient, Handle, CapabilityError, register_callback, register_handle_wrapper, register_cancellation
from base import AspireDict, AspireList, ReferenceExpression, ref_expr, HandleWrapperBase, ResourceBuilderBase, serialize_value

# ============================================================================
# Enums
# ============================================================================

class ContainerLifetime(str, Enum):
    SESSION = "Session"
    PERSISTENT = "Persistent"

class ImagePullPolicy(str, Enum):
    DEFAULT = "Default"
    ALWAYS = "Always"
    MISSING = "Missing"
    NEVER = "Never"

class DistributedApplicationOperation(str, Enum):
    RUN = "Run"
    PUBLISH = "Publish"

class ProtocolType(str, Enum):
    IP = "IP"
    I_PV6_HOP_BY_HOP_OPTIONS = "IPv6HopByHopOptions"
    UNSPECIFIED = "Unspecified"
    ICMP = "Icmp"
    IGMP = "Igmp"
    GGP = "Ggp"
    I_PV4 = "IPv4"
    TCP = "Tcp"
    PUP = "Pup"
    UDP = "Udp"
    IDP = "Idp"
    I_PV6 = "IPv6"
    I_PV6_ROUTING_HEADER = "IPv6RoutingHeader"
    I_PV6_FRAGMENT_HEADER = "IPv6FragmentHeader"
    IP_SEC_ENCAPSULATING_SECURITY_PAYLOAD = "IPSecEncapsulatingSecurityPayload"
    IP_SEC_AUTHENTICATION_HEADER = "IPSecAuthenticationHeader"
    ICMP_V6 = "IcmpV6"
    I_PV6_NO_NEXT_HEADER = "IPv6NoNextHeader"
    I_PV6_DESTINATION_OPTIONS = "IPv6DestinationOptions"
    ND = "ND"
    RAW = "Raw"
    IPX = "Ipx"
    SPX = "Spx"
    SPX_II = "SpxII"
    UNKNOWN = "Unknown"

class EndpointProperty(str, Enum):
    URL = "Url"
    HOST = "Host"
    IPV4_HOST = "IPV4Host"
    PORT = "Port"
    SCHEME = "Scheme"
    TARGET_PORT = "TargetPort"
    HOST_AND_PORT = "HostAndPort"

class IconVariant(str, Enum):
    REGULAR = "Regular"
    FILLED = "Filled"

class UrlDisplayLocation(str, Enum):
    SUMMARY_AND_DETAILS = "SummaryAndDetails"
    DETAILS_ONLY = "DetailsOnly"

class TestPersistenceMode(str, Enum):
    NONE_ = "None"
    VOLUME = "Volume"
    BIND = "Bind"

class TestResourceStatus(str, Enum):
    PENDING = "Pending"
    RUNNING = "Running"
    STOPPED = "Stopped"
    FAILED = "Failed"

# ============================================================================
# DTOs
# ============================================================================

@dataclass
class CreateBuilderOptions:
    args: list[str]
    project_directory: str
    app_host_file_path: str
    container_registry_override: str
    disable_dashboard: bool
    dashboard_application_name: str
    allow_unsecured_transport: bool
    enable_resource_logging: bool

    def to_dict(self) -> Dict[str, Any]:
        return {
            "Args": serialize_value(self.args),
            "ProjectDirectory": serialize_value(self.project_directory),
            "AppHostFilePath": serialize_value(self.app_host_file_path),
            "ContainerRegistryOverride": serialize_value(self.container_registry_override),
            "DisableDashboard": serialize_value(self.disable_dashboard),
            "DashboardApplicationName": serialize_value(self.dashboard_application_name),
            "AllowUnsecuredTransport": serialize_value(self.allow_unsecured_transport),
            "EnableResourceLogging": serialize_value(self.enable_resource_logging),
        }

@dataclass
class ResourceEventDto:
    resource_name: str
    resource_id: str
    state: str
    state_style: str
    health_status: str
    exit_code: float

    def to_dict(self) -> Dict[str, Any]:
        return {
            "ResourceName": serialize_value(self.resource_name),
            "ResourceId": serialize_value(self.resource_id),
            "State": serialize_value(self.state),
            "StateStyle": serialize_value(self.state_style),
            "HealthStatus": serialize_value(self.health_status),
            "ExitCode": serialize_value(self.exit_code),
        }

@dataclass
class CommandOptions:
    description: str
    parameter: Any
    confirmation_message: str
    icon_name: str
    icon_variant: IconVariant
    is_highlighted: bool
    update_state: Any

    def to_dict(self) -> Dict[str, Any]:
        return {
            "Description": serialize_value(self.description),
            "Parameter": serialize_value(self.parameter),
            "ConfirmationMessage": serialize_value(self.confirmation_message),
            "IconName": serialize_value(self.icon_name),
            "IconVariant": serialize_value(self.icon_variant),
            "IsHighlighted": serialize_value(self.is_highlighted),
            "UpdateState": serialize_value(self.update_state),
        }

@dataclass
class ExecuteCommandResult:
    success: bool
    canceled: bool
    error_message: str

    def to_dict(self) -> Dict[str, Any]:
        return {
            "Success": serialize_value(self.success),
            "Canceled": serialize_value(self.canceled),
            "ErrorMessage": serialize_value(self.error_message),
        }

@dataclass
class ResourceUrlAnnotation:
    url: str
    display_text: str
    endpoint: EndpointReference
    display_location: UrlDisplayLocation

    def to_dict(self) -> Dict[str, Any]:
        return {
            "Url": serialize_value(self.url),
            "DisplayText": serialize_value(self.display_text),
            "Endpoint": serialize_value(self.endpoint),
            "DisplayLocation": serialize_value(self.display_location),
        }

@dataclass
class TestConfigDto:
    name: str
    port: float
    enabled: bool
    optional_field: str

    def to_dict(self) -> Dict[str, Any]:
        return {
            "Name": serialize_value(self.name),
            "Port": serialize_value(self.port),
            "Enabled": serialize_value(self.enabled),
            "OptionalField": serialize_value(self.optional_field),
        }

@dataclass
class TestNestedDto:
    id: str
    config: TestConfigDto
    tags: AspireList[str]
    counts: AspireDict[str, float]

    def to_dict(self) -> Dict[str, Any]:
        return {
            "Id": serialize_value(self.id),
            "Config": serialize_value(self.config),
            "Tags": serialize_value(self.tags),
            "Counts": serialize_value(self.counts),
        }

@dataclass
class TestDeeplyNestedDto:
    nested_data: AspireDict[str, AspireList[TestConfigDto]]
    metadata_array: list[AspireDict[str, str]]

    def to_dict(self) -> Dict[str, Any]:
        return {
            "NestedData": serialize_value(self.nested_data),
            "MetadataArray": serialize_value(self.metadata_array),
        }

# ============================================================================
# Handle Wrappers
# ============================================================================

class CommandLineArgsCallbackContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    @property
    def args(self) -> AspireList[Any]:
        """Gets the Args property"""
        if not hasattr(self, '_args'):
            self._args = AspireList(
                self._handle,
                self._client,
                "Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args"
            )
        return self._args

    def cancellation_token(self) -> CancellationToken:
        """Gets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.cancellationToken", args)

    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.executionContext", args)

    def set_execution_context(self, value: DistributedApplicationExecutionContext) -> CommandLineArgsCallbackContext:
        """Sets the ExecutionContext property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.setExecutionContext", args)


class ContainerResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_environment(self, name: str, value: str) -> IResourceWithEnvironment:
        """Sets an environment variable"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironment", args)

    def with_environment_expression(self, name: str, value: ReferenceExpression) -> IResourceWithEnvironment:
        """Adds an environment variable with a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)

    def with_environment_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)

    def with_environment_callback_async(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)

    def with_args(self, args: list[str]) -> IResourceWithArgs:
        """Adds arguments"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withArgs", args)

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallback", args)

    def with_args_callback_async(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)

    def with_reference(self, source: IResourceWithConnectionString, connection_name: str | None = None, optional: bool = False) -> IResourceWithEnvironment:
        """Adds a reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        if connection_name is not None:
            args["connectionName"] = serialize_value(connection_name)
        args["optional"] = serialize_value(optional)
        return self._client.invoke_capability("Aspire.Hosting/withReference", args)

    def with_service_reference(self, source: IResourceWithServiceDiscovery) -> IResourceWithEnvironment:
        """Adds a service discovery reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        return self._client.invoke_capability("Aspire.Hosting/withServiceReference", args)

    def with_endpoint(self, port: float | None = None, target_port: float | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True, is_external: bool | None = None, protocol: ProtocolType | None = None) -> IResourceWithEndpoints:
        """Adds a network endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if scheme is not None:
            args["scheme"] = serialize_value(scheme)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        if is_external is not None:
            args["isExternal"] = serialize_value(is_external)
        if protocol is not None:
            args["protocol"] = serialize_value(protocol)
        return self._client.invoke_capability("Aspire.Hosting/withEndpoint", args)

    def with_http_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTP endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)

    def with_https_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTPS endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)

    def with_external_http_endpoints(self) -> IResourceWithEndpoints:
        """Makes HTTP endpoints externally accessible"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/getEndpoint", args)

    def as_http2_service(self) -> IResourceWithEndpoints:
        """Configures resource for HTTP/2"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/asHttp2Service", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> IResourceWithEndpoints:
        """Adds a URL for a specific endpoint via factory callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)

    def wait_for(self, dependency: IResource) -> IResourceWithWaitSupport:
        """Waits for another resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting/waitFor", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def wait_for_completion(self, dependency: IResource, exit_code: float = 0) -> IResourceWithWaitSupport:
        """Waits for resource completion"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        args["exitCode"] = serialize_value(exit_code)
        return self._client.invoke_capability("Aspire.Hosting/waitForCompletion", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_http_health_check(self, path: str | None = None, status_code: float | None = None, endpoint_name: str | None = None) -> IResourceWithEndpoints:
        """Adds an HTTP health check"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if path is not None:
            args["path"] = serialize_value(path)
        if status_code is not None:
            args["statusCode"] = serialize_value(status_code)
        if endpoint_name is not None:
            args["endpointName"] = serialize_value(endpoint_name)
        return self._client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    def test_with_environment_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> IResourceWithEnvironment:
        """Configures environment with callback (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_environment_variables(self, variables: dict[str, str]) -> IResourceWithEnvironment:
        """Sets environment variables"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["variables"] = serialize_value(variables)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)


class DistributedApplication(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def run(self, cancellation_token: CancellationToken | None = None) -> None:
        """Runs the distributed application"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        cancellation_token_id = register_cancellation(cancellation_token, self._client) if cancellation_token is not None else None
        if cancellation_token_id is not None:
            args["cancellationToken"] = cancellation_token_id
        self._client.invoke_capability("Aspire.Hosting/run", args)
        return None


class DistributedApplicationEventSubscription(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class DistributedApplicationExecutionContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def publisher_name(self) -> str:
        """Gets the PublisherName property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.publisherName", args)

    def set_publisher_name(self, value: str) -> DistributedApplicationExecutionContext:
        """Sets the PublisherName property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName", args)

    def operation(self) -> DistributedApplicationOperation:
        """Gets the Operation property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.operation", args)

    def is_publish_mode(self) -> bool:
        """Gets the IsPublishMode property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode", args)

    def is_run_mode(self) -> bool:
        """Gets the IsRunMode property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode", args)


class DistributedApplicationExecutionContextOptions(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class DistributedApplicationResourceEventSubscription(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class EndpointReference(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def endpoint_name(self) -> str:
        """Gets the EndpointName property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.endpointName", args)

    def error_message(self) -> str:
        """Gets the ErrorMessage property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage", args)

    def set_error_message(self, value: str) -> EndpointReference:
        """Sets the ErrorMessage property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage", args)

    def is_allocated(self) -> bool:
        """Gets the IsAllocated property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated", args)

    def exists(self) -> bool:
        """Gets the Exists property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.exists", args)

    def is_http(self) -> bool:
        """Gets the IsHttp property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttp", args)

    def is_https(self) -> bool:
        """Gets the IsHttps property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttps", args)

    def port(self) -> float:
        """Gets the Port property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.port", args)

    def target_port(self) -> float:
        """Gets the TargetPort property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.targetPort", args)

    def host(self) -> str:
        """Gets the Host property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.host", args)

    def scheme(self) -> str:
        """Gets the Scheme property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.scheme", args)

    def url(self) -> str:
        """Gets the Url property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.url", args)

    def get_value_async(self, cancellation_token: CancellationToken | None = None) -> str:
        """Gets the URL of the endpoint asynchronously"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        cancellation_token_id = register_cancellation(cancellation_token, self._client) if cancellation_token is not None else None
        if cancellation_token_id is not None:
            args["cancellationToken"] = cancellation_token_id
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/getValueAsync", args)


class EndpointReferenceExpression(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def endpoint(self) -> EndpointReference:
        """Gets the Endpoint property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.endpoint", args)

    def property(self) -> EndpointProperty:
        """Gets the Property property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.property", args)

    def value_expression(self) -> str:
        """Gets the ValueExpression property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.valueExpression", args)


class EnvironmentCallbackContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    @property
    def environment_variables(self) -> AspireDict[str, str | ReferenceExpression]:
        """Gets the EnvironmentVariables property"""
        if not hasattr(self, '_environment_variables'):
            self._environment_variables = AspireDict(
                self._handle,
                self._client,
                "Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables"
            )
        return self._environment_variables

    def cancellation_token(self) -> CancellationToken:
        """Gets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken", args)

    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext", args)


class ExecutableResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_executable_command(self, command: str) -> ExecutableResource:
        """Sets the executable command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["command"] = serialize_value(command)
        return self._client.invoke_capability("Aspire.Hosting/withExecutableCommand", args)

    def with_working_directory(self, working_directory: str) -> ExecutableResource:
        """Sets the executable working directory"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["workingDirectory"] = serialize_value(working_directory)
        return self._client.invoke_capability("Aspire.Hosting/withWorkingDirectory", args)

    def with_environment(self, name: str, value: str) -> IResourceWithEnvironment:
        """Sets an environment variable"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironment", args)

    def with_environment_expression(self, name: str, value: ReferenceExpression) -> IResourceWithEnvironment:
        """Adds an environment variable with a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)

    def with_environment_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)

    def with_environment_callback_async(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)

    def with_args(self, args: list[str]) -> IResourceWithArgs:
        """Adds arguments"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withArgs", args)

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallback", args)

    def with_args_callback_async(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)

    def with_reference(self, source: IResourceWithConnectionString, connection_name: str | None = None, optional: bool = False) -> IResourceWithEnvironment:
        """Adds a reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        if connection_name is not None:
            args["connectionName"] = serialize_value(connection_name)
        args["optional"] = serialize_value(optional)
        return self._client.invoke_capability("Aspire.Hosting/withReference", args)

    def with_service_reference(self, source: IResourceWithServiceDiscovery) -> IResourceWithEnvironment:
        """Adds a service discovery reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        return self._client.invoke_capability("Aspire.Hosting/withServiceReference", args)

    def with_endpoint(self, port: float | None = None, target_port: float | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True, is_external: bool | None = None, protocol: ProtocolType | None = None) -> IResourceWithEndpoints:
        """Adds a network endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if scheme is not None:
            args["scheme"] = serialize_value(scheme)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        if is_external is not None:
            args["isExternal"] = serialize_value(is_external)
        if protocol is not None:
            args["protocol"] = serialize_value(protocol)
        return self._client.invoke_capability("Aspire.Hosting/withEndpoint", args)

    def with_http_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTP endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)

    def with_https_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTPS endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)

    def with_external_http_endpoints(self) -> IResourceWithEndpoints:
        """Makes HTTP endpoints externally accessible"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/getEndpoint", args)

    def as_http2_service(self) -> IResourceWithEndpoints:
        """Configures resource for HTTP/2"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/asHttp2Service", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> IResourceWithEndpoints:
        """Adds a URL for a specific endpoint via factory callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)

    def wait_for(self, dependency: IResource) -> IResourceWithWaitSupport:
        """Waits for another resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting/waitFor", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def wait_for_completion(self, dependency: IResource, exit_code: float = 0) -> IResourceWithWaitSupport:
        """Waits for resource completion"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        args["exitCode"] = serialize_value(exit_code)
        return self._client.invoke_capability("Aspire.Hosting/waitForCompletion", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_http_health_check(self, path: str | None = None, status_code: float | None = None, endpoint_name: str | None = None) -> IResourceWithEndpoints:
        """Adds an HTTP health check"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if path is not None:
            args["path"] = serialize_value(path)
        if status_code is not None:
            args["statusCode"] = serialize_value(status_code)
        if endpoint_name is not None:
            args["endpointName"] = serialize_value(endpoint_name)
        return self._client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    def test_with_environment_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> IResourceWithEnvironment:
        """Configures environment with callback (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_environment_variables(self, variables: dict[str, str]) -> IResourceWithEnvironment:
        """Sets environment variables"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["variables"] = serialize_value(variables)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)


class ExecuteCommandContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def resource_name(self) -> str:
        """Gets the ResourceName property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.resourceName", args)

    def set_resource_name(self, value: str) -> ExecuteCommandContext:
        """Sets the ResourceName property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setResourceName", args)

    def cancellation_token(self) -> CancellationToken:
        """Gets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.cancellationToken", args)

    def set_cancellation_token(self, value: CancellationToken) -> ExecuteCommandContext:
        """Sets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        value_id = register_cancellation(value, self._client) if value is not None else None
        if value_id is not None:
            args["value"] = value_id
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setCancellationToken", args)


class IDistributedApplicationBuilder(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def add_container(self, name: str, image: str) -> ContainerResource:
        """Adds a container resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["image"] = serialize_value(image)
        return self._client.invoke_capability("Aspire.Hosting/addContainer", args)

    def add_executable(self, name: str, command: str, working_directory: str, args: list[str]) -> ExecutableResource:
        """Adds an executable resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["command"] = serialize_value(command)
        args["workingDirectory"] = serialize_value(working_directory)
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/addExecutable", args)

    def app_host_directory(self) -> str:
        """Gets the AppHostDirectory property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory", args)

    def eventing(self) -> IDistributedApplicationEventing:
        """Gets the Eventing property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/IDistributedApplicationBuilder.eventing", args)

    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/IDistributedApplicationBuilder.executionContext", args)

    def build(self) -> DistributedApplication:
        """Builds the distributed application"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/build", args)

    def add_parameter(self, name: str, secret: bool = False) -> ParameterResource:
        """Adds a parameter resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["secret"] = serialize_value(secret)
        return self._client.invoke_capability("Aspire.Hosting/addParameter", args)

    def add_connection_string(self, name: str, environment_variable_name: str | None = None) -> IResourceWithConnectionString:
        """Adds a connection string resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        if environment_variable_name is not None:
            args["environmentVariableName"] = serialize_value(environment_variable_name)
        return self._client.invoke_capability("Aspire.Hosting/addConnectionString", args)

    def add_project(self, name: str, project_path: str, launch_profile_name: str) -> ProjectResource:
        """Adds a .NET project resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["projectPath"] = serialize_value(project_path)
        args["launchProfileName"] = serialize_value(launch_profile_name)
        return self._client.invoke_capability("Aspire.Hosting/addProject", args)

    def add_test_redis(self, name: str, port: float | None = None) -> TestRedisResource:
        """Adds a test Redis resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        if port is not None:
            args["port"] = serialize_value(port)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/addTestRedis", args)

    def add_test_vault(self, name: str) -> TestVaultResource:
        """Adds a test vault resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/addTestVault", args)


class IDistributedApplicationEvent(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IDistributedApplicationEventing(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def unsubscribe(self, subscription: DistributedApplicationEventSubscription) -> None:
        """Invokes the Unsubscribe method"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["subscription"] = serialize_value(subscription)
        self._client.invoke_capability("Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe", args)
        return None


class IDistributedApplicationResourceEvent(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResourceWithArgs(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResourceWithConnectionString(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResourceWithEndpoints(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResourceWithEnvironment(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResourceWithServiceDiscovery(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class IResourceWithWaitSupport(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class ITestVaultResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class ParameterResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_description(self, description: str, enable_markdown: bool = False) -> ParameterResource:
        """Sets a parameter description"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["description"] = serialize_value(description)
        args["enableMarkdown"] = serialize_value(enable_markdown)
        return self._client.invoke_capability("Aspire.Hosting/withDescription", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)


class ProjectResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_replicas(self, replicas: float) -> ProjectResource:
        """Sets the number of replicas"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["replicas"] = serialize_value(replicas)
        return self._client.invoke_capability("Aspire.Hosting/withReplicas", args)

    def with_environment(self, name: str, value: str) -> IResourceWithEnvironment:
        """Sets an environment variable"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironment", args)

    def with_environment_expression(self, name: str, value: ReferenceExpression) -> IResourceWithEnvironment:
        """Adds an environment variable with a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)

    def with_environment_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)

    def with_environment_callback_async(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)

    def with_args(self, args: list[str]) -> IResourceWithArgs:
        """Adds arguments"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withArgs", args)

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallback", args)

    def with_args_callback_async(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)

    def with_reference(self, source: IResourceWithConnectionString, connection_name: str | None = None, optional: bool = False) -> IResourceWithEnvironment:
        """Adds a reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        if connection_name is not None:
            args["connectionName"] = serialize_value(connection_name)
        args["optional"] = serialize_value(optional)
        return self._client.invoke_capability("Aspire.Hosting/withReference", args)

    def with_service_reference(self, source: IResourceWithServiceDiscovery) -> IResourceWithEnvironment:
        """Adds a service discovery reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        return self._client.invoke_capability("Aspire.Hosting/withServiceReference", args)

    def with_endpoint(self, port: float | None = None, target_port: float | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True, is_external: bool | None = None, protocol: ProtocolType | None = None) -> IResourceWithEndpoints:
        """Adds a network endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if scheme is not None:
            args["scheme"] = serialize_value(scheme)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        if is_external is not None:
            args["isExternal"] = serialize_value(is_external)
        if protocol is not None:
            args["protocol"] = serialize_value(protocol)
        return self._client.invoke_capability("Aspire.Hosting/withEndpoint", args)

    def with_http_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTP endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)

    def with_https_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTPS endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)

    def with_external_http_endpoints(self) -> IResourceWithEndpoints:
        """Makes HTTP endpoints externally accessible"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/getEndpoint", args)

    def as_http2_service(self) -> IResourceWithEndpoints:
        """Configures resource for HTTP/2"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/asHttp2Service", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> IResourceWithEndpoints:
        """Adds a URL for a specific endpoint via factory callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)

    def wait_for(self, dependency: IResource) -> IResourceWithWaitSupport:
        """Waits for another resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting/waitFor", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def wait_for_completion(self, dependency: IResource, exit_code: float = 0) -> IResourceWithWaitSupport:
        """Waits for resource completion"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        args["exitCode"] = serialize_value(exit_code)
        return self._client.invoke_capability("Aspire.Hosting/waitForCompletion", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_http_health_check(self, path: str | None = None, status_code: float | None = None, endpoint_name: str | None = None) -> IResourceWithEndpoints:
        """Adds an HTTP health check"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if path is not None:
            args["path"] = serialize_value(path)
        if status_code is not None:
            args["statusCode"] = serialize_value(status_code)
        if endpoint_name is not None:
            args["endpointName"] = serialize_value(endpoint_name)
        return self._client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    def test_with_environment_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> IResourceWithEnvironment:
        """Configures environment with callback (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_environment_variables(self, variables: dict[str, str]) -> IResourceWithEnvironment:
        """Sets environment variables"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["variables"] = serialize_value(variables)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)


class ReferenceExpression(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

class ResourceUrlsCallbackContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    @property
    def urls(self) -> AspireList[ResourceUrlAnnotation]:
        """Gets the Urls property"""
        if not hasattr(self, '_urls'):
            self._urls = AspireList(
                self._handle,
                self._client,
                "Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls"
            )
        return self._urls

    def cancellation_token(self) -> CancellationToken:
        """Gets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.cancellationToken", args)

    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext", args)


class TestCallbackContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def name(self) -> str:
        """Gets the Name property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name", args)

    def set_name(self, value: str) -> TestCallbackContext:
        """Sets the Name property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName", args)

    def value(self) -> float:
        """Gets the Value property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value", args)

    def set_value(self, value: float) -> TestCallbackContext:
        """Sets the Value property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue", args)

    def cancellation_token(self) -> CancellationToken:
        """Gets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken", args)

    def set_cancellation_token(self, value: CancellationToken) -> TestCallbackContext:
        """Sets the CancellationToken property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        value_id = register_cancellation(value, self._client) if value is not None else None
        if value_id is not None:
            args["value"] = value_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken", args)


class TestCollectionContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    @property
    def items(self) -> AspireList[str]:
        """Gets the Items property"""
        if not hasattr(self, '_items'):
            self._items = AspireList(
                self._handle,
                self._client,
                "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items"
            )
        return self._items

    @property
    def metadata(self) -> AspireDict[str, str]:
        """Gets the Metadata property"""
        if not hasattr(self, '_metadata'):
            self._metadata = AspireDict(
                self._handle,
                self._client,
                "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata"
            )
        return self._metadata


class TestDatabaseResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_bind_mount(self, source: str, target: str, is_read_only: bool = False) -> ContainerResource:
        """Adds a bind mount"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        args["target"] = serialize_value(target)
        args["isReadOnly"] = serialize_value(is_read_only)
        return self._client.invoke_capability("Aspire.Hosting/withBindMount", args)

    def with_entrypoint(self, entrypoint: str) -> ContainerResource:
        """Sets the container entrypoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["entrypoint"] = serialize_value(entrypoint)
        return self._client.invoke_capability("Aspire.Hosting/withEntrypoint", args)

    def with_image_tag(self, tag: str) -> ContainerResource:
        """Sets the container image tag"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["tag"] = serialize_value(tag)
        return self._client.invoke_capability("Aspire.Hosting/withImageTag", args)

    def with_image_registry(self, registry: str) -> ContainerResource:
        """Sets the container image registry"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["registry"] = serialize_value(registry)
        return self._client.invoke_capability("Aspire.Hosting/withImageRegistry", args)

    def with_image(self, image: str, tag: str | None = None) -> ContainerResource:
        """Sets the container image"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["image"] = serialize_value(image)
        if tag is not None:
            args["tag"] = serialize_value(tag)
        return self._client.invoke_capability("Aspire.Hosting/withImage", args)

    def with_container_runtime_args(self, args: list[str]) -> ContainerResource:
        """Adds runtime arguments for the container"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withContainerRuntimeArgs", args)

    def with_lifetime(self, lifetime: ContainerLifetime) -> ContainerResource:
        """Sets the lifetime behavior of the container resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["lifetime"] = serialize_value(lifetime)
        return self._client.invoke_capability("Aspire.Hosting/withLifetime", args)

    def with_image_pull_policy(self, pull_policy: ImagePullPolicy) -> ContainerResource:
        """Sets the container image pull policy"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["pullPolicy"] = serialize_value(pull_policy)
        return self._client.invoke_capability("Aspire.Hosting/withImagePullPolicy", args)

    def with_container_name(self, name: str) -> ContainerResource:
        """Sets the container name"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/withContainerName", args)

    def with_environment(self, name: str, value: str) -> IResourceWithEnvironment:
        """Sets an environment variable"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironment", args)

    def with_environment_expression(self, name: str, value: ReferenceExpression) -> IResourceWithEnvironment:
        """Adds an environment variable with a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)

    def with_environment_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)

    def with_environment_callback_async(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)

    def with_args(self, args: list[str]) -> IResourceWithArgs:
        """Adds arguments"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withArgs", args)

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallback", args)

    def with_args_callback_async(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)

    def with_reference(self, source: IResourceWithConnectionString, connection_name: str | None = None, optional: bool = False) -> IResourceWithEnvironment:
        """Adds a reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        if connection_name is not None:
            args["connectionName"] = serialize_value(connection_name)
        args["optional"] = serialize_value(optional)
        return self._client.invoke_capability("Aspire.Hosting/withReference", args)

    def with_service_reference(self, source: IResourceWithServiceDiscovery) -> IResourceWithEnvironment:
        """Adds a service discovery reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        return self._client.invoke_capability("Aspire.Hosting/withServiceReference", args)

    def with_endpoint(self, port: float | None = None, target_port: float | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True, is_external: bool | None = None, protocol: ProtocolType | None = None) -> IResourceWithEndpoints:
        """Adds a network endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if scheme is not None:
            args["scheme"] = serialize_value(scheme)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        if is_external is not None:
            args["isExternal"] = serialize_value(is_external)
        if protocol is not None:
            args["protocol"] = serialize_value(protocol)
        return self._client.invoke_capability("Aspire.Hosting/withEndpoint", args)

    def with_http_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTP endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)

    def with_https_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTPS endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)

    def with_external_http_endpoints(self) -> IResourceWithEndpoints:
        """Makes HTTP endpoints externally accessible"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/getEndpoint", args)

    def as_http2_service(self) -> IResourceWithEndpoints:
        """Configures resource for HTTP/2"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/asHttp2Service", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> IResourceWithEndpoints:
        """Adds a URL for a specific endpoint via factory callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)

    def wait_for(self, dependency: IResource) -> IResourceWithWaitSupport:
        """Waits for another resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting/waitFor", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def wait_for_completion(self, dependency: IResource, exit_code: float = 0) -> IResourceWithWaitSupport:
        """Waits for resource completion"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        args["exitCode"] = serialize_value(exit_code)
        return self._client.invoke_capability("Aspire.Hosting/waitForCompletion", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_http_health_check(self, path: str | None = None, status_code: float | None = None, endpoint_name: str | None = None) -> IResourceWithEndpoints:
        """Adds an HTTP health check"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if path is not None:
            args["path"] = serialize_value(path)
        if status_code is not None:
            args["statusCode"] = serialize_value(status_code)
        if endpoint_name is not None:
            args["endpointName"] = serialize_value(endpoint_name)
        return self._client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def with_volume(self, target: str, name: str | None = None, is_read_only: bool = False) -> ContainerResource:
        """Adds a volume"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        args["target"] = serialize_value(target)
        if name is not None:
            args["name"] = serialize_value(name)
        args["isReadOnly"] = serialize_value(is_read_only)
        return self._client.invoke_capability("Aspire.Hosting/withVolume", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    def test_with_environment_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> IResourceWithEnvironment:
        """Configures environment with callback (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_environment_variables(self, variables: dict[str, str]) -> IResourceWithEnvironment:
        """Sets environment variables"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["variables"] = serialize_value(variables)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)


class TestEnvironmentContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def name(self) -> str:
        """Gets the Name property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name", args)

    def set_name(self, value: str) -> TestEnvironmentContext:
        """Sets the Name property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName", args)

    def description(self) -> str:
        """Gets the Description property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description", args)

    def set_description(self, value: str) -> TestEnvironmentContext:
        """Sets the Description property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription", args)

    def priority(self) -> float:
        """Gets the Priority property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority", args)

    def set_priority(self, value: float) -> TestEnvironmentContext:
        """Sets the Priority property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority", args)


class TestRedisResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_bind_mount(self, source: str, target: str, is_read_only: bool = False) -> ContainerResource:
        """Adds a bind mount"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        args["target"] = serialize_value(target)
        args["isReadOnly"] = serialize_value(is_read_only)
        return self._client.invoke_capability("Aspire.Hosting/withBindMount", args)

    def with_entrypoint(self, entrypoint: str) -> ContainerResource:
        """Sets the container entrypoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["entrypoint"] = serialize_value(entrypoint)
        return self._client.invoke_capability("Aspire.Hosting/withEntrypoint", args)

    def with_image_tag(self, tag: str) -> ContainerResource:
        """Sets the container image tag"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["tag"] = serialize_value(tag)
        return self._client.invoke_capability("Aspire.Hosting/withImageTag", args)

    def with_image_registry(self, registry: str) -> ContainerResource:
        """Sets the container image registry"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["registry"] = serialize_value(registry)
        return self._client.invoke_capability("Aspire.Hosting/withImageRegistry", args)

    def with_image(self, image: str, tag: str | None = None) -> ContainerResource:
        """Sets the container image"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["image"] = serialize_value(image)
        if tag is not None:
            args["tag"] = serialize_value(tag)
        return self._client.invoke_capability("Aspire.Hosting/withImage", args)

    def with_container_runtime_args(self, args: list[str]) -> ContainerResource:
        """Adds runtime arguments for the container"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withContainerRuntimeArgs", args)

    def with_lifetime(self, lifetime: ContainerLifetime) -> ContainerResource:
        """Sets the lifetime behavior of the container resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["lifetime"] = serialize_value(lifetime)
        return self._client.invoke_capability("Aspire.Hosting/withLifetime", args)

    def with_image_pull_policy(self, pull_policy: ImagePullPolicy) -> ContainerResource:
        """Sets the container image pull policy"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["pullPolicy"] = serialize_value(pull_policy)
        return self._client.invoke_capability("Aspire.Hosting/withImagePullPolicy", args)

    def with_container_name(self, name: str) -> ContainerResource:
        """Sets the container name"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/withContainerName", args)

    def with_environment(self, name: str, value: str) -> IResourceWithEnvironment:
        """Sets an environment variable"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironment", args)

    def with_environment_expression(self, name: str, value: ReferenceExpression) -> IResourceWithEnvironment:
        """Adds an environment variable with a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)

    def with_environment_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)

    def with_environment_callback_async(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)

    def with_args(self, args: list[str]) -> IResourceWithArgs:
        """Adds arguments"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withArgs", args)

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallback", args)

    def with_args_callback_async(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)

    def with_reference(self, source: IResourceWithConnectionString, connection_name: str | None = None, optional: bool = False) -> IResourceWithEnvironment:
        """Adds a reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        if connection_name is not None:
            args["connectionName"] = serialize_value(connection_name)
        args["optional"] = serialize_value(optional)
        return self._client.invoke_capability("Aspire.Hosting/withReference", args)

    def with_service_reference(self, source: IResourceWithServiceDiscovery) -> IResourceWithEnvironment:
        """Adds a service discovery reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        return self._client.invoke_capability("Aspire.Hosting/withServiceReference", args)

    def with_endpoint(self, port: float | None = None, target_port: float | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True, is_external: bool | None = None, protocol: ProtocolType | None = None) -> IResourceWithEndpoints:
        """Adds a network endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if scheme is not None:
            args["scheme"] = serialize_value(scheme)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        if is_external is not None:
            args["isExternal"] = serialize_value(is_external)
        if protocol is not None:
            args["protocol"] = serialize_value(protocol)
        return self._client.invoke_capability("Aspire.Hosting/withEndpoint", args)

    def with_http_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTP endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)

    def with_https_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTPS endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)

    def with_external_http_endpoints(self) -> IResourceWithEndpoints:
        """Makes HTTP endpoints externally accessible"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/getEndpoint", args)

    def as_http2_service(self) -> IResourceWithEndpoints:
        """Configures resource for HTTP/2"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/asHttp2Service", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> IResourceWithEndpoints:
        """Adds a URL for a specific endpoint via factory callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)

    def wait_for(self, dependency: IResource) -> IResourceWithWaitSupport:
        """Waits for another resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting/waitFor", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def wait_for_completion(self, dependency: IResource, exit_code: float = 0) -> IResourceWithWaitSupport:
        """Waits for resource completion"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        args["exitCode"] = serialize_value(exit_code)
        return self._client.invoke_capability("Aspire.Hosting/waitForCompletion", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_http_health_check(self, path: str | None = None, status_code: float | None = None, endpoint_name: str | None = None) -> IResourceWithEndpoints:
        """Adds an HTTP health check"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if path is not None:
            args["path"] = serialize_value(path)
        if status_code is not None:
            args["statusCode"] = serialize_value(status_code)
        if endpoint_name is not None:
            args["endpointName"] = serialize_value(endpoint_name)
        return self._client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def with_volume(self, target: str, name: str | None = None, is_read_only: bool = False) -> ContainerResource:
        """Adds a volume"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        args["target"] = serialize_value(target)
        if name is not None:
            args["name"] = serialize_value(name)
        args["isReadOnly"] = serialize_value(is_read_only)
        return self._client.invoke_capability("Aspire.Hosting/withVolume", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def add_test_child_database(self, name: str, database_name: str | None = None) -> TestDatabaseResource:
        """Adds a child database to a test Redis resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        if database_name is not None:
            args["databaseName"] = serialize_value(database_name)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/addTestChildDatabase", args)

    def with_persistence(self, mode: TestPersistenceMode = None) -> TestRedisResource:
        """Configures the Redis resource with persistence"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["mode"] = serialize_value(mode)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withPersistence", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    @property
    def get_tags(self) -> AspireList[str]:
        """Gets the tags for the resource"""
        if not hasattr(self, '_get_tags'):
            self._get_tags = AspireList(
                self._handle,
                self._client,
                "Aspire.Hosting.CodeGeneration.Python.Tests/getTags"
            )
        return self._get_tags

    @property
    def get_metadata(self) -> AspireDict[str, str]:
        """Gets the metadata for the resource"""
        if not hasattr(self, '_get_metadata'):
            self._get_metadata = AspireDict(
                self._handle,
                self._client,
                "Aspire.Hosting.CodeGeneration.Python.Tests/getMetadata"
            )
        return self._get_metadata

    def with_connection_string(self, connection_string: ReferenceExpression) -> IResourceWithConnectionString:
        """Sets the connection string using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["connectionString"] = serialize_value(connection_string)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionString", args)

    def test_with_environment_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> IResourceWithEnvironment:
        """Configures environment with callback (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def get_endpoints(self) -> list[str]:
        """Gets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/getEndpoints", args)

    def with_connection_string_direct(self, connection_string: str) -> IResourceWithConnectionString:
        """Sets connection string using direct interface target"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["connectionString"] = serialize_value(connection_string)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionStringDirect", args)

    def with_redis_specific(self, option: str) -> TestRedisResource:
        """Redis-specific configuration"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["option"] = serialize_value(option)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withRedisSpecific", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_environment_variables(self, variables: dict[str, str]) -> IResourceWithEnvironment:
        """Sets environment variables"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["variables"] = serialize_value(variables)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables", args)

    def get_status_async(self, cancellation_token: CancellationToken | None = None) -> str:
        """Gets the status of the resource asynchronously"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        cancellation_token_id = register_cancellation(cancellation_token, self._client) if cancellation_token is not None else None
        if cancellation_token_id is not None:
            args["cancellationToken"] = cancellation_token_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/getStatusAsync", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)

    def wait_for_ready_async(self, timeout: float, cancellation_token: CancellationToken | None = None) -> bool:
        """Waits for the resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["timeout"] = serialize_value(timeout)
        cancellation_token_id = register_cancellation(cancellation_token, self._client) if cancellation_token is not None else None
        if cancellation_token_id is not None:
            args["cancellationToken"] = cancellation_token_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/waitForReadyAsync", args)


class TestResourceContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def name(self) -> str:
        """Gets the Name property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name", args)

    def set_name(self, value: str) -> TestResourceContext:
        """Sets the Name property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName", args)

    def value(self) -> float:
        """Gets the Value property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value", args)

    def set_value(self, value: float) -> TestResourceContext:
        """Sets the Value property"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue", args)

    def get_value_async(self) -> str:
        """Invokes the GetValueAsync method"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync", args)

    def set_value_async(self, value: str) -> None:
        """Invokes the SetValueAsync method"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        args["value"] = serialize_value(value)
        self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync", args)
        return None

    def validate_async(self) -> bool:
        """Invokes the ValidateAsync method"""
        args: Dict[str, Any] = { "context": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync", args)


class TestVaultResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    def with_bind_mount(self, source: str, target: str, is_read_only: bool = False) -> ContainerResource:
        """Adds a bind mount"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        args["target"] = serialize_value(target)
        args["isReadOnly"] = serialize_value(is_read_only)
        return self._client.invoke_capability("Aspire.Hosting/withBindMount", args)

    def with_entrypoint(self, entrypoint: str) -> ContainerResource:
        """Sets the container entrypoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["entrypoint"] = serialize_value(entrypoint)
        return self._client.invoke_capability("Aspire.Hosting/withEntrypoint", args)

    def with_image_tag(self, tag: str) -> ContainerResource:
        """Sets the container image tag"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["tag"] = serialize_value(tag)
        return self._client.invoke_capability("Aspire.Hosting/withImageTag", args)

    def with_image_registry(self, registry: str) -> ContainerResource:
        """Sets the container image registry"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["registry"] = serialize_value(registry)
        return self._client.invoke_capability("Aspire.Hosting/withImageRegistry", args)

    def with_image(self, image: str, tag: str | None = None) -> ContainerResource:
        """Sets the container image"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["image"] = serialize_value(image)
        if tag is not None:
            args["tag"] = serialize_value(tag)
        return self._client.invoke_capability("Aspire.Hosting/withImage", args)

    def with_container_runtime_args(self, args: list[str]) -> ContainerResource:
        """Adds runtime arguments for the container"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withContainerRuntimeArgs", args)

    def with_lifetime(self, lifetime: ContainerLifetime) -> ContainerResource:
        """Sets the lifetime behavior of the container resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["lifetime"] = serialize_value(lifetime)
        return self._client.invoke_capability("Aspire.Hosting/withLifetime", args)

    def with_image_pull_policy(self, pull_policy: ImagePullPolicy) -> ContainerResource:
        """Sets the container image pull policy"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["pullPolicy"] = serialize_value(pull_policy)
        return self._client.invoke_capability("Aspire.Hosting/withImagePullPolicy", args)

    def with_container_name(self, name: str) -> ContainerResource:
        """Sets the container name"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/withContainerName", args)

    def with_environment(self, name: str, value: str) -> IResourceWithEnvironment:
        """Sets an environment variable"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironment", args)

    def with_environment_expression(self, name: str, value: ReferenceExpression) -> IResourceWithEnvironment:
        """Adds an environment variable with a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["value"] = serialize_value(value)
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)

    def with_environment_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)

    def with_environment_callback_async(self, callback: Callable[[EnvironmentCallbackContext], None]) -> IResourceWithEnvironment:
        """Sets environment variables via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)

    def with_args(self, args: list[str]) -> IResourceWithArgs:
        """Adds arguments"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["args"] = serialize_value(args)
        return self._client.invoke_capability("Aspire.Hosting/withArgs", args)

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallback", args)

    def with_args_callback_async(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> IResourceWithArgs:
        """Sets command-line arguments via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)

    def with_reference(self, source: IResourceWithConnectionString, connection_name: str | None = None, optional: bool = False) -> IResourceWithEnvironment:
        """Adds a reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        if connection_name is not None:
            args["connectionName"] = serialize_value(connection_name)
        args["optional"] = serialize_value(optional)
        return self._client.invoke_capability("Aspire.Hosting/withReference", args)

    def with_service_reference(self, source: IResourceWithServiceDiscovery) -> IResourceWithEnvironment:
        """Adds a service discovery reference to another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["source"] = serialize_value(source)
        return self._client.invoke_capability("Aspire.Hosting/withServiceReference", args)

    def with_endpoint(self, port: float | None = None, target_port: float | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True, is_external: bool | None = None, protocol: ProtocolType | None = None) -> IResourceWithEndpoints:
        """Adds a network endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if scheme is not None:
            args["scheme"] = serialize_value(scheme)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        if is_external is not None:
            args["isExternal"] = serialize_value(is_external)
        if protocol is not None:
            args["protocol"] = serialize_value(protocol)
        return self._client.invoke_capability("Aspire.Hosting/withEndpoint", args)

    def with_http_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTP endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)

    def with_https_endpoint(self, port: float | None = None, target_port: float | None = None, name: str | None = None, env: str | None = None, is_proxied: bool = True) -> IResourceWithEndpoints:
        """Adds an HTTPS endpoint"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if port is not None:
            args["port"] = serialize_value(port)
        if target_port is not None:
            args["targetPort"] = serialize_value(target_port)
        if name is not None:
            args["name"] = serialize_value(name)
        if env is not None:
            args["env"] = serialize_value(env)
        args["isProxied"] = serialize_value(is_proxied)
        return self._client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)

    def with_external_http_endpoints(self) -> IResourceWithEndpoints:
        """Makes HTTP endpoints externally accessible"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        return self._client.invoke_capability("Aspire.Hosting/getEndpoint", args)

    def as_http2_service(self) -> IResourceWithEndpoints:
        """Configures resource for HTTP/2"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/asHttp2Service", args)

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)

    def with_urls_callback_async(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> IResource:
        """Customizes displayed URLs via async callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)

    def with_url(self, url: str, display_text: str | None = None) -> IResource:
        """Adds or modifies displayed URLs"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrl", args)

    def with_url_expression(self, url: ReferenceExpression, display_text: str | None = None) -> IResource:
        """Adds a URL using a reference expression"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["url"] = serialize_value(url)
        if display_text is not None:
            args["displayText"] = serialize_value(display_text)
        return self._client.invoke_capability("Aspire.Hosting/withUrlExpression", args)

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> IResource:
        """Customizes the URL for a specific endpoint via callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> IResourceWithEndpoints:
        """Adds a URL for a specific endpoint via factory callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpointName"] = serialize_value(endpoint_name)
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)

    def wait_for(self, dependency: IResource) -> IResourceWithWaitSupport:
        """Waits for another resource to be ready"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting/waitFor", args)

    def with_explicit_start(self) -> IResource:
        """Prevents resource from starting automatically"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/withExplicitStart", args)

    def wait_for_completion(self, dependency: IResource, exit_code: float = 0) -> IResourceWithWaitSupport:
        """Waits for resource completion"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        args["exitCode"] = serialize_value(exit_code)
        return self._client.invoke_capability("Aspire.Hosting/waitForCompletion", args)

    def with_health_check(self, key: str) -> IResource:
        """Adds a health check by key"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["key"] = serialize_value(key)
        return self._client.invoke_capability("Aspire.Hosting/withHealthCheck", args)

    def with_http_health_check(self, path: str | None = None, status_code: float | None = None, endpoint_name: str | None = None) -> IResourceWithEndpoints:
        """Adds an HTTP health check"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if path is not None:
            args["path"] = serialize_value(path)
        if status_code is not None:
            args["statusCode"] = serialize_value(status_code)
        if endpoint_name is not None:
            args["endpointName"] = serialize_value(endpoint_name)
        return self._client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], command_options: CommandOptions | None = None) -> IResource:
        """Adds a resource command"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        args["displayName"] = serialize_value(display_name)
        execute_command_id = register_callback(execute_command) if execute_command is not None else None
        if execute_command_id is not None:
            args["executeCommand"] = execute_command_id
        if command_options is not None:
            args["commandOptions"] = serialize_value(command_options)
        return self._client.invoke_capability("Aspire.Hosting/withCommand", args)

    def with_parent_relationship(self, parent: IResource) -> IResource:
        """Sets the parent relationship"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["parent"] = serialize_value(parent)
        return self._client.invoke_capability("Aspire.Hosting/withParentRelationship", args)

    def with_volume(self, target: str, name: str | None = None, is_read_only: bool = False) -> ContainerResource:
        """Adds a volume"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        args["target"] = serialize_value(target)
        if name is not None:
            args["name"] = serialize_value(name)
        args["isReadOnly"] = serialize_value(is_read_only)
        return self._client.invoke_capability("Aspire.Hosting/withVolume", args)

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        args: Dict[str, Any] = { "resource": serialize_value(self._handle) }
        return self._client.invoke_capability("Aspire.Hosting/getResourceName", args)

    def with_optional_string(self, value: str | None = None, enabled: bool = True) -> IResource:
        """Adds an optional string parameter"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        if value is not None:
            args["value"] = serialize_value(value)
        args["enabled"] = serialize_value(enabled)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString", args)

    def with_config(self, config: TestConfigDto) -> IResource:
        """Configures the resource with a DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withConfig", args)

    def test_with_environment_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> IResourceWithEnvironment:
        """Configures environment with callback (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback", args)

    def with_created_at(self, created_at: str) -> IResource:
        """Sets the created timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["createdAt"] = serialize_value(created_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt", args)

    def with_modified_at(self, modified_at: str) -> IResource:
        """Sets the modified timestamp"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["modifiedAt"] = serialize_value(modified_at)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt", args)

    def with_correlation_id(self, correlation_id: str) -> IResource:
        """Sets the correlation ID"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["correlationId"] = serialize_value(correlation_id)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId", args)

    def with_optional_callback(self, callback: Callable[[TestCallbackContext], None] | None = None) -> IResource:
        """Configures with optional callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        callback_id = register_callback(callback) if callback is not None else None
        if callback_id is not None:
            args["callback"] = callback_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback", args)

    def with_status(self, status: TestResourceStatus) -> IResource:
        """Sets the resource status"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["status"] = serialize_value(status)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withStatus", args)

    def with_nested_config(self, config: TestNestedDto) -> IResource:
        """Configures with nested DTO"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["config"] = serialize_value(config)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig", args)

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> IResource:
        """Adds validation callback"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        validator_id = register_callback(validator) if validator is not None else None
        if validator_id is not None:
            args["validator"] = validator_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withValidator", args)

    def test_wait_for(self, dependency: IResource) -> IResource:
        """Waits for another resource (test version)"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor", args)

    def with_dependency(self, dependency: IResourceWithConnectionString) -> IResource:
        """Adds a dependency on another resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["dependency"] = serialize_value(dependency)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withDependency", args)

    def with_endpoints(self, endpoints: list[str]) -> IResource:
        """Sets the endpoints"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["endpoints"] = serialize_value(endpoints)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints", args)

    def with_environment_variables(self, variables: dict[str, str]) -> IResourceWithEnvironment:
        """Sets environment variables"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["variables"] = serialize_value(variables)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables", args)

    def with_cancellable_operation(self, operation: Callable[[CancellationToken], None]) -> IResource:
        """Performs a cancellable operation"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        operation_id = register_callback(operation) if operation is not None else None
        if operation_id is not None:
            args["operation"] = operation_id
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation", args)

    def with_vault_direct(self, option: str) -> ITestVaultResource:
        """Configures vault using direct interface target"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["option"] = serialize_value(option)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/withVaultDirect", args)


class UpdateCommandStateContext(HandleWrapperBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

    pass

# ============================================================================
# Handle wrapper registrations
# ============================================================================

register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplication", lambda handle, client: DistributedApplication(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext", lambda handle, client: DistributedApplicationExecutionContext(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions", lambda handle, client: DistributedApplicationExecutionContextOptions(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", lambda handle, client: IDistributedApplicationBuilder(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription", lambda handle, client: DistributedApplicationEventSubscription(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription", lambda handle, client: DistributedApplicationResourceEventSubscription(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent", lambda handle, client: IDistributedApplicationEvent(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent", lambda handle, client: IDistributedApplicationResourceEvent(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing", lambda handle, client: IDistributedApplicationEventing(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext", lambda handle, client: CommandLineArgsCallbackContext(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference", lambda handle, client: EndpointReference(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression", lambda handle, client: EndpointReferenceExpression(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext", lambda handle, client: EnvironmentCallbackContext(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression", lambda handle, client: ReferenceExpression(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext", lambda handle, client: UpdateCommandStateContext(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext", lambda handle, client: ExecuteCommandContext(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext", lambda handle, client: ResourceUrlsCallbackContext(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource", lambda handle, client: ContainerResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource", lambda handle, client: ExecutableResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource", lambda handle, client: ParameterResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString", lambda handle, client: IResourceWithConnectionString(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource", lambda handle, client: ProjectResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery", lambda handle, client: IResourceWithServiceDiscovery(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource", lambda handle, client: IResource(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", lambda handle, client: TestCallbackContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", lambda handle, client: TestResourceContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", lambda handle, client: TestEnvironmentContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", lambda handle, client: TestCollectionContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", lambda handle, client: TestRedisResource(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource", lambda handle, client: TestDatabaseResource(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource", lambda handle, client: TestVaultResource(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource", lambda handle, client: ITestVaultResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment", lambda handle, client: IResourceWithEnvironment(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs", lambda handle, client: IResourceWithArgs(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints", lambda handle, client: IResourceWithEndpoints(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport", lambda handle, client: IResourceWithWaitSupport(handle, client))
register_handle_wrapper("Aspire.Hosting/Dict<string,any>", lambda handle, client: AspireDict(handle, client))
register_handle_wrapper("Aspire.Hosting/List<any>", lambda handle, client: AspireList(handle, client))
register_handle_wrapper("Aspire.Hosting/Dict<string,string|Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression>", lambda handle, client: AspireDict(handle, client))
register_handle_wrapper("Aspire.Hosting/List<Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlAnnotation>", lambda handle, client: AspireList(handle, client))
register_handle_wrapper("Aspire.Hosting/List<string>", lambda handle, client: AspireList(handle, client))
register_handle_wrapper("Aspire.Hosting/Dict<string,string>", lambda handle, client: AspireDict(handle, client))

# ============================================================================
# Connection Helpers
# ============================================================================

def connect() -> AspireClient:
    socket_path = os.environ.get("REMOTE_APP_HOST_SOCKET_PATH")
    if not socket_path:
        raise RuntimeError("REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Run this application using `aspire run`.")
    client = AspireClient(socket_path)
    client.connect()
    client.on_disconnect(lambda: sys.exit(1))
    return client

def create_builder(options: Any | None = None) -> IDistributedApplicationBuilder:
    client = connect()
    resolved_options: Dict[str, Any] = {}
    if options is not None:
        if hasattr(options, "to_dict"):
            resolved_options.update(options.to_dict())
        elif isinstance(options, dict):
            resolved_options.update(options)
    resolved_options.setdefault("Args", sys.argv[1:])
    resolved_options.setdefault("ProjectDirectory", os.environ.get("ASPIRE_PROJECT_DIRECTORY", os.getcwd()))
    result = client.invoke_capability("Aspire.Hosting/createBuilderWithOptions", {"options": resolved_options})
    return result

# Re-export commonly used types
CapabilityError = CapabilityError
Handle = Handle
ReferenceExpression = ReferenceExpression
ref_expr = ref_expr

