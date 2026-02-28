//! aspire.rs - Capability-based Aspire SDK
//! GENERATED CODE - DO NOT EDIT

use std::collections::HashMap;
use std::sync::Arc;

use serde::{Deserialize, Serialize};
use serde_json::{json, Value};

use crate::transport::{
    AspireClient, CancellationToken, Handle,
    register_callback, register_cancellation, serialize_value,
};
use crate::base::{
    HandleWrapperBase, ResourceBuilderBase, ReferenceExpression,
    AspireList, AspireDict, serialize_handle, HasHandle,
};

// ============================================================================
// Enums
// ============================================================================

/// ContainerLifetime
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum ContainerLifetime {
    #[default]
    #[serde(rename = "Session")]
    Session,
    #[serde(rename = "Persistent")]
    Persistent,
}

impl std::fmt::Display for ContainerLifetime {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Session => write!(f, "Session"),
            Self::Persistent => write!(f, "Persistent"),
        }
    }
}

/// ImagePullPolicy
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum ImagePullPolicy {
    #[default]
    #[serde(rename = "Default")]
    Default,
    #[serde(rename = "Always")]
    Always,
    #[serde(rename = "Missing")]
    Missing,
    #[serde(rename = "Never")]
    Never,
}

impl std::fmt::Display for ImagePullPolicy {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Default => write!(f, "Default"),
            Self::Always => write!(f, "Always"),
            Self::Missing => write!(f, "Missing"),
            Self::Never => write!(f, "Never"),
        }
    }
}

/// DistributedApplicationOperation
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum DistributedApplicationOperation {
    #[default]
    #[serde(rename = "Run")]
    Run,
    #[serde(rename = "Publish")]
    Publish,
}

impl std::fmt::Display for DistributedApplicationOperation {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Run => write!(f, "Run"),
            Self::Publish => write!(f, "Publish"),
        }
    }
}

/// ProtocolType
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum ProtocolType {
    #[default]
    #[serde(rename = "IP")]
    IP,
    #[serde(rename = "IPv6HopByHopOptions")]
    IPv6HopByHopOptions,
    #[serde(rename = "Unspecified")]
    Unspecified,
    #[serde(rename = "Icmp")]
    Icmp,
    #[serde(rename = "Igmp")]
    Igmp,
    #[serde(rename = "Ggp")]
    Ggp,
    #[serde(rename = "IPv4")]
    IPv4,
    #[serde(rename = "Tcp")]
    Tcp,
    #[serde(rename = "Pup")]
    Pup,
    #[serde(rename = "Udp")]
    Udp,
    #[serde(rename = "Idp")]
    Idp,
    #[serde(rename = "IPv6")]
    IPv6,
    #[serde(rename = "IPv6RoutingHeader")]
    IPv6RoutingHeader,
    #[serde(rename = "IPv6FragmentHeader")]
    IPv6FragmentHeader,
    #[serde(rename = "IPSecEncapsulatingSecurityPayload")]
    IPSecEncapsulatingSecurityPayload,
    #[serde(rename = "IPSecAuthenticationHeader")]
    IPSecAuthenticationHeader,
    #[serde(rename = "IcmpV6")]
    IcmpV6,
    #[serde(rename = "IPv6NoNextHeader")]
    IPv6NoNextHeader,
    #[serde(rename = "IPv6DestinationOptions")]
    IPv6DestinationOptions,
    #[serde(rename = "ND")]
    ND,
    #[serde(rename = "Raw")]
    Raw,
    #[serde(rename = "Ipx")]
    Ipx,
    #[serde(rename = "Spx")]
    Spx,
    #[serde(rename = "SpxII")]
    SpxII,
    #[serde(rename = "Unknown")]
    Unknown,
}

impl std::fmt::Display for ProtocolType {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::IP => write!(f, "IP"),
            Self::IPv6HopByHopOptions => write!(f, "IPv6HopByHopOptions"),
            Self::Unspecified => write!(f, "Unspecified"),
            Self::Icmp => write!(f, "Icmp"),
            Self::Igmp => write!(f, "Igmp"),
            Self::Ggp => write!(f, "Ggp"),
            Self::IPv4 => write!(f, "IPv4"),
            Self::Tcp => write!(f, "Tcp"),
            Self::Pup => write!(f, "Pup"),
            Self::Udp => write!(f, "Udp"),
            Self::Idp => write!(f, "Idp"),
            Self::IPv6 => write!(f, "IPv6"),
            Self::IPv6RoutingHeader => write!(f, "IPv6RoutingHeader"),
            Self::IPv6FragmentHeader => write!(f, "IPv6FragmentHeader"),
            Self::IPSecEncapsulatingSecurityPayload => write!(f, "IPSecEncapsulatingSecurityPayload"),
            Self::IPSecAuthenticationHeader => write!(f, "IPSecAuthenticationHeader"),
            Self::IcmpV6 => write!(f, "IcmpV6"),
            Self::IPv6NoNextHeader => write!(f, "IPv6NoNextHeader"),
            Self::IPv6DestinationOptions => write!(f, "IPv6DestinationOptions"),
            Self::ND => write!(f, "ND"),
            Self::Raw => write!(f, "Raw"),
            Self::Ipx => write!(f, "Ipx"),
            Self::Spx => write!(f, "Spx"),
            Self::SpxII => write!(f, "SpxII"),
            Self::Unknown => write!(f, "Unknown"),
        }
    }
}

/// EndpointProperty
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum EndpointProperty {
    #[default]
    #[serde(rename = "Url")]
    Url,
    #[serde(rename = "Host")]
    Host,
    #[serde(rename = "IPV4Host")]
    IPV4Host,
    #[serde(rename = "Port")]
    Port,
    #[serde(rename = "Scheme")]
    Scheme,
    #[serde(rename = "TargetPort")]
    TargetPort,
    #[serde(rename = "HostAndPort")]
    HostAndPort,
}

impl std::fmt::Display for EndpointProperty {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Url => write!(f, "Url"),
            Self::Host => write!(f, "Host"),
            Self::IPV4Host => write!(f, "IPV4Host"),
            Self::Port => write!(f, "Port"),
            Self::Scheme => write!(f, "Scheme"),
            Self::TargetPort => write!(f, "TargetPort"),
            Self::HostAndPort => write!(f, "HostAndPort"),
        }
    }
}

/// IconVariant
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum IconVariant {
    #[default]
    #[serde(rename = "Regular")]
    Regular,
    #[serde(rename = "Filled")]
    Filled,
}

impl std::fmt::Display for IconVariant {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Regular => write!(f, "Regular"),
            Self::Filled => write!(f, "Filled"),
        }
    }
}

/// UrlDisplayLocation
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum UrlDisplayLocation {
    #[default]
    #[serde(rename = "SummaryAndDetails")]
    SummaryAndDetails,
    #[serde(rename = "DetailsOnly")]
    DetailsOnly,
}

impl std::fmt::Display for UrlDisplayLocation {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::SummaryAndDetails => write!(f, "SummaryAndDetails"),
            Self::DetailsOnly => write!(f, "DetailsOnly"),
        }
    }
}

/// TestPersistenceMode
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum TestPersistenceMode {
    #[default]
    #[serde(rename = "None")]
    None,
    #[serde(rename = "Volume")]
    Volume,
    #[serde(rename = "Bind")]
    Bind,
}

impl std::fmt::Display for TestPersistenceMode {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::None => write!(f, "None"),
            Self::Volume => write!(f, "Volume"),
            Self::Bind => write!(f, "Bind"),
        }
    }
}

/// TestResourceStatus
#[derive(Debug, Clone, Copy, Default, PartialEq, Eq, Serialize, Deserialize)]
pub enum TestResourceStatus {
    #[default]
    #[serde(rename = "Pending")]
    Pending,
    #[serde(rename = "Running")]
    Running,
    #[serde(rename = "Stopped")]
    Stopped,
    #[serde(rename = "Failed")]
    Failed,
}

