// Aspire.java - Capability-based Aspire SDK
// GENERATED CODE - DO NOT EDIT

package aspire;

import java.util.*;
import java.util.function.*;

// ============================================================================
// Enums
// ============================================================================

/** TestPersistenceMode enum. */
enum TestPersistenceMode implements WireValueEnum {
    NONE("None"),
    VOLUME("Volume"),
    BIND("Bind");

    private final String value;

    TestPersistenceMode(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static TestPersistenceMode fromValue(String value) {
        for (TestPersistenceMode e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

/** TestResourceStatus enum. */
enum TestResourceStatus implements WireValueEnum {
    PENDING("Pending"),
    RUNNING("Running"),
    STOPPED("Stopped"),
    FAILED("Failed");

    private final String value;

    TestResourceStatus(String value) {
        this.value = value;
    }

    public String getValue() { return value; }

    public static TestResourceStatus fromValue(String value) {
        for (TestResourceStatus e : values()) {
            if (e.value.equals(value)) return e;
        }
        throw new IllegalArgumentException("Unknown value: " + value);
    }
}

// ============================================================================
// DTOs
// ============================================================================

/** TestConfigDto DTO. */
class TestConfigDto {
    private String name;
    private double port;
    private boolean enabled;
    private String optionalField;

    public String getName() { return name; }
    public void setName(String value) { this.name = value; }
    public double getPort() { return port; }
    public void setPort(double value) { this.port = value; }
    public boolean getEnabled() { return enabled; }
    public void setEnabled(boolean value) { this.enabled = value; }
    public String getOptionalField() { return optionalField; }
    public void setOptionalField(String value) { this.optionalField = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Name", AspireClient.serializeValue(name));
        map.put("Port", AspireClient.serializeValue(port));
        map.put("Enabled", AspireClient.serializeValue(enabled));
        map.put("OptionalField", AspireClient.serializeValue(optionalField));
        return map;
    }
}

/** TestNestedDto DTO. */
class TestNestedDto {
    private String id;
    private TestConfigDto config;
    private AspireList<String> tags;
    private AspireDict<String, Double> counts;

    public String getId() { return id; }
    public void setId(String value) { this.id = value; }
    public TestConfigDto getConfig() { return config; }
    public void setConfig(TestConfigDto value) { this.config = value; }
    public AspireList<String> getTags() { return tags; }
    public void setTags(AspireList<String> value) { this.tags = value; }
    public AspireDict<String, Double> getCounts() { return counts; }
    public void setCounts(AspireDict<String, Double> value) { this.counts = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("Id", AspireClient.serializeValue(id));
        map.put("Config", AspireClient.serializeValue(config));
        map.put("Tags", AspireClient.serializeValue(tags));
        map.put("Counts", AspireClient.serializeValue(counts));
        return map;
    }
}

/** TestDeeplyNestedDto DTO. */
class TestDeeplyNestedDto {
    private AspireDict<String, AspireList<TestConfigDto>> nestedData;
    private AspireDict<String, String>[] metadataArray;

    public AspireDict<String, AspireList<TestConfigDto>> getNestedData() { return nestedData; }
    public void setNestedData(AspireDict<String, AspireList<TestConfigDto>> value) { this.nestedData = value; }
    public AspireDict<String, String>[] getMetadataArray() { return metadataArray; }
    public void setMetadataArray(AspireDict<String, String>[] value) { this.metadataArray = value; }

    public Map<String, Object> toMap() {
        Map<String, Object> map = new HashMap<>();
        map.put("NestedData", AspireClient.serializeValue(nestedData));
        map.put("MetadataArray", AspireClient.serializeValue(metadataArray));
        return map;
    }
}

// ============================================================================
// Options Types
// ============================================================================

/** Options for WithDataVolume. */
final class WithDataVolumeOptions {
    private String name;
    private Boolean isReadOnly;

    public String getName() { return name; }
    public WithDataVolumeOptions name(String value) {
        this.name = value;
        return this;
    }

    public Boolean isReadOnly() { return isReadOnly; }
    public WithDataVolumeOptions isReadOnly(Boolean value) {
        this.isReadOnly = value;
        return this;
    }

}

/** Options for WithOptionalString. */
final class WithOptionalStringOptions {
    private String value;
    private Boolean enabled;

    public String getValue() { return value; }
    public WithOptionalStringOptions value(String value) {
        this.value = value;
        return this;
    }

    public Boolean getEnabled() { return enabled; }
    public WithOptionalStringOptions enabled(Boolean value) {
        this.enabled = value;
        return this;
    }

}

// ============================================================================
// Handle Wrappers
// ============================================================================

/** Wrapper for Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder. */
class IDistributedApplicationBuilder extends HandleWrapperBase {
    IDistributedApplicationBuilder(Handle handle, AspireClient client) {
        super(handle, client);
    }

    public TestRedisResource addTestRedis(String name) {
        return addTestRedis(name, null);
    }

    /** Adds a test Redis resource */
    public TestRedisResource addTestRedis(String name, Double port) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (port != null) {
            reqArgs.put("port", AspireClient.serializeValue(port));
        }
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/addTestRedis", reqArgs);
    }

    /** Adds a test vault resource */
    public TestVaultResource addTestVault(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        return (TestVaultResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/addTestVault", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource. */
class IResource extends ResourceBuilderBase {
    IResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString. */
class IResourceWithConnectionString extends ResourceBuilderBase {
    IResourceWithConnectionString(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment. */
class IResourceWithEnvironment extends HandleWrapperBase {
    IResourceWithEnvironment(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource. */
class ITestVaultResource extends ResourceBuilderBase {
    ITestVaultResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext. */
class TestCallbackContext extends HandleWrapperBase {
    TestCallbackContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.name", reqArgs);
    }

    /** Sets the Name property */
    public TestCallbackContext setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestCallbackContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setName", reqArgs);
    }

    /** Gets the Value property */
    public double value() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.value", reqArgs);
    }

    /** Sets the Value property */
    public TestCallbackContext setValue(double value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestCallbackContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setValue", reqArgs);
    }

    /** Gets the CancellationToken property */
    public CancellationToken cancellationToken() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (CancellationToken) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.cancellationToken", reqArgs);
    }

    /** Sets the CancellationToken property */
    public TestCallbackContext setCancellationToken(CancellationToken value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", getClient().registerCancellation(value));
        }
        return (TestCallbackContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCallbackContext.setCancellationToken", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext. */
class TestCollectionContext extends HandleWrapperBase {
    TestCollectionContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Items property */
    private AspireList<String> itemsField;
    public AspireList<String> items() {
        if (itemsField == null) {
            itemsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.items");
        }
        return itemsField;
    }

    /** Gets the Metadata property */
    private AspireDict<String, String> metadataField;
    public AspireDict<String, String> metadata() {
        if (metadataField == null) {
            metadataField = new AspireDict<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestCollectionContext.metadata");
        }
        return metadataField;
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource. */
class TestDatabaseResource extends ResourceBuilderBase {
    TestDatabaseResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(WithOptionalStringOptions options) {
        var value = options == null ? null : options.getValue();
        var enabled = options == null ? null : options.getEnabled();
        return withOptionalStringImpl(value, enabled);
    }

    public IResource withOptionalString() {
        return withOptionalString(null);
    }

    /** Adds an optional string parameter */
    private IResource withOptionalStringImpl(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(AspireAction1<TestEnvironmentContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (TestEnvironmentContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    public IResource withOptionalCallback() {
        return withOptionalCallback(null);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(AspireAction1<TestCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = callback == null ? null : getClient().registerCallback(args -> {
            var arg = (TestCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(AspireFunc1<TestResourceContext, Boolean> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var validatorId = getClient().registerCallback(args -> {
            var arg = (TestResourceContext) args[0];
            return AspireClient.awaitValue(validator.invoke(arg));
        });
        if (validatorId != null) {
            reqArgs.put("validator", validatorId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    public IResource testWaitFor(ResourceBuilderBase dependency) {
        return testWaitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    public IResource withDependency(ResourceBuilderBase dependency) {
        return withDependency(new IResourceWithConnectionString(dependency.getHandle(), dependency.getClient()));
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(AspireAction1<CancellationToken> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var operationId = getClient().registerCallback(args -> {
            var arg = CancellationToken.fromValue(args[0]);
            operation.invoke(arg);
            return null;
        });
        if (operationId != null) {
            reqArgs.put("operation", operationId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

    /** Adds a data volume */
    public TestDatabaseResource withDataVolume(WithDataVolumeOptions options) {
        var name = options == null ? null : options.getName();
        return withDataVolumeImpl(name);
    }

    public TestDatabaseResource withDataVolume() {
        return withDataVolume(null);
    }

    /** Adds a data volume */
    private TestDatabaseResource withDataVolumeImpl(String name) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        return (TestDatabaseResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDataVolume", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext. */
class TestEnvironmentContext extends HandleWrapperBase {
    TestEnvironmentContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.name", reqArgs);
    }

    /** Sets the Name property */
    public TestEnvironmentContext setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestEnvironmentContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setName", reqArgs);
    }

    /** Gets the Description property */
    public String description() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.description", reqArgs);
    }

    /** Sets the Description property */
    public TestEnvironmentContext setDescription(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestEnvironmentContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setDescription", reqArgs);
    }

    /** Gets the Priority property */
    public double priority() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.priority", reqArgs);
    }

    /** Sets the Priority property */
    public TestEnvironmentContext setPriority(double value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestEnvironmentContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestEnvironmentContext.setPriority", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource. */
class TestRedisResource extends ResourceBuilderBase {
    TestRedisResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    public TestDatabaseResource addTestChildDatabase(String name) {
        return addTestChildDatabase(name, null);
    }

    /** Adds a child database to a test Redis resource */
    public TestDatabaseResource addTestChildDatabase(String name, String databaseName) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("name", AspireClient.serializeValue(name));
        if (databaseName != null) {
            reqArgs.put("databaseName", AspireClient.serializeValue(databaseName));
        }
        return (TestDatabaseResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/addTestChildDatabase", reqArgs);
    }

    public TestRedisResource withPersistence() {
        return withPersistence(null);
    }

    /** Configures the Redis resource with persistence */
    public TestRedisResource withPersistence(TestPersistenceMode mode) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (mode != null) {
            reqArgs.put("mode", AspireClient.serializeValue(mode));
        }
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withPersistence", reqArgs);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(WithOptionalStringOptions options) {
        var value = options == null ? null : options.getValue();
        var enabled = options == null ? null : options.getEnabled();
        return withOptionalStringImpl(value, enabled);
    }

    public IResource withOptionalString() {
        return withOptionalString(null);
    }

    /** Adds an optional string parameter */
    private IResource withOptionalStringImpl(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Gets the tags for the resource */
    private AspireList<String> getTagsField;
    public AspireList<String> getTags() {
        if (getTagsField == null) {
            getTagsField = new AspireList<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.Java.Tests/getTags");
        }
        return getTagsField;
    }

    /** Gets the metadata for the resource */
    private AspireDict<String, String> getMetadataField;
    public AspireDict<String, String> getMetadata() {
        if (getMetadataField == null) {
            getMetadataField = new AspireDict<>(getHandle(), getClient(), "Aspire.Hosting.CodeGeneration.Java.Tests/getMetadata");
        }
        return getMetadataField;
    }

    /** Sets the connection string using a reference expression */
    public IResourceWithConnectionString withConnectionString(ReferenceExpression connectionString) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("connectionString", AspireClient.serializeValue(connectionString));
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConnectionString", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(AspireAction1<TestEnvironmentContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (TestEnvironmentContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    public IResource withOptionalCallback() {
        return withOptionalCallback(null);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(AspireAction1<TestCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = callback == null ? null : getClient().registerCallback(args -> {
            var arg = (TestCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(AspireFunc1<TestResourceContext, Boolean> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var validatorId = getClient().registerCallback(args -> {
            var arg = (TestResourceContext) args[0];
            return AspireClient.awaitValue(validator.invoke(arg));
        });
        if (validatorId != null) {
            reqArgs.put("validator", validatorId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    public IResource testWaitFor(ResourceBuilderBase dependency) {
        return testWaitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Gets the endpoints */
    public String[] getEndpoints() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        return (String[]) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/getEndpoints", reqArgs);
    }

    /** Sets connection string using direct interface target */
    public IResourceWithConnectionString withConnectionStringDirect(String connectionString) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("connectionString", AspireClient.serializeValue(connectionString));
        return (IResourceWithConnectionString) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConnectionStringDirect", reqArgs);
    }

    /** Redis-specific configuration */
    public TestRedisResource withRedisSpecific(String option) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("option", AspireClient.serializeValue(option));
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withRedisSpecific", reqArgs);
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    public IResource withDependency(ResourceBuilderBase dependency) {
        return withDependency(new IResourceWithConnectionString(dependency.getHandle(), dependency.getClient()));
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    public String getStatusAsync() {
        return getStatusAsync(null);
    }

    /** Gets the status of the resource asynchronously */
    public String getStatusAsync(CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/getStatusAsync", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(AspireAction1<CancellationToken> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var operationId = getClient().registerCallback(args -> {
            var arg = CancellationToken.fromValue(args[0]);
            operation.invoke(arg);
            return null;
        });
        if (operationId != null) {
            reqArgs.put("operation", operationId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

    public boolean waitForReadyAsync(double timeout) {
        return waitForReadyAsync(timeout, null);
    }

    /** Waits for the resource to be ready */
    public boolean waitForReadyAsync(double timeout, CancellationToken cancellationToken) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("timeout", AspireClient.serializeValue(timeout));
        if (cancellationToken != null) {
            reqArgs.put("cancellationToken", getClient().registerCancellation(cancellationToken));
        }
        return (boolean) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/waitForReadyAsync", reqArgs);
    }

    /** Tests multi-param callback destructuring */
    public TestRedisResource withMultiParamHandleCallback(AspireAction2<TestCallbackContext, TestEnvironmentContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg1 = (TestCallbackContext) args[0];
            var arg2 = (TestEnvironmentContext) args[1];
            callback.invoke(arg1, arg2);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withMultiParamHandleCallback", reqArgs);
    }

    /** Adds a data volume with persistence */
    public TestRedisResource withDataVolume(WithDataVolumeOptions options) {
        var name = options == null ? null : options.getName();
        var isReadOnly = options == null ? null : options.isReadOnly();
        return withDataVolumeImpl(name, isReadOnly);
    }

    public TestRedisResource withDataVolume() {
        return withDataVolume(null);
    }

    /** Adds a data volume with persistence */
    private TestRedisResource withDataVolumeImpl(String name, Boolean isReadOnly) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (name != null) {
            reqArgs.put("name", AspireClient.serializeValue(name));
        }
        if (isReadOnly != null) {
            reqArgs.put("isReadOnly", AspireClient.serializeValue(isReadOnly));
        }
        return (TestRedisResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDataVolume", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext. */
class TestResourceContext extends HandleWrapperBase {
    TestResourceContext(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Gets the Name property */
    public String name() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.name", reqArgs);
    }

    /** Sets the Name property */
    public TestResourceContext setName(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestResourceContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setName", reqArgs);
    }

    /** Gets the Value property */
    public double value() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (double) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.value", reqArgs);
    }

    /** Sets the Value property */
    public TestResourceContext setValue(double value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        return (TestResourceContext) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValue", reqArgs);
    }

    /** Invokes the GetValueAsync method */
    public String getValueAsync() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (String) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.getValueAsync", reqArgs);
    }

    /** Invokes the SetValueAsync method */
    public void setValueAsync(String value) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        reqArgs.put("value", AspireClient.serializeValue(value));
        getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.setValueAsync", reqArgs);
    }

    /** Invokes the ValidateAsync method */
    public boolean validateAsync() {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("context", AspireClient.serializeValue(getHandle()));
        return (boolean) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes/TestResourceContext.validateAsync", reqArgs);
    }

}

/** Wrapper for Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource. */
class TestVaultResource extends ResourceBuilderBase {
    TestVaultResource(Handle handle, AspireClient client) {
        super(handle, client);
    }

    /** Adds an optional string parameter */
    public IResource withOptionalString(WithOptionalStringOptions options) {
        var value = options == null ? null : options.getValue();
        var enabled = options == null ? null : options.getEnabled();
        return withOptionalStringImpl(value, enabled);
    }

    public IResource withOptionalString() {
        return withOptionalString(null);
    }

    /** Adds an optional string parameter */
    private IResource withOptionalStringImpl(String value, Boolean enabled) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        if (value != null) {
            reqArgs.put("value", AspireClient.serializeValue(value));
        }
        if (enabled != null) {
            reqArgs.put("enabled", AspireClient.serializeValue(enabled));
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalString", reqArgs);
    }

    /** Configures the resource with a DTO */
    public IResource withConfig(TestConfigDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withConfig", reqArgs);
    }

    /** Configures environment with callback (test version) */
    public IResourceWithEnvironment testWithEnvironmentCallback(AspireAction1<TestEnvironmentContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = getClient().registerCallback(args -> {
            var arg = (TestEnvironmentContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWithEnvironmentCallback", reqArgs);
    }

    /** Sets the created timestamp */
    public IResource withCreatedAt(String createdAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("createdAt", AspireClient.serializeValue(createdAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCreatedAt", reqArgs);
    }

    /** Sets the modified timestamp */
    public IResource withModifiedAt(String modifiedAt) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("modifiedAt", AspireClient.serializeValue(modifiedAt));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withModifiedAt", reqArgs);
    }

    /** Sets the correlation ID */
    public IResource withCorrelationId(String correlationId) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("correlationId", AspireClient.serializeValue(correlationId));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCorrelationId", reqArgs);
    }

    public IResource withOptionalCallback() {
        return withOptionalCallback(null);
    }

    /** Configures with optional callback */
    public IResource withOptionalCallback(AspireAction1<TestCallbackContext> callback) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var callbackId = callback == null ? null : getClient().registerCallback(args -> {
            var arg = (TestCallbackContext) args[0];
            callback.invoke(arg);
            return null;
        });
        if (callbackId != null) {
            reqArgs.put("callback", callbackId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withOptionalCallback", reqArgs);
    }

    /** Sets the resource status */
    public IResource withStatus(TestResourceStatus status) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("status", AspireClient.serializeValue(status));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withStatus", reqArgs);
    }

    /** Configures with nested DTO */
    public IResource withNestedConfig(TestNestedDto config) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("config", AspireClient.serializeValue(config));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withNestedConfig", reqArgs);
    }

    /** Adds validation callback */
    public IResource withValidator(AspireFunc1<TestResourceContext, Boolean> validator) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var validatorId = getClient().registerCallback(args -> {
            var arg = (TestResourceContext) args[0];
            return AspireClient.awaitValue(validator.invoke(arg));
        });
        if (validatorId != null) {
            reqArgs.put("validator", validatorId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withValidator", reqArgs);
    }

    /** Waits for another resource (test version) */
    public IResource testWaitFor(IResource dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/testWaitFor", reqArgs);
    }

    public IResource testWaitFor(ResourceBuilderBase dependency) {
        return testWaitFor(new IResource(dependency.getHandle(), dependency.getClient()));
    }

    /** Adds a dependency on another resource */
    public IResource withDependency(IResourceWithConnectionString dependency) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("dependency", AspireClient.serializeValue(dependency));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withDependency", reqArgs);
    }

    public IResource withDependency(ResourceBuilderBase dependency) {
        return withDependency(new IResourceWithConnectionString(dependency.getHandle(), dependency.getClient()));
    }

    /** Sets the endpoints */
    public IResource withEndpoints(String[] endpoints) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("endpoints", AspireClient.serializeValue(endpoints));
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEndpoints", reqArgs);
    }

    /** Sets environment variables */
    public IResourceWithEnvironment withEnvironmentVariables(Map<String, String> variables) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("variables", AspireClient.serializeValue(variables));
        return (IResourceWithEnvironment) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withEnvironmentVariables", reqArgs);
    }

    /** Performs a cancellable operation */
    public IResource withCancellableOperation(AspireAction1<CancellationToken> operation) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        var operationId = getClient().registerCallback(args -> {
            var arg = CancellationToken.fromValue(args[0]);
            operation.invoke(arg);
            return null;
        });
        if (operationId != null) {
            reqArgs.put("operation", operationId);
        }
        return (IResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withCancellableOperation", reqArgs);
    }

    /** Configures vault using direct interface target */
    public ITestVaultResource withVaultDirect(String option) {
        Map<String, Object> reqArgs = new HashMap<>();
        reqArgs.put("builder", AspireClient.serializeValue(getHandle()));
        reqArgs.put("option", AspireClient.serializeValue(option));
        return (ITestVaultResource) getClient().invokeCapability("Aspire.Hosting.CodeGeneration.Java.Tests/withVaultDirect", reqArgs);
    }

}

// ============================================================================
// Handle wrapper registrations
// ============================================================================

/** Static initializer to register handle wrappers. */
class AspireRegistrations {
    static {
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCallbackContext", (h, c) -> new TestCallbackContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestResourceContext", (h, c) -> new TestResourceContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestEnvironmentContext", (h, c) -> new TestEnvironmentContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestCollectionContext", (h, c) -> new TestCollectionContext(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestRedisResource", (h, c) -> new TestRedisResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestDatabaseResource", (h, c) -> new TestDatabaseResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResource", (h, c) -> new IResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithConnectionString", (h, c) -> new IResourceWithConnectionString(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.TestVaultResource", (h, c) -> new TestVaultResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting.CodeGeneration.Java.Tests/Aspire.Hosting.CodeGeneration.TypeScript.Tests.TestTypes.ITestVaultResource", (h, c) -> new ITestVaultResource(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.IDistributedApplicationBuilder", (h, c) -> new IDistributedApplicationBuilder(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Aspire.Hosting.ApplicationModel.IResourceWithEnvironment", (h, c) -> new IResourceWithEnvironment(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/List<string>", (h, c) -> new AspireList(h, c));
        AspireClient.registerHandleWrapper("Aspire.Hosting/Dict<string,string>", (h, c) -> new AspireDict(h, c));
    }

    static void ensureRegistered() {
        // Called to trigger static initializer
    }
}

// ============================================================================
// Connection Helpers
// ============================================================================

/** Main entry point for Aspire SDK. */
public class Aspire {
    /** Connect to the AppHost server. */
    public static AspireClient connect() throws Exception {
        BaseRegistrations.ensureRegistered();
        AspireRegistrations.ensureRegistered();
        String socketPath = System.getenv("REMOTE_APP_HOST_SOCKET_PATH");
        if (socketPath == null || socketPath.isEmpty()) {
            throw new RuntimeException("REMOTE_APP_HOST_SOCKET_PATH environment variable not set. Run this application using `aspire run`.");
        }
        AspireClient client = new AspireClient(socketPath);
        client.connect();
        client.onDisconnect(() -> System.exit(1));
        return client;
    }

    /** Create a new distributed application builder. */
    public static IDistributedApplicationBuilder createBuilder(CreateBuilderOptions options) throws Exception {
        AspireClient client = connect();
        Map<String, Object> resolvedOptions = new HashMap<>();
        if (options != null) {
            resolvedOptions.putAll(options.toMap());
        }
        if (resolvedOptions.get("Args") == null) {
            // Note: Java doesn't have easy access to command line args from here
            resolvedOptions.put("Args", new String[0]);
        }
        if (resolvedOptions.get("ProjectDirectory") == null) {
            resolvedOptions.put("ProjectDirectory", System.getProperty("user.dir"));
        }
        if (resolvedOptions.get("AppHostFilePath") == null) {
            String appHostFilePath = System.getenv("ASPIRE_APPHOST_FILEPATH");
            if (appHostFilePath != null && !appHostFilePath.isEmpty()) {
                resolvedOptions.put("AppHostFilePath", appHostFilePath);
            }
        }
        Map<String, Object> args = new HashMap<>();
        args.put("options", resolvedOptions);
        return (IDistributedApplicationBuilder) client.invokeCapability("Aspire.Hosting/createBuilderWithOptions", args);
    }
}

