# mypy: ignore-errors
"""Bilingual weekend planner sample with full GenAI telemetry capture."""

from __future__ import annotations

import json
import logging
import os
import random
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Callable
from urllib.parse import urlparse

from azure.identity.aio import DefaultAzureCredential
from openai import AsyncOpenAI
from agents import (
    Agent,
    OpenAIResponsesModel,
    Runner,
    function_tool,
    set_default_openai_client,
    set_tracing_disabled,
)
from agents.tracing import (
    agent_span as tracing_agent_span,
    function_span as tracing_function_span,
    generation_span as tracing_generation_span,
    trace as tracing_trace,
)
from azure.ai.agentserver.core import AgentRunContext, FoundryCBAgent
from azure.ai.agentserver.core.models import (
    CreateResponse,
    Response as OpenAIResponse,
)
from azure.ai.agentserver.core.models.projects import (
    ItemContentOutputText,
    ResponseCompletedEvent,
    ResponseCreatedEvent,
    ResponseOutputItemAddedEvent,
    ResponsesAssistantMessageItemResource,
    ResponseTextDeltaEvent,
    ResponseTextDoneEvent,
)
from azure.ai.projects.aio import AIProjectClient
from dotenv import load_dotenv
from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.instrumentation.openai_agents import OpenAIAgentsInstrumentor
from opentelemetry.sdk.resources import Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor, ConsoleSpanExporter
from rich.logging import RichHandler

try:
    from azure.monitor.opentelemetry.exporter import (  # mypy: ignore
        AzureMonitorTraceExporter,
    )
except Exception:  # pragma: no cover
    AzureMonitorTraceExporter = None  # mypy: ignore

# Load env early so adapter init sees them
load_dotenv(override=True)


logging.basicConfig(
    level=logging.WARNING,
    format="%(message)s",
    datefmt="[%X]",
    handlers=[RichHandler()],
)
logger = logging.getLogger("bilingual_weekend_planner")
tracer = trace.get_tracer(__name__)


def _get_model(client: AsyncOpenAI) -> OpenAIResponsesModel:
    """Return the chat model configuration for the requested host."""

    if "AZURE_AI_DEPLOYMENT_NAME" not in os.environ:
        raise ValueError("AZURE_AI_DEPLOYMENT_NAME is required")
    return OpenAIResponsesModel(
        model=os.environ["AZURE_AI_DEPLOYMENT_NAME"],
        openai_client=client,
    )


def _get_openai_client() -> AsyncOpenAI:
    """Return the client configuration for the requested host."""

    # Explicitly check for required environment variables
    if "AZURE_AI_PROJECT_ENDPOINT" not in os.environ:
        raise ValueError("AZURE_AI_PROJECT_ENDPOINT is required")
    project_client = AIProjectClient(
        endpoint=os.environ["AZURE_AI_PROJECT_ENDPOINT"],
        credential=DefaultAzureCredential(),
    )
    openai_client = project_client.get_openai_client()
    return openai_client


