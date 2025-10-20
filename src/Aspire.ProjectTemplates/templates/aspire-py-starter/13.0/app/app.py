import contextlib
import datetime
import json
import logging
import os
import random
import telemetry
from typing import TYPE_CHECKING, Any

import fastapi
import fastapi.responses
import fastapi.staticfiles
import opentelemetry.instrumentation.fastapi as otel_fastapi
import opentelemetry.instrumentation.redis as otel_redis
import redis

@contextlib.asynccontextmanager
async def lifespan(app: fastapi.FastAPI):
    telemetry.configure_opentelemetry()
    yield

app = fastapi.FastAPI(lifespan=lifespan)
otel_fastapi.FastAPIInstrumentor.instrument_app(app, exclude_spans=["send"])

//#if UseRedisCache
# Initialize Redis client
redis_client: redis.Redis | None = None
otel_redis.RedisInstrumentor().instrument()

def get_redis_client() -> redis.Redis | None:
    """Get the Redis client instance."""
    global redis_client
    if redis_client is None:
        if cache_uri := os.environ.get("CACHE_URI"):
            try:
                redis_client = redis.from_url(
                    cache_uri,
                    socket_connect_timeout=5,
                    socket_timeout=5,
                    decode_responses=True,
                )
            except Exception as e:
                logger.warning(f"Failed to connect to Redis: {e}")
                redis_client = None
        else:
            logger.info(
                "No CACHE_URI environment variable found, Redis caching disabled"
            )
    return redis_client

//#endif
logger = logging.getLogger(__name__)


@app.get("/api/weatherforecast", response_model=list[dict[str, Any]])
//#if UseRedisCache
async def weather_forecast(redis_client = fastapi.Depends(get_redis_client)):
    """Weather forecast endpoint."""
    cache_key = "weatherforecast"
    cache_ttl = 5  # 5 seconds cache duration

    # Try to get data from cache.
    if redis_client:
        try:
            cached_data = redis_client.get(cache_key)
            if cached_data:
                logger.info("Returning cached weather forecast data")
                return json.loads(cached_data)
        except Exception as e:
            logger.warning(f"Redis cache read error: {e}")

//#else
async def weather_forecast():
    """Weather forecast endpoint."""
//#endif
    # Generate fresh data if not in cache or cache unavailable.
    summaries = [
        "Freezing",
        "Bracing",
        "Chilly",
        "Cool",
        "Mild",
        "Warm",
        "Balmy",
        "Hot",
        "Sweltering",
        "Scorching",
    ]

    forecast = []
    for index in range(1, 6):  # Range 1 to 5 (inclusive)
        temp_c = random.randint(-20, 55)
        forecast_date = datetime.datetime.now() + datetime.timedelta(days=index)
        forecast_item = {
            "date": forecast_date.isoformat(),
            "temperatureC": temp_c,
            "temperatureF": int(temp_c * 9 / 5) + 32,
            "summary": random.choice(summaries),
        }
        forecast.append(forecast_item)

//#if UseRedisCache
    # Cache the data
    if redis_client:
        try:
            redis_client.setex(cache_key, cache_ttl, json.dumps(forecast))
        except Exception as e:
            logger.warning(f"Redis cache write error: {e}")

//#endif
    return forecast


@app.get("/health", response_class=fastapi.responses.PlainTextResponse)
async def health_check(redis_client = fastapi.Depends(get_redis_client)):
    """Health check endpoint."""
//#if UseRedisCache
    if redis_client:
        redis_client.ping()
//#endif
    return "Healthy"


# Serve static files directly from root, if the "static" directory exists
if os.path.exists("static"):
    app.mount("/", fastapi.staticfiles.StaticFiles(directory="static", html=True), name="static")


if __name__ == "__main__":
    import uvicorn

    port = int(os.environ.get("PORT", 8111))
    host = os.environ.get("HOST", "127.0.0.1")
    reload = os.environ.get("DEBUG", "False").lower() == "true"

    uvicorn.run("app:app", host=host, port=port, reload=reload, log_level="info")
