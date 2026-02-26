#   -------------------------------------------------------------
#   Copyright (c) Microsoft Corporation. All rights reserved.
#   Licensed under the MIT License. See LICENSE in project root for information.
#
#   This is a generated file. Any modifications may be overwritten.
#   -------------------------------------------------------------

from __future__ import annotations

import os
import sys
from functools import cached_property
from abc import ABC, abstractmethod
from contextlib import AbstractContextManager
from re import compile
from dataclasses import dataclass
from warnings import warn
from collections.abc import Iterable, Mapping, Callable
from typing import (
    Any, Unpack, Self, Literal, TypedDict, Annotated, Required,
    Generic, TypeVar, get_origin, get_args, get_type_hints, cast
)

from ._base import (
    Handle,
    AspireClient,
    ReferenceExpression,
    ref_expr,
    AspireList,
    AspireDict,
)
from ._transport import (
    _register_handle_wrapper,
    _CallbackCancelled,
    AspireError,
)

uncached_property = property


def _validate_type(arg: Any, expected_type: Any) -> bool:
    if get_origin(expected_type) is Iterable:
        if isinstance(arg, str):
            return False
        item_type = get_args(expected_type)[0]
        if not isinstance(arg, Iterable):
            return False
        for item in arg:
            if not _validate_type(item, item_type):
                return False
    elif get_origin(expected_type) is Mapping:
        key_type, value_type = get_args(expected_type)
        if not isinstance(arg, Mapping):
            return False
        for key, value in arg.items():
            if not _validate_type(key, key_type):
                return False
            if not _validate_type(value, value_type):
                return False
    elif get_origin(expected_type) is Callable:
        return callable(arg)
    elif isinstance(arg, (tuple, Mapping)):
        return False
    elif get_origin(expected_type) is Literal:
        if arg not in get_args(expected_type):
            return False
    elif expected_type is None:
        if arg is not None:
            return False
    elif subtypes := get_args(expected_type):
        # This is probably a Union type
        return any([_validate_type(arg, subtype) for subtype in subtypes])
    elif not isinstance(arg, expected_type):
        return False
    return True


def _validate_tuple_types(args: Any, arg_types: tuple[Any, ...]) -> bool:
    if not isinstance(args, tuple):
        return False
    if len(args) != len(arg_types):
        return False
    for arg, expected_type in zip(args, arg_types):
        if not _validate_type(arg, expected_type):
            return False
    return True


def _validate_dict_types(args: Any, arg_types: Any) -> bool:
    if not isinstance(args, Mapping):
        return False
    type_hints = get_type_hints(arg_types, include_extras=True)
    for key, expected_type in type_hints.items():
        if get_origin(expected_type) is Required:
            expected_type = get_args(expected_type)[0]
            if key not in args:
                return False
        if key not in args:
            continue
        value = args[key]
        if not _validate_type(value, expected_type):
            return False
    return True


# ============================================================================
# Enum Types
# ============================================================================

ContainerLifetime = Literal["Session", "Persistent"]

DistributedApplicationOperation = Literal["Run", "Publish"]

EndpointProperty = Literal["Url", "Host", "IPV4Host", "Port", "Scheme", "TargetPort", "HostAndPort"]

IconVariant = Literal["Regular", "Filled"]

ImagePullPolicy = Literal["Default", "Always", "Missing", "Never"]

ProtocolType = Literal["IP", "IPv6HopByHopOptions", "Unspecified", "Icmp", "Igmp", "Ggp", "IPv4", "Tcp", "Pup", "Udp", "Idp", "IPv6", "IPv6RoutingHeader", "IPv6FragmentHeader", "IPSecEncapsulatingSecurityPayload", "IPSecAuthenticationHeader", "IcmpV6", "IPv6NoNextHeader", "IPv6DestinationOptions", "ND", "Raw", "Ipx", "Spx", "SpxII", "Unknown"]

TestPersistenceMode = Literal["None", "Volume", "Bind"]

TestResourceStatus = Literal["Pending", "Running", "Stopped", "Failed"]

UrlDisplayLocation = Literal["SummaryAndDetails", "DetailsOnly"]


# ============================================================================
# Method Parameters
# ============================================================================


class CommandParameters(TypedDict, total=False):
    name: Required[str]
    display_name: Required[str]
    execute_command: Required[Callable[[ExecuteCommandContext], ExecuteCommandResult]]
    command_options: CommandOptions


class OptionalStringParameters(TypedDict, total=False):
    value: str
    enabled: bool


class BindMountParameters(TypedDict, total=False):
    source: Required[str]
    target: Required[str]
    is_read_only: bool


class ReferenceParameters(TypedDict, total=False):
    source: Required[ResourceWithConnectionString]
    connection_name: str
    optional: bool


class EndpointParameters(TypedDict, total=False):
    port: int
    target_port: int
    scheme: str
    name: str
    env: str
    is_proxied: bool
    is_external: bool
    protocol: ProtocolType


class HttpEndpointParameters(TypedDict, total=False):
    port: int
    target_port: int
    name: str
    env: str
    is_proxied: bool


class HttpsEndpointParameters(TypedDict, total=False):
    port: int
    target_port: int
    name: str
    env: str
    is_proxied: bool


class HttpHealthCheckParameters(TypedDict, total=False):
    path: str
    status_code: int
    endpoint_name: str


class VolumeParameters(TypedDict, total=False):
    target: Required[str]
    name: str
    is_read_only: bool

# ============================================================================
# DTO Classes (Data Transfer Objects)
# ============================================================================

class CommandOptions(TypedDict, total=False):
    Description: str
    Parameter: Any
    ConfirmationMessage: str
    IconName: str
    IconVariant: IconVariant
    IsHighlighted: bool
    UpdateState: Any

class CreateBuilderOptions(TypedDict, total=False):
    Args: Iterable[str]
    ProjectDirectory: str
    AppHostFilePath: str
    ContainerRegistryOverride: str
    DisableDashboard: bool
    DashboardApplicationName: str
    AllowUnsecuredTransport: bool
    EnableResourceLogging: bool

class ExecuteCommandResult(TypedDict, total=False):
    Success: bool
    Canceled: bool
    ErrorMessage: str

class ResourceEventDto(TypedDict, total=False):
    ResourceName: str
    ResourceId: str
    State: str
    StateStyle: str
    HealthStatus: str
    ExitCode: int

class ResourceUrlAnnotation(TypedDict, total=False):
    Url: str
    DisplayText: str
    Endpoint: EndpointReference
    DisplayLocation: UrlDisplayLocation

class TestConfigDto(TypedDict, total=False):
    Name: str
    Port: int
    Enabled: bool
    OptionalField: str

class TestDeeplyNestedDto(TypedDict, total=False):
    NestedData: AspireDict[str, AspireList[TestConfigDto]]
    MetadataArray: Iterable[AspireDict[str, str]]

class TestNestedDto(TypedDict, total=False):
    Id: str
    Config: TestConfigDto
    Tags: AspireList[str]
    Counts: AspireDict[str, int]


# ============================================================================
# Type Classes
# ============================================================================

class CommandLineArgsCallbackContext:
    """Type class for CommandLineArgsCallbackContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"CommandLineArgsCallbackContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @cached_property
    def args(self) -> AspireList[Any]:
        """Gets the Args property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args',
            {'context': self._handle}
        )
        return cast(AspireList[Any], result)

    def cancel(self) -> None:
        """Cancel the operation."""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.cancellationToken',
            {'context': self._handle}
        )
        raise _CallbackCancelled(result)

    @uncached_property
    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.executionContext',
            {'context': self._handle}
        )
        return cast(DistributedApplicationExecutionContext, result)

    @execution_context.setter
    def execution_context(self, value: DistributedApplicationExecutionContext) -> None:
        """Sets the ExecutionContext property"""
        self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.setExecutionContext',
            {'context': self._handle, 'value': value}
        )


class DistributedApplication:
    """Type class for DistributedApplication."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"DistributedApplication(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    def run(self, *, timeout: int | None = None) -> None:
        """Runs the distributed application"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        if timeout is not None:
            rpc_args['cancellationToken'] = self._client.register_cancellation_token(timeout)
        self._client.invoke_capability(
            'Aspire.Hosting/run',
            rpc_args
        )


