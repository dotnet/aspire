import contextlib
import logging
import os
import sys

from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor, SpanExporter
from opentelemetry import _logs
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor, LogRecordExporter
from rich.console import Console
from rich.logging import RichHandler


AGENT_NAME = "Example Agent"


def main():
    from azure.ai.projects.aio import AIProjectClient
    from azure.identity.aio import DefaultAzureCredential
    from agent.agent import Agent

    FOUNDRY_ENDPOINT = os.environ['AZURE_AI_FOUNDRY_PROJECT_ENDPOINT']
    MODEL_DEPLOYMENT_NAME = os.environ['AZURE_AI_DEPLOYMENT_NAME']
    with contextlib.closing(AIProjectClient(FOUNDRY_ENDPOINT, DefaultAzureCredential())) as client:
        # Serve traffic
        Agent(client, MODEL_DEPLOYMENT_NAME).run()


def init_telemetry():
    from opentelemetry.instrumentation.openai_agents import OpenAIAgentsInstrumentor # type: ignore
    # Run before any other third party imports to ensure instrumentation is applied correctly
    OpenAIAgentsInstrumentor().instrument(
        capture_message_content='span_and_event',
    )

    log_level = os.getenv('LOG_LEVEL', 'INFO').upper()
    otlp_endpoint = os.getenv('OTEL_EXPORTER_OTLP_ENDPOINT')
    otlp_protocol = os.getenv('OTEL_EXPORTER_OTLP_PROTOCOL', 'http/protobuf')
    traces_exporter = os.getenv('OTEL_TRACES_EXPORTER')
    logs_exporter = os.getenv('OTEL_LOGS_EXPORTER')
    app_insights_connection_string = os.getenv('APPLICATIONINSIGHTS_CONNECTION_STRING')

    root_logger = logging.getLogger()
    root_logger.setLevel(getattr(logging, log_level, logging.INFO))

    normalized_protocol = otlp_protocol.strip().lower()
    if normalized_protocol != 'grpc':
        normalized_protocol = 'http/protobuf'

    trace_provider = TracerProvider()
    trace.set_tracer_provider(trace_provider)
    trace_exporter: SpanExporter | None = None
    if otlp_endpoint and traces_exporter:
        if normalized_protocol == 'grpc':
            from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
            trace_exporter = OTLPSpanExporter(endpoint=otlp_endpoint)
        else:
            from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter
            trace_exporter = OTLPSpanExporter(endpoint=otlp_endpoint)
    elif app_insights_connection_string:
        from azure.monitor.opentelemetry.exporter import AzureMonitorTraceExporter
        trace_exporter = AzureMonitorTraceExporter.from_connection_string(app_insights_connection_string)
    if trace_exporter:
        trace_provider.add_span_processor(BatchSpanProcessor(trace_exporter))

    log_exporter: LogRecordExporter | None = None
    if otlp_endpoint and logs_exporter:
        if normalized_protocol == 'grpc':
            from opentelemetry.exporter.otlp.proto.grpc._log_exporter import OTLPLogExporter
            log_exporter = OTLPLogExporter(endpoint=otlp_endpoint)
        else:
            from opentelemetry.exporter.otlp.proto.http._log_exporter import OTLPLogExporter
            log_exporter = OTLPLogExporter(endpoint=otlp_endpoint)
    elif app_insights_connection_string:
        from azure.monitor.opentelemetry.exporter import AzureMonitorLogExporter
        logger_provider = LoggerProvider()
        _logs.set_logger_provider(logger_provider)
        log_exporter = AzureMonitorLogExporter.from_connection_string(app_insights_connection_string)

    if log_exporter:
        logger_provider = LoggerProvider()
        _logs.set_logger_provider(logger_provider)
        logger_provider.add_log_record_processor(BatchLogRecordProcessor(log_exporter))
        logging_handler = LoggingHandler(level=root_logger.level, logger_provider=logger_provider)
    else:
        if sys.stdout.isatty():
            logging_handler = RichHandler(
                console=Console(file=sys.stdout),
                show_time=True,
                show_path=False,
                rich_tracebacks=True,
            )
            logging_handler.setFormatter(logging.Formatter("%(message)s"))
        else:
            logging_handler = logging.StreamHandler(sys.stdout)
            logging_handler.setFormatter(
                logging.Formatter("%(asctime)s - %(name)s - %(levelname)s - %(message)s")
            )

    root_logger.addHandler(logging_handler)
    for logger_name in ["uvicorn", "uvicorn.error", "uvicorn.access"]:
        uv_logger = logging.getLogger(logger_name)
        uv_logger.addHandler(logging_handler)
        uv_logger.setLevel(root_logger.level)
        uv_logger.propagate = False


if __name__ == "__main__":
    init_telemetry()
    main()
