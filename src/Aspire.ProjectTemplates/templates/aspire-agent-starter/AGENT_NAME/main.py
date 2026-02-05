import contextlib
import os
from azure.ai.projects.aio import AIProjectClient
from azure.identity.aio import DefaultAzureCredential


def main():
    dotenv_path = None
    ENV = os.getenv('AZURE_ENV_NAME', 'local')
    if ENV == 'local':
        dotenv_path = os.path.join(str(os.path.dirname(__file__)), '..', '..', '..', '.azure', ENV, '.env')
        from dotenv import load_dotenv
        load_dotenv(dotenv_path)
    
    # Load after load_dotenv() to ensure env vars get populated
    from agent.telemetry import configure_telemetry
    from agent.agent import Agent

    log_level = os.getenv('LOG_LEVEL', 'INFO').upper()
    app_insights_connection_string = os.getenv('APPINSIGHTS_CONNECTION_STRING')
    configure_telemetry(log_level, app_insights_connection_string)

    FOUNDRY_ENDPOINT = os.environ['AZURE_AI_FOUNDRY_PROJECT_ENDPOINT']
    MODEL_DEPLOYMENT_NAME = os.environ['AZURE_AI_MODEL_DEPLOYMENT_NAME']
    PORT = int(os.getenv('PORT', '8088'))

    with contextlib.closing(AIProjectClient(FOUNDRY_ENDPOINT, DefaultAzureCredential())) as client:
        # Serve traffic
        Agent(client, MODEL_DEPLOYMENT_NAME).run(PORT)


if __name__ == "__main__":
    main()