def _configure_otel(base_url: str) -> None:
    """Configure the tracer provider and exporters."""

    capture_defaults = {
        "OTEL_INSTRUMENTATION_OPENAI_AGENTS_CAPTURE_CONTENT": "true",
        "OTEL_INSTRUMENTATION_OPENAI_AGENTS_CAPTURE_METRICS": "true",
        "OTEL_GENAI_CAPTURE_MESSAGES": "true",
        "OTEL_GENAI_CAPTURE_SYSTEM_INSTRUCTIONS": "true",
        "OTEL_GENAI_CAPTURE_TOOL_DEFINITIONS": "true",
        "OTEL_GENAI_EMIT_OPERATION_DETAILS": "true",
        "OTEL_GENAI_AGENT_NAME": os.getenv(
            "OTEL_GENAI_AGENT_NAME",
            "Bilingual Weekend Planner Agent",
        ),
        "OTEL_GENAI_AGENT_DESCRIPTION": os.getenv(
            "OTEL_GENAI_AGENT_DESCRIPTION",
            "Assistant that plans weekend activities using weather and events data in multiple languages",
        ),
        "OTEL_GENAI_AGENT_ID": os.getenv(
            "OTEL_GENAI_AGENT_ID", "bilingual-weekend-planner"
        ),
    }
    for env_key, value in capture_defaults.items():
        os.environ.setdefault(env_key, value)

    parsed = urlparse(str(base_url))
    if parsed.hostname:
        os.environ.setdefault("OTEL_GENAI_SERVER_ADDRESS", parsed.hostname)
    if parsed.port:
        os.environ.setdefault("OTEL_GENAI_SERVER_PORT", str(parsed.port))

    grpc_endpoint = os.getenv("OTEL_EXPORTER_OTLP_GRPC_ENDPOINT")
    if not grpc_endpoint:
        default_otlp_endpoint = os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT")
        protocol = os.getenv("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc").lower()
        if default_otlp_endpoint and protocol == "grpc":
            grpc_endpoint = default_otlp_endpoint

    conn = os.getenv("APPLICATION_INSIGHTS_CONNECTION_STRING")
    resource = Resource.create(
        {
            "service.name": "weekend-planner-service",
            "service.namespace": "leisure-orchestration",
            "service.version": os.getenv("SERVICE_VERSION", "1.0.0"),
        }
    )

    tracer_provider = TracerProvider(resource=resource)

    if grpc_endpoint:
        tracer_provider.add_span_processor(
            BatchSpanProcessor(OTLPSpanExporter(endpoint=grpc_endpoint))
        )
        print(f"[otel] OTLP gRPC exporter configured ({grpc_endpoint})")
    elif conn:
        if AzureMonitorTraceExporter is None:
            print(
                "Warning: Azure Monitor exporter not installed. "
                "Install with: pip install azure-monitor-opentelemetry-exporter",
            )
            tracer_provider.add_span_processor(
                BatchSpanProcessor(ConsoleSpanExporter())
            )
        else:
            tracer_provider.add_span_processor(
                BatchSpanProcessor(
                    AzureMonitorTraceExporter.from_connection_string(conn)
                )
            )
            print("[otel] Azure Monitor trace exporter configured")
    else:
        tracer_provider.add_span_processor(BatchSpanProcessor(ConsoleSpanExporter()))
        print("[otel] Console span exporter configured")
        print(
            "[otel] Set APPLICATION_INSIGHTS_CONNECTION_STRING to export to Application Insights "
            "instead of the console",
        )

    trace.set_tracer_provider(tracer_provider)

    OpenAIAgentsInstrumentor().instrument(
        tracer_provider=tracer_provider,
        capture_message_content="span_and_event",
        agent_name="Weekend Planner",
        base_url=base_url,
        system='azure.ai.openai',
    )


client = _get_openai_client()
_configure_otel(client.base_url)

set_default_openai_client(client)
set_tracing_disabled(False)

model = _get_model(client)

SUNNY_WEATHER_PROBABILITY = 0.05


@function_tool
def get_weather(city: str) -> dict[str, object]:
    """Fetch mock weather information for the requested city."""

    logger.info("Getting weather for %s", city)
    if random.random() < SUNNY_WEATHER_PROBABILITY:
        return {"city": city, "temperature": 72, "description": "Sunny"}
    return {"city": city, "temperature": 60, "description": "Rainy"}


@function_tool
def get_activities(city: str, date: str) -> list[dict[str, object]]:
    """Return mock activities for the supplied city and date."""

    logger.info("Getting activities for %s on %s", city, date)
    return [
        {"name": "Hiking", "location": city},
        {"name": "Beach", "location": city},
        {"name": "Museum", "location": city},
    ]


@function_tool
def get_current_date() -> str:
    """Return the current date as YYYY-MM-DD."""

    logger.info("Getting current date")
    return datetime.now().strftime("%Y-%m-%d")