impl std::fmt::Display for TestResourceStatus {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            Self::Pending => write!(f, "Pending"),
            Self::Running => write!(f, "Running"),
            Self::Stopped => write!(f, "Stopped"),
            Self::Failed => write!(f, "Failed"),
        }
    }
}

// ============================================================================
// DTOs
// ============================================================================

/// CreateBuilderOptions
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct CreateBuilderOptions {
    #[serde(rename = "Args")]
    pub args: Vec<String>,
    #[serde(rename = "ProjectDirectory")]
    pub project_directory: String,
    #[serde(rename = "AppHostFilePath")]
    pub app_host_file_path: String,
    #[serde(rename = "ContainerRegistryOverride")]
    pub container_registry_override: String,
    #[serde(rename = "DisableDashboard")]
    pub disable_dashboard: bool,
    #[serde(rename = "DashboardApplicationName")]
    pub dashboard_application_name: String,
    #[serde(rename = "AllowUnsecuredTransport")]
    pub allow_unsecured_transport: bool,
    #[serde(rename = "EnableResourceLogging")]
    pub enable_resource_logging: bool,
}

impl CreateBuilderOptions {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("Args".to_string(), serde_json::to_value(&self.args).unwrap_or(Value::Null));
        map.insert("ProjectDirectory".to_string(), serde_json::to_value(&self.project_directory).unwrap_or(Value::Null));
        map.insert("AppHostFilePath".to_string(), serde_json::to_value(&self.app_host_file_path).unwrap_or(Value::Null));
        map.insert("ContainerRegistryOverride".to_string(), serde_json::to_value(&self.container_registry_override).unwrap_or(Value::Null));
        map.insert("DisableDashboard".to_string(), serde_json::to_value(&self.disable_dashboard).unwrap_or(Value::Null));
        map.insert("DashboardApplicationName".to_string(), serde_json::to_value(&self.dashboard_application_name).unwrap_or(Value::Null));
        map.insert("AllowUnsecuredTransport".to_string(), serde_json::to_value(&self.allow_unsecured_transport).unwrap_or(Value::Null));
        map.insert("EnableResourceLogging".to_string(), serde_json::to_value(&self.enable_resource_logging).unwrap_or(Value::Null));
        map
    }
}

/// ResourceEventDto
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct ResourceEventDto {
    #[serde(rename = "ResourceName")]
    pub resource_name: String,
    #[serde(rename = "ResourceId")]
    pub resource_id: String,
    #[serde(rename = "State")]
    pub state: String,
    #[serde(rename = "StateStyle")]
    pub state_style: String,
    #[serde(rename = "HealthStatus")]
    pub health_status: String,
    #[serde(rename = "ExitCode")]
    pub exit_code: f64,
}

impl ResourceEventDto {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("ResourceName".to_string(), serde_json::to_value(&self.resource_name).unwrap_or(Value::Null));
        map.insert("ResourceId".to_string(), serde_json::to_value(&self.resource_id).unwrap_or(Value::Null));
        map.insert("State".to_string(), serde_json::to_value(&self.state).unwrap_or(Value::Null));
        map.insert("StateStyle".to_string(), serde_json::to_value(&self.state_style).unwrap_or(Value::Null));
        map.insert("HealthStatus".to_string(), serde_json::to_value(&self.health_status).unwrap_or(Value::Null));
        map.insert("ExitCode".to_string(), serde_json::to_value(&self.exit_code).unwrap_or(Value::Null));
        map
    }
}

/// CommandOptions
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct CommandOptions {
    #[serde(rename = "Description")]
    pub description: String,
    #[serde(rename = "Parameter")]
    pub parameter: Value,
    #[serde(rename = "ConfirmationMessage")]
    pub confirmation_message: String,
    #[serde(rename = "IconName")]
    pub icon_name: String,
    #[serde(rename = "IconVariant")]
    pub icon_variant: IconVariant,
    #[serde(rename = "IsHighlighted")]
    pub is_highlighted: bool,
    #[serde(rename = "UpdateState")]
    pub update_state: Value,
}

impl CommandOptions {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("Description".to_string(), serde_json::to_value(&self.description).unwrap_or(Value::Null));
        map.insert("Parameter".to_string(), serde_json::to_value(&self.parameter).unwrap_or(Value::Null));
        map.insert("ConfirmationMessage".to_string(), serde_json::to_value(&self.confirmation_message).unwrap_or(Value::Null));
        map.insert("IconName".to_string(), serde_json::to_value(&self.icon_name).unwrap_or(Value::Null));
        map.insert("IconVariant".to_string(), serde_json::to_value(&self.icon_variant).unwrap_or(Value::Null));
        map.insert("IsHighlighted".to_string(), serde_json::to_value(&self.is_highlighted).unwrap_or(Value::Null));
        map.insert("UpdateState".to_string(), serde_json::to_value(&self.update_state).unwrap_or(Value::Null));
        map
    }
}

/// ExecuteCommandResult
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct ExecuteCommandResult {
    #[serde(rename = "Success")]
    pub success: bool,
    #[serde(rename = "Canceled")]
    pub canceled: bool,
    #[serde(rename = "ErrorMessage")]
    pub error_message: String,
}

impl ExecuteCommandResult {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("Success".to_string(), serde_json::to_value(&self.success).unwrap_or(Value::Null));
        map.insert("Canceled".to_string(), serde_json::to_value(&self.canceled).unwrap_or(Value::Null));
        map.insert("ErrorMessage".to_string(), serde_json::to_value(&self.error_message).unwrap_or(Value::Null));
        map
    }
}

/// ResourceUrlAnnotation
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct ResourceUrlAnnotation {
    #[serde(rename = "Url")]
    pub url: String,
    #[serde(rename = "DisplayText")]
    pub display_text: String,
    #[serde(rename = "Endpoint")]
    pub endpoint: Handle,
    #[serde(rename = "DisplayLocation")]
    pub display_location: UrlDisplayLocation,
}

impl ResourceUrlAnnotation {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("Url".to_string(), serde_json::to_value(&self.url).unwrap_or(Value::Null));
        map.insert("DisplayText".to_string(), serde_json::to_value(&self.display_text).unwrap_or(Value::Null));
        map.insert("Endpoint".to_string(), serde_json::to_value(&self.endpoint).unwrap_or(Value::Null));
        map.insert("DisplayLocation".to_string(), serde_json::to_value(&self.display_location).unwrap_or(Value::Null));
        map
    }
}

/// TestConfigDto
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct TestConfigDto {
    #[serde(rename = "Name")]
    pub name: String,
    #[serde(rename = "Port")]
    pub port: f64,
    #[serde(rename = "Enabled")]
    pub enabled: bool,
    #[serde(rename = "OptionalField")]
    pub optional_field: String,
}

impl TestConfigDto {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("Name".to_string(), serde_json::to_value(&self.name).unwrap_or(Value::Null));
        map.insert("Port".to_string(), serde_json::to_value(&self.port).unwrap_or(Value::Null));
        map.insert("Enabled".to_string(), serde_json::to_value(&self.enabled).unwrap_or(Value::Null));
        map.insert("OptionalField".to_string(), serde_json::to_value(&self.optional_field).unwrap_or(Value::Null));
        map
    }
}

/// TestNestedDto
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct TestNestedDto {
    #[serde(rename = "Id")]
    pub id: String,
    #[serde(rename = "Config")]
    pub config: TestConfigDto,
    #[serde(rename = "Tags")]
    pub tags: Vec<String>,
    #[serde(rename = "Counts")]
    pub counts: HashMap<String, f64>,
}

impl TestNestedDto {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("Id".to_string(), serde_json::to_value(&self.id).unwrap_or(Value::Null));
        map.insert("Config".to_string(), serde_json::to_value(&self.config).unwrap_or(Value::Null));
        map.insert("Tags".to_string(), serde_json::to_value(&self.tags).unwrap_or(Value::Null));
        map.insert("Counts".to_string(), serde_json::to_value(&self.counts).unwrap_or(Value::Null));
        map
    }
}

