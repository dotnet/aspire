// aspire.go - Capability-based Aspire SDK
// GENERATED CODE - DO NOT EDIT

package aspire

import (
	"fmt"
	"os"
)

// ============================================================================
// Enums
// ============================================================================

// ContainerLifetime represents ContainerLifetime.
type ContainerLifetime string

const (
	ContainerLifetimeSession ContainerLifetime = "Session"
	ContainerLifetimePersistent ContainerLifetime = "Persistent"
)

// ImagePullPolicy represents ImagePullPolicy.
type ImagePullPolicy string

const (
	ImagePullPolicyDefault ImagePullPolicy = "Default"
	ImagePullPolicyAlways ImagePullPolicy = "Always"
	ImagePullPolicyMissing ImagePullPolicy = "Missing"
	ImagePullPolicyNever ImagePullPolicy = "Never"
)

// DistributedApplicationOperation represents DistributedApplicationOperation.
type DistributedApplicationOperation string

const (
	DistributedApplicationOperationRun DistributedApplicationOperation = "Run"
	DistributedApplicationOperationPublish DistributedApplicationOperation = "Publish"
)

// ProtocolType represents ProtocolType.
type ProtocolType string

const (
	ProtocolTypeIP ProtocolType = "IP"
	ProtocolTypeIPv6HopByHopOptions ProtocolType = "IPv6HopByHopOptions"
	ProtocolTypeUnspecified ProtocolType = "Unspecified"
	ProtocolTypeIcmp ProtocolType = "Icmp"
	ProtocolTypeIgmp ProtocolType = "Igmp"
	ProtocolTypeGgp ProtocolType = "Ggp"
	ProtocolTypeIPv4 ProtocolType = "IPv4"
	ProtocolTypeTcp ProtocolType = "Tcp"
	ProtocolTypePup ProtocolType = "Pup"
	ProtocolTypeUdp ProtocolType = "Udp"
	ProtocolTypeIdp ProtocolType = "Idp"
	ProtocolTypeIPv6 ProtocolType = "IPv6"
	ProtocolTypeIPv6RoutingHeader ProtocolType = "IPv6RoutingHeader"
	ProtocolTypeIPv6FragmentHeader ProtocolType = "IPv6FragmentHeader"
	ProtocolTypeIPSecEncapsulatingSecurityPayload ProtocolType = "IPSecEncapsulatingSecurityPayload"
	ProtocolTypeIPSecAuthenticationHeader ProtocolType = "IPSecAuthenticationHeader"
	ProtocolTypeIcmpV6 ProtocolType = "IcmpV6"
	ProtocolTypeIPv6NoNextHeader ProtocolType = "IPv6NoNextHeader"
	ProtocolTypeIPv6DestinationOptions ProtocolType = "IPv6DestinationOptions"
	ProtocolTypeND ProtocolType = "ND"
	ProtocolTypeRaw ProtocolType = "Raw"
	ProtocolTypeIpx ProtocolType = "Ipx"
	ProtocolTypeSpx ProtocolType = "Spx"
	ProtocolTypeSpxII ProtocolType = "SpxII"
	ProtocolTypeUnknown ProtocolType = "Unknown"
)

// EndpointProperty represents EndpointProperty.
type EndpointProperty string

const (
	EndpointPropertyUrl EndpointProperty = "Url"
	EndpointPropertyHost EndpointProperty = "Host"
	EndpointPropertyIPV4Host EndpointProperty = "IPV4Host"
	EndpointPropertyPort EndpointProperty = "Port"
	EndpointPropertyScheme EndpointProperty = "Scheme"
	EndpointPropertyTargetPort EndpointProperty = "TargetPort"
	EndpointPropertyHostAndPort EndpointProperty = "HostAndPort"
)

// IconVariant represents IconVariant.
type IconVariant string

const (
	IconVariantRegular IconVariant = "Regular"
	IconVariantFilled IconVariant = "Filled"
)

// UrlDisplayLocation represents UrlDisplayLocation.
type UrlDisplayLocation string

const (
	UrlDisplayLocationSummaryAndDetails UrlDisplayLocation = "SummaryAndDetails"
	UrlDisplayLocationDetailsOnly UrlDisplayLocation = "DetailsOnly"
)

// TestPersistenceMode represents TestPersistenceMode.
type TestPersistenceMode string

const (
	TestPersistenceModeNone TestPersistenceMode = "None"
	TestPersistenceModeVolume TestPersistenceMode = "Volume"
	TestPersistenceModeBind TestPersistenceMode = "Bind"
)

// TestResourceStatus represents TestResourceStatus.
type TestResourceStatus string

const (
	TestResourceStatusPending TestResourceStatus = "Pending"
	TestResourceStatusRunning TestResourceStatus = "Running"
	TestResourceStatusStopped TestResourceStatus = "Stopped"
	TestResourceStatusFailed TestResourceStatus = "Failed"
)

// ============================================================================
// DTOs
// ============================================================================

// CreateBuilderOptions represents CreateBuilderOptions.
type CreateBuilderOptions struct {
	Args []string `json:"Args,omitempty"`
	ProjectDirectory string `json:"ProjectDirectory,omitempty"`
	AppHostFilePath string `json:"AppHostFilePath,omitempty"`
	ContainerRegistryOverride string `json:"ContainerRegistryOverride,omitempty"`
	DisableDashboard bool `json:"DisableDashboard,omitempty"`
	DashboardApplicationName string `json:"DashboardApplicationName,omitempty"`
	AllowUnsecuredTransport bool `json:"AllowUnsecuredTransport,omitempty"`
	EnableResourceLogging bool `json:"EnableResourceLogging,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *CreateBuilderOptions) ToMap() map[string]any {
	return map[string]any{
		"Args": SerializeValue(d.Args),
		"ProjectDirectory": SerializeValue(d.ProjectDirectory),
		"AppHostFilePath": SerializeValue(d.AppHostFilePath),
		"ContainerRegistryOverride": SerializeValue(d.ContainerRegistryOverride),
		"DisableDashboard": SerializeValue(d.DisableDashboard),
		"DashboardApplicationName": SerializeValue(d.DashboardApplicationName),
		"AllowUnsecuredTransport": SerializeValue(d.AllowUnsecuredTransport),
		"EnableResourceLogging": SerializeValue(d.EnableResourceLogging),
	}
}

// ResourceEventDto represents ResourceEventDto.
type ResourceEventDto struct {
	ResourceName string `json:"ResourceName,omitempty"`
	ResourceId string `json:"ResourceId,omitempty"`
	State string `json:"State,omitempty"`
	StateStyle string `json:"StateStyle,omitempty"`
	HealthStatus string `json:"HealthStatus,omitempty"`
	ExitCode float64 `json:"ExitCode,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *ResourceEventDto) ToMap() map[string]any {
	return map[string]any{
		"ResourceName": SerializeValue(d.ResourceName),
		"ResourceId": SerializeValue(d.ResourceId),
		"State": SerializeValue(d.State),
		"StateStyle": SerializeValue(d.StateStyle),
		"HealthStatus": SerializeValue(d.HealthStatus),
		"ExitCode": SerializeValue(d.ExitCode),
	}
}

// CommandOptions represents CommandOptions.
type CommandOptions struct {
	Description string `json:"Description,omitempty"`
	Parameter any `json:"Parameter,omitempty"`
	ConfirmationMessage string `json:"ConfirmationMessage,omitempty"`
	IconName string `json:"IconName,omitempty"`
	IconVariant IconVariant `json:"IconVariant,omitempty"`
	IsHighlighted bool `json:"IsHighlighted,omitempty"`
	UpdateState any `json:"UpdateState,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *CommandOptions) ToMap() map[string]any {
	return map[string]any{
		"Description": SerializeValue(d.Description),
		"Parameter": SerializeValue(d.Parameter),
		"ConfirmationMessage": SerializeValue(d.ConfirmationMessage),
		"IconName": SerializeValue(d.IconName),
		"IconVariant": SerializeValue(d.IconVariant),
		"IsHighlighted": SerializeValue(d.IsHighlighted),
		"UpdateState": SerializeValue(d.UpdateState),
	}
}

// ExecuteCommandResult represents ExecuteCommandResult.
type ExecuteCommandResult struct {
	Success bool `json:"Success,omitempty"`
	Canceled bool `json:"Canceled,omitempty"`
	ErrorMessage string `json:"ErrorMessage,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *ExecuteCommandResult) ToMap() map[string]any {
	return map[string]any{
		"Success": SerializeValue(d.Success),
		"Canceled": SerializeValue(d.Canceled),
		"ErrorMessage": SerializeValue(d.ErrorMessage),
	}
}

// ResourceUrlAnnotation represents ResourceUrlAnnotation.
type ResourceUrlAnnotation struct {
	Url string `json:"Url,omitempty"`
	DisplayText string `json:"DisplayText,omitempty"`
	Endpoint *EndpointReference `json:"Endpoint,omitempty"`
	DisplayLocation UrlDisplayLocation `json:"DisplayLocation,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *ResourceUrlAnnotation) ToMap() map[string]any {
	return map[string]any{
		"Url": SerializeValue(d.Url),
		"DisplayText": SerializeValue(d.DisplayText),
		"Endpoint": SerializeValue(d.Endpoint),
		"DisplayLocation": SerializeValue(d.DisplayLocation),
	}
}

// TestConfigDto represents TestConfigDto.
type TestConfigDto struct {
	Name string `json:"Name,omitempty"`
	Port float64 `json:"Port,omitempty"`
	Enabled bool `json:"Enabled,omitempty"`
	OptionalField string `json:"OptionalField,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *TestConfigDto) ToMap() map[string]any {
	return map[string]any{
		"Name": SerializeValue(d.Name),
		"Port": SerializeValue(d.Port),
		"Enabled": SerializeValue(d.Enabled),
		"OptionalField": SerializeValue(d.OptionalField),
	}
}

