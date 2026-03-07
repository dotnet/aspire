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
/// Supports value mode (format + args), conditional mode (condition + whenTrue + whenFalse),
/// and handle mode (wrapping a server-returned handle).
pub struct ReferenceExpression {
    // Value mode fields
    pub format: Option<String>,
    pub args: Option<Vec<Value>>,

    // Conditional mode fields
    condition: Option<Value>,
    when_true: Option<Box<ReferenceExpression>>,
    when_false: Option<Box<ReferenceExpression>>,
    match_value: Option<String>,
    is_conditional: bool,

    // Handle mode fields
    handle: Option<Handle>,
    client: Option<Arc<AspireClient>>,
}

impl ReferenceExpression {
    /// Creates a new value-mode reference expression.
    pub fn new(format: impl Into<String>, args: Vec<Value>) -> Self {
        Self {
            format: Some(format.into()),
            args: Some(args),
            condition: None,
            when_true: None,
            when_false: None,
            match_value: None,
            is_conditional: false,
            handle: None,
            client: None,
        }
    }

    /// Creates a new handle-mode reference expression from a server-returned handle.
    pub fn from_handle(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self {
            format: None,
            args: None,
            condition: None,
            when_true: None,
            when_false: None,
            match_value: None,
            is_conditional: false,
            handle: Some(handle),
            client: Some(client),
        }
    }

    /// Creates a conditional reference expression from its parts.
    pub fn create_conditional(condition: Value, match_value: Option<String>, when_true: ReferenceExpression, when_false: ReferenceExpression) -> Self {
        Self {
            format: None,
            args: None,
            condition: Some(condition),
            when_true: Some(Box::new(when_true)),
            when_false: Some(Box::new(when_false)),
            match_value: Some(match_value.unwrap_or_else(|| "True".to_string())),
            is_conditional: true,
            handle: None,
            client: None,
        }
    }

    pub fn handle(&self) -> Option<&Handle> {
        self.handle.as_ref()
    }

    pub fn client(&self) -> Option<&Arc<AspireClient>> {
        self.client.as_ref()
    }

    pub fn to_json(&self) -> Value {
        if let Some(ref handle) = self.handle {
            return handle.to_json();
        }
        if self.is_conditional {
            return json!({
                "$refExpr": {
                    "condition": serialize_value(self.condition.clone().unwrap()),
                    "whenTrue": self.when_true.as_ref().unwrap().to_json(),
                    "whenFalse": self.when_false.as_ref().unwrap().to_json(),
                    "matchValue": self.match_value.as_ref().unwrap()
                }
            });
        }
        json!({
            "$refExpr": {
                "format": self.format.as_ref().unwrap(),
                "args": self.args.as_ref().unwrap()
            }
        })
    }
}

impl HasHandle for ReferenceExpression {
    fn handle(&self) -> &Handle {
        self.handle.as_ref().expect("ReferenceExpression is not in handle mode")
    }
}

impl Serialize for ReferenceExpression {
    fn serialize<S: serde::Serializer>(&self, serializer: S) -> Result<S::Ok, S::Error> {
        self.to_json().serialize(serializer)
    }
}

impl<'de> Deserialize<'de> for ReferenceExpression {
    fn deserialize<D: serde::Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        let value = Value::deserialize(deserializer)?;
        if let Some(ref_expr) = value.get("$refExpr") {
            if let Some(condition) = ref_expr.get("condition") {
                let when_true: ReferenceExpression = serde_json::from_value(
                    ref_expr.get("whenTrue").cloned().unwrap_or(Value::Null)
                ).map_err(serde::de::Error::custom)?;
                let when_false: ReferenceExpression = serde_json::from_value(
                    ref_expr.get("whenFalse").cloned().unwrap_or(Value::Null)
                ).map_err(serde::de::Error::custom)?;
                let match_value = ref_expr.get("matchValue")
                    .and_then(|v| v.as_str())
                    .map(|s| s.to_string());
                return Ok(ReferenceExpression::create_conditional(
                    condition.clone(),
                    match_value,
                    when_true,
                    when_false,
                ));
            }
            let format = ref_expr.get("format")
                .and_then(|v| v.as_str())
                .unwrap_or("")
                .to_string();
            let args = ref_expr.get("args")
                .and_then(|v| v.as_array())
                .cloned()
                .unwrap_or_default();
            return Ok(ReferenceExpression::new(format, args));
        }
        if value.get("$handle").is_some() {
            let handle: Handle = serde_json::from_value(value)
                .map_err(serde::de::Error::custom)?;
            return Ok(Self {
                format: None, args: None,
                condition: None, when_true: None, when_false: None,
                match_value: None, is_conditional: false,
                handle: Some(handle), client: None,
            });
        }
        Err(serde::de::Error::custom("expected $refExpr or $handle"))
    }
}

