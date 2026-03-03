import {
  ConsoleSpanExporter,
  SimpleSpanProcessor,
} from '@opentelemetry/sdk-trace-base';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { ZoneContextManager } from '@opentelemetry/context-zone';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';

export function initializeTelemetry(otlpUrl, headers, resourceAttributes) {
    const otlpOptions = {
        url: `${otlpUrl}/v1/traces`,
        headers: parseDelimitedValues(headers)
    };

    var attributes = parseDelimitedValues(resourceAttributes);
    attributes['service.name'] = 'browser';

    const provider = new WebTracerProvider({
        resource: resourceFromAttributes(attributes),
        spanProcessors: [
            new SimpleSpanProcessor(new ConsoleSpanExporter()),
            new SimpleSpanProcessor(new OTLPTraceExporter(otlpOptions))
        ]
    });

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