/// TestDeeplyNestedDto
#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct TestDeeplyNestedDto {
    #[serde(rename = "NestedData")]
    pub nested_data: HashMap<String, Vec<TestConfigDto>>,
    #[serde(rename = "MetadataArray")]
    pub metadata_array: Vec<HashMap<String, String>>,
}

impl TestDeeplyNestedDto {
    pub fn to_map(&self) -> HashMap<String, Value> {
        let mut map = HashMap::new();
        map.insert("NestedData".to_string(), serde_json::to_value(&self.nested_data).unwrap_or(Value::Null));
        map.insert("MetadataArray".to_string(), serde_json::to_value(&self.metadata_array).unwrap_or(Value::Null));
        map
    }
}

// ============================================================================
// Handle Wrappers
// ============================================================================

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext
pub struct CommandLineArgsCallbackContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for CommandLineArgsCallbackContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl CommandLineArgsCallbackContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Args property
    pub fn args(&self) -> AspireList<Value> {
        AspireList::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args")
    }

    /// Gets the CancellationToken property
    pub fn cancellation_token(&self) -> Result<CancellationToken, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.cancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(CancellationToken::new(handle, self.client.clone()))
    }

    /// Gets the ExecutionContext property
    pub fn execution_context(&self) -> Result<DistributedApplicationExecutionContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.executionContext", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(DistributedApplicationExecutionContext::new(handle, self.client.clone()))
    }

    /// Sets the ExecutionContext property
    pub fn set_execution_context(&self, value: &DistributedApplicationExecutionContext) -> Result<CommandLineArgsCallbackContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), value.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.setExecutionContext", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(CommandLineArgsCallbackContext::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource
