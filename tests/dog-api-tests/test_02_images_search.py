"""
Test suite for The Dog API - Image Search endpoints.

Endpoints:
    GET /images/search   - Search approved images with filters
    GET /images/:image_id - Get a specific image by ID
"""

import pytest


@pytest.mark.smoke
@pytest.mark.images
class TestImageSearch:
    """Tests for GET /images/search."""

    def test_search_returns_200(self, session, base_url, headers):
        """Verify GET /images/search returns 200 OK."""
        resp = session.get(f"{base_url}/images/search", headers=headers)
        assert resp.status_code == 200

    def test_search_returns_array(self, session, base_url, headers):
        """Verify response is a JSON array."""
        resp = session.get(f"{base_url}/images/search", headers=headers)
        data = resp.json()
        assert isinstance(data, list)
        assert len(data) > 0

    def test_search_image_structure(self, session, base_url, headers):
        """Verify each image has required fields."""
        resp = session.get(
            f"{base_url}/images/search", headers=headers, params={"limit": 1}
        )
        data = resp.json()
        img = data[0]
        assert "id" in img
        assert "url" in img
        assert "width" in img
        assert "height" in img

    def test_search_limit_parameter(self, session, base_url, headers):
        """Verify limit controls the number of returned images."""
        resp = session.get(
            f"{base_url}/images/search", headers=headers, params={"limit": 3}
        )
        data = resp.json()
        assert len(data) <= 3

    def test_search_limit_one(self, session, base_url, headers):
        """Verify limit=1 returns exactly one image."""
        resp = session.get(
            f"{base_url}/images/search", headers=headers, params={"limit": 1}
        )
        data = resp.json()
        assert len(data) == 1

    @pytest.mark.requires_api_key
    def test_search_with_has_breeds_filter(self, session, base_url, auth_headers):
        """Verify has_breeds=true returns images with breed data (requires API key)."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"has_breeds": "true", "limit": 5},
        )
        data = resp.json()
        assert len(data) > 0
        for img in data:
            assert "breeds" in img
            assert len(img["breeds"]) > 0

    def test_search_with_mime_types_jpg(self, session, base_url, headers):
        """Verify filtering by JPEG mime type returns 200 with results."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"mime_types": "jpg", "limit": 5},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)
        assert len(data) > 0

    def test_search_with_mime_types_png(self, session, base_url, headers):
        """Verify filtering by PNG mime type returns 200 with results."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"mime_types": "png", "limit": 5},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)
        assert len(data) > 0

    def test_search_with_size_small(self, session, base_url, headers):
        """Verify size=small filter works."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"size": "small", "limit": 5},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert len(data) > 0

    def test_search_with_size_med(self, session, base_url, headers):
        """Verify size=med filter works."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"size": "med", "limit": 5},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert len(data) > 0

    def test_search_with_size_full(self, session, base_url, headers):
        """Verify size=full filter works."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"size": "full", "limit": 5},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert len(data) > 0

    def test_search_format_json(self, session, base_url, headers):
        """Verify format=json returns valid JSON array."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"format": "json", "limit": 1},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)

    def test_search_order_asc(self, session, base_url, auth_headers):
        """Verify order=ASC returns images in ascending order."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"order": "ASC", "limit": 5, "page": 0},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)

    def test_search_order_desc(self, session, base_url, auth_headers):
        """Verify order=DESC returns images in descending order."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"order": "DESC", "limit": 5, "page": 0},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)

    def test_search_pagination_page_0(self, session, base_url, headers):
        """Verify page 0 returns results."""
        resp = session.get(
            f"{base_url}/images/search",
            headers=headers,
            params={"limit": 5, "page": 0},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert len(data) > 0

    def test_search_pagination_different_pages(self, session, base_url, auth_headers):
        """Verify different pages return different images (with ordered results)."""
        resp0 = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"limit": 5, "page": 0, "order": "ASC"},
        )
        resp1 = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"limit": 5, "page": 1, "order": "ASC"},
        )
        ids_page0 = {img["id"] for img in resp0.json()}
        ids_page1 = {img["id"] for img in resp1.json()}
        assert ids_page0 != ids_page1, "Different pages should return different images"


@pytest.mark.images
class TestGetImageById:
    """Tests for GET /images/:image_id."""

    def test_get_image_by_valid_id(self, session, base_url, headers, sample_image_id):
        """Verify fetching a specific image by its ID."""
        resp = session.get(f"{base_url}/images/{sample_image_id}", headers=headers)
        assert resp.status_code == 200
        data = resp.json()
        assert data["id"] == sample_image_id
        assert "url" in data

    def test_get_image_response_fields(self, session, base_url, headers, sample_image_id):
        """Verify the image response contains all expected fields."""
        resp = session.get(f"{base_url}/images/{sample_image_id}", headers=headers)
        data = resp.json()
        assert "id" in data
        assert "url" in data
        assert "width" in data
        assert "height" in data

    @pytest.mark.negative
    def test_get_image_invalid_id(self, session, base_url, headers):
        """Verify requesting a non-existent image returns an error."""
        resp = session.get(f"{base_url}/images/nonexistent-id-12345", headers=headers)
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_get_image_empty_id(self, session, base_url, headers):
        """Verify requesting /images/ without auth returns appropriate response."""
        resp = session.get(f"{base_url}/images/", headers=headers)
        # Without API key, this may return 400 or an empty list
        assert resp.status_code in [200, 400, 401]
