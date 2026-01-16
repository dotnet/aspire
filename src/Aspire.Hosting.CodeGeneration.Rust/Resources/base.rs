//! Base types for Aspire Rust SDK.

use std::collections::HashMap;
use std::sync::Arc;

use serde::{Serialize, Deserialize};
use serde_json::{json, Value};

use crate::transport::{AspireClient, Handle};

/// Base type for all handle wrappers.
pub struct HandleWrapperBase {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HandleWrapperBase {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }
}

/// Base type for resource builders.
pub struct ResourceBuilderBase {
    base: HandleWrapperBase,
}

impl ResourceBuilderBase {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self {
            base: HandleWrapperBase::new(handle, client),
        }
    }

    pub fn handle(&self) -> &Handle {
        self.base.handle()
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        self.base.client()
    }
}

/// A reference expression for dynamic values.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ReferenceExpression {
    pub format: String,
    pub args: Vec<Value>,
}

impl ReferenceExpression {
    pub fn new(format: impl Into<String>, args: Vec<Value>) -> Self {
        Self {
            format: format.into(),
            args,
        }
    }

    pub fn to_json(&self) -> Value {
        json!({
            "$refExpr": {
                "format": self.format,
                "args": self.args
            }
        })
    }
}

/// Convenience function to create a reference expression.
pub fn ref_expr(format: impl Into<String>, args: Vec<Value>) -> ReferenceExpression {
    ReferenceExpression::new(format, args)
}

/// A handle-backed list.
pub struct AspireList<T> {
    base: HandleWrapperBase,
    _marker: std::marker::PhantomData<T>,
}

impl<T> AspireList<T> {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self {
            base: HandleWrapperBase::new(handle, client),
            _marker: std::marker::PhantomData,
        }
    }

    pub fn handle(&self) -> &Handle {
        self.base.handle()
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        self.base.client()
    }
}

/// A handle-backed dictionary.
pub struct AspireDict<K, V> {
    base: HandleWrapperBase,
    _key_marker: std::marker::PhantomData<K>,
    _value_marker: std::marker::PhantomData<V>,
}

impl<K, V> AspireDict<K, V> {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self {
            base: HandleWrapperBase::new(handle, client),
            _key_marker: std::marker::PhantomData,
            _value_marker: std::marker::PhantomData,
        }
    }

    pub fn handle(&self) -> &Handle {
        self.base.handle()
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        self.base.client()
    }
}

/// Trait for types that can be serialized to JSON.
pub trait ToJson {
    fn to_json(&self) -> Value;
}

impl ToJson for Handle {
    fn to_json(&self) -> Value {
        self.to_json()
    }
}

impl ToJson for ReferenceExpression {
    fn to_json(&self) -> Value {
        self.to_json()
    }
}

/// Serialize a value to its JSON representation.
pub fn serialize_value(value: impl Into<Value>) -> Value {
    value.into()
}

/// Serialize a handle wrapper to its JSON representation.
pub fn serialize_handle(wrapper: &impl HasHandle) -> Value {
    wrapper.handle().to_json()
}

/// Trait for types that have an underlying handle.
pub trait HasHandle {
    fn handle(&self) -> &Handle;
}

impl HasHandle for HandleWrapperBase {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl HasHandle for ResourceBuilderBase {
    fn handle(&self) -> &Handle {
        self.base.handle()
    }
}