pub struct ContainerResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ContainerResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ContainerResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Sets an environment variable
    pub fn with_environment(&self, name: &str, value: &str) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironment", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds an environment variable with a reference expression
    pub fn with_environment_expression(&self, name: &str, value: ReferenceExpression) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via callback
    pub fn with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via async callback
    pub fn with_environment_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds arguments
    pub fn with_args(&self, args: Vec<String>) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via callback
    pub fn with_args_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via async callback
    pub fn with_args_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Adds a reference to another resource
    pub fn with_reference(&self, source: &IResourceWithConnectionString, connection_name: Option<&str>, optional: Option<bool>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        if let Some(ref v) = connection_name {
            args.insert("connectionName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = optional {
            args.insert("optional".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a service discovery reference to another resource
    pub fn with_service_reference(&self, source: &IResourceWithServiceDiscovery) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withServiceReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a network endpoint
    pub fn with_endpoint(&self, port: Option<f64>, target_port: Option<f64>, scheme: Option<&str>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>, is_external: Option<bool>, protocol: Option<ProtocolType>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = scheme {
            args.insert("scheme".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_external {
            args.insert("isExternal".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = protocol {
            args.insert("protocol".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTP endpoint
    pub fn with_http_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTPS endpoint
    pub fn with_https_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Makes HTTP endpoints externally accessible
    pub fn with_external_http_endpoints(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Gets an endpoint reference
    pub fn get_endpoint(&self, name: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/getEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Configures resource for HTTP/2
    pub fn as_http2_service(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/asHttp2Service", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL for a specific endpoint via factory callback
    pub fn with_url_for_endpoint_factory(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Waits for another resource to be ready
    pub fn wait_for(&self, dependency: &IResource) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/waitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for resource completion
    pub fn wait_for_completion(&self, dependency: &IResource, exit_code: Option<f64>) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        if let Some(ref v) = exit_code {
            args.insert("exitCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/waitForCompletion", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds an HTTP health check
    pub fn with_http_health_check(&self, path: Option<&str>, status_code: Option<f64>, endpoint_name: Option<&str>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = path {
            args.insert("path".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = status_code {
            args.insert("statusCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = endpoint_name {
            args.insert("endpointName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures environment with callback (test version)
    pub fn test_with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWithEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets environment variables
    pub fn with_environment_variables(&self, variables: HashMap<String, String>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("variables".to_string(), serde_json::to_value(&variables).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEnvironmentVariables", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplication
pub struct DistributedApplication {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for DistributedApplication {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl DistributedApplication {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Runs the distributed application
    pub fn run(&self, cancellation_token: Option<&CancellationToken>) -> Result<(), Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        if let Some(token) = cancellation_token {
            let token_id = register_cancellation(token, self.client.clone());
            args.insert("cancellationToken".to_string(), Value::String(token_id));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/run", args)?;
        Ok(())
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription
pub struct DistributedApplicationEventSubscription {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for DistributedApplicationEventSubscription {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl DistributedApplicationEventSubscription {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext
pub struct DistributedApplicationExecutionContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for DistributedApplicationExecutionContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl DistributedApplicationExecutionContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the PublisherName property
    pub fn publisher_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.publisherName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the PublisherName property
    pub fn set_publisher_name(&self, value: &str) -> Result<DistributedApplicationExecutionContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(DistributedApplicationExecutionContext::new(handle, self.client.clone()))
    }

    /// Gets the Operation property
    pub fn operation(&self) -> Result<DistributedApplicationOperation, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.operation", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the IsPublishMode property
    pub fn is_publish_mode(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the IsRunMode property
    pub fn is_run_mode(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode", args)?;
        Ok(serde_json::from_value(result)?)
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions
pub struct DistributedApplicationExecutionContextOptions {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for DistributedApplicationExecutionContextOptions {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl DistributedApplicationExecutionContextOptions {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription
pub struct DistributedApplicationResourceEventSubscription {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for DistributedApplicationResourceEventSubscription {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl DistributedApplicationResourceEventSubscription {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference
pub struct EndpointReference {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for EndpointReference {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl EndpointReference {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the EndpointName property
    pub fn endpoint_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.endpointName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the ErrorMessage property
    pub fn error_message(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the ErrorMessage property
    pub fn set_error_message(&self, value: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Gets the IsAllocated property
    pub fn is_allocated(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the Exists property
    pub fn exists(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.exists", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the IsHttp property
    pub fn is_http(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttp", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the IsHttps property
    pub fn is_https(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttps", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the Port property
    pub fn port(&self) -> Result<f64, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.port", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the TargetPort property
    pub fn target_port(&self) -> Result<f64, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.targetPort", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the Host property
    pub fn host(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.host", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the Scheme property
    pub fn scheme(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.scheme", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the Url property
    pub fn url(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReference.url", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the URL of the endpoint asynchronously
    pub fn get_value_async(&self, cancellation_token: Option<&CancellationToken>) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        if let Some(token) = cancellation_token {
            let token_id = register_cancellation(token, self.client.clone());
            args.insert("cancellationToken".to_string(), Value::String(token_id));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/getValueAsync", args)?;
        Ok(serde_json::from_value(result)?)
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression
pub struct EndpointReferenceExpression {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for EndpointReferenceExpression {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl EndpointReferenceExpression {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Endpoint property
    pub fn endpoint(&self) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.endpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Gets the Property property
    pub fn property(&self) -> Result<EndpointProperty, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.property", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the ValueExpression property
    pub fn value_expression(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.valueExpression", args)?;
        Ok(serde_json::from_value(result)?)
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext
pub struct EnvironmentCallbackContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for EnvironmentCallbackContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl EnvironmentCallbackContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the EnvironmentVariables property
    pub fn environment_variables(&self) -> AspireDict<String, Value> {
        AspireDict::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables")
    }

    /// Gets the CancellationToken property
    pub fn cancellation_token(&self) -> Result<CancellationToken, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(CancellationToken::new(handle, self.client.clone()))
    }

    /// Gets the ExecutionContext property
    pub fn execution_context(&self) -> Result<DistributedApplicationExecutionContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(DistributedApplicationExecutionContext::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource
pub struct ExecutableResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ExecutableResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ExecutableResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Sets the executable command
    pub fn with_executable_command(&self, command: &str) -> Result<ExecutableResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("command".to_string(), serde_json::to_value(&command).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withExecutableCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ExecutableResource::new(handle, self.client.clone()))
    }

    /// Sets the executable working directory
    pub fn with_working_directory(&self, working_directory: &str) -> Result<ExecutableResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("workingDirectory".to_string(), serde_json::to_value(&working_directory).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withWorkingDirectory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ExecutableResource::new(handle, self.client.clone()))
    }

    /// Sets an environment variable
    pub fn with_environment(&self, name: &str, value: &str) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironment", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds an environment variable with a reference expression
    pub fn with_environment_expression(&self, name: &str, value: ReferenceExpression) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via callback
    pub fn with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via async callback
    pub fn with_environment_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds arguments
    pub fn with_args(&self, args: Vec<String>) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via callback
    pub fn with_args_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via async callback
    pub fn with_args_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Adds a reference to another resource
    pub fn with_reference(&self, source: &IResourceWithConnectionString, connection_name: Option<&str>, optional: Option<bool>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        if let Some(ref v) = connection_name {
            args.insert("connectionName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = optional {
            args.insert("optional".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a service discovery reference to another resource
    pub fn with_service_reference(&self, source: &IResourceWithServiceDiscovery) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withServiceReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a network endpoint
    pub fn with_endpoint(&self, port: Option<f64>, target_port: Option<f64>, scheme: Option<&str>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>, is_external: Option<bool>, protocol: Option<ProtocolType>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = scheme {
            args.insert("scheme".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_external {
            args.insert("isExternal".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = protocol {
            args.insert("protocol".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTP endpoint
    pub fn with_http_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTPS endpoint
    pub fn with_https_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Makes HTTP endpoints externally accessible
    pub fn with_external_http_endpoints(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Gets an endpoint reference
    pub fn get_endpoint(&self, name: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/getEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Configures resource for HTTP/2
    pub fn as_http2_service(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/asHttp2Service", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL for a specific endpoint via factory callback
    pub fn with_url_for_endpoint_factory(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Waits for another resource to be ready
    pub fn wait_for(&self, dependency: &IResource) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/waitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for resource completion
    pub fn wait_for_completion(&self, dependency: &IResource, exit_code: Option<f64>) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        if let Some(ref v) = exit_code {
            args.insert("exitCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/waitForCompletion", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds an HTTP health check
    pub fn with_http_health_check(&self, path: Option<&str>, status_code: Option<f64>, endpoint_name: Option<&str>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = path {
            args.insert("path".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = status_code {
            args.insert("statusCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = endpoint_name {
            args.insert("endpointName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures environment with callback (test version)
    pub fn test_with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWithEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets environment variables
    pub fn with_environment_variables(&self, variables: HashMap<String, String>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("variables".to_string(), serde_json::to_value(&variables).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEnvironmentVariables", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext
pub struct ExecuteCommandContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ExecuteCommandContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ExecuteCommandContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the ResourceName property
    pub fn resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.resourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the ResourceName property
    pub fn set_resource_name(&self, value: &str) -> Result<ExecuteCommandContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setResourceName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ExecuteCommandContext::new(handle, self.client.clone()))
    }

    /// Gets the CancellationToken property
    pub fn cancellation_token(&self) -> Result<CancellationToken, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.cancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(CancellationToken::new(handle, self.client.clone()))
    }

    /// Sets the CancellationToken property
    pub fn set_cancellation_token(&self, value: Option<&CancellationToken>) -> Result<ExecuteCommandContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        if let Some(token) = value {
            let token_id = register_cancellation(token, self.client.clone());
            args.insert("value".to_string(), Value::String(token_id));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setCancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ExecuteCommandContext::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder
pub struct IDistributedApplicationBuilder {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IDistributedApplicationBuilder {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IDistributedApplicationBuilder {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Adds a container resource
    pub fn add_container(&self, name: &str, image: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("image".to_string(), serde_json::to_value(&image).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/addContainer", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Adds an executable resource
    pub fn add_executable(&self, name: &str, command: &str, working_directory: &str, args: Vec<String>) -> Result<ExecutableResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("command".to_string(), serde_json::to_value(&command).unwrap_or(Value::Null));
        args.insert("workingDirectory".to_string(), serde_json::to_value(&working_directory).unwrap_or(Value::Null));
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/addExecutable", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ExecutableResource::new(handle, self.client.clone()))
    }

    /// Gets the AppHostDirectory property
    pub fn app_host_directory(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Gets the Eventing property
    pub fn eventing(&self) -> Result<IDistributedApplicationEventing, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/IDistributedApplicationBuilder.eventing", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IDistributedApplicationEventing::new(handle, self.client.clone()))
    }

    /// Gets the ExecutionContext property
    pub fn execution_context(&self) -> Result<DistributedApplicationExecutionContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/IDistributedApplicationBuilder.executionContext", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(DistributedApplicationExecutionContext::new(handle, self.client.clone()))
    }

    /// Builds the distributed application
    pub fn build(&self) -> Result<DistributedApplication, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/build", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(DistributedApplication::new(handle, self.client.clone()))
    }

    /// Adds a parameter resource
    pub fn add_parameter(&self, name: &str, secret: Option<bool>) -> Result<ParameterResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        if let Some(ref v) = secret {
            args.insert("secret".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/addParameter", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ParameterResource::new(handle, self.client.clone()))
    }

    /// Adds a connection string resource
    pub fn add_connection_string(&self, name: &str, environment_variable_name: Option<&str>) -> Result<IResourceWithConnectionString, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        if let Some(ref v) = environment_variable_name {
            args.insert("environmentVariableName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/addConnectionString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithConnectionString::new(handle, self.client.clone()))
    }

    /// Adds a .NET project resource
    pub fn add_project(&self, name: &str, project_path: &str, launch_profile_name: &str) -> Result<ProjectResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("projectPath".to_string(), serde_json::to_value(&project_path).unwrap_or(Value::Null));
        args.insert("launchProfileName".to_string(), serde_json::to_value(&launch_profile_name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/addProject", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ProjectResource::new(handle, self.client.clone()))
    }

    /// Adds a test Redis resource
    pub fn add_test_redis(&self, name: &str, port: Option<f64>) -> Result<TestRedisResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/addTestRedis", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestRedisResource::new(handle, self.client.clone()))
    }

    /// Adds a test vault resource
    pub fn add_test_vault(&self, name: &str) -> Result<TestVaultResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/addTestVault", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestVaultResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent
pub struct IDistributedApplicationEvent {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IDistributedApplicationEvent {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IDistributedApplicationEvent {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing
pub struct IDistributedApplicationEventing {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IDistributedApplicationEventing {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IDistributedApplicationEventing {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Invokes the Unsubscribe method
    pub fn unsubscribe(&self, subscription: &DistributedApplicationEventSubscription) -> Result<(), Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("subscription".to_string(), subscription.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe", args)?;
        Ok(())
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent
pub struct IDistributedApplicationResourceEvent {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IDistributedApplicationResourceEvent {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IDistributedApplicationResourceEvent {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource
pub struct IResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResource {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs
pub struct IResourceWithArgs {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResourceWithArgs {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResourceWithArgs {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString
pub struct IResourceWithConnectionString {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResourceWithConnectionString {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResourceWithConnectionString {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints
pub struct IResourceWithEndpoints {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResourceWithEndpoints {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResourceWithEndpoints {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment
pub struct IResourceWithEnvironment {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResourceWithEnvironment {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResourceWithEnvironment {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery
pub struct IResourceWithServiceDiscovery {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResourceWithServiceDiscovery {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResourceWithServiceDiscovery {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport
pub struct IResourceWithWaitSupport {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for IResourceWithWaitSupport {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl IResourceWithWaitSupport {
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

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource
pub struct ITestVaultResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ITestVaultResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ITestVaultResource {
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

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource
pub struct ParameterResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ParameterResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ParameterResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Sets a parameter description
    pub fn with_description(&self, description: &str, enable_markdown: Option<bool>) -> Result<ParameterResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("description".to_string(), serde_json::to_value(&description).unwrap_or(Value::Null));
        if let Some(ref v) = enable_markdown {
            args.insert("enableMarkdown".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withDescription", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ParameterResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource
pub struct ProjectResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ProjectResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ProjectResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Sets the number of replicas
    pub fn with_replicas(&self, replicas: f64) -> Result<ProjectResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("replicas".to_string(), serde_json::to_value(&replicas).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withReplicas", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ProjectResource::new(handle, self.client.clone()))
    }

    /// Sets an environment variable
    pub fn with_environment(&self, name: &str, value: &str) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironment", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds an environment variable with a reference expression
    pub fn with_environment_expression(&self, name: &str, value: ReferenceExpression) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via callback
    pub fn with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via async callback
    pub fn with_environment_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds arguments
    pub fn with_args(&self, args: Vec<String>) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via callback
    pub fn with_args_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via async callback
    pub fn with_args_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Adds a reference to another resource
    pub fn with_reference(&self, source: &IResourceWithConnectionString, connection_name: Option<&str>, optional: Option<bool>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        if let Some(ref v) = connection_name {
            args.insert("connectionName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = optional {
            args.insert("optional".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a service discovery reference to another resource
    pub fn with_service_reference(&self, source: &IResourceWithServiceDiscovery) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withServiceReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a network endpoint
    pub fn with_endpoint(&self, port: Option<f64>, target_port: Option<f64>, scheme: Option<&str>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>, is_external: Option<bool>, protocol: Option<ProtocolType>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = scheme {
            args.insert("scheme".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_external {
            args.insert("isExternal".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = protocol {
            args.insert("protocol".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTP endpoint
    pub fn with_http_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTPS endpoint
    pub fn with_https_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Makes HTTP endpoints externally accessible
    pub fn with_external_http_endpoints(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Gets an endpoint reference
    pub fn get_endpoint(&self, name: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/getEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Configures resource for HTTP/2
    pub fn as_http2_service(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/asHttp2Service", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL for a specific endpoint via factory callback
    pub fn with_url_for_endpoint_factory(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Waits for another resource to be ready
    pub fn wait_for(&self, dependency: &IResource) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/waitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for resource completion
    pub fn wait_for_completion(&self, dependency: &IResource, exit_code: Option<f64>) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        if let Some(ref v) = exit_code {
            args.insert("exitCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/waitForCompletion", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds an HTTP health check
    pub fn with_http_health_check(&self, path: Option<&str>, status_code: Option<f64>, endpoint_name: Option<&str>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = path {
            args.insert("path".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = status_code {
            args.insert("statusCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = endpoint_name {
            args.insert("endpointName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures environment with callback (test version)
    pub fn test_with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWithEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets environment variables
    pub fn with_environment_variables(&self, variables: HashMap<String, String>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("variables".to_string(), serde_json::to_value(&variables).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEnvironmentVariables", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext
pub struct ResourceUrlsCallbackContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for ResourceUrlsCallbackContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl ResourceUrlsCallbackContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Urls property
    pub fn urls(&self) -> AspireList<ResourceUrlAnnotation> {
        AspireList::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls")
    }

    /// Gets the CancellationToken property
    pub fn cancellation_token(&self) -> Result<CancellationToken, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.cancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(CancellationToken::new(handle, self.client.clone()))
    }

    /// Gets the ExecutionContext property
    pub fn execution_context(&self) -> Result<DistributedApplicationExecutionContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(DistributedApplicationExecutionContext::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext
pub struct TestCallbackContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestCallbackContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestCallbackContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Name property
    pub fn name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Name property
    pub fn set_name(&self, value: &str) -> Result<TestCallbackContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestCallbackContext::new(handle, self.client.clone()))
    }

    /// Gets the Value property
    pub fn value(&self) -> Result<f64, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Value property
    pub fn set_value(&self, value: f64) -> Result<TestCallbackContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestCallbackContext::new(handle, self.client.clone()))
    }

    /// Gets the CancellationToken property
    pub fn cancellation_token(&self) -> Result<CancellationToken, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(CancellationToken::new(handle, self.client.clone()))
    }

    /// Sets the CancellationToken property
    pub fn set_cancellation_token(&self, value: Option<&CancellationToken>) -> Result<TestCallbackContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        if let Some(token) = value {
            let token_id = register_cancellation(token, self.client.clone());
            args.insert("value".to_string(), Value::String(token_id));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestCallbackContext::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext
pub struct TestCollectionContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestCollectionContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestCollectionContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Items property
    pub fn items(&self) -> AspireList<String> {
        AspireList::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items")
    }

    /// Gets the Metadata property
    pub fn metadata(&self) -> AspireDict<String, String> {
        AspireDict::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata")
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource
pub struct TestDatabaseResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestDatabaseResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestDatabaseResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Adds a bind mount
    pub fn with_bind_mount(&self, source: &str, target: &str, is_read_only: Option<bool>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), serde_json::to_value(&source).unwrap_or(Value::Null));
        args.insert("target".to_string(), serde_json::to_value(&target).unwrap_or(Value::Null));
        if let Some(ref v) = is_read_only {
            args.insert("isReadOnly".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withBindMount", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container entrypoint
    pub fn with_entrypoint(&self, entrypoint: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("entrypoint".to_string(), serde_json::to_value(&entrypoint).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEntrypoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image tag
    pub fn with_image_tag(&self, tag: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("tag".to_string(), serde_json::to_value(&tag).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImageTag", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image registry
    pub fn with_image_registry(&self, registry: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("registry".to_string(), serde_json::to_value(&registry).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImageRegistry", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image
    pub fn with_image(&self, image: &str, tag: Option<&str>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("image".to_string(), serde_json::to_value(&image).unwrap_or(Value::Null));
        if let Some(ref v) = tag {
            args.insert("tag".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withImage", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Adds runtime arguments for the container
    pub fn with_container_runtime_args(&self, args: Vec<String>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withContainerRuntimeArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the lifetime behavior of the container resource
    pub fn with_lifetime(&self, lifetime: ContainerLifetime) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("lifetime".to_string(), serde_json::to_value(&lifetime).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withLifetime", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image pull policy
    pub fn with_image_pull_policy(&self, pull_policy: ImagePullPolicy) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("pullPolicy".to_string(), serde_json::to_value(&pull_policy).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImagePullPolicy", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container name
    pub fn with_container_name(&self, name: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withContainerName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets an environment variable
    pub fn with_environment(&self, name: &str, value: &str) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironment", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds an environment variable with a reference expression
    pub fn with_environment_expression(&self, name: &str, value: ReferenceExpression) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via callback
    pub fn with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via async callback
    pub fn with_environment_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds arguments
    pub fn with_args(&self, args: Vec<String>) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via callback
    pub fn with_args_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via async callback
    pub fn with_args_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Adds a reference to another resource
    pub fn with_reference(&self, source: &IResourceWithConnectionString, connection_name: Option<&str>, optional: Option<bool>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        if let Some(ref v) = connection_name {
            args.insert("connectionName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = optional {
            args.insert("optional".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a service discovery reference to another resource
    pub fn with_service_reference(&self, source: &IResourceWithServiceDiscovery) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withServiceReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a network endpoint
    pub fn with_endpoint(&self, port: Option<f64>, target_port: Option<f64>, scheme: Option<&str>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>, is_external: Option<bool>, protocol: Option<ProtocolType>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = scheme {
            args.insert("scheme".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_external {
            args.insert("isExternal".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = protocol {
            args.insert("protocol".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTP endpoint
    pub fn with_http_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTPS endpoint
    pub fn with_https_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Makes HTTP endpoints externally accessible
    pub fn with_external_http_endpoints(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Gets an endpoint reference
    pub fn get_endpoint(&self, name: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/getEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Configures resource for HTTP/2
    pub fn as_http2_service(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/asHttp2Service", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL for a specific endpoint via factory callback
    pub fn with_url_for_endpoint_factory(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Waits for another resource to be ready
    pub fn wait_for(&self, dependency: &IResource) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/waitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for resource completion
    pub fn wait_for_completion(&self, dependency: &IResource, exit_code: Option<f64>) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        if let Some(ref v) = exit_code {
            args.insert("exitCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/waitForCompletion", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds an HTTP health check
    pub fn with_http_health_check(&self, path: Option<&str>, status_code: Option<f64>, endpoint_name: Option<&str>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = path {
            args.insert("path".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = status_code {
            args.insert("statusCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = endpoint_name {
            args.insert("endpointName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a volume
    pub fn with_volume(&self, target: &str, name: Option<&str>, is_read_only: Option<bool>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        args.insert("target".to_string(), serde_json::to_value(&target).unwrap_or(Value::Null));
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_read_only {
            args.insert("isReadOnly".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withVolume", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures environment with callback (test version)
    pub fn test_with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWithEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets environment variables
    pub fn with_environment_variables(&self, variables: HashMap<String, String>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("variables".to_string(), serde_json::to_value(&variables).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEnvironmentVariables", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext
pub struct TestEnvironmentContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestEnvironmentContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestEnvironmentContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Name property
    pub fn name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Name property
    pub fn set_name(&self, value: &str) -> Result<TestEnvironmentContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestEnvironmentContext::new(handle, self.client.clone()))
    }

    /// Gets the Description property
    pub fn description(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Description property
    pub fn set_description(&self, value: &str) -> Result<TestEnvironmentContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestEnvironmentContext::new(handle, self.client.clone()))
    }

    /// Gets the Priority property
    pub fn priority(&self) -> Result<f64, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Priority property
    pub fn set_priority(&self, value: f64) -> Result<TestEnvironmentContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestEnvironmentContext::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource
pub struct TestRedisResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestRedisResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestRedisResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Adds a bind mount
    pub fn with_bind_mount(&self, source: &str, target: &str, is_read_only: Option<bool>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), serde_json::to_value(&source).unwrap_or(Value::Null));
        args.insert("target".to_string(), serde_json::to_value(&target).unwrap_or(Value::Null));
        if let Some(ref v) = is_read_only {
            args.insert("isReadOnly".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withBindMount", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container entrypoint
    pub fn with_entrypoint(&self, entrypoint: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("entrypoint".to_string(), serde_json::to_value(&entrypoint).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEntrypoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image tag
    pub fn with_image_tag(&self, tag: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("tag".to_string(), serde_json::to_value(&tag).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImageTag", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image registry
    pub fn with_image_registry(&self, registry: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("registry".to_string(), serde_json::to_value(&registry).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImageRegistry", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image
    pub fn with_image(&self, image: &str, tag: Option<&str>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("image".to_string(), serde_json::to_value(&image).unwrap_or(Value::Null));
        if let Some(ref v) = tag {
            args.insert("tag".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withImage", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Adds runtime arguments for the container
    pub fn with_container_runtime_args(&self, args: Vec<String>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withContainerRuntimeArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the lifetime behavior of the container resource
    pub fn with_lifetime(&self, lifetime: ContainerLifetime) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("lifetime".to_string(), serde_json::to_value(&lifetime).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withLifetime", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image pull policy
    pub fn with_image_pull_policy(&self, pull_policy: ImagePullPolicy) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("pullPolicy".to_string(), serde_json::to_value(&pull_policy).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImagePullPolicy", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container name
    pub fn with_container_name(&self, name: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withContainerName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets an environment variable
    pub fn with_environment(&self, name: &str, value: &str) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironment", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds an environment variable with a reference expression
    pub fn with_environment_expression(&self, name: &str, value: ReferenceExpression) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via callback
    pub fn with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via async callback
    pub fn with_environment_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds arguments
    pub fn with_args(&self, args: Vec<String>) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via callback
    pub fn with_args_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via async callback
    pub fn with_args_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Adds a reference to another resource
    pub fn with_reference(&self, source: &IResourceWithConnectionString, connection_name: Option<&str>, optional: Option<bool>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        if let Some(ref v) = connection_name {
            args.insert("connectionName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = optional {
            args.insert("optional".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a service discovery reference to another resource
    pub fn with_service_reference(&self, source: &IResourceWithServiceDiscovery) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withServiceReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a network endpoint
    pub fn with_endpoint(&self, port: Option<f64>, target_port: Option<f64>, scheme: Option<&str>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>, is_external: Option<bool>, protocol: Option<ProtocolType>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = scheme {
            args.insert("scheme".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_external {
            args.insert("isExternal".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = protocol {
            args.insert("protocol".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTP endpoint
    pub fn with_http_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTPS endpoint
    pub fn with_https_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Makes HTTP endpoints externally accessible
    pub fn with_external_http_endpoints(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Gets an endpoint reference
    pub fn get_endpoint(&self, name: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/getEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Configures resource for HTTP/2
    pub fn as_http2_service(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/asHttp2Service", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL for a specific endpoint via factory callback
    pub fn with_url_for_endpoint_factory(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Waits for another resource to be ready
    pub fn wait_for(&self, dependency: &IResource) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/waitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for resource completion
    pub fn wait_for_completion(&self, dependency: &IResource, exit_code: Option<f64>) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        if let Some(ref v) = exit_code {
            args.insert("exitCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/waitForCompletion", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds an HTTP health check
    pub fn with_http_health_check(&self, path: Option<&str>, status_code: Option<f64>, endpoint_name: Option<&str>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = path {
            args.insert("path".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = status_code {
            args.insert("statusCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = endpoint_name {
            args.insert("endpointName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a volume
    pub fn with_volume(&self, target: &str, name: Option<&str>, is_read_only: Option<bool>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        args.insert("target".to_string(), serde_json::to_value(&target).unwrap_or(Value::Null));
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_read_only {
            args.insert("isReadOnly".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withVolume", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds a child database to a test Redis resource
    pub fn add_test_child_database(&self, name: &str, database_name: Option<&str>) -> Result<TestDatabaseResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        if let Some(ref v) = database_name {
            args.insert("databaseName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/addTestChildDatabase", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestDatabaseResource::new(handle, self.client.clone()))
    }

    /// Configures the Redis resource with persistence
    pub fn with_persistence(&self, mode: Option<TestPersistenceMode>) -> Result<TestRedisResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = mode {
            args.insert("mode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withPersistence", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestRedisResource::new(handle, self.client.clone()))
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Gets the tags for the resource
    pub fn get_tags(&self) -> AspireList<String> {
        AspireList::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.CodeGeneration.Rust.Tests/getTags")
    }

    /// Gets the metadata for the resource
    pub fn get_metadata(&self) -> AspireDict<String, String> {
        AspireDict::with_getter(self.handle.clone(), self.client.clone(), "Aspire.Hosting.CodeGeneration.Rust.Tests/getMetadata")
    }

    /// Sets the connection string using a reference expression
    pub fn with_connection_string(&self, connection_string: ReferenceExpression) -> Result<IResourceWithConnectionString, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("connectionString".to_string(), serde_json::to_value(&connection_string).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConnectionString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithConnectionString::new(handle, self.client.clone()))
    }

    /// Configures environment with callback (test version)
    pub fn test_with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWithEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Gets the endpoints
    pub fn get_endpoints(&self) -> Result<Vec<String>, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/getEndpoints", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets connection string using direct interface target
    pub fn with_connection_string_direct(&self, connection_string: &str) -> Result<IResourceWithConnectionString, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("connectionString".to_string(), serde_json::to_value(&connection_string).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConnectionStringDirect", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithConnectionString::new(handle, self.client.clone()))
    }

    /// Redis-specific configuration
    pub fn with_redis_specific(&self, option: &str) -> Result<TestRedisResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("option".to_string(), serde_json::to_value(&option).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withRedisSpecific", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestRedisResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets environment variables
    pub fn with_environment_variables(&self, variables: HashMap<String, String>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("variables".to_string(), serde_json::to_value(&variables).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEnvironmentVariables", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Gets the status of the resource asynchronously
    pub fn get_status_async(&self, cancellation_token: Option<&CancellationToken>) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(token) = cancellation_token {
            let token_id = register_cancellation(token, self.client.clone());
            args.insert("cancellationToken".to_string(), Value::String(token_id));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/getStatusAsync", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for the resource to be ready
    pub fn wait_for_ready_async(&self, timeout: f64, cancellation_token: Option<&CancellationToken>) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("timeout".to_string(), serde_json::to_value(&timeout).unwrap_or(Value::Null));
        if let Some(token) = cancellation_token {
            let token_id = register_cancellation(token, self.client.clone());
            args.insert("cancellationToken".to_string(), Value::String(token_id));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/waitForReadyAsync", args)?;
        Ok(serde_json::from_value(result)?)
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext
pub struct TestResourceContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestResourceContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestResourceContext {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Gets the Name property
    pub fn name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Name property
    pub fn set_name(&self, value: &str) -> Result<TestResourceContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestResourceContext::new(handle, self.client.clone()))
    }

    /// Gets the Value property
    pub fn value(&self) -> Result<f64, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Sets the Value property
    pub fn set_value(&self, value: f64) -> Result<TestResourceContext, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(TestResourceContext::new(handle, self.client.clone()))
    }

    /// Invokes the GetValueAsync method
    pub fn get_value_async(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Invokes the SetValueAsync method
    pub fn set_value_async(&self, value: &str) -> Result<(), Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync", args)?;
        Ok(())
    }

    /// Invokes the ValidateAsync method
    pub fn validate_async(&self) -> Result<bool, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("context".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync", args)?;
        Ok(serde_json::from_value(result)?)
    }
}

/// Wrapper for Aspire.Hosting.CodeGeneration.Rust.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource
pub struct TestVaultResource {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for TestVaultResource {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl TestVaultResource {
    pub fn new(handle: Handle, client: Arc<AspireClient>) -> Self {
        Self { handle, client }
    }

    pub fn handle(&self) -> &Handle {
        &self.handle
    }

    pub fn client(&self) -> &Arc<AspireClient> {
        &self.client
    }

    /// Adds a bind mount
    pub fn with_bind_mount(&self, source: &str, target: &str, is_read_only: Option<bool>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), serde_json::to_value(&source).unwrap_or(Value::Null));
        args.insert("target".to_string(), serde_json::to_value(&target).unwrap_or(Value::Null));
        if let Some(ref v) = is_read_only {
            args.insert("isReadOnly".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withBindMount", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container entrypoint
    pub fn with_entrypoint(&self, entrypoint: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("entrypoint".to_string(), serde_json::to_value(&entrypoint).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEntrypoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image tag
    pub fn with_image_tag(&self, tag: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("tag".to_string(), serde_json::to_value(&tag).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImageTag", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image registry
    pub fn with_image_registry(&self, registry: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("registry".to_string(), serde_json::to_value(&registry).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImageRegistry", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image
    pub fn with_image(&self, image: &str, tag: Option<&str>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("image".to_string(), serde_json::to_value(&image).unwrap_or(Value::Null));
        if let Some(ref v) = tag {
            args.insert("tag".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withImage", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Adds runtime arguments for the container
    pub fn with_container_runtime_args(&self, args: Vec<String>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withContainerRuntimeArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the lifetime behavior of the container resource
    pub fn with_lifetime(&self, lifetime: ContainerLifetime) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("lifetime".to_string(), serde_json::to_value(&lifetime).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withLifetime", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container image pull policy
    pub fn with_image_pull_policy(&self, pull_policy: ImagePullPolicy) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("pullPolicy".to_string(), serde_json::to_value(&pull_policy).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withImagePullPolicy", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets the container name
    pub fn with_container_name(&self, name: &str) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withContainerName", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Sets an environment variable
    pub fn with_environment(&self, name: &str, value: &str) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironment", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds an environment variable with a reference expression
    pub fn with_environment_expression(&self, name: &str, value: ReferenceExpression) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("value".to_string(), serde_json::to_value(&value).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via callback
    pub fn with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets environment variables via async callback
    pub fn with_environment_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withEnvironmentCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds arguments
    pub fn with_args(&self, args: Vec<String>) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("args".to_string(), serde_json::to_value(&args).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgs", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via callback
    pub fn with_args_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Sets command-line arguments via async callback
    pub fn with_args_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithArgs, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withArgsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithArgs::new(handle, self.client.clone()))
    }

    /// Adds a reference to another resource
    pub fn with_reference(&self, source: &IResourceWithConnectionString, connection_name: Option<&str>, optional: Option<bool>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        if let Some(ref v) = connection_name {
            args.insert("connectionName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = optional {
            args.insert("optional".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a service discovery reference to another resource
    pub fn with_service_reference(&self, source: &IResourceWithServiceDiscovery) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("source".to_string(), source.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withServiceReference", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Adds a network endpoint
    pub fn with_endpoint(&self, port: Option<f64>, target_port: Option<f64>, scheme: Option<&str>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>, is_external: Option<bool>, protocol: Option<ProtocolType>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = scheme {
            args.insert("scheme".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_external {
            args.insert("isExternal".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = protocol {
            args.insert("protocol".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTP endpoint
    pub fn with_http_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds an HTTPS endpoint
    pub fn with_https_endpoint(&self, port: Option<f64>, target_port: Option<f64>, name: Option<&str>, env: Option<&str>, is_proxied: Option<bool>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = port {
            args.insert("port".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = target_port {
            args.insert("targetPort".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = env {
            args.insert("env".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_proxied {
            args.insert("isProxied".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpsEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Makes HTTP endpoints externally accessible
    pub fn with_external_http_endpoints(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExternalHttpEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Gets an endpoint reference
    pub fn get_endpoint(&self, name: &str) -> Result<EndpointReference, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/getEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(EndpointReference::new(handle, self.client.clone()))
    }

    /// Configures resource for HTTP/2
    pub fn as_http2_service(&self) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/asHttp2Service", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via callback
    pub fn with_urls_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes displayed URLs via async callback
    pub fn with_urls_callback_async(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlsCallbackAsync", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds or modifies displayed URLs
    pub fn with_url(&self, url: &str, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrl", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL using a reference expression
    pub fn with_url_expression(&self, url: ReferenceExpression, display_text: Option<&str>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("url".to_string(), serde_json::to_value(&url).unwrap_or(Value::Null));
        if let Some(ref v) = display_text {
            args.insert("displayText".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlExpression", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Customizes the URL for a specific endpoint via callback
    pub fn with_url_for_endpoint(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpoint", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a URL for a specific endpoint via factory callback
    pub fn with_url_for_endpoint_factory(&self, endpoint_name: &str, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpointName".to_string(), serde_json::to_value(&endpoint_name).unwrap_or(Value::Null));
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting/withUrlForEndpointFactory", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Waits for another resource to be ready
    pub fn wait_for(&self, dependency: &IResource) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/waitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Prevents resource from starting automatically
    pub fn with_explicit_start(&self) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withExplicitStart", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for resource completion
    pub fn wait_for_completion(&self, dependency: &IResource, exit_code: Option<f64>) -> Result<IResourceWithWaitSupport, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        if let Some(ref v) = exit_code {
            args.insert("exitCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/waitForCompletion", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithWaitSupport::new(handle, self.client.clone()))
    }

    /// Adds a health check by key
    pub fn with_health_check(&self, key: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("key".to_string(), serde_json::to_value(&key).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting/withHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds an HTTP health check
    pub fn with_http_health_check(&self, path: Option<&str>, status_code: Option<f64>, endpoint_name: Option<&str>) -> Result<IResourceWithEndpoints, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = path {
            args.insert("path".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = status_code {
            args.insert("statusCode".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = endpoint_name {
            args.insert("endpointName".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withHttpHealthCheck", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEndpoints::new(handle, self.client.clone()))
    }

    /// Adds a resource command
    pub fn with_command(&self, name: &str, display_name: &str, execute_command: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static, command_options: Option<CommandOptions>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("name".to_string(), serde_json::to_value(&name).unwrap_or(Value::Null));
        args.insert("displayName".to_string(), serde_json::to_value(&display_name).unwrap_or(Value::Null));
        let callback_id = register_callback(execute_command);
        args.insert("executeCommand".to_string(), Value::String(callback_id));
        if let Some(ref v) = command_options {
            args.insert("commandOptions".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withCommand", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the parent relationship
    pub fn with_parent_relationship(&self, parent: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("parent".to_string(), parent.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/withParentRelationship", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a volume
    pub fn with_volume(&self, target: &str, name: Option<&str>, is_read_only: Option<bool>) -> Result<ContainerResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        args.insert("target".to_string(), serde_json::to_value(&target).unwrap_or(Value::Null));
        if let Some(ref v) = name {
            args.insert("name".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = is_read_only {
            args.insert("isReadOnly".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting/withVolume", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ContainerResource::new(handle, self.client.clone()))
    }

    /// Gets the resource name
    pub fn get_resource_name(&self) -> Result<String, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("resource".to_string(), self.handle.to_json());
        let result = self.client.invoke_capability("Aspire.Hosting/getResourceName", args)?;
        Ok(serde_json::from_value(result)?)
    }

    /// Adds an optional string parameter
    pub fn with_optional_string(&self, value: Option<&str>, enabled: Option<bool>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        if let Some(ref v) = value {
            args.insert("value".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        if let Some(ref v) = enabled {
            args.insert("enabled".to_string(), serde_json::to_value(v).unwrap_or(Value::Null));
        }
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalString", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures the resource with a DTO
    pub fn with_config(&self, config: TestConfigDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures environment with callback (test version)
    pub fn test_with_environment_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWithEnvironmentCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Sets the created timestamp
    pub fn with_created_at(&self, created_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("createdAt".to_string(), serde_json::to_value(&created_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCreatedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the modified timestamp
    pub fn with_modified_at(&self, modified_at: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("modifiedAt".to_string(), serde_json::to_value(&modified_at).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withModifiedAt", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the correlation ID
    pub fn with_correlation_id(&self, correlation_id: &str) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("correlationId".to_string(), serde_json::to_value(&correlation_id).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCorrelationId", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with optional callback
    pub fn with_optional_callback(&self, callback: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(callback);
        args.insert("callback".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withOptionalCallback", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the resource status
    pub fn with_status(&self, status: TestResourceStatus) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("status".to_string(), serde_json::to_value(&status).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withStatus", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures with nested DTO
    pub fn with_nested_config(&self, config: TestNestedDto) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("config".to_string(), serde_json::to_value(&config).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withNestedConfig", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds validation callback
    pub fn with_validator(&self, validator: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(validator);
        args.insert("validator".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withValidator", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Waits for another resource (test version)
    pub fn test_wait_for(&self, dependency: &IResource) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/testWaitFor", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Adds a dependency on another resource
    pub fn with_dependency(&self, dependency: &IResourceWithConnectionString) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("dependency".to_string(), dependency.handle().to_json());
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withDependency", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets the endpoints
    pub fn with_endpoints(&self, endpoints: Vec<String>) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("endpoints".to_string(), serde_json::to_value(&endpoints).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEndpoints", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Sets environment variables
    pub fn with_environment_variables(&self, variables: HashMap<String, String>) -> Result<IResourceWithEnvironment, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("variables".to_string(), serde_json::to_value(&variables).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withEnvironmentVariables", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResourceWithEnvironment::new(handle, self.client.clone()))
    }

    /// Performs a cancellable operation
    pub fn with_cancellable_operation(&self, operation: impl Fn(Vec<Value>) -> Value + Send + Sync + 'static) -> Result<IResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        let callback_id = register_callback(operation);
        args.insert("operation".to_string(), Value::String(callback_id));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withCancellableOperation", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(IResource::new(handle, self.client.clone()))
    }

    /// Configures vault using direct interface target
    pub fn with_vault_direct(&self, option: &str) -> Result<ITestVaultResource, Box<dyn std::error::Error>> {
        let mut args: HashMap<String, Value> = HashMap::new();
        args.insert("builder".to_string(), self.handle.to_json());
        args.insert("option".to_string(), serde_json::to_value(&option).unwrap_or(Value::Null));
        let result = self.client.invoke_capability("Aspire.Hosting.CodeGeneration.Rust.Tests/withVaultDirect", args)?;
        let handle: Handle = serde_json::from_value(result)?;
        Ok(ITestVaultResource::new(handle, self.client.clone()))
    }
}

/// Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext
pub struct UpdateCommandStateContext {
    handle: Handle,
    client: Arc<AspireClient>,
}

impl HasHandle for UpdateCommandStateContext {
    fn handle(&self) -> &Handle {
        &self.handle
    }
}

impl UpdateCommandStateContext {
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

// ============================================================================
// Handle wrapper registrations
// ============================================================================

pub fn register_all_wrappers() {
    // Handle wrappers are created inline in generated code
    // This function is provided for API compatibility
}

// ============================================================================
// Connection Helpers
// ============================================================================

/// Establishes a connection to the AppHost server.
pub fn connect() -> Result<Arc<AspireClient>, Box<dyn std::error::Error>> {
    let socket_path = std::env::var("REMOTE_APP_HOST_SOCKET_PATH")
        .map_err(|_| "REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Run this application using `aspire run`")?;
    let client = Arc::new(AspireClient::new(&socket_path));
    client.connect()?;
    Ok(client)
}

/// Creates a new distributed application builder.
pub fn create_builder(options: Option<CreateBuilderOptions>) -> Result<IDistributedApplicationBuilder, Box<dyn std::error::Error>> {
    let client = connect()?;
    let mut resolved_options: HashMap<String, Value> = HashMap::new();
    if let Some(opts) = options {
        for (k, v) in opts.to_map() {
            resolved_options.insert(k, v);
        }
    }
    if !resolved_options.contains_key("Args") {
        let args: Vec<String> = std::env::args().skip(1).collect();
        resolved_options.insert("Args".to_string(), serde_json::to_value(args).unwrap_or(Value::Null));
    }
    if !resolved_options.contains_key("ProjectDirectory") {
        if let Ok(pwd) = std::env::current_dir() {
            resolved_options.insert("ProjectDirectory".to_string(), Value::String(pwd.to_string_lossy().to_string()));
        }
    }
    let mut args: HashMap<String, Value> = HashMap::new();
    args.insert("options".to_string(), serde_json::to_value(resolved_options).unwrap_or(Value::Null));
    let result = client.invoke_capability("Aspire.Hosting/createBuilderWithOptions", args)?;
    let handle: Handle = serde_json::from_value(result)?;
    Ok(IDistributedApplicationBuilder::new(handle, client))
}