class DistributedApplicationBuilder:
    '''Type class for DistributedApplicationBuilder.'''

    def __init__(self, client: AspireClient, options: CreateBuilderOptions) -> None:
        self._handle = None
        self._client = client
        self._options = options

    @property
    def handle(self) -> Handle:
        '''Gets the underlying handle for the builder.'''
        if not self._handle:
            raise RuntimeError("Builder connection not initialized.")
        return self._handle

    def __enter__(self) -> DistributedApplicationBuilder:
        self._client.connect()
        self._handle = self._client.invoke_capability(
            'Aspire.Hosting/createBuilderWithOptions',
            {'options': self._options}
        )
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:
        self._client.disconnect()

    def run(self, *, timeout: int | None = None) -> None:
        '''Builds and runs the distributed application.'''
        app = self.build()
        app.run(timeout=timeout)

    @cached_property
    def app_host_dir(self) -> str:
        """Gets the AppHostDirectory property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory',
            {'context': self._handle}
        )
        return cast(str, result)

    @cached_property
    def eventing(self) -> DistributedApplicationEventing:
        """Gets the Eventing property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/IDistributedApplicationBuilder.eventing',
            {'context': self._handle}
        )
        return cast(DistributedApplicationEventing, result)

    @cached_property
    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/IDistributedApplicationBuilder.executionContext',
            {'context': self._handle}
        )
        return cast(DistributedApplicationExecutionContext, result)

    def build(self) -> DistributedApplication:
        """Builds the distributed application"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/build',
            rpc_args,
        )
        return cast(DistributedApplication, result)

    def add_container(self, name: str, image: str, **kwargs: Unpack["ContainerResourceOptions"]) -> ContainerResource:
        """Adds a container resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['image'] = image
        result = self._client.invoke_capability(
            'Aspire.Hosting/addContainer',
            rpc_args,
            kwargs,
        )
        return cast(ContainerResource, result)

    def add_executable(self, name: str, command: str, working_dir: str, args: Iterable[str], **kwargs: Unpack["ExecutableResourceOptions"]) -> ExecutableResource:
        """Adds an executable resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['command'] = command
        rpc_args['workingDirectory'] = working_dir
        rpc_args['args'] = args
        result = self._client.invoke_capability(
            'Aspire.Hosting/addExecutable',
            rpc_args,
            kwargs,
        )
        return cast(ExecutableResource, result)

    def add_parameter(self, name: str, *, secret: bool | None = None, **kwargs: Unpack["ParameterResourceOptions"]) -> ParameterResource:
        """Adds a parameter resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        if secret is not None:
            rpc_args['secret'] = secret
        result = self._client.invoke_capability(
            'Aspire.Hosting/addParameter',
            rpc_args,
            kwargs,
        )
        return cast(ParameterResource, result)

    def add_connection_string(self, name: str, *, env_var_name: str | None = None) -> ResourceWithConnectionString:
        """Adds a connection string resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        if env_var_name is not None:
            rpc_args['environmentVariableName'] = env_var_name
        result = self._client.invoke_capability(
            'Aspire.Hosting/addConnectionString',
            rpc_args,
        )
        return cast(ResourceWithConnectionString, result)

    def add_project(self, name: str, project_path: str, launch_profile_name: str, **kwargs: Unpack["ProjectResourceOptions"]) -> ProjectResource:
        """Adds a .NET project resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['projectPath'] = project_path
        rpc_args['launchProfileName'] = launch_profile_name
        result = self._client.invoke_capability(
            'Aspire.Hosting/addProject',
            rpc_args,
            kwargs,
        )
        return cast(ProjectResource, result)

    def add_test_redis(self, name: str, *, port: int | None = None, **kwargs: Unpack["TestRedisResourceOptions"]) -> TestRedisResource:
        """Adds a test Redis resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        if port is not None:
            rpc_args['port'] = port
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/addTestRedis',
            rpc_args,
            kwargs,
        )
        return cast(TestRedisResource, result)


class DistributedApplicationEventing:
    """Type class for DistributedApplicationEventing."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"DistributedApplicationEventing(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    def unsubscribe(self, subscription: DistributedApplicationEventSubscription) -> None:
        """Invokes the Unsubscribe method"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        rpc_args['subscription'] = subscription
        self._client.invoke_capability(
            'Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe',
            rpc_args
        )


class DistributedApplicationEventSubscription:
    """Type class for DistributedApplicationEventSubscription."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"DistributedApplicationEventSubscription(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle


class DistributedApplicationExecutionContext:
    """Type class for DistributedApplicationExecutionContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"DistributedApplicationExecutionContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @uncached_property
    def publisher_name(self) -> str:
        """Gets the PublisherName property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/DistributedApplicationExecutionContext.publisherName',
            {'context': self._handle}
        )
        return cast(str, result)

    @publisher_name.setter
    def publisher_name(self, value: str) -> None:
        """Sets the PublisherName property"""
        self._client.invoke_capability(
            'Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName',
            {'context': self._handle, 'value': value}
        )

    @cached_property
    def operation(self) -> DistributedApplicationOperation:
        """Gets the Operation property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/DistributedApplicationExecutionContext.operation',
            {'context': self._handle}
        )
        return cast(DistributedApplicationOperation, result)

    @cached_property
    def is_publish_mode(self) -> bool:
        """Gets the IsPublishMode property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode',
            {'context': self._handle}
        )
        return cast(bool, result)

    @cached_property
    def is_run_mode(self) -> bool:
        """Gets the IsRunMode property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode',
            {'context': self._handle}
        )
        return cast(bool, result)


class ManifestExpressionProvider(ABC):
    """Abstract base class for ManifestExpressionProvider."""

class ValueProvider(ABC):
    """Abstract base class for ValueProvider."""

class ValueWithReferences(ABC):
    """Abstract base class for ValueWithReferences."""

class EndpointReference:
    """Type class for EndpointReference."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"EndpointReference(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @cached_property
    def endpoint_name(self) -> str:
        """Gets the EndpointName property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.endpointName',
            {'context': self._handle}
        )
        return cast(str, result)

    @uncached_property
    def error_message(self) -> str:
        """Gets the ErrorMessage property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage',
            {'context': self._handle}
        )
        return cast(str, result)

    @error_message.setter
    def error_message(self, value: str) -> None:
        """Sets the ErrorMessage property"""
        self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage',
            {'context': self._handle, 'value': value}
        )

    @cached_property
    def is_allocated(self) -> bool:
        """Gets the IsAllocated property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated',
            {'context': self._handle}
        )
        return cast(bool, result)

    @cached_property
    def exists(self) -> bool:
        """Gets the Exists property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.exists',
            {'context': self._handle}
        )
        return cast(bool, result)

    @cached_property
    def is_http(self) -> bool:
        """Gets the IsHttp property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.isHttp',
            {'context': self._handle}
        )
        return cast(bool, result)

    @cached_property
    def is_https(self) -> bool:
        """Gets the IsHttps property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.isHttps',
            {'context': self._handle}
        )
        return cast(bool, result)

    @cached_property
    def port(self) -> int:
        """Gets the Port property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.port',
            {'context': self._handle}
        )
        return cast(int, result)

    @cached_property
    def target_port(self) -> int:
        """Gets the TargetPort property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.targetPort',
            {'context': self._handle}
        )
        return cast(int, result)

    @cached_property
    def host(self) -> str:
        """Gets the Host property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.host',
            {'context': self._handle}
        )
        return cast(str, result)

    @cached_property
    def scheme(self) -> str:
        """Gets the Scheme property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.scheme',
            {'context': self._handle}
        )
        return cast(str, result)

    @cached_property
    def url(self) -> str:
        """Gets the Url property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReference.url',
            {'context': self._handle}
        )
        return cast(str, result)

    def get_value(self, *, timeout: int | None = None) -> str:
        """Gets the URL of the endpoint asynchronously"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        if timeout is not None:
            rpc_args['cancellationToken'] = self._client.register_cancellation_token(timeout)
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/getValueAsync',
            rpc_args,
        )
        return result


class EndpointReferenceExpression:
    """Type class for EndpointReferenceExpression."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"EndpointReferenceExpression(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @cached_property
    def endpoint(self) -> EndpointReference:
        """Gets the Endpoint property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.endpoint',
            {'context': self._handle}
        )
        return cast(EndpointReference, result)

    @cached_property
    def property(self) -> EndpointProperty:
        """Gets the Property property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.property',
            {'context': self._handle}
        )
        return cast(EndpointProperty, result)

    @cached_property
    def value_expression(self) -> str:
        """Gets the ValueExpression property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.valueExpression',
            {'context': self._handle}
        )
        return cast(str, result)


class EnvironmentCallbackContext:
    """Type class for EnvironmentCallbackContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"EnvironmentCallbackContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @cached_property
    def env_vars(self) -> AspireDict[str, str | ReferenceExpression]:
        """Gets the EnvironmentVariables property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables',
            {'context': self._handle}
        )
        return cast(AspireDict[str, str | ReferenceExpression], result)

    def cancel(self) -> None:
        """Cancel the operation."""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken',
            {'context': self._handle}
        )
        raise _CallbackCancelled(result)

    @cached_property
    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext',
            {'context': self._handle}
        )
        return cast(DistributedApplicationExecutionContext, result)


class ExecuteCommandContext:
    """Type class for ExecuteCommandContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"ExecuteCommandContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @uncached_property
    def resource_name(self) -> str:
        """Gets the ResourceName property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.resourceName',
            {'context': self._handle}
        )
        return cast(str, result)

    @resource_name.setter
    def resource_name(self, value: str) -> None:
        """Sets the ResourceName property"""
        self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setResourceName',
            {'context': self._handle, 'value': value}
        )

    def cancel(self) -> None:
        """Cancel the operation."""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/ExecuteCommandContext.cancellationToken',
            {'context': self._handle}
        )
        raise _CallbackCancelled(result)


