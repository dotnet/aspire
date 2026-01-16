# base.py - Core Aspire types: base classes, reference expressions, collections
from __future__ import annotations

from enum import Enum
from typing import Any, Dict, Iterable, List

from transport import AspireClient, Handle


class ReferenceExpression:
    """Represents a reference expression passed to capabilities."""

    def __init__(self, format_string: str, value_providers: List[Any]) -> None:
        self._format_string = format_string
        self._value_providers = value_providers

    @staticmethod
    def create(format_string: str, *values: Any) -> "ReferenceExpression":
        value_providers = [_extract_reference_value(value) for value in values]
        return ReferenceExpression(format_string, value_providers)

    def to_json(self) -> Dict[str, Any]:
        payload: Dict[str, Any] = {"format": self._format_string}
        if self._value_providers:
            payload["valueProviders"] = self._value_providers
        return {"$expr": payload}

    def __str__(self) -> str:
        return f"ReferenceExpression({self._format_string})"


def ref_expr(format_string: str, *values: Any) -> ReferenceExpression:
    """Create a reference expression using a format string."""
    return ReferenceExpression.create(format_string, *values)


class HandleWrapperBase:
    """Base wrapper for ATS handle types."""

    def __init__(self, handle: Handle, client: AspireClient) -> None:
        self._handle = handle
        self._client = client

    def to_json(self) -> Dict[str, str]:
        return self._handle.to_json()


class ResourceBuilderBase(HandleWrapperBase):
    """Base class for resource builder wrappers."""


class AspireList(HandleWrapperBase):
    """Wrapper for mutable list handles."""

    def count(self) -> int:
        return self._client.invoke_capability(
            "Aspire.Hosting/List.length",
            {"list": self._handle}
        )

    def get(self, index: int) -> Any:
        return self._client.invoke_capability(
            "Aspire.Hosting/List.get",
            {"list": self._handle, "index": index}
        )

    def add(self, item: Any) -> None:
        self._client.invoke_capability(
            "Aspire.Hosting/List.add",
            {"list": self._handle, "item": serialize_value(item)}
        )

    def remove_at(self, index: int) -> bool:
        return self._client.invoke_capability(
            "Aspire.Hosting/List.removeAt",
            {"list": self._handle, "index": index}
        )

    def clear(self) -> None:
        self._client.invoke_capability(
            "Aspire.Hosting/List.clear",
            {"list": self._handle}
        )

    def to_list(self) -> List[Any]:
        return self._client.invoke_capability(
            "Aspire.Hosting/List.toArray",
            {"list": self._handle}
        )


class AspireDict(HandleWrapperBase):
    """Wrapper for mutable dictionary handles."""

    def count(self) -> int:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.count",
            {"dict": self._handle}
        )

    def get(self, key: str) -> Any:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.get",
            {"dict": self._handle, "key": key}
        )

    def set(self, key: str, value: Any) -> None:
        self._client.invoke_capability(
            "Aspire.Hosting/Dict.set",
            {"dict": self._handle, "key": key, "value": serialize_value(value)}
        )

    def contains_key(self, key: str) -> bool:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.has",
            {"dict": self._handle, "key": key}
        )

    def remove(self, key: str) -> bool:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.remove",
            {"dict": self._handle, "key": key}
        )

    def keys(self) -> List[str]:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.keys",
            {"dict": self._handle}
        )

    def values(self) -> List[Any]:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.values",
            {"dict": self._handle}
        )

    def to_dict(self) -> Dict[str, Any]:
        return self._client.invoke_capability(
            "Aspire.Hosting/Dict.toObject",
            {"dict": self._handle}
        )


def serialize_value(value: Any) -> Any:
    if isinstance(value, ReferenceExpression):
        return value.to_json()

    if isinstance(value, Handle):
        return value.to_json()

    if hasattr(value, "to_json") and callable(value.to_json):
        return value.to_json()

    if hasattr(value, "to_dict") and callable(value.to_dict):
        return {key: serialize_value(val) for key, val in value.to_dict().items()}

    if isinstance(value, Enum):
        return value.value

    if isinstance(value, list):
        return [serialize_value(item) for item in value]

    if isinstance(value, tuple):
        return [serialize_value(item) for item in value]

    if isinstance(value, dict):
        return {key: serialize_value(val) for key, val in value.items()}

    return value


def _extract_reference_value(value: Any) -> Any:
    if value is None:
        raise ValueError("Cannot use None in reference expressions.")

    if isinstance(value, (str, int, float)):
        return value

    if isinstance(value, Handle):
        return value.to_json()

    if hasattr(value, "to_json") and callable(value.to_json):
        return value.to_json()

    raise ValueError(f"Unsupported reference expression value: {type(value).__name__}")
