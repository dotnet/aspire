# base.py - Core Aspire types: base classes, ReferenceExpression

from __future__ import annotations

from typing import Any, Mapping, TypeVar, Iterator, Iterable, cast, overload
from collections.abc import MutableSequence, MutableMapping

from ._transport import (
    Handle,
    ReferenceHandle,
    AspireClient,
    _register_handle_wrapper,
)



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

    def __init__(self, ref: Handle | str, *value_providers: Any) -> None:
        """
        Creates a reference expression from a format string and value providers.

        Args:
            format_str: Format string with {0}, {1}, etc. placeholders
            *value_providers: Handles to value providers

        Returns:
            A ReferenceExpression instance
        """
        providers = [_extract_handle_for_expr(v) for v in value_providers]
        self._handle = None
        self._format = None
        if isinstance(ref, Handle):
            self._handle = ref
        else:
            self._format = ref
        self._value_providers = providers

    def to_json(self) -> Mapping[str, Any]:
        """
        Serializes the reference expression for JSON-RPC transport.
        Uses the $expr format recognized by the server.
        """
        if self._handle:
            return self._handle.to_json()

        result: dict[str, Any] = {
            "$expr": {
                "format": self._format,
            }
        }
        if self._value_providers:
            result["$expr"]["valueProviders"] = self._value_providers
        return result

    def __repr__(self) -> str:
        return "ReferenceExpression()"


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
    if isinstance(value, (Handle, ReferenceHandle)):
        return value

    # Objects with $handle property (already in handle format)
    if isinstance(value, dict) and "$handle" in value:
        return value

    raise ValueError(
        f"Cannot use value of type {type(value).__name__} in reference expression. "
        f"Expected a handle object, string, or number."
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

    return ReferenceExpression(format_str, *value_providers)


# ============================================================================
# AspireList[T] - Mutable List Wrapper
# ============================================================================

TItem = TypeVar("TItem")


class AspireList(MutableSequence[TItem]):
    """
    Wrapper for a mutable .NET List<T>.
    Maintains an in-memory list for all operations and syncs to the server on commit.

    This class implements the Python MutableSequence protocol. All list
    operations are performed on the in-memory list, and changes are pushed to
    the server when `commit()` is called.

    Example:
        ```python
        items = await resource.get_items()  # Returns AspireList[ItemBuilder]

        # Modify the list locally
        items.append(new_item)
        items[0] = another_item
        del items[1]

        # Push changes to the server
        items.commit()
        ```
    """

    def __init__(
        self,
        handle: Handle,
        client: AspireClient,
    ) -> None:
        self._handle = handle
        self._client = client

    # ---- Required abstract methods from MutableSequence ----

    def __len__(self) -> int:
        """Gets the number of elements in the list."""
        result = self._client.invoke_capability(
            "Aspire.Hosting/List.length",
            {"list": self._handle}
        )
        return int(result)

    @overload
    def __getitem__(self, index: int) -> TItem: ...
    @overload
    def __getitem__(self, index: slice) -> list[TItem]: ...
    def __getitem__(self, index: int | slice) -> TItem | list[TItem]:
        """Gets the element at the specified index or a slice of elements."""
        if not isinstance(index, int):
            raise NotImplementedError("Get by slice not yet supported.")
        result = self._client.invoke_capability(
            "Aspire.Hosting/List.get",
            {"list": self._handle, "index": index}
        )
        return list(result)

    @overload
    def __setitem__(self, index: int, value: TItem) -> None: ...
    @overload
    def __setitem__(self, index: slice, value: Iterable[TItem]) -> None: ...
    def __setitem__(self, index: int | slice, value: TItem | Iterable[TItem]) -> None:
        """Sets the element at the specified index or replaces a slice of elements."""
        if not isinstance(index, int):
            raise NotImplementedError("Set by slice not yet supported.")
        self._client.invoke_capability(
            "Aspire.Hosting/List.set",
            {"list": self._handle, "index": index, "value": value}
        )

    @overload
    def __delitem__(self, index: int) -> None: ...
    @overload
    def __delitem__(self, index: slice) -> None: ...
    def __delitem__(self, index: int | slice) -> None:
        """Deletes the element at the specified index or a slice of elements."""
        if not isinstance(index, int):
            raise NotImplementedError("Delete by slice not yet supported.")
        self._client.invoke_capability(
            "Aspire.Hosting/List.removeAt",
            {"list": self._handle, "index": index}
        )

    def insert(self, index: int, value: TItem) -> None:
        """Inserts an element at the specified index."""
        self._client.invoke_capability(
            "Aspire.Hosting/List.set",
            {"list": self._handle, "index": index, "item": value}
        )

    def __repr__(self) -> str:
        """Returns a string representation of the list."""
        return f"AspireList()"


# ============================================================================
# AspireDict[K, V] - Mutable Dictionary Wrapper
# ============================================================================

TKey = TypeVar("TKey")
TValue = TypeVar("TValue")


class AspireDict(MutableMapping[TKey, TValue]):
    """
    Wrapper for a mutable .NET Dictionary<K, V>.
    Maintains an in-memory dict for all operations and syncs to the server on commit.

    This class implements the Python MutableMapping protocol. All dict
    operations are performed on the in-memory dict, and changes are pushed to
    the server when `commit()` is called.

    Example:
        ```python
        config = await resource.get_config()  # Returns AspireDict[str, str]

        # Modify the dict locally
        config["key"] = "value"
        del config["old_key"]

        # Push changes to the server
        config.commit()
        ```
    """

    def __init__(
        self,
        handle: Handle,
        client: AspireClient,
    ) -> None:
        self._handle = handle
        self._client = client

    def __len__(self) -> int:
        """Gets the number of key-value pairs in the dictionary."""
        result = self._client.invoke_capability(
            "Aspire.Hosting/Dict.count",
            {"dict": self._handle}
        )
        return int(result)

    def __getitem__(self, key: TKey) -> TValue:
        """Gets the value associated with the specified key."""
        result = self._client.invoke_capability(
            "Aspire.Hosting/Dict.get",
            {"dict": self._handle, "key": key}
        )
        if result is None:
            raise KeyError(key)
        return result

    def __setitem__(self, key: TKey, value: TValue) -> None:
        """Sets the value for the specified key."""
        self._client.invoke_capability(
            "Aspire.Hosting/Dict.set",
            {"dict": self._handle, "key": key, "value": value}
        )

    def __delitem__(self, key: TKey) -> None:
        """Removes the key-value pair with the specified key."""
        result = self._client.invoke_capability(
            "Aspire.Hosting/Dict.remove",
            {"dict": self._handle, "key": key}
        )
        if result is False:
            raise KeyError(key)

    def __iter__(self) -> Iterator[TKey]:
        """Returns an iterator over the keys."""
        result = self._client.invoke_capability(
            "Aspire.Hosting/Dict.keys",
            {"dict": self._handle}
        )
        return iter(result)

    def __contains__(self, key: object) -> bool:
        result = self._client.invoke_capability(
            "Aspire.Hosting/Dict.has",
            {"dict": self._handle, "key": key}
        )
        return result

    def __repr__(self) -> str:
        """Returns a string representation of the dictionary."""
        return f"AspireDict()"

    def clear(self) -> None:
        self._client.invoke_capability(
            "Aspire.Hosting/Dict.clear",
            {"dict": self._handle}
        )


_register_handle_wrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression", lambda handle, _: ReferenceExpression(handle))
