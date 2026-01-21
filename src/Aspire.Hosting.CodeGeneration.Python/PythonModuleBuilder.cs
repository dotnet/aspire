// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Aspire.Hosting.CodeGeneration.Python;

/// <summary>
/// Represents a builder for generating Python module structure and related code components.
/// Sections are built independently and combined in the correct order when Write() is called.
/// </summary>
internal sealed class PythonModuleBuilder
{
    /// <summary>
    /// Gets the enum type definitions.
    /// </summary>
    public StringBuilder Enums { get; } = new();

    /// <summary>
    /// Gets the DTO class definitions.
    /// </summary>
    public StringBuilder DtoClasses { get; } = new();

    /// <summary>
    /// Gets the type class definitions (context types, wrapper types).
    /// </summary>
    public StringBuilder TypeClasses { get; } = new();

    /// <summary>
    /// Gets the interface class definitions
    /// </summary>
    public StringBuilder InterfaceClasses { get; } = new();

    /// <summary>
    /// Gets the resource builder class definitions.
    /// </summary>
    public StringBuilder ResourceBuilders { get; } = new();

    /// <summary>
    /// Gets the entry point function definitions.
    /// </summary>
    public StringBuilder EntryPoints { get; } = new();

    /// <summary>
    /// Gets the handle registration definitions.
    /// </summary>
    public StringBuilder HandleRegistrations { get; } = new();

    /// <summary>
    /// Gets the connection helper definitions.
    /// </summary>
    public StringBuilder ConnectionHelper { get; } = new();

    /// <summary>
    /// Writes the complete Python module content.
    /// </summary>
    /// <returns>The complete Python module as a string.</returns>
    public string Write()
    {
        var output = new StringBuilder();
        output.AppendLine(Header);
        output.AppendLine(StandardImports);
        output.AppendLine(Utils);

        // Enums
        if (Enums.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Enum Types");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(Enums);
        }

        // DTO Classes
        if (DtoClasses.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# DTO Classes (Data Transfer Objects)");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(DtoClasses);
        }

        // Type Classes
        if (TypeClasses.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Type Classes");
            output.AppendLine("# ============================================================================");
            output.Append(TypeClasses);
        }

        // Interface Classes
        if (InterfaceClasses.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Interface Classes");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(InterfaceClasses);
        }

        // Resource Builder Classes
        if (ResourceBuilders.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Builder Classes");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(ResourceBuilders);
        }

        // Entry Points
        if (EntryPoints.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Entry Point Functions");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(EntryPoints);
        }

        // Connection Helper
        if (ConnectionHelper.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Connection Helper");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(ConnectionHelper);
        }

        // Handle Registrations
        if (HandleRegistrations.Length > 0)
        {
            output.AppendLine();
            output.AppendLine("# ============================================================================");
            output.AppendLine("# Handle Registrations");
            output.AppendLine("# ============================================================================");
            output.AppendLine();
            output.Append(HandleRegistrations);
        }

        return output.ToString();
    }

    /// <summary>
    /// The file header with copyright notice.
    /// </summary>
    public const string Header = """
        #   -------------------------------------------------------------
        #   Copyright (c) Microsoft Corporation. All rights reserved.
        #   Licensed under the MIT License. See LICENSE in project root for information.
        #
        #   This is a generated file. Any modifications may be overwritten.
        #   -------------------------------------------------------------

        """;

    /// <summary>
    /// Standard Python imports for the generated SDK.
    /// </summary>
    public const string StandardImports = """
        from __future__ import annotations

        import os
        import sys
        import logging
        import threading
        from functools import cached_property
        from abc import ABC, abstractmethod
        from contextlib import AbstractContextManager
        from re import compile
        from dataclasses import dataclass
        from warnings import warn
        from collections.abc import Iterable, Mapping, Callable
        from typing import (
            Any, Unpack, Self, Literal, TypedDict, Annotated, Required, Generic, TypeVar,
            get_origin, get_args, get_type_hints, cast, overload, runtime_checkable
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

        """;

    /// <summary>
    /// Utility functions for the generated SDK.
    /// </summary>
    public const string Utils = """
        _VALID_NAME = compile(r'^[a-zA-Z0-9-]+$')
        _LOG = logging.getLogger("aspyre")
        uncached_property = property


        def _valid_var_name(name: str) -> str:
            if not _VALID_NAME.match(name):
                raise ValueError(f"Invalid name '{name}'. Only alphanumeric characters and hyphens are allowed.")
            return name.replace("-", "_")


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


        def _default(value: Any, default: Any) -> Any:
            if value is None:
                return default
            return value


        @dataclass
        class Warnings:
            experimental: str | None


        class AspyreExperimentalWarning(Warning):
            '''Custom warning for experimental features in Aspire.'''


        class AspyreOperationError(Exception):
            '''Error in constructing an Aspire resource.'''


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


        """;

        public const string DistributedApplicationBuilder = """
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

            def run(self, timeout: int | None = None) -> None:
                '''Builds and runs the distributed application.'''
                app = self.build()
                app.run(timeout)

        """;
}
