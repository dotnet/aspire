# base.py - Core Aspire types: base classes, ReferenceExpression

from __future__ import annotations

from typing import Any, Generic, TypeVar, Callable, Awaitable, cast

from ._transport import (
    Handle,
    AspireClient,
    MarshalledHandle,
    register_callback,
    unregister_callback,
    CapabilityError,
)

# Re-export transport types for convenience
__all__ = [
    "Handle",
    "AspireClient",
    "CapabilityError",
    "register_callback",
    "unregister_callback",
    "MarshalledHandle",
    "ReferenceExpression",
    "ref_expr",
    "ResourceBuilderBase",
    "AspireList",
    "AspireDict",
]


# ============================================================================
# Reference Expression
# ============================================================================

class ReferenceExpression:
    """
    Represents a reference expression that can be passed to capabilities.

    Reference expressions are serialized in the protocol as:
    ```json
    {
      "$expr": {
        "format": "redis://{0}:{1}",
        "valueProviders": [
          { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReference:1" },
          { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReference:2" }
        ]
      }
    }
    ```

    Example:
        ```python
        redis = await builder.add_redis("cache")
        endpoint = await redis.get_endpoint("tcp")

        # Create a reference expression
        expr = ref_expr(f"redis://{endpoint}:6379")

        # Use it in an environment variable
        await api.with_environment("REDIS_URL", expr)
        ```
    """

    def __init__(self, format_str: str, value_providers: list[Any]) -> None:
        self._format = format_str
        self._value_providers = value_providers

    @staticmethod
    def create(format_str: str, *value_providers: Any) -> ReferenceExpression:
        """
        Creates a reference expression from a format string and value providers.

        Args:
            format_str: Format string with {0}, {1}, etc. placeholders
            *value_providers: Handles to value providers

        Returns:
            A ReferenceExpression instance
        """
        providers = [_extract_handle_for_expr(v) for v in value_providers]
        return ReferenceExpression(format_str, providers)

    def to_json(self) -> dict[str, Any]:
        """
        Serializes the reference expression for JSON-RPC transport.
        Uses the $expr format recognized by the server.
        """
        result: dict[str, Any] = {
            "$expr": {
                "format": self._format,
            }
        }
        if self._value_providers:
            result["$expr"]["valueProviders"] = self._value_providers
        return result

    def __str__(self) -> str:
        """String representation for debugging."""
        return f"ReferenceExpression({self._format})"

    def __repr__(self) -> str:
        return self.__str__()


def _extract_handle_for_expr(value: Any) -> Any:
    """
    Extracts a value for use in reference expressions.
    Supports handles (objects) and string literals.
    """
    if value is None:
        raise ValueError("Cannot use None in reference expression")

    # String literals - include directly in the expression
    if isinstance(value, str):
        return value

    # Number literals - convert to string
    if isinstance(value, (int, float)):
        return str(value)

    # Handle objects - get their JSON representation
    if isinstance(value, Handle):
        return value.to_json()

    # Objects with to_json method that returns a handle
    if hasattr(value, "to_json"):
        json_val = value.to_json()
        if isinstance(json_val, dict) and "$handle" in json_val:
            return json_val

    # Objects with $handle property (already in handle format)
    if isinstance(value, dict) and "$handle" in value:
        return value

    raise ValueError(
        f"Cannot use value of type {type(value).__name__} in reference expression. "
        f"Expected a Handle, string, or number."
    )


def ref_expr(template: str, **kwargs: Any) -> ReferenceExpression:
    """
    Helper function for creating reference expressions with named placeholders.

    Use this to create dynamic expressions that reference endpoints, parameters, and other
    value providers. The expression is evaluated at runtime by Aspire.

    Example:
        ```python
        redis = await builder.add_redis("cache")
        endpoint = await redis.get_endpoint("tcp")

        # Create a reference expression using named placeholders
        expr = ref_expr("redis://{host}:{port}", host=endpoint, port=6379)

        # Use it in an environment variable
        await api.with_environment("REDIS_URL", expr)
        ```
    """
    # Replace named placeholders with numbered ones
    value_providers = []
    format_str = template

    for key, value in kwargs.items():
        placeholder = f"{{{key}}}"
        if placeholder in format_str:
            index = len(value_providers)
            format_str = format_str.replace(placeholder, f"{{{index}}}")
            value_providers.append(value)

    return ReferenceExpression.create(format_str, *value_providers)


# ============================================================================
# ResourceBuilderBase
# ============================================================================

T = TypeVar("T", bound=Handle)


