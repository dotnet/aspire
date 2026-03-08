import { env } from 'node:process';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { OTLPLogExporter } from '@opentelemetry/exporter-logs-otlp-grpc';
import { BatchLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { credentials } from '@grpc/grpc-js';

if (env.OTEL_EXPORTER_OTLP_ENDPOINT) {
  const isHttps = env.OTEL_EXPORTER_OTLP_ENDPOINT.startsWith('https://');
  const collectorOptions = {
    credentials: !isHttps
      ? credentials.createInsecure()
      : credentials.createSsl(),
  };

  const sdk = new NodeSDK({
    traceExporter: new OTLPTraceExporter(collectorOptions),
    metricReader: new PeriodicExportingMetricReader({
      exporter: new OTLPMetricExporter(collectorOptions),
    }),
    logRecordProcessor: new BatchLogRecordProcessor(
      new OTLPLogExporter(collectorOptions),
    ),
    instrumentations: [getNodeAutoInstrumentations()],
  });

  sdk.start();
}