class ResourceUrlsCallbackContext:
    """Type class for ResourceUrlsCallbackContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"ResourceUrlsCallbackContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @cached_property
    def urls(self) -> AspireList[ResourceUrlAnnotation]:
        """Gets the Urls property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls',
            {'context': self._handle}
        )
        return cast(AspireList[ResourceUrlAnnotation], result)

    def cancel(self) -> None:
        """Cancel the operation."""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.cancellationToken',
            {'context': self._handle}
        )
        raise _CallbackCancelled(result)

    @cached_property
    def execution_context(self) -> DistributedApplicationExecutionContext:
        """Gets the ExecutionContext property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext',
            {'context': self._handle}
        )
        return cast(DistributedApplicationExecutionContext, result)


class TestCallbackContext:
    """Type class for TestCallbackContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"TestCallbackContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @uncached_property
    def name(self) -> str:
        """Gets the Name property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name',
            {'context': self._handle}
        )
        return cast(str, result)

    @name.setter
    def name(self, value: str) -> None:
        """Sets the Name property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName',
            {'context': self._handle, 'value': value}
        )

    @uncached_property
    def value(self) -> int:
        """Gets the Value property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value',
            {'context': self._handle}
        )
        return cast(int, result)

    @value.setter
    def value(self, value: int) -> None:
        """Sets the Value property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue',
            {'context': self._handle, 'value': value}
        )

    def cancel(self) -> None:
        """Cancel the operation."""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken',
            {'context': self._handle}
        )
        raise _CallbackCancelled(result)


class TestCollectionContext:
    """Type class for TestCollectionContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"TestCollectionContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @cached_property
    def items(self) -> AspireList[str]:
        """Gets the Items property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items',
            {'context': self._handle}
        )
        return cast(AspireList[str], result)

    @cached_property
    def metadata(self) -> AspireDict[str, str]:
        """Gets the Metadata property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata',
            {'context': self._handle}
        )
        return cast(AspireDict[str, str], result)


class TestEnvironmentContext:
    """Type class for TestEnvironmentContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"TestEnvironmentContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @uncached_property
    def name(self) -> str:
        """Gets the Name property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name',
            {'context': self._handle}
        )
        return cast(str, result)

    @name.setter
    def name(self, value: str) -> None:
        """Sets the Name property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName',
            {'context': self._handle, 'value': value}
        )

    @uncached_property
    def description(self) -> str:
        """Gets the Description property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description',
            {'context': self._handle}
        )
        return cast(str, result)

    @description.setter
    def description(self, value: str) -> None:
        """Sets the Description property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription',
            {'context': self._handle, 'value': value}
        )

    @uncached_property
    def priority(self) -> int:
        """Gets the Priority property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority',
            {'context': self._handle}
        )
        return cast(int, result)

    @priority.setter
    def priority(self, value: int) -> None:
        """Sets the Priority property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority',
            {'context': self._handle, 'value': value}
        )


class TestResourceContext:
    """Type class for TestResourceContext."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def __repr__(self) -> str:
        return f"TestResourceContext(handle={self._handle.handle_id})"

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    @uncached_property
    def name(self) -> str:
        """Gets the Name property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name',
            {'context': self._handle}
        )
        return cast(str, result)

    @name.setter
    def name(self, value: str) -> None:
        """Sets the Name property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName',
            {'context': self._handle, 'value': value}
        )

    @uncached_property
    def value(self) -> int:
        """Gets the Value property"""
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value',
            {'context': self._handle}
        )
        return cast(int, result)

    @value.setter
    def value(self, value: int) -> None:
        """Sets the Value property"""
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue',
            {'context': self._handle, 'value': value}
        )

    def get_value(self) -> str:
        """Invokes the GetValueAsync method"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync',
            rpc_args,
        )
        return result

    def set_value(self, value: str) -> None:
        """Invokes the SetValueAsync method"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        rpc_args['value'] = value
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync',
            rpc_args
        )

    def validate(self) -> bool:
        """Invokes the ValidateAsync method"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync',
            rpc_args,
        )
        return result


# ============================================================================
# Interface Classes
# ============================================================================

class Resource(ABC):
    """Abstract base class for Resource interface."""

    @abstractmethod
    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> Self:
        """Customizes displayed URLs via callback"""

    @abstractmethod
    def with_url(self, url: str, *, display_text: str | None = None) -> Self:
        """Adds or modifies displayed URLs"""

    @abstractmethod
    def with_url_expression(self, url: ReferenceExpression, *, display_text: str | None = None) -> Self:
        """Adds a URL using a reference expression"""

    @abstractmethod
    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> Self:
        """Customizes the URL for a specific endpoint via callback"""

    @abstractmethod
    def with_explicit_start(self) -> Self:
        """Prevents resource from starting automatically"""

    @abstractmethod
    def with_health_check(self, key: str) -> Self:
        """Adds a health check by key"""

    @abstractmethod
    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], *, command_options: CommandOptions | None = None) -> Self:
        """Adds a resource command"""

    @abstractmethod
    def with_parent_relationship(self, parent: Resource) -> Self:
        """Sets the parent relationship"""

    @abstractmethod
    def get_resource_name(self) -> str:
        """Gets the resource name"""

    @abstractmethod
    def with_optional_string(self, *, value: str | None = None, enabled: bool | None = None) -> Self:
        """Adds an optional string parameter"""

    @abstractmethod
    def with_config(self, config: TestConfigDto) -> Self:
        """Configures the resource with a DTO"""

    @abstractmethod
    def with_created_at(self, created_at: str) -> Self:
        """Sets the created timestamp"""

    @abstractmethod
    def with_modified_at(self, modified_at: str) -> Self:
        """Sets the modified timestamp"""

    @abstractmethod
    def with_correlation_id(self, correlation_id: str) -> Self:
        """Sets the correlation ID"""

    @abstractmethod
    def with_optional_callback(self, *, callback: Callable[[TestCallbackContext], None] | None = None) -> Self:
        """Configures with optional callback"""

    @abstractmethod
    def with_status(self, status: TestResourceStatus) -> Self:
        """Sets the resource status"""

    @abstractmethod
    def with_nested_config(self, config: TestNestedDto) -> Self:
        """Configures with nested DTO"""

    @abstractmethod
    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> Self:
        """Adds validation callback"""

    @abstractmethod
    def test_wait_for(self, dependency: Resource) -> Self:
        """Waits for another resource (test version)"""

    @abstractmethod
    def with_dependency(self, dependency: ResourceWithConnectionString) -> Self:
        """Adds a dependency on another resource"""

    @abstractmethod
    def with_endpoints(self, endpoints: Iterable[str]) -> Self:
        """Sets the endpoints"""

    @abstractmethod
    def with_cancellable_operation(self, operation: Callable[[int], None]) -> Self:
        """Performs a cancellable operation"""


class ComputeResource(Resource):
    """Abstract base class for ComputeResource interface."""


class ContainerFilesDestinationResource(Resource):
    """Abstract base class for ContainerFilesDestinationResource interface."""


class ResourceWithEnvironment(Resource):
    """Abstract base class for ResourceWithEnvironment interface."""

    @abstractmethod
    def with_env(self, name: str, value: str) -> Self:
        """Sets an environment variable"""

    @abstractmethod
    def with_env_expression(self, name: str, value: ReferenceExpression) -> Self:
        """Adds an environment variable with a reference expression"""

    @abstractmethod
    def with_env_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> Self:
        """Sets environment variables via callback"""

    @abstractmethod
    def with_reference(self, source: ResourceWithConnectionString, *, connection_name: str | None = None, optional: bool | None = None) -> Self:
        """Adds a reference to another resource"""

    @abstractmethod
    def with_service_reference(self, source: ResourceWithServiceDiscovery) -> Self:
        """Adds a service discovery reference to another resource"""

    @abstractmethod
    def test_with_env_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> Self:
        """Configures environment with callback (test version)"""

    @abstractmethod
    def with_env_vars(self, vars: Mapping[str, str]) -> Self:
        """Sets environment variables"""


class ResourceWithArgs(Resource):
    """Abstract base class for ResourceWithArgs interface."""

    @abstractmethod
    def with_args(self, args: Iterable[str]) -> Self:
        """Adds arguments"""

    @abstractmethod
    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> Self:
        """Sets command-line arguments via callback"""


class ResourceWithEndpoints(Resource):
    """Abstract base class for ResourceWithEndpoints interface."""

    @abstractmethod
    def with_endpoint(self, *, port: int | None = None, target_port: int | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None, is_external: bool | None = None, protocol: ProtocolType | None = None) -> Self:
        """Adds a network endpoint"""

    @abstractmethod
    def with_http_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTP endpoint"""

    @abstractmethod
    def with_https_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTPS endpoint"""

    @abstractmethod
    def with_external_http_endpoints(self) -> Self:
        """Makes HTTP endpoints externally accessible"""

    @abstractmethod
    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""

    @abstractmethod
    def as_http2_service(self) -> Self:
        """Configures resource for HTTP/2"""

    @abstractmethod
    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> Self:
        """Adds a URL for a specific endpoint via factory callback"""

    @abstractmethod
    def with_http_health_check(self, *, path: str | None = None, status_code: int | None = None, endpoint_name: str | None = None) -> Self:
        """Adds an HTTP health check"""


