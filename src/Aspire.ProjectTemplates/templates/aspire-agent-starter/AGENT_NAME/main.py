import contextlib
import logging
import os
import sys

from azure.ai.projects.aio import AIProjectClient
from azure.identity.aio import DefaultAzureCredential
from rich.console import Console
from rich.logging import RichHandler

from agent.agent import Agent


def main():
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
        console_handler.setFormatter(logging.Formatter("%(message)s"))
    else:
        console_handler = logging.StreamHandler(sys.stderr)
        console_handler.setFormatter(
            logging.Formatter("%(asctime)s - %(name)s - %(levelname)s - %(message)s")
        )
    root_logger.addHandler(console_handler)

    FOUNDRY_ENDPOINT = os.environ['AZURE_AI_FOUNDRY_PROJECT_ENDPOINT']
    MODEL_DEPLOYMENT_NAME = os.environ['AZURE_AI_DEPLOYMENT_NAME']
    PORT = int(os.getenv('PORT', '8088'))
    print(f'Starting agent on port {PORT}')
    with contextlib.closing(AIProjectClient(FOUNDRY_ENDPOINT, DefaultAzureCredential())) as client:
        # Serve traffic
        Agent(client, MODEL_DEPLOYMENT_NAME).run(PORT)


if __name__ == "__main__":
    main()
