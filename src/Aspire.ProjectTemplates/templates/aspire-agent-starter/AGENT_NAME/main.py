import contextlib
import logging
import os
import sys

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
    root_logger = logging.getLogger()
    root_logger.setLevel(getattr(logging, log_level, logging.INFO))
    if sys.stderr.isatty():
        console_handler = RichHandler(
            console=Console(stderr=True),
            show_time=True,
            show_path=False,
            rich_tracebacks=True,
        )
        console_handler.name = 'console'
        console_handler.setFormatter(logging.Formatter("%(message)s"))
    else:
        console_handler = logging.StreamHandler(sys.stdout)
        console_handler.setFormatter(
            logging.Formatter("%(asctime)s - %(name)s - %(levelname)s - %(message)s")
        )
        console_handler.name = 'console'
    root_logger.addHandler(console_handler)
    for logger_name in ["uvicorn", "uvicorn.error", "uvicorn.access"]:
        uv_logger = logging.getLogger(logger_name)
        uv_logger.addHandler(console_handler)
        uv_logger.setLevel(root_logger.level)
        uv_logger.propagate = False


if __name__ == "__main__":
    main()
