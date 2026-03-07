// Base.java - Base types and utilities for Aspire Java SDK
// GENERATED CODE - DO NOT EDIT

package aspire;

import java.util.*;

/**
 * HandleWrapperBase is the base class for all handle wrappers.
 */
class HandleWrapperBase {
    private final Handle handle;
    private final AspireClient client;

    HandleWrapperBase(Handle handle, AspireClient client) {
        this.handle = handle;
        this.client = client;
    }

    Handle getHandle() {
        return handle;
    }

    AspireClient getClient() {
        return client;
    }
}

/**
 * ResourceBuilderBase extends HandleWrapperBase for resource builders.
 */
class ResourceBuilderBase extends HandleWrapperBase {
    ResourceBuilderBase(Handle handle, AspireClient client) {
        super(handle, client);
    }
}

/**
 * ReferenceExpression represents a reference expression.
 */
class ReferenceExpression {
    private final String format;
    private final Object[] args;

    ReferenceExpression(String format, Object... args) {
        this.format = format;
        this.args = args;
    }

    String getFormat() {
        return format;
    }

    Object[] getArgs() {
        return args;
    }

    Map<String, Object> toJson() {
        Map<String, Object> refExpr = new HashMap<>();
        refExpr.put("format", format);
        refExpr.put("args", Arrays.asList(args));
        
        Map<String, Object> result = new HashMap<>();
        result.put("$refExpr", refExpr);
        return result;
    }

    /**
     * Creates a new reference expression.
     */
    static ReferenceExpression refExpr(String format, Object... args) {
        return new ReferenceExpression(format, args);
    }
}

/**
 * ConditionalReferenceExpression represents a conditional expression that selects
 * between two ReferenceExpression branches. The condition and branches are evaluated
 * on the AppHost server.
 */
class ConditionalReferenceExpression {
    // Expression mode fields
    private final Object condition;
    private final ReferenceExpression whenTrue;
    private final ReferenceExpression whenFalse;

    // Handle mode fields
    private final Handle handle;
    private final AspireClient client;

    // Handle mode constructor
    ConditionalReferenceExpression(Handle handle, AspireClient client) {
        this.handle = handle;
        this.client = client;
        this.condition = null;
        this.whenTrue = null;
        this.whenFalse = null;
    }

    // Expression mode constructor
    private ConditionalReferenceExpression(Object condition, ReferenceExpression whenTrue, ReferenceExpression whenFalse) {
        this.handle = null;
        this.client = null;
        this.condition = condition;
        this.whenTrue = whenTrue;
        this.whenFalse = whenFalse;
    }

    /**
     * Creates a conditional reference expression from its parts.
     */
    static ConditionalReferenceExpression create(Object condition, ReferenceExpression whenTrue, ReferenceExpression whenFalse) {
        return new ConditionalReferenceExpression(condition, whenTrue, whenFalse);
    }

    Map<String, Object> toJson() {
        if (handle != null) {
            return handle.toJson();
        }
        var condExpr = new java.util.HashMap<String, Object>();
        condExpr.put("condition", AspireClient.serializeValue(condition));
        condExpr.put("whenTrue", whenTrue.toJson());
        condExpr.put("whenFalse", whenFalse.toJson());

        var result = new java.util.HashMap<String, Object>();
        result.put("$condExpr", condExpr);
        return result;
    }
}

/**
 * AspireList is a handle-backed list with lazy handle resolution.
 */
class AspireList<T> extends HandleWrapperBase {
    private final String getterCapabilityId;
    private Handle resolvedHandle;

    AspireList(Handle handle, AspireClient client) {
        super(handle, client);
        this.getterCapabilityId = null;
        this.resolvedHandle = handle;
    }

    AspireList(Handle contextHandle, AspireClient client, String getterCapabilityId) {
        super(contextHandle, client);
        this.getterCapabilityId = getterCapabilityId;
        this.resolvedHandle = null;
    }

    private Handle ensureHandle() {
        if (resolvedHandle != null) {
            return resolvedHandle;
        }
        if (getterCapabilityId != null) {
            Map<String, Object> args = new HashMap<>();
            args.put("context", getHandle().toJson());
            Object result = getClient().invokeCapability(getterCapabilityId, args);
            if (result instanceof Handle) {
                resolvedHandle = (Handle) result;
            }
        }
        if (resolvedHandle == null) {
            resolvedHandle = getHandle();
        }
        return resolvedHandle;
    }
}

/**
 * AspireDict is a handle-backed dictionary with lazy handle resolution.
 */
class AspireDict<K, V> extends HandleWrapperBase {
    private final String getterCapabilityId;
    private Handle resolvedHandle;

    AspireDict(Handle handle, AspireClient client) {
        super(handle, client);
        this.getterCapabilityId = null;
        this.resolvedHandle = handle;
    }

    AspireDict(Handle contextHandle, AspireClient client, String getterCapabilityId) {
        super(contextHandle, client);
        this.getterCapabilityId = getterCapabilityId;
        this.resolvedHandle = null;
    }

    private Handle ensureHandle() {
        if (resolvedHandle != null) {
            return resolvedHandle;
        }
        if (getterCapabilityId != null) {
            Map<String, Object> args = new HashMap<>();
            args.put("context", getHandle().toJson());
            Object result = getClient().invokeCapability(getterCapabilityId, args);
            if (result instanceof Handle) {
                resolvedHandle = (Handle) result;
            }
        }
        if (resolvedHandle == null) {
            resolvedHandle = getHandle();
        }
        return resolvedHandle;
    }
}
