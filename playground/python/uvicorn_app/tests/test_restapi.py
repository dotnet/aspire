# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

"""Integration tests for the uvicorn app endpoints."""

import os

import httpx
import pytest


@pytest.fixture
def base_url():
    """Get the base URL from environment variable."""
    url = os.environ.get("UVICORNAPP_HTTP")
    if not url:
        pytest.skip("UVICORNAPP_HTTP environment variable not set")
    return url.rstrip("/")


def test_root_endpoint(base_url):
    """Test the root endpoint returns expected message."""
    response = httpx.get(f"{base_url}/")
    assert response.status_code == 200
    assert "API service is running" in response.text


def test_weatherforecast_endpoint(base_url):
    """Test the weatherforecast endpoint returns valid forecast data."""
    response = httpx.get(f"{base_url}/weatherforecast")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)
    assert len(data) == 5
    for item in data:
        assert "date" in item
        assert "temperatureC" in item
        assert "temperatureF" in item
        assert "summary" in item


def test_health_endpoint(base_url):
    """Test the health check endpoint returns healthy status."""
    response = httpx.get(f"{base_url}/health")
    assert response.status_code == 200
    assert response.text == "Healthy"
