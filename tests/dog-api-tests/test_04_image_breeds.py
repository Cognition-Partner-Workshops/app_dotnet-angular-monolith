"""
Test suite for The Dog API - Image Breed Tagging endpoints.

Endpoints:
    GET  /images/:image_id/breeds             - Get breeds for an image
    POST /images/:image_id/breeds             - Add a breed to an image
    DEL  /images/:image_id/breeds/:breed_id   - Remove a breed from an image
"""

import io
import struct
import zlib
import pytest


def _create_tiny_png():
    """Create a minimal valid PNG file in memory."""
    def _chunk(chunk_type, data):
        c = chunk_type + data
        crc = struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF)
        return struct.pack(">I", len(data)) + c + crc

    signature = b"\x89PNG\r\n\x1a\n"
    ihdr_data = struct.pack(">IIBBBBB", 1, 1, 8, 2, 0, 0, 0)
    ihdr = _chunk(b"IHDR", ihdr_data)
    raw_data = b"\x00\xff\x00\x00"
    idat = _chunk(b"IDAT", zlib.compress(raw_data))
    iend = _chunk(b"IEND", b"")
    return signature + ihdr + idat + iend


@pytest.fixture()
def uploaded_image(session, base_url, api_key, sub_id):
    """Upload an image and return its ID. Clean up after the test."""
    png_data = _create_tiny_png()
    files = {"file": ("breed_tag_test.png", io.BytesIO(png_data), "image/png")}
    resp = session.post(
        f"{base_url}/images/upload",
        headers={"x-api-key": api_key},
        files=files,
        data={"sub_id": sub_id},
    )
    assert resp.status_code in [200, 201], f"Upload failed: {resp.text}"
    image_id = resp.json()["id"]
    yield image_id
    # Cleanup
    session.delete(
        f"{base_url}/images/{image_id}",
        headers={"Content-Type": "application/json", "x-api-key": api_key},
    )


@pytest.mark.images
@pytest.mark.requires_api_key
class TestGetImageBreeds:
    """Tests for GET /images/:image_id/breeds."""

    def test_get_breeds_for_image(self, session, base_url, auth_headers, sample_image_id):
        """Verify getting breeds for a known image returns 200."""
        resp = session.get(
            f"{base_url}/images/{sample_image_id}/breeds", headers=auth_headers
        )
        assert resp.status_code == 200

    @pytest.mark.negative
    def test_get_breeds_for_invalid_image(self, session, base_url, auth_headers):
        """Verify getting breeds for a non-existent image returns error."""
        resp = session.get(
            f"{base_url}/images/nonexistent-id/breeds", headers=auth_headers
        )
        assert resp.status_code in [400, 404]


@pytest.mark.images
@pytest.mark.requires_api_key
class TestAddBreedToImage:
    """Tests for POST /images/:image_id/breeds."""

    def test_add_breed_to_uploaded_image(
        self, session, base_url, auth_headers, uploaded_image, sample_breed_id
    ):
        """Verify adding a breed to an uploaded image."""
        resp = session.post(
            f"{base_url}/images/{uploaded_image}/breeds",
            headers=auth_headers,
            json={"breed_id": sample_breed_id},
        )
        # Some API versions may return different codes
        assert resp.status_code in [200, 201, 204], f"Add breed failed: {resp.text}"

    @pytest.mark.negative
    def test_add_breed_without_api_key(self, session, base_url, headers, sample_image_id):
        """Verify adding breed without API key fails."""
        resp = session.post(
            f"{base_url}/images/{sample_image_id}/breeds",
            headers=headers,
            json={"breed_id": 1},
        )
        assert resp.status_code in [400, 401, 403]

    @pytest.mark.negative
    def test_add_invalid_breed_to_image(
        self, session, base_url, auth_headers, uploaded_image
    ):
        """Verify adding a non-existent breed returns error."""
        resp = session.post(
            f"{base_url}/images/{uploaded_image}/breeds",
            headers=auth_headers,
            json={"breed_id": 999999},
        )
        assert resp.status_code in [400, 404]


@pytest.mark.images
@pytest.mark.requires_api_key
class TestRemoveBreedFromImage:
    """Tests for DELETE /images/:image_id/breeds/:breed_id."""

    def test_add_then_remove_breed(
        self, session, base_url, auth_headers, uploaded_image, sample_breed_id
    ):
        """Verify adding and then removing a breed from an image."""
        # Add breed
        add_resp = session.post(
            f"{base_url}/images/{uploaded_image}/breeds",
            headers=auth_headers,
            json={"breed_id": sample_breed_id},
        )
        assert add_resp.status_code in [200, 201, 204], f"Add breed failed: {add_resp.text}"

        # Remove breed
        del_resp = session.delete(
            f"{base_url}/images/{uploaded_image}/breeds/{sample_breed_id}",
            headers=auth_headers,
        )
        assert del_resp.status_code in [200, 204], f"Remove breed failed: {del_resp.text}"

    @pytest.mark.negative
    def test_remove_breed_without_api_key(self, session, base_url, headers, sample_image_id):
        """Verify removing breed without API key fails."""
        resp = session.delete(
            f"{base_url}/images/{sample_image_id}/breeds/1",
            headers=headers,
        )
        assert resp.status_code in [400, 401, 403]
