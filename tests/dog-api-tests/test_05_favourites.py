"""
Test suite for The Dog API - Favourites endpoints.

Endpoints:
    GET    /favourites               - List all favourites
    GET    /favourites/:favourite_id - Get a specific favourite
    POST   /favourites               - Create a favourite
    DELETE /favourites/:favourite_id - Delete a favourite
"""

import pytest


@pytest.mark.favourites
@pytest.mark.requires_api_key
class TestCreateFavourite:
    """Tests for POST /favourites."""

    def test_create_favourite_success(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify creating a favourite returns success with an ID."""
        resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id},
        )
        assert resp.status_code in [200, 201], f"Create favourite failed: {resp.text}"
        data = resp.json()
        assert "id" in data
        # Cleanup
        fav_id = data["id"]
        session.delete(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )

    @pytest.mark.negative
    def test_create_favourite_missing_image_id(self, session, base_url, auth_headers, sub_id):
        """Verify creating a favourite without image_id returns error."""
        resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"sub_id": sub_id},
        )
        assert resp.status_code in [400, 422]

    @pytest.mark.negative
    def test_create_favourite_invalid_image_id(self, session, base_url, auth_headers, sub_id):
        """Verify creating a favourite with invalid image_id returns error."""
        resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": "nonexistent-image-xyz", "sub_id": sub_id},
        )
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_create_favourite_without_api_key(self, session, base_url, headers, sample_image_id):
        """Verify creating a favourite without API key returns 401."""
        resp = session.post(
            f"{base_url}/favourites",
            headers=headers,
            json={"image_id": sample_image_id, "sub_id": "test"},
        )
        assert resp.status_code in [400, 401, 403]


@pytest.mark.favourites
@pytest.mark.requires_api_key
class TestListFavourites:
    """Tests for GET /favourites."""

    def test_list_favourites_returns_200(self, session, base_url, auth_headers):
        """Verify GET /favourites returns 200."""
        resp = session.get(f"{base_url}/favourites", headers=auth_headers)
        assert resp.status_code == 200

    def test_list_favourites_returns_array(self, session, base_url, auth_headers):
        """Verify response is a JSON array."""
        resp = session.get(f"{base_url}/favourites", headers=auth_headers)
        data = resp.json()
        assert isinstance(data, list)

    def test_list_favourites_after_create(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify a newly created favourite appears in the list."""
        # Create
        create_resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id},
        )
        assert create_resp.status_code in [200, 201]
        fav_id = create_resp.json()["id"]

        # List and verify
        list_resp = session.get(f"{base_url}/favourites", headers=auth_headers)
        assert list_resp.status_code == 200
        fav_ids = [f["id"] for f in list_resp.json()]
        assert fav_id in fav_ids

        # Cleanup
        session.delete(f"{base_url}/favourites/{fav_id}", headers=auth_headers)

    @pytest.mark.negative
    def test_list_favourites_without_api_key(self, session, base_url, headers):
        """Verify listing favourites without API key returns error."""
        resp = session.get(f"{base_url}/favourites", headers=headers)
        assert resp.status_code in [400, 401, 403]


@pytest.mark.favourites
@pytest.mark.requires_api_key
class TestGetFavouriteById:
    """Tests for GET /favourites/:favourite_id."""

    def test_get_favourite_by_id(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify getting a specific favourite by ID."""
        # Create
        create_resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id},
        )
        assert create_resp.status_code in [200, 201]
        fav_id = create_resp.json()["id"]

        # Get by ID
        get_resp = session.get(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert get_resp.status_code == 200
        data = get_resp.json()
        assert data["id"] == fav_id
        assert data["image_id"] == sample_image_id

        # Cleanup
        session.delete(f"{base_url}/favourites/{fav_id}", headers=auth_headers)

    @pytest.mark.negative
    def test_get_favourite_invalid_id(self, session, base_url, auth_headers):
        """Verify getting a non-existent favourite returns error."""
        resp = session.get(f"{base_url}/favourites/999999999", headers=auth_headers)
        assert resp.status_code in [400, 404]


@pytest.mark.favourites
@pytest.mark.requires_api_key
class TestDeleteFavourite:
    """Tests for DELETE /favourites/:favourite_id."""

    def test_delete_favourite_success(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify deleting a favourite returns success."""
        # Create
        create_resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id},
        )
        assert create_resp.status_code in [200, 201]
        fav_id = create_resp.json()["id"]

        # Delete
        del_resp = session.delete(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert del_resp.status_code == 200

    def test_delete_favourite_verify_removal(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify deleted favourite no longer appears in listing."""
        # Create
        create_resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id},
        )
        assert create_resp.status_code in [200, 201]
        fav_id = create_resp.json()["id"]

        # Delete
        session.delete(f"{base_url}/favourites/{fav_id}", headers=auth_headers)

        # Verify removal
        get_resp = session.get(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert get_resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_delete_favourite_invalid_id(self, session, base_url, auth_headers):
        """Verify deleting a non-existent favourite returns error."""
        resp = session.delete(
            f"{base_url}/favourites/999999999", headers=auth_headers
        )
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_delete_favourite_without_api_key(self, session, base_url, headers):
        """Verify deleting without API key returns error."""
        resp = session.delete(f"{base_url}/favourites/1", headers=headers)
        assert resp.status_code in [400, 401, 403]