class ResourceWithWaitSupport(Resource):
    """Abstract base class for ResourceWithWaitSupport interface."""

    @abstractmethod
    def wait_for(self, dependency: Resource) -> Self:
        """Waits for another resource to be ready"""

    @abstractmethod
    def wait_for_completion(self, dependency: Resource, *, exit_code: int | None = None) -> Self:
        """Waits for resource completion"""


class ResourceWithProbes(Resource):
    """Abstract base class for ResourceWithProbes interface."""


class ResourceWithServiceDiscovery(ResourceWithEndpoints):
    """Abstract base class for ResourceWithServiceDiscovery interface."""


class ResourceWithConnectionString(Resource, ManifestExpressionProvider, ValueProvider, ValueWithReferences):
    """Abstract base class for ResourceWithConnectionString interface."""

    @abstractmethod
    def with_connection_string(self, connection_string: ReferenceExpression) -> Self:
        """Sets the connection string using a reference expression"""

    @abstractmethod
    def with_connection_string_direct(self, connection_string: str) -> Self:
        """Sets connection string using direct interface target"""


# ============================================================================
# Builder Classes
# ============================================================================

class _BaseResourceOptions(TypedDict, total=False):
    """Base resource options."""

    urls_callback: Callable[[ResourceUrlsCallbackContext], None]
    url: str | tuple[str, str]
    url_expression: ReferenceExpression | tuple[ReferenceExpression, str]
    url_for_endpoint: tuple[str, Callable[[ResourceUrlAnnotation], None]]
    explicit_start: Literal[True]
    health_check: str
    command: tuple[str, str, Callable[[ExecuteCommandContext], ExecuteCommandResult]] | CommandParameters
    parent_relationship: Resource
    optional_string: OptionalStringParameters | Literal[True]
    config: TestConfigDto
    created_at: str
    modified_at: str
    correlation_id: str
    optional_callback: Callable[[TestCallbackContext], None] | Literal[True]
    status: TestResourceStatus
    nested_config: TestNestedDto
    validator: Callable[[TestResourceContext], bool]
    test_wait_for: Resource
    dependency: ResourceWithConnectionString
    endpoints: Iterable[str]
    cancellable_operation: Callable[[int], None]

