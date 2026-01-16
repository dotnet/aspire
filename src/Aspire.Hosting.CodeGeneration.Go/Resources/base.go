// Package aspire provides base types and utilities for Aspire Go SDK.
package aspire

import (
	"fmt"
)

// HandleWrapperBase is the base type for all handle wrappers.
type HandleWrapperBase struct {
	handle *Handle
	client *AspireClient
}

// NewHandleWrapperBase creates a new handle wrapper base.
func NewHandleWrapperBase(handle *Handle, client *AspireClient) HandleWrapperBase {
	return HandleWrapperBase{handle: handle, client: client}
}

// Handle returns the underlying handle.
func (h *HandleWrapperBase) Handle() *Handle {
	return h.handle
}

// Client returns the client.
func (h *HandleWrapperBase) Client() *AspireClient {
	return h.client
}

// ResourceBuilderBase extends HandleWrapperBase for resource builders.
type ResourceBuilderBase struct {
	HandleWrapperBase
}

// NewResourceBuilderBase creates a new resource builder base.
func NewResourceBuilderBase(handle *Handle, client *AspireClient) ResourceBuilderBase {
	return ResourceBuilderBase{HandleWrapperBase: NewHandleWrapperBase(handle, client)}
}

// ReferenceExpression represents a reference expression.
type ReferenceExpression struct {
	Format string
	Args   []any
}

// NewReferenceExpression creates a new reference expression.
func NewReferenceExpression(format string, args ...any) *ReferenceExpression {
	return &ReferenceExpression{Format: format, Args: args}
}

// RefExpr is a convenience function for creating reference expressions.
func RefExpr(format string, args ...any) *ReferenceExpression {
	return NewReferenceExpression(format, args...)
}

// ToJSON returns the reference expression as a JSON-serializable map.
func (r *ReferenceExpression) ToJSON() map[string]any {
	return map[string]any{
		"$refExpr": map[string]any{
			"format": r.Format,
			"args":   r.Args,
		},
	}
}

// AspireList is a handle-backed list.
type AspireList[T any] struct {
	HandleWrapperBase
}

// NewAspireList creates a new AspireList.
func NewAspireList[T any](handle *Handle, client *AspireClient) *AspireList[T] {
	return &AspireList[T]{HandleWrapperBase: NewHandleWrapperBase(handle, client)}
}

// AspireDict is a handle-backed dictionary.
type AspireDict[K comparable, V any] struct {
	HandleWrapperBase
}

// NewAspireDict creates a new AspireDict.
func NewAspireDict[K comparable, V any](handle *Handle, client *AspireClient) *AspireDict[K, V] {
	return &AspireDict[K, V]{HandleWrapperBase: NewHandleWrapperBase(handle, client)}
}

// SerializeValue converts a value to its JSON representation.
func SerializeValue(value any) any {
	if value == nil {
		return nil
	}

	switch v := value.(type) {
	case *Handle:
		return v.ToJSON()
	case *ReferenceExpression:
		return v.ToJSON()
	case interface{ ToJSON() map[string]any }:
		return v.ToJSON()
	case interface{ Handle() *Handle }:
		return v.Handle().ToJSON()
	case []any:
		result := make([]any, len(v))
		for i, item := range v {
			result[i] = SerializeValue(item)
		}
		return result
	case map[string]any:
		result := make(map[string]any)
		for k, val := range v {
			result[k] = SerializeValue(val)
		}
		return result
	case fmt.Stringer:
		return v.String()
	default:
		return value
	}
}
