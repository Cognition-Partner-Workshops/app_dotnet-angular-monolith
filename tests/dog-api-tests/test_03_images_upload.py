"""
Test suite for The Dog API - Image Upload and Management endpoints.

Endpoints:
    GET  /images/              - List your uploaded images (requires API key)
    POST /images/upload        - Upload an image (requires API key)
    DEL  /images/:image_id     - Delete an uploaded image (requires API key)
"""

import io
import pytest


def _create_tiny_png():
    """Create a minimal valid PNG file in memory (1x1 pixel, red)."""
    # Minimal 1x1 red PNG
    import struct
    import zlib

    def _chunk(chunk_type, data):
        c = chunk_type + data
        crc = struct.pack(">I", zlib.crc32(c) & 0xFFFFFFFF)
        return struct.pack(">I", len(data)) + c + crc

    signature = b"\x89PNG\r\n\x1a\n"
    ihdr_data = struct.pack(">IIBBBBB", 1, 1, 8, 2, 0, 0, 0)
    ihdr = _chunk(b"IHDR", ihdr_data)
    raw_data = b"\x00\xff\x00\x00"  # filter byte + RGB
    idat = _chunk(b"IDAT", zlib.compress(raw_data))
    iend = _chunk(b"IEND", b"")
    return signature + ihdr + idat + iend


@pytest.mark.images
@pytest.mark.requires_api_key
class TestImageUpload:
    """Tests for POST /images/upload."""

    def test_upload_image_success(self, session, base_url, api_key, sub_id):
        """Verify uploading a valid image returns success."""
        png_data = _create_tiny_png()
        files = {"file": ("test_dog.png", io.BytesIO(png_data), "image/png")}
        data = {"sub_id": sub_id}
        resp = session.post(
            f"{base_url}/images/upload",
            headers={"x-api-key": api_key},
            files=files,
            data=data,
        )
        assert resp.status_code in [200, 201], f"Upload failed: {resp.text}"
        result = resp.json()
        assert "id" in result
        assert "url" in result

    def test_upload_image_with_breed_ids(self, session, base_url, api_key, sub_id, sample_breed_id):
        """Verify uploading an image with breed_ids."""
        png_data = _create_tiny_png()
        files = {"file": ("test_breed_dog.png", io.BytesIO(png_data), "image/png")}
        data = {"sub_id": sub_id, "breed_ids": str(sample_breed_id)}
        resp = session.post(
            f"{base_url}/images/upload",
            headers={"x-api-key": api_key},
            files=files,
            data=data,
        )
        assert resp.status_code in [200, 201], f"Upload with breed failed: {resp.text}"

    @pytest.mark.negative
    def test_upload_without_api_key(self, session, base_url):
        """Verify uploading without API key returns 401."""
        png_data = _create_tiny_png()
        files = {"file": ("test.png", io.BytesIO(png_data), "image/png")}
        resp = session.post(
            f"{base_url}/images/upload",
            headers={"Content-Type": "multipart/form-data"},
            files=files,
        )
        assert resp.status_code in [400, 401, 403]

    @pytest.mark.negative
    def test_upload_without_file(self, session, base_url, api_key):
        """Verify uploading without a file returns an error."""
        resp = session.post(
            f"{base_url}/images/upload",
            headers={"x-api-key": api_key},
            data={"sub_id": "test"},
        )
        assert resp.status_code in [400, 422]


@pytest.mark.images
@pytest.mark.requires_api_key
class TestListUploadedImages:
    """Tests for GET /images/ (user's uploaded images)."""

    def test_list_uploaded_images(self, session, base_url, auth_headers):
        """Verify listing uploaded images returns 200."""
        resp = session.get(f"{base_url}/images/", headers=auth_headers)
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)

    def test_list_uploaded_images_pagination(self, session, base_url, auth_headers):
        """Verify pagination works for uploaded images."""
        resp = session.get(
            f"{base_url}/images/",
            headers=auth_headers,
            params={"limit": 2, "page": 0},
        )
        assert resp.status_code == 200
        data = resp.json()
        assert isinstance(data, list)
        assert len(data) <= 2

    @pytest.mark.negative
    def test_list_uploaded_images_no_auth(self, session, base_url, headers):
        """Verify listing images without API key returns error or empty."""
        resp = session.get(f"{base_url}/images/", headers=headers)
        assert resp.status_code in [200, 400, 401]


@pytest.mark.images
@pytest.mark.requires_api_key
class TestDeleteImage:
    """Tests for DELETE /images/:image_id."""

    def test_delete_uploaded_image(self, session, base_url, api_key, sub_id):
        """Upload an image, then delete it successfully."""
        # Upload first
        png_data = _create_tiny_png()
        files = {"file": ("delete_test.png", io.BytesIO(png_data), "image/png")}
        upload_resp = session.post(
            f"{base_url}/images/upload",
            headers={"x-api-key": api_key},
            files=files,
            data={"sub_id": sub_id},
        )
        assert upload_resp.status_code in [200, 201]
        image_id = upload_resp.json()["id"]

        # Delete
        del_resp = session.delete(
            f"{base_url}/images/{image_id}",
            headers={"Content-Type": "application/json", "x-api-key": api_key},
        )
        assert del_resp.status_code in [200, 204]

    @pytest.mark.negative
    def test_delete_nonexistent_image(self, session, base_url, api_key):
        """Verify deleting a non-existent image returns an error."""
        resp = session.delete(
            f"{base_url}/images/nonexistent-image-id",
            headers={"Content-Type": "application/json", "x-api-key": api_key},
        )
        assert resp.status_code in [400, 404]

    @pytest.mark.negative
    def test_delete_without_api_key(self, session, base_url, headers):
        """Verify deleting without API key returns 401."""
        resp = session.delete(
            f"{base_url}/images/some-image-id", headers=headers
        )
        assert resp.status_code in [400, 401, 403]
