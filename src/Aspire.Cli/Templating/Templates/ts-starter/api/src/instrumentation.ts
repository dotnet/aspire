import { env } from 'node:process';
import { NodeSDK } from '@opentelemetry/sdk-node';
import { getNodeAutoInstrumentations } from '@opentelemetry/auto-instrumentations-node';
import { OTLPTraceExporter as GrpcTraceExporter } from '@opentelemetry/exporter-trace-otlp-grpc';
import { OTLPMetricExporter as GrpcMetricExporter } from '@opentelemetry/exporter-metrics-otlp-grpc';
import { OTLPLogExporter as GrpcLogExporter } from '@opentelemetry/exporter-logs-otlp-grpc';
import { OTLPTraceExporter as ProtoTraceExporter } from '@opentelemetry/exporter-trace-otlp-proto';
import { OTLPMetricExporter as ProtoMetricExporter } from '@opentelemetry/exporter-metrics-otlp-proto';
import { OTLPLogExporter as ProtoLogExporter } from '@opentelemetry/exporter-logs-otlp-proto';
import { BatchLogRecordProcessor } from '@opentelemetry/sdk-logs';
import { PeriodicExportingMetricReader } from '@opentelemetry/sdk-metrics';
import { credentials } from '@grpc/grpc-js';

if (env.OTEL_EXPORTER_OTLP_ENDPOINT) {
  // Aspire's WithOtlpExporter defaults to gRPC but falls back to http/protobuf
  // when only the HTTP OTLP endpoint is configured. Match the protocol Aspire sets.
  const protocol = env.OTEL_EXPORTER_OTLP_PROTOCOL?.toLowerCase();
  const useHttp = protocol === 'http/protobuf' || protocol === 'http/json';

  let traceExporter;
  let metricExporter;
  let logExporter;

  if (useHttp) {
    traceExporter = new ProtoTraceExporter();
    metricExporter = new ProtoMetricExporter();
    logExporter = new ProtoLogExporter();
  } else {
    // Default: gRPC (matches Aspire's default OTEL_EXPORTER_OTLP_PROTOCOL=grpc)
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
