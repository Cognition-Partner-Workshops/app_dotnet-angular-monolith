"""
Test suite for The Dog API - Breeds endpoints.

Endpoints:
    GET /breeds          - List all breeds with pagination
    GET /breeds/:breed_id - Get a specific breed by ID
"""

import pytest


@pytest.mark.smoke
@pytest.mark.breeds
@pytest.mark.requires_api_key
class TestListBreeds:
    """Tests for GET /breeds."""

    def test_list_breeds_returns_200(self, session, base_url, auth_headers):
        """Verify GET /breeds returns 200 OK."""
        resp = session.get(f"{base_url}/breeds", headers=auth_headers)
        assert resp.status_code == 200

    def test_list_breeds_returns_array(self, session, base_url, auth_headers):
        """Verify response is a non-empty JSON array."""
        resp = session.get(f"{base_url}/breeds", headers=auth_headers)
        data = resp.json()
        assert isinstance(data, list)
        assert len(data) > 0

    def test_list_breeds_default_fields(self, session, base_url, auth_headers):
        """Verify each breed object contains expected fields."""
        resp = session.get(f"{base_url}/breeds", headers=auth_headers, params={"limit": 1})
        data = resp.json()
        breed = data[0]
        assert "id" in breed
        assert "name" in breed

    def test_list_breeds_pagination_limit(self, session, base_url, auth_headers):
        """Verify limit parameter constrains the number of results."""
        resp = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5}
        )
        data = resp.json()
        assert len(data) <= 5

    def test_list_breeds_pagination_page(self, session, base_url, auth_headers):
        """Verify page parameter returns different results."""
        resp_page0 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5, "page": 0}
        )
        resp_page1 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5, "page": 1}
        )
        data_page0 = resp_page0.json()
        data_page1 = resp_page1.json()
        assert data_page0 != data_page1, "Page 0 and Page 1 should return different breeds"

    def test_list_breeds_pagination_consistency(self, session, base_url, auth_headers):
        """Verify that fetching all breeds with small pages covers the same breeds."""
        all_breeds = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 200}
        ).json()
        page0 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 10, "page": 0}
        ).json()
        page1 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 10, "page": 1}
        ).json()
        # First 10 from all should match page 0
        assert [b["id"] for b in page0] == [b["id"] for b in all_breeds[:10]]
        assert [b["id"] for b in page1] == [b["id"] for b in all_breeds[10:20]]


@pytest.mark.breeds
@pytest.mark.requires_api_key
class TestGetBreedById:
    """Tests for GET /breeds/:breed_id."""

    def test_get_breed_by_valid_id(self, session, base_url, auth_headers, sample_breed_id):
        """Verify fetching a breed by a known valid ID."""
        resp = session.get(f"{base_url}/breeds/{sample_breed_id}", headers=auth_headers)
        assert resp.status_code == 200
        data = resp.json()
        assert data["id"] == sample_breed_id

    def test_get_breed_response_structure(self, session, base_url, auth_headers, sample_breed_id):
        """Verify the breed response contains key fields."""
        resp = session.get(f"{base_url}/breeds/{sample_breed_id}", headers=auth_headers)
        data = resp.json()
        assert "id" in data
        assert "name" in data

    @pytest.mark.negative
    def test_get_breed_invalid_id_returns_error(self, session, base_url, auth_headers):
        """Verify requesting a non-existent breed ID returns an error status."""
        resp = session.get(f"{base_url}/breeds/999999", headers=auth_headers)
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_get_breed_string_id_returns_error(self, session, base_url, auth_headers):
        """Verify requesting a breed with a string ID returns an error."""
        resp = session.get(f"{base_url}/breeds/invalid-id", headers=auth_headers)
        assert resp.status_code in [400, 404]