// TestNestedDto represents TestNestedDto.
type TestNestedDto struct {
	Id string `json:"Id,omitempty"`
	Config *TestConfigDto `json:"Config,omitempty"`
	Tags *AspireList[string] `json:"Tags,omitempty"`
	Counts *AspireDict[string, float64] `json:"Counts,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *TestNestedDto) ToMap() map[string]any {
	return map[string]any{
		"Id": SerializeValue(d.Id),
		"Config": SerializeValue(d.Config),
		"Tags": SerializeValue(d.Tags),
		"Counts": SerializeValue(d.Counts),
	}
}

// TestDeeplyNestedDto represents TestDeeplyNestedDto.
type TestDeeplyNestedDto struct {
	NestedData *AspireDict[string, *AspireList[*TestConfigDto]] `json:"NestedData,omitempty"`
	MetadataArray []*AspireDict[string, string] `json:"MetadataArray,omitempty"`
}

// ToMap converts the DTO to a map for JSON serialization.
func (d *TestDeeplyNestedDto) ToMap() map[string]any {
	return map[string]any{
		"NestedData": SerializeValue(d.NestedData),
		"MetadataArray": SerializeValue(d.MetadataArray),
	}
}

// ============================================================================
// Handle Wrappers
// ============================================================================

// CommandLineArgsCallbackContext wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext.
type CommandLineArgsCallbackContext struct {
	HandleWrapperBase
	args *AspireList[any]
}

// NewCommandLineArgsCallbackContext creates a new CommandLineArgsCallbackContext.
func NewCommandLineArgsCallbackContext(handle *Handle, client *AspireClient) *CommandLineArgsCallbackContext {
	return &CommandLineArgsCallbackContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Args gets the Args property
func (s *CommandLineArgsCallbackContext) Args() *AspireList[any] {
	if s.args == nil {
		s.args = NewAspireListWithGetter[any](s.Handle(), s.Client(), "Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.args")
	}
	return s.args
}

// CancellationToken gets the CancellationToken property
func (s *CommandLineArgsCallbackContext) CancellationToken() (*CancellationToken, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.cancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*CancellationToken), nil
}

// ExecutionContext gets the ExecutionContext property
func (s *CommandLineArgsCallbackContext) ExecutionContext() (*DistributedApplicationExecutionContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.executionContext", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplicationExecutionContext), nil
}

// SetExecutionContext sets the ExecutionContext property
func (s *CommandLineArgsCallbackContext) SetExecutionContext(value *DistributedApplicationExecutionContext) (*CommandLineArgsCallbackContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/CommandLineArgsCallbackContext.setExecutionContext", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*CommandLineArgsCallbackContext), nil
}

// ContainerResource wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource.
type ContainerResource struct {
	ResourceBuilderBase
}

// NewContainerResource creates a new ContainerResource.
func NewContainerResource(handle *Handle, client *AspireClient) *ContainerResource {
	return &ContainerResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithEnvironment sets an environment variable
func (s *ContainerResource) WithEnvironment(name string, value string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironment", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentExpression adds an environment variable with a reference expression
func (s *ContainerResource) WithEnvironmentExpression(name string, value *ReferenceExpression) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallback sets environment variables via callback
func (s *ContainerResource) WithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallbackAsync sets environment variables via async callback
func (s *ContainerResource) WithEnvironmentCallbackAsync(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithArgs adds arguments
func (s *ContainerResource) WithArgs(args []string) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallback sets command-line arguments via callback
func (s *ContainerResource) WithArgsCallback(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallbackAsync sets command-line arguments via async callback
func (s *ContainerResource) WithArgsCallbackAsync(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithReference adds a reference to another resource
func (s *ContainerResource) WithReference(source *IResourceWithConnectionString, connectionName string, optional bool) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["connectionName"] = SerializeValue(connectionName)
	reqArgs["optional"] = SerializeValue(optional)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithServiceReference adds a service discovery reference to another resource
func (s *ContainerResource) WithServiceReference(source *IResourceWithServiceDiscovery) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withServiceReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEndpoint adds a network endpoint
func (s *ContainerResource) WithEndpoint(port float64, targetPort float64, scheme string, name string, env string, isProxied bool, isExternal bool, protocol ProtocolType) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["scheme"] = SerializeValue(scheme)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	reqArgs["isExternal"] = SerializeValue(isExternal)
	reqArgs["protocol"] = SerializeValue(protocol)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpEndpoint adds an HTTP endpoint
func (s *ContainerResource) WithHttpEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpsEndpoint adds an HTTPS endpoint
func (s *ContainerResource) WithHttpsEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithExternalHttpEndpoints makes HTTP endpoints externally accessible
func (s *ContainerResource) WithExternalHttpEndpoints() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// GetEndpoint gets an endpoint reference
func (s *ContainerResource) GetEndpoint(name string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// AsHttp2Service configures resource for HTTP/2
func (s *ContainerResource) AsHttp2Service() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/asHttp2Service", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *ContainerResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *ContainerResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *ContainerResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *ContainerResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *ContainerResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpointFactory adds a URL for a specific endpoint via factory callback
func (s *ContainerResource) WithUrlForEndpointFactory(endpointName string, callback func(...any) any) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WaitFor waits for another resource to be ready
func (s *ContainerResource) WaitFor(dependency *IResource) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *ContainerResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForCompletion waits for resource completion
func (s *ContainerResource) WaitForCompletion(dependency *IResource, exitCode float64) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	reqArgs["exitCode"] = SerializeValue(exitCode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitForCompletion", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithHealthCheck adds a health check by key
func (s *ContainerResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHttpHealthCheck adds an HTTP health check
func (s *ContainerResource) WithHttpHealthCheck(path string, statusCode float64, endpointName string) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["path"] = SerializeValue(path)
	reqArgs["statusCode"] = SerializeValue(statusCode)
	reqArgs["endpointName"] = SerializeValue(endpointName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithCommand adds a resource command
func (s *ContainerResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *ContainerResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// GetResourceName gets the resource name
func (s *ContainerResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithOptionalString adds an optional string parameter
func (s *ContainerResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *ContainerResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWithEnvironmentCallback configures environment with callback (test version)
func (s *ContainerResource) TestWithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWithEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCreatedAt sets the created timestamp
func (s *ContainerResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *ContainerResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *ContainerResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *ContainerResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *ContainerResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *ContainerResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *ContainerResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *ContainerResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithDependency adds a dependency on another resource
func (s *ContainerResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *ContainerResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEnvironmentVariables sets environment variables
func (s *ContainerResource) WithEnvironmentVariables(variables map[string]string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["variables"] = SerializeValue(variables)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEnvironmentVariables", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *ContainerResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// DistributedApplication wraps a handle for Aspire.Hosting/Aspire.Hosting.DistributedApplication.
type DistributedApplication struct {
	HandleWrapperBase
}

// NewDistributedApplication creates a new DistributedApplication.
func NewDistributedApplication(handle *Handle, client *AspireClient) *DistributedApplication {
	return &DistributedApplication{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Run runs the distributed application
func (s *DistributedApplication) Run(cancellationToken *CancellationToken) error {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	if cancellationToken != nil {
		reqArgs["cancellationToken"] = RegisterCancellation(cancellationToken, s.Client())
	}
	_, err := s.Client().InvokeCapability("Aspire.Hosting/run", reqArgs)
	return err
}

// DistributedApplicationEventSubscription wraps a handle for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription.
type DistributedApplicationEventSubscription struct {
	HandleWrapperBase
}

// NewDistributedApplicationEventSubscription creates a new DistributedApplicationEventSubscription.
func NewDistributedApplicationEventSubscription(handle *Handle, client *AspireClient) *DistributedApplicationEventSubscription {
	return &DistributedApplicationEventSubscription{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// DistributedApplicationExecutionContext wraps a handle for Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext.
type DistributedApplicationExecutionContext struct {
	HandleWrapperBase
}

// NewDistributedApplicationExecutionContext creates a new DistributedApplicationExecutionContext.
func NewDistributedApplicationExecutionContext(handle *Handle, client *AspireClient) *DistributedApplicationExecutionContext {
	return &DistributedApplicationExecutionContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// PublisherName gets the PublisherName property
func (s *DistributedApplicationExecutionContext) PublisherName() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.publisherName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetPublisherName sets the PublisherName property
func (s *DistributedApplicationExecutionContext) SetPublisherName(value string) (*DistributedApplicationExecutionContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.setPublisherName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplicationExecutionContext), nil
}

// Operation gets the Operation property
func (s *DistributedApplicationExecutionContext) Operation() (*DistributedApplicationOperation, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.operation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplicationOperation), nil
}

// IsPublishMode gets the IsPublishMode property
func (s *DistributedApplicationExecutionContext) IsPublishMode() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.isPublishMode", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// IsRunMode gets the IsRunMode property
func (s *DistributedApplicationExecutionContext) IsRunMode() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/DistributedApplicationExecutionContext.isRunMode", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// DistributedApplicationExecutionContextOptions wraps a handle for Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions.
type DistributedApplicationExecutionContextOptions struct {
	HandleWrapperBase
}

// NewDistributedApplicationExecutionContextOptions creates a new DistributedApplicationExecutionContextOptions.
func NewDistributedApplicationExecutionContextOptions(handle *Handle, client *AspireClient) *DistributedApplicationExecutionContextOptions {
	return &DistributedApplicationExecutionContextOptions{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// DistributedApplicationResourceEventSubscription wraps a handle for Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription.
type DistributedApplicationResourceEventSubscription struct {
	HandleWrapperBase
}

// NewDistributedApplicationResourceEventSubscription creates a new DistributedApplicationResourceEventSubscription.
func NewDistributedApplicationResourceEventSubscription(handle *Handle, client *AspireClient) *DistributedApplicationResourceEventSubscription {
	return &DistributedApplicationResourceEventSubscription{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// EndpointReference wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference.
type EndpointReference struct {
	HandleWrapperBase
}

// NewEndpointReference creates a new EndpointReference.
func NewEndpointReference(handle *Handle, client *AspireClient) *EndpointReference {
	return &EndpointReference{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// EndpointName gets the EndpointName property
func (s *EndpointReference) EndpointName() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.endpointName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// ErrorMessage gets the ErrorMessage property
func (s *EndpointReference) ErrorMessage() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.errorMessage", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetErrorMessage sets the ErrorMessage property
func (s *EndpointReference) SetErrorMessage(value string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.setErrorMessage", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// IsAllocated gets the IsAllocated property
func (s *EndpointReference) IsAllocated() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.isAllocated", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// Exists gets the Exists property
func (s *EndpointReference) Exists() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.exists", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// IsHttp gets the IsHttp property
func (s *EndpointReference) IsHttp() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttp", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// IsHttps gets the IsHttps property
func (s *EndpointReference) IsHttps() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.isHttps", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// Port gets the Port property
func (s *EndpointReference) Port() (*float64, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.port", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*float64), nil
}

// TargetPort gets the TargetPort property
func (s *EndpointReference) TargetPort() (*float64, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.targetPort", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*float64), nil
}

// Host gets the Host property
func (s *EndpointReference) Host() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.host", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// Scheme gets the Scheme property
func (s *EndpointReference) Scheme() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.scheme", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// Url gets the Url property
func (s *EndpointReference) Url() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReference.url", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// GetValueAsync gets the URL of the endpoint asynchronously
func (s *EndpointReference) GetValueAsync(cancellationToken *CancellationToken) (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	if cancellationToken != nil {
		reqArgs["cancellationToken"] = RegisterCancellation(cancellationToken, s.Client())
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/getValueAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// EndpointReferenceExpression wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression.
type EndpointReferenceExpression struct {
	HandleWrapperBase
}

// NewEndpointReferenceExpression creates a new EndpointReferenceExpression.
func NewEndpointReferenceExpression(handle *Handle, client *AspireClient) *EndpointReferenceExpression {
	return &EndpointReferenceExpression{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Endpoint gets the Endpoint property
func (s *EndpointReferenceExpression) Endpoint() (*EndpointReference, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.endpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// Property gets the Property property
func (s *EndpointReferenceExpression) Property() (*EndpointProperty, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.property", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointProperty), nil
}

// ValueExpression gets the ValueExpression property
func (s *EndpointReferenceExpression) ValueExpression() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EndpointReferenceExpression.valueExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// EnvironmentCallbackContext wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext.
type EnvironmentCallbackContext struct {
	HandleWrapperBase
	environmentVariables *AspireDict[string, any]
}

// NewEnvironmentCallbackContext creates a new EnvironmentCallbackContext.
func NewEnvironmentCallbackContext(handle *Handle, client *AspireClient) *EnvironmentCallbackContext {
	return &EnvironmentCallbackContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// EnvironmentVariables gets the EnvironmentVariables property
func (s *EnvironmentCallbackContext) EnvironmentVariables() *AspireDict[string, any] {
	if s.environmentVariables == nil {
		s.environmentVariables = NewAspireDictWithGetter[string, any](s.Handle(), s.Client(), "Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.environmentVariables")
	}
	return s.environmentVariables
}

// CancellationToken gets the CancellationToken property
func (s *EnvironmentCallbackContext) CancellationToken() (*CancellationToken, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.cancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*CancellationToken), nil
}

// ExecutionContext gets the ExecutionContext property
func (s *EnvironmentCallbackContext) ExecutionContext() (*DistributedApplicationExecutionContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/EnvironmentCallbackContext.executionContext", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplicationExecutionContext), nil
}

// ExecutableResource wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource.
type ExecutableResource struct {
	ResourceBuilderBase
}

// NewExecutableResource creates a new ExecutableResource.
func NewExecutableResource(handle *Handle, client *AspireClient) *ExecutableResource {
	return &ExecutableResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithExecutableCommand sets the executable command
func (s *ExecutableResource) WithExecutableCommand(command string) (*ExecutableResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["command"] = SerializeValue(command)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExecutableCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ExecutableResource), nil
}

// WithWorkingDirectory sets the executable working directory
func (s *ExecutableResource) WithWorkingDirectory(workingDirectory string) (*ExecutableResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["workingDirectory"] = SerializeValue(workingDirectory)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withWorkingDirectory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ExecutableResource), nil
}

// WithEnvironment sets an environment variable
func (s *ExecutableResource) WithEnvironment(name string, value string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironment", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentExpression adds an environment variable with a reference expression
func (s *ExecutableResource) WithEnvironmentExpression(name string, value *ReferenceExpression) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallback sets environment variables via callback
func (s *ExecutableResource) WithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallbackAsync sets environment variables via async callback
func (s *ExecutableResource) WithEnvironmentCallbackAsync(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithArgs adds arguments
func (s *ExecutableResource) WithArgs(args []string) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallback sets command-line arguments via callback
func (s *ExecutableResource) WithArgsCallback(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallbackAsync sets command-line arguments via async callback
func (s *ExecutableResource) WithArgsCallbackAsync(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithReference adds a reference to another resource
func (s *ExecutableResource) WithReference(source *IResourceWithConnectionString, connectionName string, optional bool) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["connectionName"] = SerializeValue(connectionName)
	reqArgs["optional"] = SerializeValue(optional)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithServiceReference adds a service discovery reference to another resource
func (s *ExecutableResource) WithServiceReference(source *IResourceWithServiceDiscovery) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withServiceReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEndpoint adds a network endpoint
func (s *ExecutableResource) WithEndpoint(port float64, targetPort float64, scheme string, name string, env string, isProxied bool, isExternal bool, protocol ProtocolType) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["scheme"] = SerializeValue(scheme)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	reqArgs["isExternal"] = SerializeValue(isExternal)
	reqArgs["protocol"] = SerializeValue(protocol)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpEndpoint adds an HTTP endpoint
func (s *ExecutableResource) WithHttpEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpsEndpoint adds an HTTPS endpoint
func (s *ExecutableResource) WithHttpsEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithExternalHttpEndpoints makes HTTP endpoints externally accessible
func (s *ExecutableResource) WithExternalHttpEndpoints() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// GetEndpoint gets an endpoint reference
func (s *ExecutableResource) GetEndpoint(name string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// AsHttp2Service configures resource for HTTP/2
func (s *ExecutableResource) AsHttp2Service() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/asHttp2Service", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *ExecutableResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *ExecutableResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *ExecutableResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *ExecutableResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *ExecutableResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpointFactory adds a URL for a specific endpoint via factory callback
func (s *ExecutableResource) WithUrlForEndpointFactory(endpointName string, callback func(...any) any) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WaitFor waits for another resource to be ready
func (s *ExecutableResource) WaitFor(dependency *IResource) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *ExecutableResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForCompletion waits for resource completion
func (s *ExecutableResource) WaitForCompletion(dependency *IResource, exitCode float64) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	reqArgs["exitCode"] = SerializeValue(exitCode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitForCompletion", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithHealthCheck adds a health check by key
func (s *ExecutableResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHttpHealthCheck adds an HTTP health check
func (s *ExecutableResource) WithHttpHealthCheck(path string, statusCode float64, endpointName string) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["path"] = SerializeValue(path)
	reqArgs["statusCode"] = SerializeValue(statusCode)
	reqArgs["endpointName"] = SerializeValue(endpointName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithCommand adds a resource command
func (s *ExecutableResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *ExecutableResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// GetResourceName gets the resource name
func (s *ExecutableResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithOptionalString adds an optional string parameter
func (s *ExecutableResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *ExecutableResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWithEnvironmentCallback configures environment with callback (test version)
func (s *ExecutableResource) TestWithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWithEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCreatedAt sets the created timestamp
func (s *ExecutableResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *ExecutableResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *ExecutableResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *ExecutableResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *ExecutableResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *ExecutableResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *ExecutableResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *ExecutableResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithDependency adds a dependency on another resource
func (s *ExecutableResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *ExecutableResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEnvironmentVariables sets environment variables
func (s *ExecutableResource) WithEnvironmentVariables(variables map[string]string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["variables"] = SerializeValue(variables)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEnvironmentVariables", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *ExecutableResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// ExecuteCommandContext wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext.
type ExecuteCommandContext struct {
	HandleWrapperBase
}

// NewExecuteCommandContext creates a new ExecuteCommandContext.
func NewExecuteCommandContext(handle *Handle, client *AspireClient) *ExecuteCommandContext {
	return &ExecuteCommandContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// ResourceName gets the ResourceName property
func (s *ExecuteCommandContext) ResourceName() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.resourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetResourceName sets the ResourceName property
func (s *ExecuteCommandContext) SetResourceName(value string) (*ExecuteCommandContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ExecuteCommandContext), nil
}

// CancellationToken gets the CancellationToken property
func (s *ExecuteCommandContext) CancellationToken() (*CancellationToken, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.cancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*CancellationToken), nil
}

// SetCancellationToken sets the CancellationToken property
func (s *ExecuteCommandContext) SetCancellationToken(value *CancellationToken) (*ExecuteCommandContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	if value != nil {
		reqArgs["value"] = RegisterCancellation(value, s.Client())
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/ExecuteCommandContext.setCancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ExecuteCommandContext), nil
}

// IDistributedApplicationBuilder wraps a handle for Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder.
type IDistributedApplicationBuilder struct {
	HandleWrapperBase
}

// NewIDistributedApplicationBuilder creates a new IDistributedApplicationBuilder.
func NewIDistributedApplicationBuilder(handle *Handle, client *AspireClient) *IDistributedApplicationBuilder {
	return &IDistributedApplicationBuilder{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// AddContainer adds a container resource
func (s *IDistributedApplicationBuilder) AddContainer(name string, image string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["image"] = SerializeValue(image)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/addContainer", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// AddExecutable adds an executable resource
func (s *IDistributedApplicationBuilder) AddExecutable(name string, command string, workingDirectory string, args []string) (*ExecutableResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["command"] = SerializeValue(command)
	reqArgs["workingDirectory"] = SerializeValue(workingDirectory)
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/addExecutable", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ExecutableResource), nil
}

// AppHostDirectory gets the AppHostDirectory property
func (s *IDistributedApplicationBuilder) AppHostDirectory() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.appHostDirectory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// Eventing gets the Eventing property
func (s *IDistributedApplicationBuilder) Eventing() (*IDistributedApplicationEventing, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.eventing", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IDistributedApplicationEventing), nil
}

// ExecutionContext gets the ExecutionContext property
func (s *IDistributedApplicationBuilder) ExecutionContext() (*DistributedApplicationExecutionContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/IDistributedApplicationBuilder.executionContext", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplicationExecutionContext), nil
}

// Build builds the distributed application
func (s *IDistributedApplicationBuilder) Build() (*DistributedApplication, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/build", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplication), nil
}

// AddParameter adds a parameter resource
func (s *IDistributedApplicationBuilder) AddParameter(name string, secret bool) (*ParameterResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["secret"] = SerializeValue(secret)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/addParameter", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ParameterResource), nil
}

// AddConnectionString adds a connection string resource
func (s *IDistributedApplicationBuilder) AddConnectionString(name string, environmentVariableName string) (*IResourceWithConnectionString, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["environmentVariableName"] = SerializeValue(environmentVariableName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/addConnectionString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithConnectionString), nil
}

// AddProject adds a .NET project resource
func (s *IDistributedApplicationBuilder) AddProject(name string, projectPath string, launchProfileName string) (*ProjectResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["projectPath"] = SerializeValue(projectPath)
	reqArgs["launchProfileName"] = SerializeValue(launchProfileName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/addProject", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ProjectResource), nil
}

// AddTestRedis adds a test Redis resource
func (s *IDistributedApplicationBuilder) AddTestRedis(name string, port float64) (*TestRedisResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["port"] = SerializeValue(port)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/addTestRedis", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestRedisResource), nil
}

// AddTestVault adds a test vault resource
func (s *IDistributedApplicationBuilder) AddTestVault(name string) (*TestVaultResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/addTestVault", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestVaultResource), nil
}

// IDistributedApplicationEvent wraps a handle for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent.
type IDistributedApplicationEvent struct {
	HandleWrapperBase
}

// NewIDistributedApplicationEvent creates a new IDistributedApplicationEvent.
func NewIDistributedApplicationEvent(handle *Handle, client *AspireClient) *IDistributedApplicationEvent {
	return &IDistributedApplicationEvent{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// IDistributedApplicationEventing wraps a handle for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing.
type IDistributedApplicationEventing struct {
	HandleWrapperBase
}

// NewIDistributedApplicationEventing creates a new IDistributedApplicationEventing.
func NewIDistributedApplicationEventing(handle *Handle, client *AspireClient) *IDistributedApplicationEventing {
	return &IDistributedApplicationEventing{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Unsubscribe invokes the Unsubscribe method
func (s *IDistributedApplicationEventing) Unsubscribe(subscription *DistributedApplicationEventSubscription) error {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["subscription"] = SerializeValue(subscription)
	_, err := s.Client().InvokeCapability("Aspire.Hosting.Eventing/IDistributedApplicationEventing.unsubscribe", reqArgs)
	return err
}

// IDistributedApplicationResourceEvent wraps a handle for Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent.
type IDistributedApplicationResourceEvent struct {
	HandleWrapperBase
}

// NewIDistributedApplicationResourceEvent creates a new IDistributedApplicationResourceEvent.
func NewIDistributedApplicationResourceEvent(handle *Handle, client *AspireClient) *IDistributedApplicationResourceEvent {
	return &IDistributedApplicationResourceEvent{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// IResource wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource.
type IResource struct {
	ResourceBuilderBase
}

// NewIResource creates a new IResource.
func NewIResource(handle *Handle, client *AspireClient) *IResource {
	return &IResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// IResourceWithArgs wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs.
type IResourceWithArgs struct {
	HandleWrapperBase
}

// NewIResourceWithArgs creates a new IResourceWithArgs.
func NewIResourceWithArgs(handle *Handle, client *AspireClient) *IResourceWithArgs {
	return &IResourceWithArgs{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// IResourceWithConnectionString wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString.
type IResourceWithConnectionString struct {
	ResourceBuilderBase
}

// NewIResourceWithConnectionString creates a new IResourceWithConnectionString.
func NewIResourceWithConnectionString(handle *Handle, client *AspireClient) *IResourceWithConnectionString {
	return &IResourceWithConnectionString{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// IResourceWithEndpoints wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints.
type IResourceWithEndpoints struct {
	HandleWrapperBase
}

// NewIResourceWithEndpoints creates a new IResourceWithEndpoints.
func NewIResourceWithEndpoints(handle *Handle, client *AspireClient) *IResourceWithEndpoints {
	return &IResourceWithEndpoints{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// IResourceWithEnvironment wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment.
type IResourceWithEnvironment struct {
	HandleWrapperBase
}

// NewIResourceWithEnvironment creates a new IResourceWithEnvironment.
func NewIResourceWithEnvironment(handle *Handle, client *AspireClient) *IResourceWithEnvironment {
	return &IResourceWithEnvironment{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// IResourceWithServiceDiscovery wraps a handle for Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery.
type IResourceWithServiceDiscovery struct {
	ResourceBuilderBase
}

// NewIResourceWithServiceDiscovery creates a new IResourceWithServiceDiscovery.
func NewIResourceWithServiceDiscovery(handle *Handle, client *AspireClient) *IResourceWithServiceDiscovery {
	return &IResourceWithServiceDiscovery{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// IResourceWithWaitSupport wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport.
type IResourceWithWaitSupport struct {
	HandleWrapperBase
}

// NewIResourceWithWaitSupport creates a new IResourceWithWaitSupport.
func NewIResourceWithWaitSupport(handle *Handle, client *AspireClient) *IResourceWithWaitSupport {
	return &IResourceWithWaitSupport{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// ITestVaultResource wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource.
type ITestVaultResource struct {
	ResourceBuilderBase
}

// NewITestVaultResource creates a new ITestVaultResource.
func NewITestVaultResource(handle *Handle, client *AspireClient) *ITestVaultResource {
	return &ITestVaultResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// ParameterResource wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource.
type ParameterResource struct {
	ResourceBuilderBase
}

// NewParameterResource creates a new ParameterResource.
func NewParameterResource(handle *Handle, client *AspireClient) *ParameterResource {
	return &ParameterResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithDescription sets a parameter description
func (s *ParameterResource) WithDescription(description string, enableMarkdown bool) (*ParameterResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["description"] = SerializeValue(description)
	reqArgs["enableMarkdown"] = SerializeValue(enableMarkdown)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withDescription", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ParameterResource), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *ParameterResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *ParameterResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *ParameterResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *ParameterResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *ParameterResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *ParameterResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHealthCheck adds a health check by key
func (s *ParameterResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCommand adds a resource command
func (s *ParameterResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *ParameterResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// GetResourceName gets the resource name
func (s *ParameterResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithOptionalString adds an optional string parameter
func (s *ParameterResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *ParameterResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCreatedAt sets the created timestamp
func (s *ParameterResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *ParameterResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *ParameterResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *ParameterResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *ParameterResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *ParameterResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *ParameterResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *ParameterResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithDependency adds a dependency on another resource
func (s *ParameterResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *ParameterResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *ParameterResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// ProjectResource wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource.
type ProjectResource struct {
	ResourceBuilderBase
}

// NewProjectResource creates a new ProjectResource.
func NewProjectResource(handle *Handle, client *AspireClient) *ProjectResource {
	return &ProjectResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithReplicas sets the number of replicas
func (s *ProjectResource) WithReplicas(replicas float64) (*ProjectResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["replicas"] = SerializeValue(replicas)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReplicas", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ProjectResource), nil
}

// WithEnvironment sets an environment variable
func (s *ProjectResource) WithEnvironment(name string, value string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironment", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentExpression adds an environment variable with a reference expression
func (s *ProjectResource) WithEnvironmentExpression(name string, value *ReferenceExpression) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallback sets environment variables via callback
func (s *ProjectResource) WithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallbackAsync sets environment variables via async callback
func (s *ProjectResource) WithEnvironmentCallbackAsync(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithArgs adds arguments
func (s *ProjectResource) WithArgs(args []string) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallback sets command-line arguments via callback
func (s *ProjectResource) WithArgsCallback(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallbackAsync sets command-line arguments via async callback
func (s *ProjectResource) WithArgsCallbackAsync(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithReference adds a reference to another resource
func (s *ProjectResource) WithReference(source *IResourceWithConnectionString, connectionName string, optional bool) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["connectionName"] = SerializeValue(connectionName)
	reqArgs["optional"] = SerializeValue(optional)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithServiceReference adds a service discovery reference to another resource
func (s *ProjectResource) WithServiceReference(source *IResourceWithServiceDiscovery) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withServiceReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEndpoint adds a network endpoint
func (s *ProjectResource) WithEndpoint(port float64, targetPort float64, scheme string, name string, env string, isProxied bool, isExternal bool, protocol ProtocolType) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["scheme"] = SerializeValue(scheme)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	reqArgs["isExternal"] = SerializeValue(isExternal)
	reqArgs["protocol"] = SerializeValue(protocol)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpEndpoint adds an HTTP endpoint
func (s *ProjectResource) WithHttpEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpsEndpoint adds an HTTPS endpoint
func (s *ProjectResource) WithHttpsEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithExternalHttpEndpoints makes HTTP endpoints externally accessible
func (s *ProjectResource) WithExternalHttpEndpoints() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// GetEndpoint gets an endpoint reference
func (s *ProjectResource) GetEndpoint(name string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// AsHttp2Service configures resource for HTTP/2
func (s *ProjectResource) AsHttp2Service() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/asHttp2Service", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *ProjectResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *ProjectResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *ProjectResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *ProjectResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *ProjectResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpointFactory adds a URL for a specific endpoint via factory callback
func (s *ProjectResource) WithUrlForEndpointFactory(endpointName string, callback func(...any) any) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WaitFor waits for another resource to be ready
func (s *ProjectResource) WaitFor(dependency *IResource) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *ProjectResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForCompletion waits for resource completion
func (s *ProjectResource) WaitForCompletion(dependency *IResource, exitCode float64) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	reqArgs["exitCode"] = SerializeValue(exitCode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitForCompletion", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithHealthCheck adds a health check by key
func (s *ProjectResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHttpHealthCheck adds an HTTP health check
func (s *ProjectResource) WithHttpHealthCheck(path string, statusCode float64, endpointName string) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["path"] = SerializeValue(path)
	reqArgs["statusCode"] = SerializeValue(statusCode)
	reqArgs["endpointName"] = SerializeValue(endpointName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithCommand adds a resource command
func (s *ProjectResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *ProjectResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// GetResourceName gets the resource name
func (s *ProjectResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithOptionalString adds an optional string parameter
func (s *ProjectResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *ProjectResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWithEnvironmentCallback configures environment with callback (test version)
func (s *ProjectResource) TestWithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWithEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCreatedAt sets the created timestamp
func (s *ProjectResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *ProjectResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *ProjectResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *ProjectResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *ProjectResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *ProjectResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *ProjectResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *ProjectResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithDependency adds a dependency on another resource
func (s *ProjectResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *ProjectResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEnvironmentVariables sets environment variables
func (s *ProjectResource) WithEnvironmentVariables(variables map[string]string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["variables"] = SerializeValue(variables)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEnvironmentVariables", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *ProjectResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// ResourceUrlsCallbackContext wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext.
type ResourceUrlsCallbackContext struct {
	HandleWrapperBase
	urls *AspireList[*ResourceUrlAnnotation]
}

// NewResourceUrlsCallbackContext creates a new ResourceUrlsCallbackContext.
func NewResourceUrlsCallbackContext(handle *Handle, client *AspireClient) *ResourceUrlsCallbackContext {
	return &ResourceUrlsCallbackContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Urls gets the Urls property
func (s *ResourceUrlsCallbackContext) Urls() *AspireList[*ResourceUrlAnnotation] {
	if s.urls == nil {
		s.urls = NewAspireListWithGetter[*ResourceUrlAnnotation](s.Handle(), s.Client(), "Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.urls")
	}
	return s.urls
}

// CancellationToken gets the CancellationToken property
func (s *ResourceUrlsCallbackContext) CancellationToken() (*CancellationToken, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.cancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*CancellationToken), nil
}

// ExecutionContext gets the ExecutionContext property
func (s *ResourceUrlsCallbackContext) ExecutionContext() (*DistributedApplicationExecutionContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.ApplicationModel/ResourceUrlsCallbackContext.executionContext", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*DistributedApplicationExecutionContext), nil
}

// TestCallbackContext wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext.
type TestCallbackContext struct {
	HandleWrapperBase
}

// NewTestCallbackContext creates a new TestCallbackContext.
func NewTestCallbackContext(handle *Handle, client *AspireClient) *TestCallbackContext {
	return &TestCallbackContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Name gets the Name property
func (s *TestCallbackContext) Name() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetName sets the Name property
func (s *TestCallbackContext) SetName(value string) (*TestCallbackContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestCallbackContext), nil
}

// Value gets the Value property
func (s *TestCallbackContext) Value() (*float64, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*float64), nil
}

// SetValue sets the Value property
func (s *TestCallbackContext) SetValue(value float64) (*TestCallbackContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestCallbackContext), nil
}

// CancellationToken gets the CancellationToken property
func (s *TestCallbackContext) CancellationToken() (*CancellationToken, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*CancellationToken), nil
}

// SetCancellationToken sets the CancellationToken property
func (s *TestCallbackContext) SetCancellationToken(value *CancellationToken) (*TestCallbackContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	if value != nil {
		reqArgs["value"] = RegisterCancellation(value, s.Client())
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestCallbackContext), nil
}

// TestCollectionContext wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext.
type TestCollectionContext struct {
	HandleWrapperBase
	items *AspireList[string]
	metadata *AspireDict[string, string]
}

// NewTestCollectionContext creates a new TestCollectionContext.
func NewTestCollectionContext(handle *Handle, client *AspireClient) *TestCollectionContext {
	return &TestCollectionContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Items gets the Items property
func (s *TestCollectionContext) Items() *AspireList[string] {
	if s.items == nil {
		s.items = NewAspireListWithGetter[string](s.Handle(), s.Client(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items")
	}
	return s.items
}

// Metadata gets the Metadata property
func (s *TestCollectionContext) Metadata() *AspireDict[string, string] {
	if s.metadata == nil {
		s.metadata = NewAspireDictWithGetter[string, string](s.Handle(), s.Client(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata")
	}
	return s.metadata
}

// TestDatabaseResource wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource.
type TestDatabaseResource struct {
	ResourceBuilderBase
}

// NewTestDatabaseResource creates a new TestDatabaseResource.
func NewTestDatabaseResource(handle *Handle, client *AspireClient) *TestDatabaseResource {
	return &TestDatabaseResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithBindMount adds a bind mount
func (s *TestDatabaseResource) WithBindMount(source string, target string, isReadOnly bool) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["target"] = SerializeValue(target)
	reqArgs["isReadOnly"] = SerializeValue(isReadOnly)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withBindMount", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithEntrypoint sets the container entrypoint
func (s *TestDatabaseResource) WithEntrypoint(entrypoint string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["entrypoint"] = SerializeValue(entrypoint)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEntrypoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImageTag sets the container image tag
func (s *TestDatabaseResource) WithImageTag(tag string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["tag"] = SerializeValue(tag)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImageTag", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImageRegistry sets the container image registry
func (s *TestDatabaseResource) WithImageRegistry(registry string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["registry"] = SerializeValue(registry)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImageRegistry", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImage sets the container image
func (s *TestDatabaseResource) WithImage(image string, tag string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["image"] = SerializeValue(image)
	reqArgs["tag"] = SerializeValue(tag)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImage", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithContainerRuntimeArgs adds runtime arguments for the container
func (s *TestDatabaseResource) WithContainerRuntimeArgs(args []string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withContainerRuntimeArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithLifetime sets the lifetime behavior of the container resource
func (s *TestDatabaseResource) WithLifetime(lifetime ContainerLifetime) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["lifetime"] = SerializeValue(lifetime)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withLifetime", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImagePullPolicy sets the container image pull policy
func (s *TestDatabaseResource) WithImagePullPolicy(pullPolicy ImagePullPolicy) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["pullPolicy"] = SerializeValue(pullPolicy)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImagePullPolicy", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithContainerName sets the container name
func (s *TestDatabaseResource) WithContainerName(name string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withContainerName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithEnvironment sets an environment variable
func (s *TestDatabaseResource) WithEnvironment(name string, value string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironment", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentExpression adds an environment variable with a reference expression
func (s *TestDatabaseResource) WithEnvironmentExpression(name string, value *ReferenceExpression) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallback sets environment variables via callback
func (s *TestDatabaseResource) WithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallbackAsync sets environment variables via async callback
func (s *TestDatabaseResource) WithEnvironmentCallbackAsync(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithArgs adds arguments
func (s *TestDatabaseResource) WithArgs(args []string) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallback sets command-line arguments via callback
func (s *TestDatabaseResource) WithArgsCallback(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallbackAsync sets command-line arguments via async callback
func (s *TestDatabaseResource) WithArgsCallbackAsync(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithReference adds a reference to another resource
func (s *TestDatabaseResource) WithReference(source *IResourceWithConnectionString, connectionName string, optional bool) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["connectionName"] = SerializeValue(connectionName)
	reqArgs["optional"] = SerializeValue(optional)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithServiceReference adds a service discovery reference to another resource
func (s *TestDatabaseResource) WithServiceReference(source *IResourceWithServiceDiscovery) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withServiceReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEndpoint adds a network endpoint
func (s *TestDatabaseResource) WithEndpoint(port float64, targetPort float64, scheme string, name string, env string, isProxied bool, isExternal bool, protocol ProtocolType) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["scheme"] = SerializeValue(scheme)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	reqArgs["isExternal"] = SerializeValue(isExternal)
	reqArgs["protocol"] = SerializeValue(protocol)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpEndpoint adds an HTTP endpoint
func (s *TestDatabaseResource) WithHttpEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpsEndpoint adds an HTTPS endpoint
func (s *TestDatabaseResource) WithHttpsEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithExternalHttpEndpoints makes HTTP endpoints externally accessible
func (s *TestDatabaseResource) WithExternalHttpEndpoints() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// GetEndpoint gets an endpoint reference
func (s *TestDatabaseResource) GetEndpoint(name string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// AsHttp2Service configures resource for HTTP/2
func (s *TestDatabaseResource) AsHttp2Service() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/asHttp2Service", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *TestDatabaseResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *TestDatabaseResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *TestDatabaseResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *TestDatabaseResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *TestDatabaseResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpointFactory adds a URL for a specific endpoint via factory callback
func (s *TestDatabaseResource) WithUrlForEndpointFactory(endpointName string, callback func(...any) any) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WaitFor waits for another resource to be ready
func (s *TestDatabaseResource) WaitFor(dependency *IResource) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *TestDatabaseResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForCompletion waits for resource completion
func (s *TestDatabaseResource) WaitForCompletion(dependency *IResource, exitCode float64) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	reqArgs["exitCode"] = SerializeValue(exitCode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitForCompletion", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithHealthCheck adds a health check by key
func (s *TestDatabaseResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHttpHealthCheck adds an HTTP health check
func (s *TestDatabaseResource) WithHttpHealthCheck(path string, statusCode float64, endpointName string) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["path"] = SerializeValue(path)
	reqArgs["statusCode"] = SerializeValue(statusCode)
	reqArgs["endpointName"] = SerializeValue(endpointName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithCommand adds a resource command
func (s *TestDatabaseResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *TestDatabaseResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithVolume adds a volume
func (s *TestDatabaseResource) WithVolume(target string, name string, isReadOnly bool) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	reqArgs["target"] = SerializeValue(target)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["isReadOnly"] = SerializeValue(isReadOnly)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withVolume", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// GetResourceName gets the resource name
func (s *TestDatabaseResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithOptionalString adds an optional string parameter
func (s *TestDatabaseResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *TestDatabaseResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWithEnvironmentCallback configures environment with callback (test version)
func (s *TestDatabaseResource) TestWithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWithEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCreatedAt sets the created timestamp
func (s *TestDatabaseResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *TestDatabaseResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *TestDatabaseResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *TestDatabaseResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *TestDatabaseResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *TestDatabaseResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *TestDatabaseResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *TestDatabaseResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithDependency adds a dependency on another resource
func (s *TestDatabaseResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *TestDatabaseResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEnvironmentVariables sets environment variables
func (s *TestDatabaseResource) WithEnvironmentVariables(variables map[string]string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["variables"] = SerializeValue(variables)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEnvironmentVariables", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *TestDatabaseResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestEnvironmentContext wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext.
type TestEnvironmentContext struct {
	HandleWrapperBase
}

// NewTestEnvironmentContext creates a new TestEnvironmentContext.
func NewTestEnvironmentContext(handle *Handle, client *AspireClient) *TestEnvironmentContext {
	return &TestEnvironmentContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Name gets the Name property
func (s *TestEnvironmentContext) Name() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetName sets the Name property
func (s *TestEnvironmentContext) SetName(value string) (*TestEnvironmentContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestEnvironmentContext), nil
}

// Description gets the Description property
func (s *TestEnvironmentContext) Description() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetDescription sets the Description property
func (s *TestEnvironmentContext) SetDescription(value string) (*TestEnvironmentContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestEnvironmentContext), nil
}

// Priority gets the Priority property
func (s *TestEnvironmentContext) Priority() (*float64, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*float64), nil
}

// SetPriority sets the Priority property
func (s *TestEnvironmentContext) SetPriority(value float64) (*TestEnvironmentContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestEnvironmentContext), nil
}

// TestRedisResource wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource.
type TestRedisResource struct {
	ResourceBuilderBase
	getTags *AspireList[string]
	getMetadata *AspireDict[string, string]
}

// NewTestRedisResource creates a new TestRedisResource.
func NewTestRedisResource(handle *Handle, client *AspireClient) *TestRedisResource {
	return &TestRedisResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithBindMount adds a bind mount
func (s *TestRedisResource) WithBindMount(source string, target string, isReadOnly bool) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["target"] = SerializeValue(target)
	reqArgs["isReadOnly"] = SerializeValue(isReadOnly)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withBindMount", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithEntrypoint sets the container entrypoint
func (s *TestRedisResource) WithEntrypoint(entrypoint string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["entrypoint"] = SerializeValue(entrypoint)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEntrypoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImageTag sets the container image tag
func (s *TestRedisResource) WithImageTag(tag string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["tag"] = SerializeValue(tag)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImageTag", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImageRegistry sets the container image registry
func (s *TestRedisResource) WithImageRegistry(registry string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["registry"] = SerializeValue(registry)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImageRegistry", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImage sets the container image
func (s *TestRedisResource) WithImage(image string, tag string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["image"] = SerializeValue(image)
	reqArgs["tag"] = SerializeValue(tag)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImage", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithContainerRuntimeArgs adds runtime arguments for the container
func (s *TestRedisResource) WithContainerRuntimeArgs(args []string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withContainerRuntimeArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithLifetime sets the lifetime behavior of the container resource
func (s *TestRedisResource) WithLifetime(lifetime ContainerLifetime) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["lifetime"] = SerializeValue(lifetime)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withLifetime", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImagePullPolicy sets the container image pull policy
func (s *TestRedisResource) WithImagePullPolicy(pullPolicy ImagePullPolicy) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["pullPolicy"] = SerializeValue(pullPolicy)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImagePullPolicy", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithContainerName sets the container name
func (s *TestRedisResource) WithContainerName(name string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withContainerName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithEnvironment sets an environment variable
func (s *TestRedisResource) WithEnvironment(name string, value string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironment", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentExpression adds an environment variable with a reference expression
func (s *TestRedisResource) WithEnvironmentExpression(name string, value *ReferenceExpression) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallback sets environment variables via callback
func (s *TestRedisResource) WithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallbackAsync sets environment variables via async callback
func (s *TestRedisResource) WithEnvironmentCallbackAsync(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithArgs adds arguments
func (s *TestRedisResource) WithArgs(args []string) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallback sets command-line arguments via callback
func (s *TestRedisResource) WithArgsCallback(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallbackAsync sets command-line arguments via async callback
func (s *TestRedisResource) WithArgsCallbackAsync(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithReference adds a reference to another resource
func (s *TestRedisResource) WithReference(source *IResourceWithConnectionString, connectionName string, optional bool) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["connectionName"] = SerializeValue(connectionName)
	reqArgs["optional"] = SerializeValue(optional)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithServiceReference adds a service discovery reference to another resource
func (s *TestRedisResource) WithServiceReference(source *IResourceWithServiceDiscovery) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withServiceReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEndpoint adds a network endpoint
func (s *TestRedisResource) WithEndpoint(port float64, targetPort float64, scheme string, name string, env string, isProxied bool, isExternal bool, protocol ProtocolType) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["scheme"] = SerializeValue(scheme)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	reqArgs["isExternal"] = SerializeValue(isExternal)
	reqArgs["protocol"] = SerializeValue(protocol)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpEndpoint adds an HTTP endpoint
func (s *TestRedisResource) WithHttpEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpsEndpoint adds an HTTPS endpoint
func (s *TestRedisResource) WithHttpsEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithExternalHttpEndpoints makes HTTP endpoints externally accessible
func (s *TestRedisResource) WithExternalHttpEndpoints() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// GetEndpoint gets an endpoint reference
func (s *TestRedisResource) GetEndpoint(name string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// AsHttp2Service configures resource for HTTP/2
func (s *TestRedisResource) AsHttp2Service() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/asHttp2Service", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *TestRedisResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *TestRedisResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *TestRedisResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *TestRedisResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *TestRedisResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpointFactory adds a URL for a specific endpoint via factory callback
func (s *TestRedisResource) WithUrlForEndpointFactory(endpointName string, callback func(...any) any) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WaitFor waits for another resource to be ready
func (s *TestRedisResource) WaitFor(dependency *IResource) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *TestRedisResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForCompletion waits for resource completion
func (s *TestRedisResource) WaitForCompletion(dependency *IResource, exitCode float64) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	reqArgs["exitCode"] = SerializeValue(exitCode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitForCompletion", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithHealthCheck adds a health check by key
func (s *TestRedisResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHttpHealthCheck adds an HTTP health check
func (s *TestRedisResource) WithHttpHealthCheck(path string, statusCode float64, endpointName string) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["path"] = SerializeValue(path)
	reqArgs["statusCode"] = SerializeValue(statusCode)
	reqArgs["endpointName"] = SerializeValue(endpointName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithCommand adds a resource command
func (s *TestRedisResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *TestRedisResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithVolume adds a volume
func (s *TestRedisResource) WithVolume(target string, name string, isReadOnly bool) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	reqArgs["target"] = SerializeValue(target)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["isReadOnly"] = SerializeValue(isReadOnly)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withVolume", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// GetResourceName gets the resource name
func (s *TestRedisResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// AddTestChildDatabase adds a child database to a test Redis resource
func (s *TestRedisResource) AddTestChildDatabase(name string, databaseName string) (*TestDatabaseResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["databaseName"] = SerializeValue(databaseName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/addTestChildDatabase", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestDatabaseResource), nil
}

// WithPersistence configures the Redis resource with persistence
func (s *TestRedisResource) WithPersistence(mode TestPersistenceMode) (*TestRedisResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["mode"] = SerializeValue(mode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withPersistence", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestRedisResource), nil
}

// WithOptionalString adds an optional string parameter
func (s *TestRedisResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *TestRedisResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// GetTags gets the tags for the resource
func (s *TestRedisResource) GetTags() *AspireList[string] {
	if s.getTags == nil {
		s.getTags = NewAspireListWithGetter[string](s.Handle(), s.Client(), "Aspire.Hosting.CodeGeneration.Go.Tests/getTags")
	}
	return s.getTags
}

// GetMetadata gets the metadata for the resource
func (s *TestRedisResource) GetMetadata() *AspireDict[string, string] {
	if s.getMetadata == nil {
		s.getMetadata = NewAspireDictWithGetter[string, string](s.Handle(), s.Client(), "Aspire.Hosting.CodeGeneration.Go.Tests/getMetadata")
	}
	return s.getMetadata
}

// WithConnectionString sets the connection string using a reference expression
func (s *TestRedisResource) WithConnectionString(connectionString *ReferenceExpression) (*IResourceWithConnectionString, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["connectionString"] = SerializeValue(connectionString)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConnectionString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithConnectionString), nil
}

// TestWithEnvironmentCallback configures environment with callback (test version)
func (s *TestRedisResource) TestWithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWithEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCreatedAt sets the created timestamp
func (s *TestRedisResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *TestRedisResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *TestRedisResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *TestRedisResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *TestRedisResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *TestRedisResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *TestRedisResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *TestRedisResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// GetEndpoints gets the endpoints
func (s *TestRedisResource) GetEndpoints() (*[]string, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/getEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*[]string), nil
}

// WithConnectionStringDirect sets connection string using direct interface target
func (s *TestRedisResource) WithConnectionStringDirect(connectionString string) (*IResourceWithConnectionString, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["connectionString"] = SerializeValue(connectionString)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConnectionStringDirect", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithConnectionString), nil
}

// WithRedisSpecific redis-specific configuration
func (s *TestRedisResource) WithRedisSpecific(option string) (*TestRedisResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["option"] = SerializeValue(option)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withRedisSpecific", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestRedisResource), nil
}

// WithDependency adds a dependency on another resource
func (s *TestRedisResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *TestRedisResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEnvironmentVariables sets environment variables
func (s *TestRedisResource) WithEnvironmentVariables(variables map[string]string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["variables"] = SerializeValue(variables)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEnvironmentVariables", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// GetStatusAsync gets the status of the resource asynchronously
func (s *TestRedisResource) GetStatusAsync(cancellationToken *CancellationToken) (*string, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if cancellationToken != nil {
		reqArgs["cancellationToken"] = RegisterCancellation(cancellationToken, s.Client())
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/getStatusAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *TestRedisResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForReadyAsync waits for the resource to be ready
func (s *TestRedisResource) WaitForReadyAsync(timeout float64, cancellationToken *CancellationToken) (*bool, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["timeout"] = SerializeValue(timeout)
	if cancellationToken != nil {
		reqArgs["cancellationToken"] = RegisterCancellation(cancellationToken, s.Client())
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/waitForReadyAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// TestResourceContext wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext.
type TestResourceContext struct {
	HandleWrapperBase
}

// NewTestResourceContext creates a new TestResourceContext.
func NewTestResourceContext(handle *Handle, client *AspireClient) *TestResourceContext {
	return &TestResourceContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// Name gets the Name property
func (s *TestResourceContext) Name() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetName sets the Name property
func (s *TestResourceContext) SetName(value string) (*TestResourceContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestResourceContext), nil
}

// Value gets the Value property
func (s *TestResourceContext) Value() (*float64, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*float64), nil
}

// SetValue sets the Value property
func (s *TestResourceContext) SetValue(value float64) (*TestResourceContext, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*TestResourceContext), nil
}

// GetValueAsync invokes the GetValueAsync method
func (s *TestResourceContext) GetValueAsync() (*string, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// SetValueAsync invokes the SetValueAsync method
func (s *TestResourceContext) SetValueAsync(value string) error {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	_, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync", reqArgs)
	return err
}

// ValidateAsync invokes the ValidateAsync method
func (s *TestResourceContext) ValidateAsync() (*bool, error) {
	reqArgs := map[string]any{
		"context": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*bool), nil
}

// TestVaultResource wraps a handle for Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource.
type TestVaultResource struct {
	ResourceBuilderBase
}

// NewTestVaultResource creates a new TestVaultResource.
func NewTestVaultResource(handle *Handle, client *AspireClient) *TestVaultResource {
	return &TestVaultResource{
		ResourceBuilderBase: NewResourceBuilderBase(handle, client),
	}
}

// WithBindMount adds a bind mount
func (s *TestVaultResource) WithBindMount(source string, target string, isReadOnly bool) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["target"] = SerializeValue(target)
	reqArgs["isReadOnly"] = SerializeValue(isReadOnly)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withBindMount", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithEntrypoint sets the container entrypoint
func (s *TestVaultResource) WithEntrypoint(entrypoint string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["entrypoint"] = SerializeValue(entrypoint)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEntrypoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImageTag sets the container image tag
func (s *TestVaultResource) WithImageTag(tag string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["tag"] = SerializeValue(tag)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImageTag", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImageRegistry sets the container image registry
func (s *TestVaultResource) WithImageRegistry(registry string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["registry"] = SerializeValue(registry)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImageRegistry", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImage sets the container image
func (s *TestVaultResource) WithImage(image string, tag string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["image"] = SerializeValue(image)
	reqArgs["tag"] = SerializeValue(tag)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImage", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithContainerRuntimeArgs adds runtime arguments for the container
func (s *TestVaultResource) WithContainerRuntimeArgs(args []string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withContainerRuntimeArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithLifetime sets the lifetime behavior of the container resource
func (s *TestVaultResource) WithLifetime(lifetime ContainerLifetime) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["lifetime"] = SerializeValue(lifetime)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withLifetime", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithImagePullPolicy sets the container image pull policy
func (s *TestVaultResource) WithImagePullPolicy(pullPolicy ImagePullPolicy) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["pullPolicy"] = SerializeValue(pullPolicy)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withImagePullPolicy", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithContainerName sets the container name
func (s *TestVaultResource) WithContainerName(name string) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withContainerName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// WithEnvironment sets an environment variable
func (s *TestVaultResource) WithEnvironment(name string, value string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironment", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentExpression adds an environment variable with a reference expression
func (s *TestVaultResource) WithEnvironmentExpression(name string, value *ReferenceExpression) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["value"] = SerializeValue(value)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallback sets environment variables via callback
func (s *TestVaultResource) WithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEnvironmentCallbackAsync sets environment variables via async callback
func (s *TestVaultResource) WithEnvironmentCallbackAsync(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEnvironmentCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithArgs adds arguments
func (s *TestVaultResource) WithArgs(args []string) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["args"] = SerializeValue(args)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgs", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallback sets command-line arguments via callback
func (s *TestVaultResource) WithArgsCallback(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithArgsCallbackAsync sets command-line arguments via async callback
func (s *TestVaultResource) WithArgsCallbackAsync(callback func(...any) any) (*IResourceWithArgs, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withArgsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithArgs), nil
}

// WithReference adds a reference to another resource
func (s *TestVaultResource) WithReference(source *IResourceWithConnectionString, connectionName string, optional bool) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	reqArgs["connectionName"] = SerializeValue(connectionName)
	reqArgs["optional"] = SerializeValue(optional)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithServiceReference adds a service discovery reference to another resource
func (s *TestVaultResource) WithServiceReference(source *IResourceWithServiceDiscovery) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["source"] = SerializeValue(source)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withServiceReference", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithEndpoint adds a network endpoint
func (s *TestVaultResource) WithEndpoint(port float64, targetPort float64, scheme string, name string, env string, isProxied bool, isExternal bool, protocol ProtocolType) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["scheme"] = SerializeValue(scheme)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	reqArgs["isExternal"] = SerializeValue(isExternal)
	reqArgs["protocol"] = SerializeValue(protocol)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpEndpoint adds an HTTP endpoint
func (s *TestVaultResource) WithHttpEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithHttpsEndpoint adds an HTTPS endpoint
func (s *TestVaultResource) WithHttpsEndpoint(port float64, targetPort float64, name string, env string, isProxied bool) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["port"] = SerializeValue(port)
	reqArgs["targetPort"] = SerializeValue(targetPort)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["env"] = SerializeValue(env)
	reqArgs["isProxied"] = SerializeValue(isProxied)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpsEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithExternalHttpEndpoints makes HTTP endpoints externally accessible
func (s *TestVaultResource) WithExternalHttpEndpoints() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExternalHttpEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// GetEndpoint gets an endpoint reference
func (s *TestVaultResource) GetEndpoint(name string) (*EndpointReference, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*EndpointReference), nil
}

// AsHttp2Service configures resource for HTTP/2
func (s *TestVaultResource) AsHttp2Service() (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/asHttp2Service", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithUrlsCallback customizes displayed URLs via callback
func (s *TestVaultResource) WithUrlsCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlsCallbackAsync customizes displayed URLs via async callback
func (s *TestVaultResource) WithUrlsCallbackAsync(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlsCallbackAsync", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrl adds or modifies displayed URLs
func (s *TestVaultResource) WithUrl(url string, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrl", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlExpression adds a URL using a reference expression
func (s *TestVaultResource) WithUrlExpression(url *ReferenceExpression, displayText string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["url"] = SerializeValue(url)
	reqArgs["displayText"] = SerializeValue(displayText)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlExpression", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpoint customizes the URL for a specific endpoint via callback
func (s *TestVaultResource) WithUrlForEndpoint(endpointName string, callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpoint", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithUrlForEndpointFactory adds a URL for a specific endpoint via factory callback
func (s *TestVaultResource) WithUrlForEndpointFactory(endpointName string, callback func(...any) any) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpointName"] = SerializeValue(endpointName)
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withUrlForEndpointFactory", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WaitFor waits for another resource to be ready
func (s *TestVaultResource) WaitFor(dependency *IResource) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithExplicitStart prevents resource from starting automatically
func (s *TestVaultResource) WithExplicitStart() (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withExplicitStart", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WaitForCompletion waits for resource completion
func (s *TestVaultResource) WaitForCompletion(dependency *IResource, exitCode float64) (*IResourceWithWaitSupport, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	reqArgs["exitCode"] = SerializeValue(exitCode)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/waitForCompletion", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithWaitSupport), nil
}

// WithHealthCheck adds a health check by key
func (s *TestVaultResource) WithHealthCheck(key string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["key"] = SerializeValue(key)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithHttpHealthCheck adds an HTTP health check
func (s *TestVaultResource) WithHttpHealthCheck(path string, statusCode float64, endpointName string) (*IResourceWithEndpoints, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["path"] = SerializeValue(path)
	reqArgs["statusCode"] = SerializeValue(statusCode)
	reqArgs["endpointName"] = SerializeValue(endpointName)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withHttpHealthCheck", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEndpoints), nil
}

// WithCommand adds a resource command
func (s *TestVaultResource) WithCommand(name string, displayName string, executeCommand func(...any) any, commandOptions *CommandOptions) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["name"] = SerializeValue(name)
	reqArgs["displayName"] = SerializeValue(displayName)
	if executeCommand != nil {
		reqArgs["executeCommand"] = RegisterCallback(executeCommand)
	}
	if commandOptions != nil {
		reqArgs["commandOptions"] = SerializeValue(commandOptions)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withCommand", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithParentRelationship sets the parent relationship
func (s *TestVaultResource) WithParentRelationship(parent *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["parent"] = SerializeValue(parent)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withParentRelationship", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithVolume adds a volume
func (s *TestVaultResource) WithVolume(target string, name string, isReadOnly bool) (*ContainerResource, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	reqArgs["target"] = SerializeValue(target)
	reqArgs["name"] = SerializeValue(name)
	reqArgs["isReadOnly"] = SerializeValue(isReadOnly)
	result, err := s.Client().InvokeCapability("Aspire.Hosting/withVolume", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ContainerResource), nil
}

// GetResourceName gets the resource name
func (s *TestVaultResource) GetResourceName() (*string, error) {
	reqArgs := map[string]any{
		"resource": SerializeValue(s.Handle()),
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting/getResourceName", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*string), nil
}

// WithOptionalString adds an optional string parameter
func (s *TestVaultResource) WithOptionalString(value string, enabled bool) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["value"] = SerializeValue(value)
	reqArgs["enabled"] = SerializeValue(enabled)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalString", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithConfig configures the resource with a DTO
func (s *TestVaultResource) WithConfig(config *TestConfigDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWithEnvironmentCallback configures environment with callback (test version)
func (s *TestVaultResource) TestWithEnvironmentCallback(callback func(...any) any) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWithEnvironmentCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCreatedAt sets the created timestamp
func (s *TestVaultResource) WithCreatedAt(createdAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["createdAt"] = SerializeValue(createdAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCreatedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithModifiedAt sets the modified timestamp
func (s *TestVaultResource) WithModifiedAt(modifiedAt string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["modifiedAt"] = SerializeValue(modifiedAt)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withModifiedAt", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithCorrelationId sets the correlation ID
func (s *TestVaultResource) WithCorrelationId(correlationId string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["correlationId"] = SerializeValue(correlationId)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCorrelationId", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithOptionalCallback configures with optional callback
func (s *TestVaultResource) WithOptionalCallback(callback func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if callback != nil {
		reqArgs["callback"] = RegisterCallback(callback)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withOptionalCallback", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithStatus sets the resource status
func (s *TestVaultResource) WithStatus(status TestResourceStatus) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["status"] = SerializeValue(status)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withStatus", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithNestedConfig configures with nested DTO
func (s *TestVaultResource) WithNestedConfig(config *TestNestedDto) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["config"] = SerializeValue(config)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withNestedConfig", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithValidator adds validation callback
func (s *TestVaultResource) WithValidator(validator func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if validator != nil {
		reqArgs["validator"] = RegisterCallback(validator)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withValidator", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// TestWaitFor waits for another resource (test version)
func (s *TestVaultResource) TestWaitFor(dependency *IResource) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/testWaitFor", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithDependency adds a dependency on another resource
func (s *TestVaultResource) WithDependency(dependency *IResourceWithConnectionString) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["dependency"] = SerializeValue(dependency)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withDependency", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEndpoints sets the endpoints
func (s *TestVaultResource) WithEndpoints(endpoints []string) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["endpoints"] = SerializeValue(endpoints)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEndpoints", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithEnvironmentVariables sets environment variables
func (s *TestVaultResource) WithEnvironmentVariables(variables map[string]string) (*IResourceWithEnvironment, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["variables"] = SerializeValue(variables)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withEnvironmentVariables", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResourceWithEnvironment), nil
}

// WithCancellableOperation performs a cancellable operation
func (s *TestVaultResource) WithCancellableOperation(operation func(...any) any) (*IResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	if operation != nil {
		reqArgs["operation"] = RegisterCallback(operation)
	}
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withCancellableOperation", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*IResource), nil
}

// WithVaultDirect configures vault using direct interface target
func (s *TestVaultResource) WithVaultDirect(option string) (*ITestVaultResource, error) {
	reqArgs := map[string]any{
		"builder": SerializeValue(s.Handle()),
	}
	reqArgs["option"] = SerializeValue(option)
	result, err := s.Client().InvokeCapability("Aspire.Hosting.CodeGeneration.Go.Tests/withVaultDirect", reqArgs)
	if err != nil {
		return nil, err
	}
	return result.(*ITestVaultResource), nil
}

// UpdateCommandStateContext wraps a handle for Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext.
type UpdateCommandStateContext struct {
	HandleWrapperBase
}

// NewUpdateCommandStateContext creates a new UpdateCommandStateContext.
func NewUpdateCommandStateContext(handle *Handle, client *AspireClient) *UpdateCommandStateContext {
	return &UpdateCommandStateContext{
		HandleWrapperBase: NewHandleWrapperBase(handle, client),
	}
}

// ============================================================================
// Handle wrapper registrations
// ============================================================================

func init() {
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplication", func(h *Handle, c *AspireClient) any {
		return NewDistributedApplication(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContext", func(h *Handle, c *AspireClient) any {
		return NewDistributedApplicationExecutionContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.DistributedApplicationExecutionContextOptions", func(h *Handle, c *AspireClient) any {
		return NewDistributedApplicationExecutionContextOptions(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", func(h *Handle, c *AspireClient) any {
		return NewIDistributedApplicationBuilder(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationEventSubscription", func(h *Handle, c *AspireClient) any {
		return NewDistributedApplicationEventSubscription(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.DistributedApplicationResourceEventSubscription", func(h *Handle, c *AspireClient) any {
		return NewDistributedApplicationResourceEventSubscription(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEvent", func(h *Handle, c *AspireClient) any {
		return NewIDistributedApplicationEvent(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationResourceEvent", func(h *Handle, c *AspireClient) any {
		return NewIDistributedApplicationResourceEvent(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.Eventing.IDistributedApplicationEventing", func(h *Handle, c *AspireClient) any {
		return NewIDistributedApplicationEventing(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.CommandLineArgsCallbackContext", func(h *Handle, c *AspireClient) any {
		return NewCommandLineArgsCallbackContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReference", func(h *Handle, c *AspireClient) any {
		return NewEndpointReference(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EndpointReferenceExpression", func(h *Handle, c *AspireClient) any {
		return NewEndpointReferenceExpression(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.EnvironmentCallbackContext", func(h *Handle, c *AspireClient) any {
		return NewEnvironmentCallbackContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.UpdateCommandStateContext", func(h *Handle, c *AspireClient) any {
		return NewUpdateCommandStateContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecuteCommandContext", func(h *Handle, c *AspireClient) any {
		return NewExecuteCommandContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlsCallbackContext", func(h *Handle, c *AspireClient) any {
		return NewResourceUrlsCallbackContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ContainerResource", func(h *Handle, c *AspireClient) any {
		return NewContainerResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ExecutableResource", func(h *Handle, c *AspireClient) any {
		return NewExecutableResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ParameterResource", func(h *Handle, c *AspireClient) any {
		return NewParameterResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString", func(h *Handle, c *AspireClient) any {
		return NewIResourceWithConnectionString(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.ProjectResource", func(h *Handle, c *AspireClient) any {
		return NewProjectResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.IResourceWithServiceDiscovery", func(h *Handle, c *AspireClient) any {
		return NewIResourceWithServiceDiscovery(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource", func(h *Handle, c *AspireClient) any {
		return NewIResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", func(h *Handle, c *AspireClient) any {
		return NewTestCallbackContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", func(h *Handle, c *AspireClient) any {
		return NewTestResourceContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", func(h *Handle, c *AspireClient) any {
		return NewTestEnvironmentContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", func(h *Handle, c *AspireClient) any {
		return NewTestCollectionContext(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", func(h *Handle, c *AspireClient) any {
		return NewTestRedisResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource", func(h *Handle, c *AspireClient) any {
		return NewTestDatabaseResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource", func(h *Handle, c *AspireClient) any {
		return NewTestVaultResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting.CodeGeneration.Go.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource", func(h *Handle, c *AspireClient) any {
		return NewITestVaultResource(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment", func(h *Handle, c *AspireClient) any {
		return NewIResourceWithEnvironment(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithArgs", func(h *Handle, c *AspireClient) any {
		return NewIResourceWithArgs(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEndpoints", func(h *Handle, c *AspireClient) any {
		return NewIResourceWithEndpoints(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithWaitSupport", func(h *Handle, c *AspireClient) any {
		return NewIResourceWithWaitSupport(h, c)
	})
	RegisterHandleWrapper("Aspire.Hosting/Dict<string,any>", func(h *Handle, c *AspireClient) any {
		return &AspireDict[any, any]{HandleWrapperBase: NewHandleWrapperBase(h, c)}
	})
	RegisterHandleWrapper("Aspire.Hosting/List<any>", func(h *Handle, c *AspireClient) any {
		return &AspireList[any]{HandleWrapperBase: NewHandleWrapperBase(h, c)}
	})
	RegisterHandleWrapper("Aspire.Hosting/Dict<string,string|Aspire.Hosting/Aspire.Hosting.ApplicationModel.ReferenceExpression>", func(h *Handle, c *AspireClient) any {
		return &AspireDict[any, any]{HandleWrapperBase: NewHandleWrapperBase(h, c)}
	})
	RegisterHandleWrapper("Aspire.Hosting/List<Aspire.Hosting/Aspire.Hosting.ApplicationModel.ResourceUrlAnnotation>", func(h *Handle, c *AspireClient) any {
		return &AspireList[any]{HandleWrapperBase: NewHandleWrapperBase(h, c)}
	})
	RegisterHandleWrapper("Aspire.Hosting/List<string>", func(h *Handle, c *AspireClient) any {
		return &AspireList[any]{HandleWrapperBase: NewHandleWrapperBase(h, c)}
	})
	RegisterHandleWrapper("Aspire.Hosting/Dict<string,string>", func(h *Handle, c *AspireClient) any {
		return &AspireDict[any, any]{HandleWrapperBase: NewHandleWrapperBase(h, c)}
	})
}

// ============================================================================
// Connection Helpers
// ============================================================================

// Connect establishes a connection to the AppHost server.
func Connect() (*AspireClient, error) {
	socketPath := os.Getenv("REMOTE_APP_HOST_SOCKET_PATH")
	if socketPath == "" {
		return nil, fmt.Errorf("REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Run this application using `aspire run`")
	}
	client := NewAspireClient(socketPath)
	if err := client.Connect(); err != nil {
		return nil, err
	}
	client.OnDisconnect(func() { os.Exit(1) })
	return client, nil
}

// CreateBuilder creates a new distributed application builder.
func CreateBuilder(options *CreateBuilderOptions) (*IDistributedApplicationBuilder, error) {
	client, err := Connect()
	if err != nil {
		return nil, err
	}
	resolvedOptions := make(map[string]any)
	if options != nil {
		for k, v := range options.ToMap() {
			resolvedOptions[k] = v
		}
	}
	if _, ok := resolvedOptions["Args"]; !ok {
		resolvedOptions["Args"] = os.Args[1:]
	}
	if _, ok := resolvedOptions["ProjectDirectory"]; !ok {
		if pwd, err := os.Getwd(); err == nil {
			resolvedOptions["ProjectDirectory"] = pwd
		}
	}
	result, err := client.InvokeCapability("Aspire.Hosting/createBuilderWithOptions", map[string]any{"options": resolvedOptions})
	if err != nil {
		return nil, err
	}
	return result.(*IDistributedApplicationBuilder), nil
}

