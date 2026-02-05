import logging
import sys

from azure.monitor.opentelemetry.exporter import (
    AzureMonitorLogExporter,
    AzureMonitorTraceExporter,
)
from opentelemetry import trace
from opentelemetry.semconv.attributes import service_attributes
from opentelemetry._logs import set_logger_provider
from opentelemetry.sdk._logs import LoggerProvider, LoggingHandler
from opentelemetry.sdk._logs.export import BatchLogRecordProcessor
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter
from rich.console import Console
from rich.logging import RichHandler


def configure_telemetry(log_level: str = 'INFO', app_insights_connection_string: str | None = None) -> None:
    """
    Configure logging and telemetry for the agent

    If given a valid Application Insights connection string, it configures tracing and logs to
    go to the Application Insights component. The data will be sent to standard out, with
    rich color formatting if the console is a pseudo-TTY.

    Args:
        app_insights_connection_string (str): The Application Insights connection string
            This is usually the APPINSIGHTS_CONNECTION_STRING environment variable, which
            usually derives its value from the ConnectionString property of the resource.
    """
    if app_insights_connection_string:
        configure_appinsights_telemetry(
            app_insights_connection_string=app_insights_connection_string,
            log_level=log_level
        )
    elif sys.stderr.isatty():
        configure_console_color_telemetry(log_level=log_level)
    else:
        configure_console_telemetry(log_level=log_level)


def get_resource() -> Resource:
    return Resource.create({
        service_attributes.SERVICE_NAME: "agent"
    })


def configure_console_telemetry(log_level: str = 'INFO') -> None:
    resource = get_resource()

    # logging
    root_logger = logging.getLogger()
    root_logger.setLevel(getattr(logging, log_level, logging.INFO))
    console_handler = logging.StreamHandler(sys.stderr)
    console_handler.setFormatter(
        logging.Formatter("%(asctime)s - %(name)s - %(levelname)s - %(message)s")
    )
    root_logger.addHandler(console_handler)

    # tracing
    provider = TracerProvider(resource=resource)
    exporter = ConsoleSpanExporter()
    processor = BatchSpanProcessor(exporter)
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)

    logging.info("Telemetry configured to export to console")


def configure_console_color_telemetry(log_level: str = 'INFO') -> None:
    resource = get_resource()

    # logging
    root_logger = logging.getLogger()
    root_logger.setLevel(getattr(logging, log_level, logging.INFO))
    console = Console(stderr=True)
    console_handler = RichHandler(
        console=console,
        show_time=True,
        show_path=False,
        rich_tracebacks=True,
    )
    console_handler.setFormatter(logging.Formatter("%(message)s"))
    root_logger.addHandler(console_handler)

    # tracing
    provider = TracerProvider(resource=resource)
    processor = BatchSpanProcessor(ConsoleSpanExporter())
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)

    logging.info("Telemetry configured to export to console")



def configure_appinsights_telemetry(app_insights_connection_string: str, log_level: str = 'INFO') -> None:
    resource = get_resource()

    # logging
    root_logger = logging.getLogger()
    root_logger.setLevel(getattr(logging, log_level, logging.INFO))
    logger_provider = LoggerProvider(resource=resource)
    # Send otel logs -> appinsights
    logger_provider.add_log_record_processor(
        BatchLogRecordProcessor(
            AzureMonitorLogExporter(connection_string=app_insights_connection_string)
        )
    )
    set_logger_provider(logger_provider)
    # Send standard logs -> appinsights
    root_logger.addHandler(LoggingHandler(logger_provider=logger_provider))

    # tracing
    provider = TracerProvider(resource=resource)
    processor = BatchSpanProcessor(AzureMonitorTraceExporter(
        connection_string=app_insights_connection_string
    ))
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)

    logging.info("Telemetry configured to export to Application Insights")