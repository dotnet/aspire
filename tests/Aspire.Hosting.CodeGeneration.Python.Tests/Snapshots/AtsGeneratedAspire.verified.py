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
    AspyreError,
    CapabilityError,
    ParameterTypeError,
    CallbackCancelled,
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


@dataclass
class Warnings:
    experimental: str | None


class AspyreExperimentalWarning(Warning):
    '''Custom warning for experimental features in Aspire.'''


def _experimental(arg_name: str, func_or_cls: str | type, code: str):
    if isinstance(func_or_cls, str):
        warn(
            f"The '{arg_name}' option in '{func_or_cls}' is for evaluation purposes only and is subject "
            f"to change or removal in future updates. (Code: {code})",
            category=AspyreExperimentalWarning,
        )
    else:
        warn(
            f"The '{arg_name}' method of '{func_or_cls.__name__}' is for evaluation purposes only and is subject "
            f"to change or removal in future updates. (Code: {code})",
            category=AspyreExperimentalWarning,
        )


def _check_warnings(kwargs: Mapping[str, Any], annotations: Any, func_name: str):
    type_hints = get_type_hints(annotations, include_extras=True)
    for key in kwargs.keys():
        if get_origin(type_hint := type_hints.get(key)) is Annotated:
            annotated_warnings = cast(Warnings, get_args(type_hint)[1])
            if annotated_warnings.experimental:
                warn(
                    f"The '{key}' option in '{func_name}' is for evaluation purposes only and is subject to change"
                    f"or removal in future updates. (Code: {annotated_warnings.experimental})",
                    category=AspyreExperimentalWarning,
                )



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

    def add_test_redis(self, name: str, /, *, port: int | None = None, **kwargs: Unpack["TestRedisResourceOptions"]) -> TestRedisResource:
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
        raise CallbackCancelled(result)


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

    def get_value(self, /) -> str:
        """Invokes the GetValueAsync method"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync',
            rpc_args,
        )
        return result

    def set_value(self, value: str, /) -> None:
        """Invokes the SetValueAsync method"""
        rpc_args: dict[str, Any] = {'context': self._handle}
        rpc_args['value'] = value
        self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync',
            rpc_args
        )

    def validate(self, /) -> bool:
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
    def with_optional_string(self, /, *, value: str | None = None, enabled: bool | None = None) -> Self:
        """Adds an optional string parameter"""

    @abstractmethod
    def with_config(self, config: TestConfigDto, /) -> Self:
        """Configures the resource with a DTO"""

    @abstractmethod
    def with_created_at(self, created_at: str, /) -> Self:
        """Sets the created timestamp"""

    @abstractmethod
    def with_modified_at(self, modified_at: str, /) -> Self:
        """Sets the modified timestamp"""

    @abstractmethod
    def with_correlation_id(self, correlation_id: str, /) -> Self:
        """Sets the correlation ID"""

    @abstractmethod
    def with_optional_callback(self, /, *, callback: Callable[[TestCallbackContext], None] | None = None) -> Self:
        """Configures with optional callback"""

    @abstractmethod
    def with_status(self, status: TestResourceStatus, /) -> Self:
        """Sets the resource status"""

    @abstractmethod
    def with_nested_config(self, config: TestNestedDto, /) -> Self:
        """Configures with nested DTO"""

    @abstractmethod
    def with_validator(self, validator: Callable[[TestResourceContext], bool], /) -> Self:
        """Adds validation callback"""

    @abstractmethod
    def test_wait_for(self, dependency: Resource, /) -> Self:
        """Waits for another resource (test version)"""

    @abstractmethod
    def with_dependency(self, dependency: ResourceWithConnectionString, /) -> Self:
        """Adds a dependency on another resource"""

    @abstractmethod
    def with_endpoints(self, endpoints: Iterable[str], /) -> Self:
        """Sets the endpoints"""

    @abstractmethod
    def with_cancellable_operation(self, operation: Callable[[int], None], /) -> Self:
        """Performs a cancellable operation"""


class ComputeResource(Resource):
    """Abstract base class for ComputeResource interface."""


class ResourceWithEnvironment(Resource):
    """Abstract base class for ResourceWithEnvironment interface."""

    @abstractmethod
    def test_with_env_callback(self, callback: Callable[[TestEnvironmentContext], None], /) -> Self:
        """Configures environment with callback (test version)"""

    @abstractmethod
    def with_env_vars(self, vars: Mapping[str, str], /) -> Self:
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
    def with_connection_string(self, connection_string: ReferenceExpression, /) -> Self:
        """Sets the connection string using a reference expression"""

    @abstractmethod
    def with_connection_string_direct(self, connection_string: str, /) -> Self:
        """Sets connection string using direct interface target"""


# ============================================================================
# Builder Classes
# ============================================================================

class _BaseResourceOptions(TypedDict, total=False):
    """Base resource options."""

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

    def with_optional_string(self, /, *, value: str | None = None, enabled: bool | None = None) -> Self:
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

    def with_config(self, config: TestConfigDto, /) -> Self:
        """Configures the resource with a DTO"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['config'] = config
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withConfig',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_created_at(self, created_at: str, /) -> Self:
        """Sets the created timestamp"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['createdAt'] = created_at
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_modified_at(self, modified_at: str, /) -> Self:
        """Sets the modified timestamp"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['modifiedAt'] = modified_at
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_correlation_id(self, correlation_id: str, /) -> Self:
        """Sets the correlation ID"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['correlationId'] = correlation_id
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_optional_callback(self, /, *, callback: Callable[[TestCallbackContext], None] | None = None) -> Self:
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

    def with_status(self, status: TestResourceStatus, /) -> Self:
        """Sets the resource status"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['status'] = status
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withStatus',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_nested_config(self, config: TestNestedDto, /) -> Self:
        """Configures with nested DTO"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['config'] = config
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_validator(self, validator: Callable[[TestResourceContext], bool], /) -> Self:
        """Adds validation callback"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['validator'] = self._client.register_callback(validator)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withValidator',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def test_wait_for(self, dependency: Resource, /) -> Self:
        """Waits for another resource (test version)"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_dependency(self, dependency: ResourceWithConnectionString, /) -> Self:
        """Adds a dependency on another resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['dependency'] = dependency
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withDependency',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_endpoints(self, endpoints: Iterable[str], /) -> Self:
        """Sets the endpoints"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['endpoints'] = endpoints
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_cancellable_operation(self, operation: Callable[[int], None], /) -> Self:
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
                raise TypeError("Invalid type for option 'optional_string'")
        if _config := kwargs.pop("config", None):
            if _validate_type(_config, TestConfigDto):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["config"] = _config
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withConfig', rpc_args))
            else:
                raise TypeError("Invalid type for option 'config'")
        if _created_at := kwargs.pop("created_at", None):
            if _validate_type(_created_at, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["createdAt"] = _created_at
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withCreatedAt', rpc_args))
            else:
                raise TypeError("Invalid type for option 'created_at'")
        if _modified_at := kwargs.pop("modified_at", None):
            if _validate_type(_modified_at, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["modifiedAt"] = _modified_at
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withModifiedAt', rpc_args))
            else:
                raise TypeError("Invalid type for option 'modified_at'")
        if _correlation_id := kwargs.pop("correlation_id", None):
            if _validate_type(_correlation_id, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["correlationId"] = _correlation_id
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withCorrelationId', rpc_args))
            else:
                raise TypeError("Invalid type for option 'correlation_id'")
        if _optional_callback := kwargs.pop("optional_callback", None):
            if _validate_type(_optional_callback, Callable[[TestCallbackContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_optional_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback', rpc_args))
            elif _optional_callback is True:
                rpc_args: dict[str, Any] = {"builder": handle}
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withOptionalCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'optional_callback'")
        if _status := kwargs.pop("status", None):
            if _validate_type(_status, TestResourceStatus):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["status"] = _status
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withStatus', rpc_args))
            else:
                raise TypeError("Invalid type for option 'status'")
        if _nested_config := kwargs.pop("nested_config", None):
            if _validate_type(_nested_config, TestNestedDto):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["config"] = _nested_config
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withNestedConfig', rpc_args))
            else:
                raise TypeError("Invalid type for option 'nested_config'")
        if _validator := kwargs.pop("validator", None):
            if _validate_type(_validator, Callable[[TestResourceContext], bool]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["validator"] = client.register_callback(_validator)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withValidator', rpc_args))
            else:
                raise TypeError("Invalid type for option 'validator'")
        if _test_wait_for := kwargs.pop("test_wait_for", None):
            if _validate_type(_test_wait_for, Resource):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = _test_wait_for
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/testWaitFor', rpc_args))
            else:
                raise TypeError("Invalid type for option 'test_wait_for'")
        if _dependency := kwargs.pop("dependency", None):
            if _validate_type(_dependency, ResourceWithConnectionString):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["dependency"] = _dependency
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withDependency', rpc_args))
            else:
                raise TypeError("Invalid type for option 'dependency'")
        if _endpoints := kwargs.pop("endpoints", None):
            if _validate_type(_endpoints, Iterable[str]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["endpoints"] = _endpoints
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withEndpoints', rpc_args))
            else:
                raise TypeError("Invalid type for option 'endpoints'")
        if _cancellable_operation := kwargs.pop("cancellable_operation", None):
            if _validate_type(_cancellable_operation, Callable[[int], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["operation"] = client.register_callback(_cancellable_operation)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withCancellableOperation', rpc_args))
            else:
                raise TypeError("Invalid type for option 'cancellable_operation'")
        self._handle = handle
        self._client = client
        if kwargs:
            raise TypeError(f"Unexpected keyword arguments: {list(kwargs.keys())}")


class ContainerResourceOptions(_BaseResourceOptions, total=False):
    """ContainerResource options."""

    test_with_env_callback: Callable[[TestEnvironmentContext], None]
    env_vars: Mapping[str, str]

class ContainerResource(_BaseResource, ResourceWithEnvironment, ResourceWithArgs, ResourceWithEndpoints, ResourceWithWaitSupport, ResourceWithProbes, ComputeResource):
    """ContainerResource resource."""

    def __repr__(self) -> str:
        return "ContainerResource(handle={self._handle.handle_id})"

    def test_with_env_callback(self, callback: Callable[[TestEnvironmentContext], None], /) -> Self:
        """Configures environment with callback (test version)"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['callback'] = self._client.register_callback(callback)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_env_vars(self, vars: Mapping[str, str], /) -> Self:
        """Sets environment variables"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['variables'] = vars
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def __init__(self, handle: Handle, client: AspireClient, **kwargs: Unpack[ContainerResourceOptions]) -> None:
        if _test_with_env_callback := kwargs.pop("test_with_env_callback", None):
            if _validate_type(_test_with_env_callback, Callable[[TestEnvironmentContext], None]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["callback"] = client.register_callback(_test_with_env_callback)
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/testWithEnvironmentCallback', rpc_args))
            else:
                raise TypeError("Invalid type for option 'test_with_env_callback'")
        if _env_vars := kwargs.pop("env_vars", None):
            if _validate_type(_env_vars, Mapping[str, str]):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["variables"] = _env_vars
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withEnvironmentVariables', rpc_args))
            else:
                raise TypeError("Invalid type for option 'env_vars'")
        super().__init__(handle, client, **kwargs)


class TestRedisResourceOptions(ContainerResourceOptions, total=False):
    """TestRedisResource options."""

    persistence: TestPersistenceMode | Literal[True]
    connection_string: ReferenceExpression
    connection_string_direct: str
    redis_specific: str

class TestRedisResource(ContainerResource, ResourceWithConnectionString):
    """TestRedisResource resource."""

    def __repr__(self) -> str:
        return "TestRedisResource(handle={self._handle.handle_id})"

    def with_persistence(self, /, *, mode: TestPersistenceMode | None = None) -> Self:
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

    def get_tags(self, /) -> AspireList[str]:
        """Gets the tags for the resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getTags',
            rpc_args,
        )
        return cast(AspireList[str], result)

    def get_metadata(self, /) -> AspireDict[str, str]:
        """Gets the metadata for the resource"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getMetadata',
            rpc_args,
        )
        return cast(AspireDict[str, str], result)

    def with_connection_string(self, connection_string: ReferenceExpression, /) -> Self:
        """Sets the connection string using a reference expression"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['connectionString'] = connection_string
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionString',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_endpoints(self, /) -> Iterable[str]:
        """Gets the endpoints"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getEndpoints',
            rpc_args,
        )
        return cast(Iterable[str], result)

    def with_connection_string_direct(self, connection_string: str, /) -> Self:
        """Sets connection string using direct interface target"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['connectionString'] = connection_string
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionStringDirect',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def with_redis_specific(self, option: str, /) -> Self:
        """Redis-specific configuration"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        rpc_args['option'] = option
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/withRedisSpecific',
            rpc_args,
        )
        self._handle = self._wrap_builder(result)
        return self

    def get_status(self, /, *, timeout: int | None = None) -> str:
        """Gets the status of the resource asynchronously"""
        rpc_args: dict[str, Any] = {'builder': self._handle}
        if timeout is not None:
            rpc_args['cancellationToken'] = self._client.register_cancellation_token(timeout)
        result = self._client.invoke_capability(
            'Aspire.Hosting.CodeGeneration.Python.Tests/getStatusAsync',
            rpc_args,
        )
        return cast(str, result)

    def wait_for_ready(self, timeout: float, /, *, timeout: int | None = None) -> bool:
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
                raise TypeError("Invalid type for option 'persistence'")
        if _connection_string := kwargs.pop("connection_string", None):
            if _validate_type(_connection_string, ReferenceExpression):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["connectionString"] = _connection_string
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionString', rpc_args))
            else:
                raise TypeError("Invalid type for option 'connection_string'")
        if _connection_string_direct := kwargs.pop("connection_string_direct", None):
            if _validate_type(_connection_string_direct, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["connectionString"] = _connection_string_direct
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withConnectionStringDirect', rpc_args))
            else:
                raise TypeError("Invalid type for option 'connection_string_direct'")
        if _redis_specific := kwargs.pop("redis_specific", None):
            if _validate_type(_redis_specific, str):
                rpc_args: dict[str, Any] = {"builder": handle}
                rpc_args["option"] = _redis_specific
                handle = self._wrap_builder(client.invoke_capability('Aspire.Hosting.CodeGeneration.Python.Tests/withRedisSpecific', rpc_args))
            else:
                raise TypeError("Invalid type for option 'redis_specific'")
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


# TODO: These kwargs should be generated dynamically based on CreateBuilderOptions
def create_builder(
    *,
    debug: bool | None = None,
    args: Iterable[str] | None = None,
    project_directory: str | None = None,
    container_registry_override: str | None = None,
    disable_dashboard: bool | None = None,
    dashboard_application_name: str | None = None,
    allow_unsecured_transport: bool | None = None,
    enable_resource_logging: bool | None = None,
    heartbeat_interval: int | None = None,
 ) -> AbstractContextManager[DistributedApplicationBuilder]:
    '''
    Creates a new distributed application builder.
    This is the entry point for building Aspire applications.

    Args:
        **options: Optional configuration options for the builder

    Returns:
        A DistributedApplicationBuilder instance
    '''
    is_debug = debug if debug is not None else os.environ.get('ASPIRE_DEBUG', 'false').lower() == 'true'
    client = _get_client(debug=is_debug, heartbeat_interval=heartbeat_interval)

    # Default args and project_directory if not provided
    effective_options = CreateBuilderOptions(
        Args = args if args is not None else sys.argv[1:],
        ProjectDirectory = project_directory if project_directory is not None else os.environ.get('ASPIRE_PROJECT_DIRECTORY', os.getcwd()),
    )
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
