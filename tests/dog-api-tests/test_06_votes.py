"""
Test suite for The Dog API - Votes endpoints.

Endpoints:
    GET    /votes            - List all votes
    GET    /votes/:vote_id   - Get a specific vote
    POST   /votes            - Create a vote
    DELETE /votes/:vote_id   - Delete a vote
"""

import pytest


@pytest.mark.votes
@pytest.mark.requires_api_key
class TestCreateVote:
    """Tests for POST /votes."""

    def test_create_upvote(self, session, base_url, auth_headers, sample_image_id, sub_id):
        """Verify creating an upvote (value=1) returns success."""
        resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id, "value": 1},
        )
        assert resp.status_code in [200, 201], f"Create upvote failed: {resp.text}"
        data = resp.json()
        assert "id" in data
        # Cleanup
        session.delete(f"{base_url}/votes/{data['id']}", headers=auth_headers)

    def test_create_downvote(self, session, base_url, auth_headers, sample_image_id, sub_id):
        """Verify creating a downvote (value=0) returns success."""
        resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id, "value": 0},
        )
        assert resp.status_code in [200, 201], f"Create downvote failed: {resp.text}"
        data = resp.json()
        assert "id" in data
        # Cleanup
        session.delete(f"{base_url}/votes/{data['id']}", headers=auth_headers)

    @pytest.mark.negative
    def test_create_vote_missing_image_id(self, session, base_url, auth_headers, sub_id):
        """Verify creating a vote without image_id returns error."""
        resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"sub_id": sub_id, "value": 1},
        )
        assert resp.status_code in [400, 422]

    @pytest.mark.negative
    def test_create_vote_invalid_image_id(self, session, base_url, auth_headers, sub_id):
        """Verify creating a vote with invalid image_id returns error."""
        resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": "nonexistent-xyz", "sub_id": sub_id, "value": 1},
        )
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_create_vote_without_api_key(self, session, base_url, headers, sample_image_id):
        """Verify creating a vote without API key returns 401."""
        resp = session.post(
            f"{base_url}/votes",
            headers=headers,
            json={"image_id": sample_image_id, "sub_id": "test", "value": 1},
        )
        assert resp.status_code in [400, 401, 403]


@pytest.mark.votes
@pytest.mark.requires_api_key
class TestListVotes:
    """Tests for GET /votes."""

    def test_list_votes_returns_200(self, session, base_url, auth_headers):
        """Verify GET /votes returns 200."""
        resp = session.get(f"{base_url}/votes", headers=auth_headers)
        assert resp.status_code == 200

    def test_list_votes_returns_array(self, session, base_url, auth_headers):
        """Verify response is a JSON array."""
        resp = session.get(f"{base_url}/votes", headers=auth_headers)
        data = resp.json()
        assert isinstance(data, list)

    def test_list_votes_after_create(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify a newly created vote appears in the list."""
        # Create
        create_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id, "value": 1},
        )
        assert create_resp.status_code in [200, 201]
        vote_id = create_resp.json()["id"]

        # List and verify
        list_resp = session.get(f"{base_url}/votes", headers=auth_headers)
        assert list_resp.status_code == 200
        vote_ids = [v["id"] for v in list_resp.json()]
        assert vote_id in vote_ids

        # Cleanup
        session.delete(f"{base_url}/votes/{vote_id}", headers=auth_headers)

    @pytest.mark.negative
    def test_list_votes_without_api_key(self, session, base_url, headers):
        """Verify listing votes without API key returns error."""
        resp = session.get(f"{base_url}/votes", headers=headers)
        assert resp.status_code in [400, 401, 403]


@pytest.mark.votes
@pytest.mark.requires_api_key
class TestGetVoteById:
    """Tests for GET /votes/:vote_id."""

    def test_get_vote_by_id(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify getting a specific vote by ID."""
        # Create
        create_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id, "value": 1},
        )
        assert create_resp.status_code in [200, 201]
        vote_id = create_resp.json()["id"]

        # Get by ID
        get_resp = session.get(
            f"{base_url}/votes/{vote_id}", headers=auth_headers
        )
        assert get_resp.status_code == 200
        data = get_resp.json()
        assert data["id"] == vote_id
        assert data["image_id"] == sample_image_id
        assert data["value"] == 1

        # Cleanup
        session.delete(f"{base_url}/votes/{vote_id}", headers=auth_headers)

    @pytest.mark.negative
    def test_get_vote_invalid_id(self, session, base_url, auth_headers):
        """Verify getting a non-existent vote returns error."""
        resp = session.get(f"{base_url}/votes/999999999", headers=auth_headers)
        assert resp.status_code in [400, 404]


@pytest.mark.votes
@pytest.mark.requires_api_key
class TestDeleteVote:
    """Tests for DELETE /votes/:vote_id."""

    def test_delete_vote_success(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify deleting a vote returns success."""
        # Create
        create_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id, "value": 1},
        )
        assert create_resp.status_code in [200, 201]
        vote_id = create_resp.json()["id"]

        # Delete
        del_resp = session.delete(
            f"{base_url}/votes/{vote_id}", headers=auth_headers
        )
        assert del_resp.status_code == 200

    def test_delete_vote_verify_removal(
        self, session, base_url, auth_headers, sample_image_id, sub_id
    ):
        """Verify deleted vote no longer appears in listing."""
        # Create
        create_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": sample_image_id, "sub_id": sub_id, "value": 1},
        )
        assert create_resp.status_code in [200, 201]
        vote_id = create_resp.json()["id"]

        # Delete
        session.delete(f"{base_url}/votes/{vote_id}", headers=auth_headers)

        # Verify removal
        get_resp = session.get(
            f"{base_url}/votes/{vote_id}", headers=auth_headers
        )
        assert get_resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_delete_vote_invalid_id(self, session, base_url, auth_headers):
        """Verify deleting a non-existent vote returns error."""
        resp = session.delete(
            f"{base_url}/votes/999999999", headers=auth_headers
        )
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_delete_vote_without_api_key(self, session, base_url, headers):
        """Verify deleting without API key returns error."""
        resp = session.delete(f"{base_url}/votes/1", headers=headers)
        assert resp.status_code in [400, 401, 403]