class ResourceBuilderBase(Generic[T]):
    """
    Base class for resource builders (e.g., RedisBuilder, ContainerBuilder).
    Provides handle management and JSON serialization.
    """

    def __init__(self, handle: T, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def to_json(self) -> MarshalledHandle:
        """Serialize for JSON-RPC transport"""
        return self._handle.to_json()


# ============================================================================
# AspireList[T] - Mutable List Wrapper
# ============================================================================

TItem = TypeVar("TItem")


class AspireList(Generic[TItem]):
    """
    Wrapper for a mutable .NET List<T>.
    Provides list-like methods that invoke capabilities on the underlying collection.

    Example:
        ```python
        items = await resource.get_items()  # Returns AspireList[ItemBuilder]
        count = await items.count()
        first = await items.get(0)
        await items.add(new_item)
        ```
    """

    def __init__(
        self,
        handle: Handle,
        client: AspireClient,
        type_id: str
    ) -> None:
        self._handle = handle
        self._client = client
        self._type_id = type_id

    async def count(self) -> int:
        """Gets the number of elements in the list."""
        result = await self._client.invoke_capability(
            "Aspire.Hosting/List.length",
            {"list": self._handle}
        )
        return int(result)

    async def get(self, index: int) -> TItem:
        """Gets the element at the specified index."""
        result = await self._client.invoke_capability(
            "Aspire.Hosting/List.get",
            {"list": self._handle, "index": index}
        )
        return result  # type: ignore

    async def add(self, item: TItem) -> None:
        """Adds an element to the end of the list."""
        await self._client.invoke_capability(
            "Aspire.Hosting/List.add",
            {"list": self._handle, "item": item}
        )

    async def remove_at(self, index: int) -> None:
        """Removes the element at the specified index."""
        await self._client.invoke_capability(
            "Aspire.Hosting/List.removeAt",
            {"list": self._handle, "index": index}
        )

    async def clear(self) -> None:
        """Clears all elements from the list."""
        await self._client.invoke_capability(
            "Aspire.Hosting/List.clear",
            {"list": self._handle}
        )

    async def to_array(self) -> list[TItem]:
        """Converts the list to a Python list (creates a copy)."""
        result = await self._client.invoke_capability(
            "Aspire.Hosting/List.toArray",
            {"list": self._handle}
        )
        return result  # type: ignore

    def to_json(self) -> MarshalledHandle:
        """Serialize for JSON-RPC transport"""
        return self._handle.to_json()


# ============================================================================
# AspireDict[K, V] - Mutable Dictionary Wrapper
# ============================================================================

TKey = TypeVar("TKey")
TValue = TypeVar("TValue")


class AspireDict(Generic[TKey, TValue]):
    """
    Wrapper for a mutable .NET Dictionary<K, V>.
    Provides dict-like methods that invoke capabilities on the underlying collection.

    Example:
        ```python
        config = await resource.get_config()  # Returns AspireDict[str, str]
        value = await config.get("key")
        await config.set("key", "value")
        has_key = await config.contains_key("key")
        ```
    """

    def __init__(
        self,
        handle_or_context: Handle,
        client: AspireClient,
        type_id: str,
        getter_capability_id: str | None = None
    ) -> None:
        self._handle_or_context = handle_or_context
        self._client = client
        self._type_id = type_id
        self._getter_capability_id = getter_capability_id
        self._resolved_handle: Handle | None = None

    async def _ensure_handle(self) -> Handle:
        """Ensures we have the actual dictionary handle by calling the getter if needed."""
        if self._resolved_handle:
            return self._resolved_handle

        # If no getter capability, the handle is already the dictionary handle
        if not self._getter_capability_id:
            self._resolved_handle = self._handle_or_context
            return self._resolved_handle

        # Call the getter capability to get the actual dictionary handle
        result = await self._client.invoke_capability(
            self._getter_capability_id,
            {"context": self._handle_or_context}
        )
        self._resolved_handle = result
        return cast(Handle, self._resolved_handle)

    async def count(self) -> int:
        """Gets the number of key-value pairs in the dictionary."""
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.count",
            {"dict": handle}
        )
        return int(result)

    async def get(self, key: TKey) -> TValue:
        """
        Gets the value associated with the specified key.
        Raises KeyError if the key is not found.
        """
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.get",
            {"dict": handle, "key": key}
        )
        return result  # type: ignore

    async def set(self, key: TKey, value: TValue) -> None:
        """Sets the value for the specified key."""
        handle = await self._ensure_handle()
        await self._client.invoke_capability(
            "Aspire.Hosting/Dict.set",
            {"dict": handle, "key": key, "value": value}
        )

    async def contains_key(self, key: TKey) -> bool:
        """Determines whether the dictionary contains the specified key."""
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.has",
            {"dict": handle, "key": key}
        )
        return bool(result)

    async def remove(self, key: TKey) -> bool:
        """
        Removes the value with the specified key.
        Returns True if the element was removed; False if the key was not found.
        """
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.remove",
            {"dict": handle, "key": key}
        )
        return bool(result)

    async def clear(self) -> None:
        """Clears all key-value pairs from the dictionary."""
        handle = await self._ensure_handle()
        await self._client.invoke_capability(
            "Aspire.Hosting/Dict.clear",
            {"dict": handle}
        )

    async def keys(self) -> list[TKey]:
        """Gets all keys in the dictionary."""
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.keys",
            {"dict": handle}
        )
        return result  # type: ignore

    async def values(self) -> list[TValue]:
        """Gets all values in the dictionary."""
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.values",
            {"dict": handle}
        )
        return result  # type: ignore

    async def to_dict(self) -> dict[TKey, TValue]:
        """Converts the dictionary to a Python dict (creates a copy)."""
        handle = await self._ensure_handle()
        result = await self._client.invoke_capability(
            "Aspire.Hosting/Dict.toObject",
            {"dict": handle}
        )
        return result  # type: ignore

    async def to_json(self) -> MarshalledHandle:
        """Serialize for JSON-RPC transport"""
        handle = await self._ensure_handle()
        return handle.to_json()
