"""
Shared fixtures and configuration for The Dog API test suite.
"""

import os
import pytest
import requests
from dotenv import load_dotenv

load_dotenv()

BASE_URL = "https://api.thedogapi.com/v1"
API_KEY = os.getenv("DOG_API_KEY", "")
SUB_ID = "test-user-dogapi"


@pytest.fixture(scope="session")
def base_url():
    """Base URL for The Dog API."""
    return BASE_URL


@pytest.fixture(scope="session")
def api_key():
    """API key for authenticated requests."""
    return API_KEY


@pytest.fixture(scope="session")
def headers():
    """Default headers without API key."""
    return {"Content-Type": "application/json"}


@pytest.fixture(scope="session")
def auth_headers():
    """Headers with API key for authenticated requests."""
    return {
        "Content-Type": "application/json",
        "x-api-key": API_KEY,
    }


@pytest.fixture(scope="session")
def sub_id():
    """Sub ID used to segment test data."""
    return SUB_ID


@pytest.fixture(scope="session")
def session():
    """Reusable requests session for performance."""
    with requests.Session() as s:
        yield s


@pytest.fixture(scope="session")
def sample_image_id(session, base_url, auth_headers):
    """Fetch a sample image ID from the search endpoint for use in tests."""
    resp = session.get(
        f"{base_url}/images/search",
        headers=auth_headers,
        params={"limit": 1, "has_breeds": "true"},
    )
    assert resp.status_code == 200
    data = resp.json()
    assert len(data) > 0, "No images returned from search"
    return data[0]["id"]


@pytest.fixture(scope="session")
def sample_breed(session, base_url, auth_headers):
    """Fetch a sample breed for use in tests."""
    resp = session.get(
        f"{base_url}/breeds",
        headers=auth_headers,
        params={"limit": 1},
    )
    assert resp.status_code == 200
    data = resp.json()
    assert len(data) > 0, "No breeds returned"
    return data[0]


@pytest.fixture(scope="session")
def sample_breed_id(sample_breed):
    """A single breed ID for convenience."""
    return sample_breed["id"]