ENGLISH_WEEKEND_PLANNER = Agent(
    name="Weekend Planner (English)",
    instructions=(
        "You help English-speaking travelers plan their weekends. "
        "Use the available tools to gather the weekend date, current weather, and local activities. "
        "Only recommend activities that align with the weather and include the date in your final response."
    ),
    tools=[get_weather, get_activities, get_current_date],
    model=model,
)

# cSpell:disable
SPANISH_WEEKEND_PLANNER = Agent(
    name="Planificador de fin de semana (Español)",
    instructions=(
        "Ayudas a viajeros hispanohablantes a planificar su fin de semana. "
        "Usa las herramientas disponibles para obtener la fecha, el clima y actividades locales. "
        "Recomienda actividades acordes al clima e incluye la fecha del fin de semana en tu respuesta."
    ),
    tools=[get_weather, get_activities, get_current_date],
    model=model,
)

TRIAGE_AGENT = Agent(
    name="Weekend Planner Triage",
    instructions=(
        "Revisa el idioma del viajero. "
        "Si el mensaje está en español, realiza un handoff a 'Planificador de fin de semana (Español)'. "
        "De lo contrario, usa 'Weekend Planner (English)'."
    ),
    handoffs=[SPANISH_WEEKEND_PLANNER, ENGLISH_WEEKEND_PLANNER],
    model=model,
)
# cSpell:enable


def _apply_weekend_semconv(
    client: AsyncOpenAI,
    model: OpenAIResponsesModel,
    span: trace.Span,
    *,
    user_text: str,
    final_text: str,
    conversation_id: str | None,
    response_id: str,
    final_agent_name: str | None,
    success: bool,
) -> None:
    parsed = urlparse(client.base_url)
    if parsed.hostname:
        span.set_attribute("server.address", parsed.hostname)
    if parsed.port:
        span.set_attribute("server.port", parsed.port)

    span.set_attribute("gen_ai.operation.name", "invoke_agent")
    span.set_attribute("gen_ai.provider.name", "azure.ai.openai")
    span.set_attribute("gen_ai.request.model", model.model)
    span.set_attribute("gen_ai.output.type", "text")
    span.set_attribute("gen_ai.response.model", model.model)
    span.set_attribute("gen_ai.response.id", response_id)
    span.set_attribute(
        "gen_ai.response.finish_reasons",
        ["stop"] if success else ["error"],
    )

    if conversation_id:
        span.set_attribute("gen_ai.conversation.id", conversation_id)
    if TRIAGE_AGENT.instructions:
        span.set_attribute("gen_ai.system_instructions", TRIAGE_AGENT.instructions)
    if final_agent_name:
        span.set_attribute("gen_ai.agent.name", final_agent_name)
    else:
        span.set_attribute("gen_ai.agent.name", TRIAGE_AGENT.name)
    if user_text:
        span.set_attribute(
            "gen_ai.input.messages",
            json.dumps([{"role": "user", "content": user_text}]),
        )
    if final_text:
        span.set_attribute(
            "gen_ai.output.messages",
            json.dumps([{"role": "assistant", "content": final_text}]),
        )


def _extract_user_text(request: CreateResponse) -> str:
    """Extract the first user text input from the request body."""

    input = request.get("input")
    if not input:
        return ""

    first = input[0]
    content = first.get("content", None) if isinstance(first, dict) else first
    if isinstance(content, str):
        return content

    if isinstance(content, list):
        for item in content:
            text = item.get("text", None)
            if text:
                return text
    return ""


