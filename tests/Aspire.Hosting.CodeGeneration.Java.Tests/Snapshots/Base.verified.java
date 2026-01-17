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
 * AspireList is a handle-backed list.
 */
class AspireList<T> extends HandleWrapperBase {
    AspireList(Handle handle, AspireClient client) {
        super(handle, client);
    }
}

/**
 * AspireDict is a handle-backed dictionary.
 */
class AspireDict<K, V> extends HandleWrapperBase {
    AspireDict(Handle handle, AspireClient client) {
        super(handle, client);
    }
}