class _BaseResource(Resource):
    """Base resource class."""

    def _wrap_builder(self, builder: Any) -> Handle:
        if isinstance(builder, Handle):
            return builder
        return cast(Self, builder).handle

    @uncached_property
    def handle(self) -> Handle:
        """The underlying object reference handle."""
        return self._handle

    def with_urls_callback(self, callback: Callable[[ResourceUrlsCallbackContext], None]) -> Self:
        """Customizes displayed URLs via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withUrlsCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_url(self, url: str, *, display_text: str | None = None) -> Self:
        """Adds or modifies displayed URLs"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['url'] = url
        if display_text is not None:
            rpc_args['displayText'] = display_text
        result = self._client.invoke_capability(
            'Aspire.Hosting/withUrl',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_url_expression(self, url: ReferenceExpression, *, display_text: str | None = None) -> Self:
        """Adds a URL using a reference expression"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['url'] = url
        if display_text is not None:
            rpc_args['displayText'] = display_text
        result = self._client.invoke_capability(
            'Aspire.Hosting/withUrlExpression',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_url_for_endpoint(self, endpoint_name: str, callback: Callable[[ResourceUrlAnnotation], None]) -> Self:
        """Customizes the URL for a specific endpoint via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['endpointName'] = endpoint_name
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withUrlForEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_explicit_start(self) -> Self:
        """Prevents resource from starting automatically"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/withExplicitStart',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_health_check(self, key: str) -> Self:
        """Adds a health check by key"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['key'] = key
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHealthCheck',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_command(self, name: str, display_name: str, execute_command: Callable[[ExecuteCommandContext], ExecuteCommandResult], *, command_options: CommandOptions | None = None) -> Self:
        """Adds a resource command"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['displayName'] = display_name
        rpc_args['executeCommand'] = self._client.register_callback(execute_command)
        if command_options is not None:
            rpc_args['commandOptions'] = command_options
        result = self._client.invoke_capability(
            'Aspire.Hosting/withCommand',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_parent_relationship(self, parent: Resource) -> Self:
        """Sets the parent relationship"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['parent'] = parent
        result = self._client.invoke_capability(
            'Aspire.Hosting/withParentRelationship',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_resource_name(self) -> str:
        """Gets the resource name"""
        rpc_args: dict[str, Any] = {'resource': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/getResourceName',
            rpc_args,
        )
        return cast(str, result)

    def with_optional_string(self, *, value: str | None = None, enabled: bool | None = None) -> Self:
        """Adds an optional string parameter"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if value is not None:
            rpc_args['value'] = value
        if enabled is not None:
            rpc_args['enabled'] = enabled
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_config(self, config: TestConfigDto) -> Self:
        """Configures the resource with a DTO"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['config'] = config
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withConfig',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_created_at(self, created_at: str) -> Self:
        """Sets the created timestamp"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['createdAt'] = created_at
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_modified_at(self, modified_at: str) -> Self:
        """Sets the modified timestamp"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['modifiedAt'] = modified_at
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_correlation_id(self, correlation_id: str) -> Self:
        """Sets the correlation ID"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['correlationId'] = correlation_id
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_optional_callback(self, *, callback: Callable[[TestCallbackContext], None] | None = None) -> Self:
        """Configures with optional callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if callback is not None:
            rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_status(self, status: TestResourceStatus) -> Self:
        """Sets the resource status"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['status'] = status
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withStatus',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_nested_config(self, config: TestNestedDto) -> Self:
        """Configures with nested DTO"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['config'] = config
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_validator(self, validator: Callable[[TestResourceContext], bool]) -> Self:
        """Adds validation callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['validator'] = self._client.register_callback(validator)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withValidator',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def test_wait_for(self, dependency: Resource) -> Self:
        """Waits for another resource (test version)"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_dependency(self, dependency: ResourceWithConnectionString) -> Self:
        """Adds a dependency on another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withDependency',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_endpoints(self, endpoints: Iterable[str]) -> Self:
        """Sets the endpoints"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['endpoints'] = endpoints
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_cancellable_operation(self, operation: Callable[[int], None]) -> Self:
        """Performs a cancellable operation"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['operation'] = self._client.register_callback(operation)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def __init__(self, handle: Handle, client: AspireClient, **kwargs: Unpack[_BaseResourceOptions]) -> None:
        if _urls_callback := kwargs.pop("urls_callback", None):
            if _validate_type(_urls_callback, Callable[[ResourceUrlsCallbackContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_urls_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrlsCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'urls_callback'. Expected: Callable[[ResourceUrlsCallbackContext], None]")
        if _url := kwargs.pop("url", None):
            if _validate_type(_url, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["url"] = _url
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrl', rpc_args))
            elif _validate_tuple_types(_url, (str, str)):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["url"] = cast(tuple[str, str], _url)[0]
                rpc_args["displayText"] = cast(tuple[str, str], _url)[1]
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrl', rpc_args))
            else:
                raise TypeError("Invalid type for option 'url'. Expected: str or (str, str)")
        if _url_expression := kwargs.pop("url_expression", None):
            if _validate_type(_url_expression, ReferenceExpression):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["url"] = _url_expression
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrlExpression', rpc_args))
            elif _validate_tuple_types(_url_expression, (ReferenceExpression, str)):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["url"] = cast(tuple[ReferenceExpression, str], _url_expression)[0]
                rpc_args["displayText"] = cast(tuple[ReferenceExpression, str], _url_expression)[1]
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrlExpression', rpc_args))
            else:
                raise TypeError("Invalid type for option 'url_expression'. Expected: ReferenceExpression or (ReferenceExpression, str)")
        if _url_for_endpoint := kwargs.pop("url_for_endpoint", None):
            if _validate_tuple_types(_url_for_endpoint, (str, Callable[[ResourceUrlAnnotation], None])):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["endpointName"] = cast(tuple[str, Callable[[ResourceUrlAnnotation], None]], _url_for_endpoint)[0]
                rpc_args["callback"] = client.register_callback(cast(tuple[str, Callable[[ResourceUrlAnnotation], None]], _url_for_endpoint)[1])
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrlForEndpoint', rpc_args))
            else:
                raise TypeError("Invalid type for option 'url_for_endpoint'. Expected: (str, Callable[[ResourceUrlAnnotation], None])")
        if _explicit_start := kwargs.pop("explicit_start", None):
            if _explicit_start is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withExplicitStart', rpc_args))
            else:
                raise TypeError("Invalid type for option 'explicit_start'. Expected: Literal[True]")
        if _health_check := kwargs.pop("health_check", None):
            if _validate_type(_health_check, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["key"] = _health_check
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHealthCheck', rpc_args))
            else:
                raise TypeError("Invalid type for option 'health_check'. Expected: str")
        if _command := kwargs.pop("command", None):
            if _validate_tuple_types(_command, (str, str, Callable[[ExecuteCommandContext], ExecuteCommandResult])):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["name"] = cast(tuple[str, str, Callable[[ExecuteCommandContext], ExecuteCommandResult]], _command)[0]
                rpc_args["displayName"] = cast(tuple[str, str, Callable[[ExecuteCommandContext], ExecuteCommandResult]], _command)[1]
                rpc_args["executeCommand"] = client.register_callback(cast(tuple[str, str, Callable[[ExecuteCommandContext], ExecuteCommandResult]], _command)[2])
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withCommand', rpc_args))
            elif _validate_dict_types(_command, CommandParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["name"] = cast(CommandParameters, _command)["name"]
                rpc_args["displayName"] = cast(CommandParameters, _command)["display_name"]
                rpc_args["executeCommand"] = client.register_callback(cast(CommandParameters, _command)["execute_command"])
                rpc_args["commandOptions"] = cast(CommandParameters, _command).get("command_options")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withCommand', rpc_args))
            else:
                raise TypeError("Invalid type for option 'command'. Expected: (str, str, Callable[[ExecuteCommandContext], ExecuteCommandResult]) or CommandParameters")
        if _parent_relationship := kwargs.pop("parent_relationship", None):
            if _validate_type(_parent_relationship, Resource):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["parent"] = _parent_relationship
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withParentRelationship', rpc_args))
            else:
                raise TypeError("Invalid type for option 'parent_relationship'. Expected: Resource")
        if _optional_string := kwargs.pop("optional_string", None):
            if _validate_dict_types(_optional_string, OptionalStringParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["value"] = cast(OptionalStringParameters, _optional_string).get("value")
                rpc_args["enabled"] = cast(OptionalStringParameters, _optional_string).get("enabled")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString', rpc_args))
            elif _optional_string is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalString', rpc_args))
            else:
                raise TypeError("Invalid type for option 'optional_string'. Expected: OptionalStringParameters or Literal[True]")
        if _config := kwargs.pop("config", None):
            if _validate_type(_config, TestConfigDto):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["config"] = _config
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withConfig', rpc_args))
            else:
                raise TypeError("Invalid type for option 'config'. Expected: TestConfigDto")
        if _created_at := kwargs.pop("created_at", None):
            if _validate_type(_created_at, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["createdAt"] = _created_at
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt', rpc_args))
            else:
                raise TypeError("Invalid type for option 'created_at'. Expected: str")
        if _modified_at := kwargs.pop("modified_at", None):
            if _validate_type(_modified_at, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["modifiedAt"] = _modified_at
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt', rpc_args))
            else:
                raise TypeError("Invalid type for option 'modified_at'. Expected: str")
        if _correlation_id := kwargs.pop("correlation_id", None):
            if _validate_type(_correlation_id, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["correlationId"] = _correlation_id
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId', rpc_args))
            else:
                raise TypeError("Invalid type for option 'correlation_id'. Expected: str")
        if _optional_callback := kwargs.pop("optional_callback", None):
            if _validate_type(_optional_callback, Callable[[TestCallbackContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_optional_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback', rpc_args))
            elif _optional_callback is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'optional_callback'. Expected: Callable[[TestCallbackContext], None] or Literal[True]")
        if _status := kwargs.pop("status", None):
            if _validate_type(_status, TestResourceStatus):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["status"] = _status
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withStatus', rpc_args))
            else:
                raise TypeError("Invalid type for option 'status'. Expected: TestResourceStatus")
        if _nested_config := kwargs.pop("nested_config", None):
            if _validate_type(_nested_config, TestNestedDto):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["config"] = _nested_config
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig', rpc_args))
            else:
                raise TypeError("Invalid type for option 'nested_config'. Expected: TestNestedDto")
        if _validator := kwargs.pop("validator", None):
            if _validate_type(_validator, Callable[[TestResourceContext], bool]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["validator"] = client.register_callback(_validator)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withValidator', rpc_args))
            else:
                raise TypeError("Invalid type for option 'validator'. Expected: Callable[[TestResourceContext], bool]")
        if _test_wait_for := kwargs.pop("test_wait_for", None):
            if _validate_type(_test_wait_for, Resource):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = _test_wait_for
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor', rpc_args))
            else:
                raise TypeError("Invalid type for option 'test_wait_for'. Expected: Resource")
        if _dependency := kwargs.pop("dependency", None):
            if _validate_type(_dependency, ResourceWithConnectionString):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = _dependency
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withDependency', rpc_args))
            else:
                raise TypeError("Invalid type for option 'dependency'. Expected: ResourceWithConnectionString")
        if _endpoints := kwargs.pop("endpoints", None):
            if _validate_type(_endpoints, Iterable[str]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["endpoints"] = _endpoints
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints', rpc_args))
            else:
                raise TypeError("Invalid type for option 'endpoints'. Expected: Iterable[str]")
        if _cancellable_operation := kwargs.pop("cancellable_operation", None):
            if _validate_type(_cancellable_operation, Callable[[int], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["operation"] = client.register_callback(_cancellable_operation)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation', rpc_args))
            else:
                raise TypeError("Invalid type for option 'cancellable_operation'. Expected: Callable[[int], None]")
        self._handle = handle
        self._client = client
        if kwargs:
            raise TypeError(f"Unexpected keyword arguments: {list(kwargs.keys())}")


class ContainerResourceOptions(_BaseResourceOptions, total=False):
    """ContainerResource options."""

    bind_mount: tuple[str, str] | BindMountParameters
    entrypoint: str
    image_tag: str
    image_registry: str
    image: str | tuple[str, str]
    container_runtime_args: Iterable[str]
    lifetime: ContainerLifetime
    image_pull_policy: ImagePullPolicy
    container_name: str
    env: tuple[str, str]
    env_expression: tuple[str, ReferenceExpression]
    env_callback: Callable[[EnvironmentCallbackContext], None]
    args: Iterable[str]
    args_callback: Callable[[CommandLineArgsCallbackContext], None]
    reference: ResourceWithConnectionString | ReferenceParameters
    service_reference: ResourceWithServiceDiscovery
    endpoint: EndpointParameters | Literal[True]
    http_endpoint: HttpEndpointParameters | Literal[True]
    https_endpoint: HttpsEndpointParameters | Literal[True]
    external_http_endpoints: Literal[True]
    as_http2_service: Literal[True]
    url_for_endpoint_factory: tuple[str, Callable[[EndpointReference], ResourceUrlAnnotation]]
    wait_for: Resource
    wait_for_completion: Resource | tuple[Resource, int]
    http_health_check: HttpHealthCheckParameters | Literal[True]
    volume: str | VolumeParameters
    test_with_env_callback: Callable[[TestEnvironmentContext], None]
    env_vars: Mapping[str, str]

class ContainerResource(_BaseResource, ResourceWithEnvironment, ResourceWithArgs, ResourceWithEndpoints, ResourceWithWaitSupport, ResourceWithProbes, ComputeResource):
    """ContainerResource resource."""

    def __repr__(self) -> str:
        return "ContainerResource(handle={self._handle.handle_id})"

    def with_bind_mount(self, source: str, target: str, *, is_read_only: bool | None = None) -> Self:
        """Adds a bind mount"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        rpc_args['target'] = target
        if is_read_only is not None:
            rpc_args['isReadOnly'] = is_read_only
        result = self._client.invoke_capability(
            'Aspire.Hosting/withBindMount',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_entrypoint(self, entrypoint: str) -> Self:
        """Sets the container entrypoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['entrypoint'] = entrypoint
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEntrypoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_image_tag(self, tag: str) -> Self:
        """Sets the container image tag"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['tag'] = tag
        result = self._client.invoke_capability(
            'Aspire.Hosting/withImageTag',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_image_registry(self, registry: str) -> Self:
        """Sets the container image registry"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['registry'] = registry
        result = self._client.invoke_capability(
            'Aspire.Hosting/withImageRegistry',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_image(self, image: str, *, tag: str | None = None) -> Self:
        """Sets the container image"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['image'] = image
        if tag is not None:
            rpc_args['tag'] = tag
        result = self._client.invoke_capability(
            'Aspire.Hosting/withImage',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_container_runtime_args(self, args: Iterable[str]) -> Self:
        """Adds runtime arguments for the container"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['args'] = args
        result = self._client.invoke_capability(
            'Aspire.Hosting/withContainerRuntimeArgs',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_lifetime(self, lifetime: ContainerLifetime) -> Self:
        """Sets the lifetime behavior of the container resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['lifetime'] = lifetime
        result = self._client.invoke_capability(
            'Aspire.Hosting/withLifetime',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_image_pull_policy(self, pull_policy: ImagePullPolicy) -> Self:
        """Sets the container image pull policy"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['pullPolicy'] = pull_policy
        result = self._client.invoke_capability(
            'Aspire.Hosting/withImagePullPolicy',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_container_name(self, name: str) -> Self:
        """Sets the container name"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        result = self._client.invoke_capability(
            'Aspire.Hosting/withContainerName',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env(self, name: str, value: str) -> Self:
        """Sets an environment variable"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['value'] = value
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironment',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_expression(self, name: str, value: ReferenceExpression) -> Self:
        """Adds an environment variable with a reference expression"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['value'] = value
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironmentExpression',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> Self:
        """Sets environment variables via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironmentCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_args(self, args: Iterable[str]) -> Self:
        """Adds arguments"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['args'] = args
        result = self._client.invoke_capability(
            'Aspire.Hosting/withArgs',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> Self:
        """Sets command-line arguments via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withArgsCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_reference(self, source: ResourceWithConnectionString, *, connection_name: str | None = None, optional: bool | None = None) -> Self:
        """Adds a reference to another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        if connection_name is not None:
            rpc_args['connectionName'] = connection_name
        if optional is not None:
            rpc_args['optional'] = optional
        result = self._client.invoke_capability(
            'Aspire.Hosting/withReference',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_service_reference(self, source: ResourceWithServiceDiscovery) -> Self:
        """Adds a service discovery reference to another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        result = self._client.invoke_capability(
            'Aspire.Hosting/withServiceReference',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_endpoint(self, *, port: int | None = None, target_port: int | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None, is_external: bool | None = None, protocol: ProtocolType | None = None) -> Self:
        """Adds a network endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if scheme is not None:
            rpc_args['scheme'] = scheme
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        if is_external is not None:
            rpc_args['isExternal'] = is_external
        if protocol is not None:
            rpc_args['protocol'] = protocol
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_http_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTP endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_https_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTPS endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpsEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_external_http_endpoints(self) -> Self:
        """Makes HTTP endpoints externally accessible"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        result = self._client.invoke_capability(
            'Aspire.Hosting/getEndpoint',
            rpc_args,
        )
        return cast(EndpointReference, result)

    def as_http2_service(self) -> Self:
        """Configures resource for HTTP/2"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/asHttp2Service',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> Self:
        """Adds a URL for a specific endpoint via factory callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['endpointName'] = endpoint_name
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def wait_for(self, dependency: Resource) -> Self:
        """Waits for another resource to be ready"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        result = self._client.invoke_capability(
            'Aspire.Hosting/waitFor',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def wait_for_completion(self, dependency: Resource, *, exit_code: int | None = None) -> Self:
        """Waits for resource completion"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        if exit_code is not None:
            rpc_args['exitCode'] = exit_code
        result = self._client.invoke_capability(
            'Aspire.Hosting/waitForCompletion',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_http_health_check(self, *, path: str | None = None, status_code: int | None = None, endpoint_name: str | None = None) -> Self:
        """Adds an HTTP health check"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if path is not None:
            rpc_args['path'] = path
        if status_code is not None:
            rpc_args['statusCode'] = status_code
        if endpoint_name is not None:
            rpc_args['endpointName'] = endpoint_name
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpHealthCheck',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_volume(self, target: str, *, name: str | None = None, is_read_only: bool | None = None) -> Self:
        """Adds a volume"""
        rpc_args: dict[str, Any] = {'resource': self._handle}
        rpc_args['target'] = target
        if name is not None:
            rpc_args['name'] = name
        if is_read_only is not None:
            rpc_args['isReadOnly'] = is_read_only
        result = self._client.invoke_capability(
            'Aspire.Hosting/withVolume',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def test_with_env_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> Self:
        """Configures environment with callback (test version)"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_vars(self, vars: Mapping[str, str]) -> Self:
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
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['value'] = value
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironment',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_expression(self, name: str, value: ReferenceExpression) -> Self:
        """Adds an environment variable with a reference expression"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['value'] = value
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironmentExpression',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> Self:
        """Sets environment variables via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironmentCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_args(self, args: Iterable[str]) -> Self:
        """Adds arguments"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['args'] = args
        result = self._client.invoke_capability(
            'Aspire.Hosting/withArgs',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> Self:
        """Sets command-line arguments via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withArgsCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_reference(self, source: ResourceWithConnectionString, *, connection_name: str | None = None, optional: bool | None = None) -> Self:
        """Adds a reference to another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        if connection_name is not None:
            rpc_args['connectionName'] = connection_name
        if optional is not None:
            rpc_args['optional'] = optional
        result = self._client.invoke_capability(
            'Aspire.Hosting/withReference',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_service_reference(self, source: ResourceWithServiceDiscovery) -> Self:
        """Adds a service discovery reference to another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        result = self._client.invoke_capability(
            'Aspire.Hosting/withServiceReference',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_endpoint(self, *, port: int | None = None, target_port: int | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None, is_external: bool | None = None, protocol: ProtocolType | None = None) -> Self:
        """Adds a network endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if scheme is not None:
            rpc_args['scheme'] = scheme
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        if is_external is not None:
            rpc_args['isExternal'] = is_external
        if protocol is not None:
            rpc_args['protocol'] = protocol
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_http_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTP endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_https_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTPS endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpsEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_external_http_endpoints(self) -> Self:
        """Makes HTTP endpoints externally accessible"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_endpoint(self, name: str) -> EndpointReference:
        """Gets an endpoint reference"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        result = self._client.invoke_capability(
            'Aspire.Hosting/getEndpoint',
            rpc_args,
        )
        return cast(EndpointReference, result)

    def as_http2_service(self) -> Self:
        """Configures resource for HTTP/2"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/asHttp2Service',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_url_for_endpoint_factory(self, endpoint_name: str, callback: Callable[[EndpointReference], ResourceUrlAnnotation]) -> Self:
        """Adds a URL for a specific endpoint via factory callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['endpointName'] = endpoint_name
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withUrlForEndpointFactory',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def wait_for(self, dependency: Resource) -> Self:
        """Waits for another resource to be ready"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        result = self._client.invoke_capability(
            'Aspire.Hosting/waitFor',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def wait_for_completion(self, dependency: Resource, *, exit_code: int | None = None) -> Self:
        """Waits for resource completion"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        if exit_code is not None:
            rpc_args['exitCode'] = exit_code
        result = self._client.invoke_capability(
            'Aspire.Hosting/waitForCompletion',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_http_health_check(self, *, path: str | None = None, status_code: int | None = None, endpoint_name: str | None = None) -> Self:
        """Adds an HTTP health check"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if path is not None:
            rpc_args['path'] = path
        if status_code is not None:
            rpc_args['statusCode'] = status_code
        if endpoint_name is not None:
            rpc_args['endpointName'] = endpoint_name
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpHealthCheck',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def test_with_env_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> Self:
        """Configures environment with callback (test version)"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_vars(self, vars: Mapping[str, str]) -> Self:
        """Sets environment variables"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['variables'] = vars
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def __init__(self, handle: Handle, client: AspireClient, **kwargs: Unpack[ExecutableResourceOptions]) -> None:
        if _executable_command := kwargs.pop("executable_command", None):
            if _validate_type(_executable_command, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["command"] = _executable_command
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withExecutableCommand', rpc_args))
            else:
                raise TypeError("Invalid type for option 'executable_command'. Expected: str")
        if _working_dir := kwargs.pop("working_dir", None):
            if _validate_type(_working_dir, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["workingDirectory"] = _working_dir
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withWorkingDirectory', rpc_args))
            else:
                raise TypeError("Invalid type for option 'working_dir'. Expected: str")
        if _env := kwargs.pop("env", None):
            if _validate_tuple_types(_env, (str, str)):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["name"] = cast(tuple[str, str], _env)[0]
                rpc_args["value"] = cast(tuple[str, str], _env)[1]
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withEnvironment', rpc_args))
            else:
                raise TypeError("Invalid type for option 'env'. Expected: (str, str)")
        if _env_expression := kwargs.pop("env_expression", None):
            if _validate_tuple_types(_env_expression, (str, ReferenceExpression)):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["name"] = cast(tuple[str, ReferenceExpression], _env_expression)[0]
                rpc_args["value"] = cast(tuple[str, ReferenceExpression], _env_expression)[1]
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withEnvironmentExpression', rpc_args))
            else:
                raise TypeError("Invalid type for option 'env_expression'. Expected: (str, ReferenceExpression)")
        if _env_callback := kwargs.pop("env_callback", None):
            if _validate_type(_env_callback, Callable[[EnvironmentCallbackContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_env_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withEnvironmentCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'env_callback'. Expected: Callable[[EnvironmentCallbackContext], None]")
        if _args := kwargs.pop("args", None):
            if _validate_type(_args, Iterable[str]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["args"] = _args
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withArgs', rpc_args))
            else:
                raise TypeError("Invalid type for option 'args'. Expected: Iterable[str]")
        if _args_callback := kwargs.pop("args_callback", None):
            if _validate_type(_args_callback, Callable[[CommandLineArgsCallbackContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_args_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withArgsCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'args_callback'. Expected: Callable[[CommandLineArgsCallbackContext], None]")
        if _reference := kwargs.pop("reference", None):
            if _validate_type(_reference, ResourceWithConnectionString):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["source"] = _reference
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withReference', rpc_args))
            elif _validate_dict_types(_reference, ReferenceParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["source"] = cast(ReferenceParameters, _reference)["source"]
                rpc_args["connectionName"] = cast(ReferenceParameters, _reference).get("connection_name")
                rpc_args["optional"] = cast(ReferenceParameters, _reference).get("optional")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withReference', rpc_args))
            else:
                raise TypeError("Invalid type for option 'reference'. Expected: ResourceWithConnectionString or ReferenceParameters")
        if _service_reference := kwargs.pop("service_reference", None):
            if _validate_type(_service_reference, ResourceWithServiceDiscovery):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["source"] = _service_reference
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withServiceReference', rpc_args))
            else:
                raise TypeError("Invalid type for option 'service_reference'. Expected: ResourceWithServiceDiscovery")
        if _endpoint := kwargs.pop("endpoint", None):
            if _validate_dict_types(_endpoint, EndpointParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["port"] = cast(EndpointParameters, _endpoint).get("port")
                rpc_args["targetPort"] = cast(EndpointParameters, _endpoint).get("target_port")
                rpc_args["scheme"] = cast(EndpointParameters, _endpoint).get("scheme")
                rpc_args["name"] = cast(EndpointParameters, _endpoint).get("name")
                rpc_args["env"] = cast(EndpointParameters, _endpoint).get("env")
                rpc_args["isProxied"] = cast(EndpointParameters, _endpoint).get("is_proxied")
                rpc_args["isExternal"] = cast(EndpointParameters, _endpoint).get("is_external")
                rpc_args["protocol"] = cast(EndpointParameters, _endpoint).get("protocol")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withEndpoint', rpc_args))
            elif _endpoint is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withEndpoint', rpc_args))
            else:
                raise TypeError("Invalid type for option 'endpoint'. Expected: EndpointParameters or Literal[True]")
        if _http_endpoint := kwargs.pop("http_endpoint", None):
            if _validate_dict_types(_http_endpoint, HttpEndpointParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["port"] = cast(HttpEndpointParameters, _http_endpoint).get("port")
                rpc_args["targetPort"] = cast(HttpEndpointParameters, _http_endpoint).get("target_port")
                rpc_args["name"] = cast(HttpEndpointParameters, _http_endpoint).get("name")
                rpc_args["env"] = cast(HttpEndpointParameters, _http_endpoint).get("env")
                rpc_args["isProxied"] = cast(HttpEndpointParameters, _http_endpoint).get("is_proxied")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHttpEndpoint', rpc_args))
            elif _http_endpoint is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHttpEndpoint', rpc_args))
            else:
                raise TypeError("Invalid type for option 'http_endpoint'. Expected: HttpEndpointParameters or Literal[True]")
        if _https_endpoint := kwargs.pop("https_endpoint", None):
            if _validate_dict_types(_https_endpoint, HttpsEndpointParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["port"] = cast(HttpsEndpointParameters, _https_endpoint).get("port")
                rpc_args["targetPort"] = cast(HttpsEndpointParameters, _https_endpoint).get("target_port")
                rpc_args["name"] = cast(HttpsEndpointParameters, _https_endpoint).get("name")
                rpc_args["env"] = cast(HttpsEndpointParameters, _https_endpoint).get("env")
                rpc_args["isProxied"] = cast(HttpsEndpointParameters, _https_endpoint).get("is_proxied")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHttpsEndpoint', rpc_args))
            elif _https_endpoint is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHttpsEndpoint', rpc_args))
            else:
                raise TypeError("Invalid type for option 'https_endpoint'. Expected: HttpsEndpointParameters or Literal[True]")
        if _external_http_endpoints := kwargs.pop("external_http_endpoints", None):
            if _external_http_endpoints is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withExternalHttpEndpoints', rpc_args))
            else:
                raise TypeError("Invalid type for option 'external_http_endpoints'. Expected: Literal[True]")
        if _as_http2_service := kwargs.pop("as_http2_service", None):
            if _as_http2_service is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/asHttp2Service', rpc_args))
            else:
                raise TypeError("Invalid type for option 'as_http2_service'. Expected: Literal[True]")
        if _url_for_endpoint_factory := kwargs.pop("url_for_endpoint_factory", None):
            if _validate_tuple_types(_url_for_endpoint_factory, (str, Callable[[EndpointReference], ResourceUrlAnnotation])):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["endpointName"] = cast(tuple[str, Callable[[EndpointReference], ResourceUrlAnnotation]], _url_for_endpoint_factory)[0]
                rpc_args["callback"] = client.register_callback(cast(tuple[str, Callable[[EndpointReference], ResourceUrlAnnotation]], _url_for_endpoint_factory)[1])
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withUrlForEndpointFactory', rpc_args))
            else:
                raise TypeError("Invalid type for option 'url_for_endpoint_factory'. Expected: (str, Callable[[EndpointReference], ResourceUrlAnnotation])")
        if _wait_for := kwargs.pop("wait_for", None):
            if _validate_type(_wait_for, Resource):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = _wait_for
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/waitFor', rpc_args))
            else:
                raise TypeError("Invalid type for option 'wait_for'. Expected: Resource")
        if _wait_for_completion := kwargs.pop("wait_for_completion", None):
            if _validate_type(_wait_for_completion, Resource):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = _wait_for_completion
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/waitForCompletion', rpc_args))
            elif _validate_tuple_types(_wait_for_completion, (Resource, int)):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = cast(tuple[Resource, int], _wait_for_completion)[0]
                rpc_args["exitCode"] = cast(tuple[Resource, int], _wait_for_completion)[1]
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/waitForCompletion', rpc_args))
            else:
                raise TypeError("Invalid type for option 'wait_for_completion'. Expected: Resource or (Resource, int)")
        if _http_health_check := kwargs.pop("http_health_check", None):
            if _validate_dict_types(_http_health_check, HttpHealthCheckParameters):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["path"] = cast(HttpHealthCheckParameters, _http_health_check).get("path")
                rpc_args["statusCode"] = cast(HttpHealthCheckParameters, _http_health_check).get("status_code")
                rpc_args["endpointName"] = cast(HttpHealthCheckParameters, _http_health_check).get("endpoint_name")
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHttpHealthCheck', rpc_args))
            elif _http_health_check is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withHttpHealthCheck', rpc_args))
            else:
                raise TypeError("Invalid type for option 'http_health_check'. Expected: HttpHealthCheckParameters or Literal[True]")
        if _test_with_env_callback := kwargs.pop("test_with_env_callback", None):
            if _validate_type(_test_with_env_callback, Callable[[TestEnvironmentContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_test_with_env_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'test_with_env_callback'. Expected: Callable[[TestEnvironmentContext], None]")
        if _env_vars := kwargs.pop("env_vars", None):
            if _validate_type(_env_vars, Mapping[str, str]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["variables"] = _env_vars
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables', rpc_args))
            else:
                raise TypeError("Invalid type for option 'env_vars'. Expected: Mapping[str, str]")
        super().__init__(handle, client, **kwargs)


class ParameterResourceOptions(_BaseResourceOptions, total=False):
    """ParameterResource options."""

    description: str | tuple[str, bool]

class ParameterResource(_BaseResource, ManifestExpressionProvider, ValueProvider):
    """ParameterResource resource."""

    def __repr__(self) -> str:
        return "ParameterResource(handle={self._handle.handle_id})"

    def with_description(self, description: str, *, enable_markdown: bool | None = None) -> Self:
        """Sets a parameter description"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['description'] = description
        if enable_markdown is not None:
            rpc_args['enableMarkdown'] = enable_markdown
        result = self._client.invoke_capability(
            'Aspire.Hosting/withDescription',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def __init__(self, handle: Handle, client: AspireClient, **kwargs: Unpack[ParameterResourceOptions]) -> None:
        if _description := kwargs.pop("description", None):
            if _validate_type(_description, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["description"] = _description
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withDescription', rpc_args))
            elif _validate_tuple_types(_description, (str, bool)):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["description"] = cast(tuple[str, bool], _description)[0]
                rpc_args["enableMarkdown"] = cast(tuple[str, bool], _description)[1]
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting/withDescription', rpc_args))
            else:
                raise TypeError("Invalid type for option 'description'. Expected: str or (str, bool)")
        super().__init__(handle, client, **kwargs)


class ProjectResourceOptions(_BaseResourceOptions, total=False):
    """ProjectResource options."""

    replicas: int
    env: tuple[str, str]
    env_expression: tuple[str, ReferenceExpression]
    env_callback: Callable[[EnvironmentCallbackContext], None]
    args: Iterable[str]
    args_callback: Callable[[CommandLineArgsCallbackContext], None]
    reference: ResourceWithConnectionString | ReferenceParameters
    service_reference: ResourceWithServiceDiscovery
    endpoint: EndpointParameters | Literal[True]
    http_endpoint: HttpEndpointParameters | Literal[True]
    https_endpoint: HttpsEndpointParameters | Literal[True]
    external_http_endpoints: Literal[True]
    as_http2_service: Literal[True]
    url_for_endpoint_factory: tuple[str, Callable[[EndpointReference], ResourceUrlAnnotation]]
    wait_for: Resource
    wait_for_completion: Resource | tuple[Resource, int]
    http_health_check: HttpHealthCheckParameters | Literal[True]
    test_with_env_callback: Callable[[TestEnvironmentContext], None]
    env_vars: Mapping[str, str]

class ProjectResource(_BaseResource, ResourceWithEnvironment, ResourceWithArgs, ResourceWithServiceDiscovery, ResourceWithWaitSupport, ResourceWithProbes, ComputeResource, ContainerFilesDestinationResource):
    """ProjectResource resource."""

    def __repr__(self) -> str:
        return "ProjectResource(handle={self._handle.handle_id})"

    def with_replicas(self, replicas: int) -> Self:
        """Sets the number of replicas"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['replicas'] = replicas
        result = self._client.invoke_capability(
            'Aspire.Hosting/withReplicas',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env(self, name: str, value: str) -> Self:
        """Sets an environment variable"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['value'] = value
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironment',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_expression(self, name: str, value: ReferenceExpression) -> Self:
        """Adds an environment variable with a reference expression"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['name'] = name
        rpc_args['value'] = value
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironmentExpression',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_callback(self, callback: Callable[[EnvironmentCallbackContext], None]) -> Self:
        """Sets environment variables via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEnvironmentCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_args(self, args: Iterable[str]) -> Self:
        """Adds arguments"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['args'] = args
        result = self._client.invoke_capability(
            'Aspire.Hosting/withArgs',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_args_callback(self, callback: Callable[[CommandLineArgsCallbackContext], None]) -> Self:
        """Sets command-line arguments via callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting/withArgsCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_reference(self, source: ResourceWithConnectionString, *, connection_name: str | None = None, optional: bool | None = None) -> Self:
        """Adds a reference to another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        if connection_name is not None:
            rpc_args['connectionName'] = connection_name
        if optional is not None:
            rpc_args['optional'] = optional
        result = self._client.invoke_capability(
            'Aspire.Hosting/withReference',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_service_reference(self, source: ResourceWithServiceDiscovery) -> Self:
        """Adds a service discovery reference to another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['source'] = source
        result = self._client.invoke_capability(
            'Aspire.Hosting/withServiceReference',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_endpoint(self, *, port: int | None = None, target_port: int | None = None, scheme: str | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None, is_external: bool | None = None, protocol: ProtocolType | None = None) -> Self:
        """Adds a network endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if scheme is not None:
            rpc_args['scheme'] = scheme
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        if is_external is not None:
            rpc_args['isExternal'] = is_external
        if protocol is not None:
            rpc_args['protocol'] = protocol
        result = self._client.invoke_capability(
            'Aspire.Hosting/withEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_http_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTP endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_https_endpoint(self, *, port: int | None = None, target_port: int | None = None, name: str | None = None, env: str | None = None, is_proxied: bool | None = None) -> Self:
        """Adds an HTTPS endpoint"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if port is not None:
            rpc_args['port'] = port
        if target_port is not None:
            rpc_args['targetPort'] = target_port
        if name is not None:
            rpc_args['name'] = name
        if env is not None:
            rpc_args['env'] = env
        if is_proxied is not None:
            rpc_args['isProxied'] = is_proxied
        result = self._client.invoke_capability(
            'Aspire.Hosting/withHttpsEndpoint',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_external_http_endpoints(self) -> Self:
        """Makes HTTP endpoints externally accessible"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting/withExternalHttpEndpoints',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def add_test_child_database(self, name: str, database_name: str | None = None) -> TestDatabaseResource:
        """Adds a child database to a test Redis resource"""
        args: Dict[str, Any] = { "builder": serialize_value(self._handle) }
        args["name"] = serialize_value(name)
        if database_name is not None:
            args["databaseName"] = serialize_value(database_name)
        return self._client.invoke_capability("Aspire.Hosting.CodeGeneration.Python.Tests/addTestChildDatabase", args)

    def with_persistence(self, mode: TestPersistenceMode = None) -> TestRedisResource:
        """Configures the Redis resource with persistence"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if mode is not None:
            rpc_args['mode'] = mode
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withPersistence',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_tags(self) -> AspireList[str]:
        """Gets the tags for the resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getTags',
            rpc_args,
        )
        return cast(AspireList[str], result)

    def get_metadata(self) -> AspireDict[str, str]:
        """Gets the metadata for the resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getMetadata',
            rpc_args,
        )
        return cast(AspireDict[str, str], result)

    def with_connection_string(self, connection_string: ReferenceExpression) -> Self:
        """Sets the connection string using a reference expression"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['connectionString'] = connection_string
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionString',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_endpoints(self) -> Iterable[str]:
        """Gets the endpoints"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getEndpoints',
            rpc_args,
        )
        return cast(Iterable[str], result)

    def with_connection_string_direct(self, connection_string: str) -> Self:
        """Sets connection string using direct interface target"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['connectionString'] = connection_string
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionStringDirect',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_redis_specific(self, option: str) -> Self:
        """Redis-specific configuration"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['option'] = option
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withRedisSpecific',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_status(self, *, timeout: int | None = None) -> str:
        """Gets the status of the resource asynchronously"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if timeout is not None:
            rpc_args['cancellationToken'] = self._client.register_cancellation_token(timeout)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getStatusAsync',
            rpc_args,
        )
        return cast(str, result)

    def wait_for_ready(self, timeout: float, *, timeout: int | None = None) -> bool:
        """Waits for the resource to be ready"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['timeout'] = timeout
        if timeout is not None:
            rpc_args['cancellationToken'] = self._client.register_cancellation_token(timeout)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/waitForReadyAsync',
            rpc_args,
        )
        return cast(bool, result)

    def __init__(self, handle: Handle, client: AspireClient, **kwargs: Unpack[TestRedisResourceOptions]) -> None:
        if _persistence := kwargs.pop("persistence", None):
            if _validate_type(_persistence, TestPersistenceMode):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["mode"] = _persistence
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withPersistence', rpc_args))
            elif _persistence is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withPersistence', rpc_args))
            else:
                raise TypeError("Invalid type for option 'persistence'. Expected: TestPersistenceMode or Literal[True]")
        if _connection_string := kwargs.pop("connection_string", None):
            if _validate_type(_connection_string, ReferenceExpression):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["connectionString"] = _connection_string
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionString', rpc_args))
            else:
                raise TypeError("Invalid type for option 'connection_string'. Expected: ReferenceExpression")
        if _connection_string_direct := kwargs.pop("connection_string_direct", None):
            if _validate_type(_connection_string_direct, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["connectionString"] = _connection_string_direct
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionStringDirect', rpc_args))
            else:
                raise TypeError("Invalid type for option 'connection_string_direct'. Expected: str")
        if _redis_specific := kwargs.pop("redis_specific", None):
            if _validate_type(_redis_specific, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["option"] = _redis_specific
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withRedisSpecific', rpc_args))
            else:
                raise TypeError("Invalid type for option 'redis_specific'. Expected: str")
        super().__init__(handle, client, **kwargs)


# ============================================================================
# Connection Helper
# ============================================================================

def _get_client(*, debug: bool, heartbeat_interval: int | None) -> AspireClient:
    '''
    Creates and connects to the Aspire AppHost.
    Reads connection info from environment variables set by `aspire run`.
    '''
    socket_path = os.environ.get('REMOTE_APP_HOST_SOCKET_PATH')
    if not socket_path:
        raise ValueError(
            'REMOTE_APP_HOST_SOCKET_PATH environment variable not set. '
            'Run this application using `aspire run`.'
        )

    client = AspireClient(socket_path, debug=debug, heartbeat_interval=heartbeat_interval)
    return client


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
# Handle Registrations
# ============================================================================

_register_handle_wrapper("Aspire.Hosting/Dict<string,any>", AspireDict)
_register_handle_wrapper("Aspire.Hosting/List<any>", AspireList)
_register_handle_wrapper("Aspire.Hosting/Dict<string,string|Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression>", AspireDict)
_register_handle_wrapper("Aspire.Hosting/List<Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlAnnotation>", AspireList)
_register_handle_wrapper("Aspire.Hosting/List<string>", AspireList)
_register_handle_wrapper("Aspire.Hosting/Dict<string,string>", AspireDict)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext", CommandLineArgsCallbackContext)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplication", DistributedApplication)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing", DistributedApplicationEventing)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription", DistributedApplicationEventSubscription)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext", DistributedApplicationExecutionContext)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference", EndpointReference)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression", EndpointReferenceExpression)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext", EnvironmentCallbackContext)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext", ExecuteCommandContext)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext", ResourceUrlsCallbackContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", TestCallbackContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", TestCollectionContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", TestEnvironmentContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", TestResourceContext)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.Resource", _BaseResource)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource", ContainerResource)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource", ExecutableResource)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource", ParameterResource)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource", ProjectResource)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", TestRedisResource)