def _stream_final_text(final_text: str, context: AgentRunContext):
    """Yield streaming events for the provided final text."""

    async def _async_stream():
        assembled = ""
        yield ResponseCreatedEvent(response=OpenAIResponse(output=[]))
        item_id = context.id_generator.generate_message_id()
        yield ResponseOutputItemAddedEvent(
            output_index=0,
            item=ResponsesAssistantMessageItemResource(
                id=item_id,
                status="in_progress",
                content=[ItemContentOutputText(text="", annotations=[])],
            ),
        )

        words = final_text.split(" ")
        for idx, token in enumerate(words):
            piece = token if idx == len(words) - 1 else token + " "
            assembled += piece
            yield ResponseTextDeltaEvent(output_index=0, content_index=0, delta=piece)

        yield ResponseTextDoneEvent(output_index=0, content_index=0, text=assembled)
        yield ResponseCompletedEvent(
            response=OpenAIResponse(
                metadata={},
                temperature=0.0,
                top_p=0.0,
                user="user",
                id=context.response_id,
                created_at=datetime.now(timezone.utc),
                output=[
                    ResponsesAssistantMessageItemResource(
                        id=item_id,
                        status="completed",
                        content=[ItemContentOutputText(text=assembled, annotations=[])],
                    )
                ],
            )
        )

    return _async_stream()


def dump(title: str, payload: object) -> None:
    """Pretty print helper for the tracing demo."""

    print(f"\n=== {title} ===")
    print(json.dumps(payload, indent=2))



class WeekendPlannerContainer(FoundryCBAgent):
    """Container entry point that surfaces the weekend planner agent via FoundryCBAgent."""

    def __init__(self, client: AsyncOpenAI = client, model: OpenAIResponsesModel = model) -> None:
        super().__init__()
        self._client = client
        self._model = model

    @tracer.start_as_current_span("weekend_planner_session")
    async def agent_run(self, context: AgentRunContext):
        request = context.request
        user_text = _extract_user_text(request)

        span = trace.get_current_span()
        span.set_attribute("user.request", user_text)
        span.set_attribute("api.host", "azure")
        span.set_attribute("model.name", self._model.model_name)
        span.set_attribute("agent.name", TRIAGE_AGENT.name)
        span.set_attribute("triage.languages", "en,es")

        try:
            result = await Runner.run(TRIAGE_AGENT, input=user_text)
            final_text = str(result.final_output or "")
            span.set_attribute(
                "agent.response", final_text[:500] if final_text else ""
            )
            final_agent = getattr(result, "last_agent", None)
            if final_agent and getattr(final_agent, "name", None):
                span.set_attribute("agent.final", final_agent.name)
            span.set_attribute("request.success", True)
            _apply_weekend_semconv(
                self._client,
                self._model,
                span,
                user_text=user_text,
                final_text=final_text,
                conversation_id=context.conversation_id,
                response_id=context.response_id,
                final_agent_name=getattr(final_agent, "name", None),
                success=True,
            )
            logger.info("Weekend planning completed successfully")
        except Exception as exc:  # pragma: no cover - defensive logging path
            span.record_exception(exc)
            span.set_attribute("request.success", False)
            span.set_attribute("error.type", exc.__class__.__name__)
            logger.error("Error during weekend planning: %s", exc)
            final_text = f"Error running agent: {exc}"
            _apply_weekend_semconv(
                self._client,
                self._model,
                span,
                user_text=user_text,
                final_text=final_text,
                conversation_id=context.conversation_id,
                response_id=context.response_id,
                final_agent_name=None,
                success=False,
            )

        if request.get("stream", False):
            return _stream_final_text(final_text, context)

        response = OpenAIResponse(
            metadata={},
            temperature=0.0,
            top_p=0.0,
            user="user",
            id=context.response_id,
            created_at=datetime.now(timezone.utc),
            output=[
                ResponsesAssistantMessageItemResource(
                    id=context.id_generator.generate_message_id(),
                    status="completed",
                    content=[ItemContentOutputText(text=final_text, annotations=[])],
                )
            ],
        )
        return response


if __name__ == "__main__":
    logger.setLevel(logging.INFO)
    try:
        WeekendPlannerContainer(client=client, model=model).run()
    finally:
        trace.get_tracer_provider().shutdown()
