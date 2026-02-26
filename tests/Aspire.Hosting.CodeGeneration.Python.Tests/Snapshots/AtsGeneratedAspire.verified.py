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

TestPersistenceMode = Literal["None", "Volume", "Bind"]

TestResourceStatus = Literal["Pending", "Running", "Stopped", "Failed"]


# ============================================================================
# Method Parameters
# ============================================================================


class OptionalStringParameters(TypedDict, total=False):
    value: str
    enabled: bool

# ============================================================================
# DTO Classes (Data Transfer Objects)
# ============================================================================

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


class ManifestExpressionProvider(ABC):
    """Abstract base class for ManifestExpressionProvider."""

class ValueProvider(ABC):
    """Abstract base class for ValueProvider."""

class ValueWithReferences(ABC):
    """Abstract base class for ValueWithReferences."""

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

class TestDatabaseResource(ResourceBuilderBase):
    def __init__(self, handle: Handle, client: AspireClient):
        super().__init__(handle, client)

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


class ResourceWithEnvironment(Resource):
    """Abstract base class for ResourceWithEnvironment interface."""

    @abstractmethod
    def test_with_env_callback(self, callback: Callable[[TestEnvironmentContext], None]) -> Self:
        """Configures environment with callback (test version)"""

    @abstractmethod
    def with_env_vars(self, vars: Mapping[str, str]) -> Self:
        """Sets environment variables"""


class ResourceWithArgs(Resource):
    """Abstract base class for ResourceWithArgs interface."""


class ResourceWithEndpoints(Resource):
    """Abstract base class for ResourceWithEndpoints interface."""


class ResourceWithWaitSupport(Resource):
    """Abstract base class for ResourceWithWaitSupport interface."""


class ResourceWithProbes(Resource):
    """Abstract base class for ResourceWithProbes interface."""


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

register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", lambda handle, client: TestCallbackContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", lambda handle, client: TestResourceContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", lambda handle, client: TestEnvironmentContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", lambda handle, client: TestCollectionContext(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", lambda handle, client: TestRedisResource(handle, client))
register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource", lambda handle, client: TestDatabaseResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource", lambda handle, client: IResource(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString", lambda handle, client: IResourceWithConnectionString(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", lambda handle, client: IDistributedApplicationBuilder(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression", lambda handle, client: ReferenceExpression(handle, client))
register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment", lambda handle, client: IResourceWithEnvironment(handle, client))
register_handle_wrapper("Aspire.Hosting/List<string>", lambda handle, client: AspireList(handle, client))
register_handle_wrapper("Aspire.Hosting/Dict<string,string>", lambda handle, client: AspireDict(handle, client))

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


def create_builder(
    *,
    args: Iterable[str] | None = None,
    project_directory: str | None = None,
    container_registry_override: str | None = None,
    disable_dashboard: bool | None = None,
    dashboard_application_name: str | None = None,
    allow_unsecured_transport: bool | None = None,
    enable_resource_logging: bool | None = None,
    options: CreateBuilderOptions | None = None,
    debug: bool | None = None,
    heartbeat_interval: int | None = None,
 ) -> AbstractContextManager[DistributedApplicationBuilder]:
    '''
    Creates a new distributed application builder.
    This is the entry point for building Aspire applications.

    Args:
        args (Iterable[str]): Command-line arguments to pass to the AppHost. By default, this will be set to any additional arguments
            passed to the Aspire command line (arguments specified after '--'). Specifying them here will override that default.
        project_directory (str): The directory containing the AppHost project file. By default, this will  use the ASPIRE_PROJECT_DIRECTORY
            environment variable if set, otherwise it will use the current working directory.
        container_registry_override (str): When containers are used, use this value to override the container registry.
        disable_dashboard (bool): Determines whether the dashboard is disabled.
        dashboard_application_name (str): The application name to display in the dashboard.
        allow_unsecured_transport (bool): Allows the use of HTTP urls for the AppHost resource endpoint.
        enable_resource_logging (bool): Enables resource logging.
        options (CreateBuilderOptions): An optional dict containing any of the above options. Specifying options here will override default behaviours,
           but individual parameters will take precedence.
        debug (bool): Whether to enable logging of the communication between the client and AppHost server.
            Default behaviour will be determined by whether `--debug` is passed as an Aspire command-line argument, or
            if the ASPIRE_DEBUG environment variable is set. Enabling or disabling here will override those defaults.
            Messages will be logged as INFO, with the 'aspire_app' logger name (connection heartbeat messages will be logged at DEBUG).
        heartbeat_interval (int): Optional interval in seconds for sending heartbeat messages to the AppHost. Default value is 5 seconds.

    Returns:
        A DistributedApplicationBuilder instance
    '''
    is_debug = debug if debug is not None else os.environ.get('ASPIRE_DEBUG', 'false').lower() == 'true'
    client = _get_client(debug=is_debug, heartbeat_interval=heartbeat_interval)

    # Default args and project_directory if not provided
    effective_options = options or CreateBuilderOptions()
    if args is not None:
        effective_options['Args'] = args
    elif not effective_options.get('Args'):
        effective_options['Args'] = sys.argv[1:]
    if project_directory is not None:
        effective_options['ProjectDirectory'] = project_directory
    elif not effective_options.get('ProjectDirectory'):
        effective_options['ProjectDirectory'] = os.environ.get('ASPIRE_PROJECT_DIRECTORY', os.getcwd())
    if container_registry_override is not None:
        effective_options['ContainerRegistryOverride'] = container_registry_override
    if disable_dashboard is not None:
        effective_options['DisableDashboard'] = disable_dashboard
    if dashboard_application_name is not None:
        effective_options['DashboardApplicationName'] = dashboard_application_name
    if allow_unsecured_transport is not None:
        effective_options['AllowUnsecuredTransport'] = allow_unsecured_transport
    if enable_resource_logging is not None:
        effective_options['EnableResourceLogging'] = enable_resource_logging

    return DistributedApplicationBuilder(client, effective_options)

# ============================================================================
# Handle Registrations
# ============================================================================

_register_handle_wrapper("Aspire.Hosting/List<string>", AspireList)
_register_handle_wrapper("Aspire.Hosting/Dict<string,string>", AspireDict)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", TestCallbackContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", TestCollectionContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", TestEnvironmentContext)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", TestResourceContext)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.Resource", _BaseResource)
_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource", ContainerResource)
_register_handle_wrapper("Aspire.Hosting.CodeGeneration.Python.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", TestRedisResource)