/// Convenience function to create a reference expression.
pub fn ref_expr(format: impl Into<String>, args: Vec<Value>) -> ReferenceExpression {
    ReferenceExpression::new(format, args)
}

/// A handle-backed list with lazy handle resolution.
pub struct AspireList<T> {
    context_handle: Handle,
    client: Arc<AspireClient>,
    getter_capability_id: Option<String>,
    resolved_handle: std::cell::OnceCell<Handle>,
    _marker: std::marker::PhantomData<T>,
}

impl<T> AspireList<T> {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        let resolved = std::cell::OnceCell::new();
        let _ = resolved.set(handle.clone());
        Self {
            context_handle: handle,
            client,
            getter_capability_id: None,
            resolved_handle: resolved,
            _marker: std::marker::PhantomData,
        }
    }

    pub fn with_getter(context_handle: Handle, client: Arc<AspireClient>, getter_capability_id: impl Into<String>) -> Self {
        Self {
            context_handle,
            client,
            getter_capability_id: Some(getter_capability_id.into()),
            resolved_handle: std::cell::OnceCell::new(),
            _marker: std::marker::PhantomData,
        }
    }

    fn ensure_handle(&self) -> &Handle {
        self.resolved_handle.get_or_init(|| {
            if let Some(ref cap_id) = self.getter_capability_id {
                let mut args = HashMap::new();
                args.insert("context".to_string(), self.context_handle.to_json());
                if let Ok(result) = self.client.invoke_capability(cap_id, args) {
                    if let Ok(handle) = serde_json::from_value::<Handle>(result) {
                        return handle;
                    }
                }
            }
            self.context_handle.clone()
        })
    }

    pub fn handle(&self) -> &Handle {
        self.ensure_handle()
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }
}

/// A handle-backed dictionary with lazy handle resolution.
pub struct AspireDict<K, V> {
    context_handle: Handle,
    client: Arc<AspireClient>,
    getter_capability_id: Option<String>,
    resolved_handle: std::cell::OnceCell<Handle>,
    _key_marker: std::marker::PhantomData<K>,
    _value_marker: std::marker::PhantomData<V>,
}

impl<K, V> AspireDict<K, V> {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        let resolved = std::cell::OnceCell::new();
        let _ = resolved.set(handle.clone());
        Self {
            context_handle: handle,
            client,
            getter_capability_id: None,
            resolved_handle: resolved,
            _key_marker: std::marker::PhantomData,
            _value_marker: std::marker::PhantomData,
        }
    }

    pub fn with_getter(context_handle: Handle, client: Arc<AspireClient>, getter_capability_id: impl Into<String>) -> Self {
        Self {
            context_handle,
            client,
            getter_capability_id: Some(getter_capability_id.into()),
            resolved_handle: std::cell::OnceCell::new(),
            _key_marker: std::marker::PhantomData,
            _value_marker: std::marker::PhantomData,
        }
    }

    fn ensure_handle(&self) -> &Handle {
        self.resolved_handle.get_or_init(|| {
            if let Some(ref cap_id) = self.getter_capability_id {
                let mut args = HashMap::new();
                args.insert("context".to_string(), self.context_handle.to_json());
                if let Ok(result) = self.client.invoke_capability(cap_id, args) {
                    if let Ok(handle) = serde_json::from_value::<Handle>(result) {
                        return handle;
                    }
                }
            }
            self.context_handle.clone()
        })
    }

    pub fn handle(&self) -> &Handle {
        self.ensure_handle()
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
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
