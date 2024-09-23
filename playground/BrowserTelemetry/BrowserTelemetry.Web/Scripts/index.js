import {
  ConsoleSpanExporter,
  SimpleSpanProcessor,
} from '@opentelemetry/sdk-trace-base';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { Resource } from '@opentelemetry/resources';
import { SemanticResourceAttributes } from '@opentelemetry/semantic-conventions'
import { diag, DiagConsoleLogger, DiagLogLevel } from "@opentelemetry/api";

export function initializeTelemetry(otlpUrl, headers, resourceAttributes) {
    const otlpOptions = {
        url: `${otlpUrl}/v1/traces`,
        headers: parseDelimitedValues(headers)
    };

    var attributes = parseDelimitedValues(resourceAttributes);
    attributes[SemanticResourceAttributes.SERVICE_NAME] = 'browser';

    const provider = new WebTracerProvider({
        resource: new Resource(attributes),
    });
    provider.addSpanProcessor(new SimpleSpanProcessor(new ConsoleSpanExporter()));
    provider.addSpanProcessor(new SimpleSpanProcessor(new OTLPTraceExporter(otlpOptions)));

    provider.register({
        // Changing default contextManager to use ZoneContextManager - supports asynchronous operations - optional
        contextManager: new ZoneContextManager(),
    });

    // Registering instrumentations
    registerInstrumentations({
        instrumentations: [new DocumentLoadInstrumentation()],
    });
}

function parseDelimitedValues(s) {
    const headers = s.split(','); // Split by comma
    const o = {};

    headers.forEach(header => {
        const [key, value] = header.split('='); // Split by equal sign
        o[key.trim()] = value.trim(); // Add to the object, trimming spaces
    });

    return o;
}
