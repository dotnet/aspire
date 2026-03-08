import { env } from 'node:process';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter as GrpcTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter as GrpcMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { OTLPLogExporter as GrpcLogExporter } from '@opentelemetry/exporter-logs-otlp-grpc';
import { OTLPTraceExporter as HttpTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { OTLPMetricExporter as HttpMetricExporter } from '@opentelemetry/exporter-metrics-otlp-http';
import { OTLPLogExporter as HttpLogExporter } from '@opentelemetry/exporter-logs-otlp-http';
import { BatchLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { credentials } from '@grpc/grpc-js';

if (env.OTEL_EXPORTER_OTLP_ENDPOINT) {
  const protocol = env.OTEL_EXPORTER_OTLP_PROTOCOL?.toLowerCase();
  const useHttp = protocol === 'http/protobuf' || protocol === 'http/json';

  let traceExporter;
  let metricExporter;
  let logExporter;

  if (useHttp) {
    traceExporter = new HttpTraceExporter();
    metricExporter = new HttpMetricExporter();
    logExporter = new HttpLogExporter();
  } else {
    const isHttps = env.OTEL_EXPORTER_OTLP_ENDPOINT.startsWith('https://');
    const grpcOptions = {
      credentials: isHttps
        ? credentials.createSsl()
        : credentials.createInsecure(),
    };
    traceExporter = new GrpcTraceExporter(grpcOptions);
    metricExporter = new GrpcMetricExporter(grpcOptions);
    logExporter = new GrpcLogExporter(grpcOptions);
  }

  const sdk = new NodeSDK({
    traceExporter,
    metricReader: new PeriodicExportingMetricReader({
      exporter: metricExporter,
    }),
    logRecordProcessor: new BatchLogRecordProcessor(logExporter),
    instrumentations: [getNodeAutoInstrumentations()],
  });

  sdk.start();

  process.on('SIGTERM', () => {
    sdk.shutdown().finally(() => process.exit(0));
  });
  process.on('SIGINT', () => {
    sdk.shutdown().finally(() => process.exit(0));
  });
}
