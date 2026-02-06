from contextlib import closing
from typing import Generator, Any, AsyncGenerator, cast

from azure.ai.agentserver.core import AgentRunContext, FoundryCBAgent
from azure.ai.agentserver.core.models import Response as OpenAIResponse, ResponseStreamEvent
from azure.ai.projects.aio import AIProjectClient
from openai import AsyncStream


class Agent(FoundryCBAgent):
    """An simple example agent that proxies requests to an OpenAI-compatible endpoint"""
    _client: AIProjectClient
    _model: str

    def __init__(self, client: AIProjectClient, model_deployment_name: str) -> None:
        super().__init__()
        self._client = client
        self._model = model_deployment_name

    async def agent_run(
        self, context: AgentRunContext
    ) -> OpenAIResponse | Generator[ResponseStreamEvent, Any, Any] | AsyncGenerator[ResponseStreamEvent, Any]:
        # Nothing special here, just pass through requests to the OpenAI client
        openai_client = self._client.get_openai_client()
        result = await openai_client.responses.create(
            stream=context.stream,
            instructions="Be helpful and concise.",
            model=self._model,
            input=context.request.get('input', ''),
            background=context.request.get('background'),
        )
        if context.stream:
            result = cast(AsyncStream[ResponseStreamEvent], result)
            # can't mix return/AsyncGenerator syntax in same function
            async def _stream_response() -> AsyncGenerator[ResponseStreamEvent, Any]:
                with closing(result):
                    async for event in result:
                        yield event
            return _stream_response()
        else:
            return cast(OpenAIResponse, result)

