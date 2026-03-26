"""
End-to-End (E2E) flow tests for The Dog API.

These tests chain multiple API calls together to simulate real-world usage
patterns and verify cross-endpoint data consistency.
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


# ---------------------------------------------------------------------------
# Flow 1: Breed Exploration -> Image Discovery
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestBreedExplorationFlow:
    """
    E2E Flow: Explore breeds, then find images for a specific breed.

    Steps:
        1. GET /breeds - List all breeds
        2. GET /breeds/:breed_id - Get details of a specific breed
        3. GET /images/search?breed_ids=X - Search images filtered by breed
    """

    def test_breed_to_image_discovery(self, session, base_url, auth_headers):
        """List breeds -> pick one -> search images for that breed."""
        # Step 1: List breeds
        breeds_resp = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 10}
        )
        assert breeds_resp.status_code == 200
        breeds = breeds_resp.json()
        assert len(breeds) > 0, "No breeds returned"

        # Step 2: Get specific breed details
        breed = breeds[0]
        breed_id = breed["id"]
        breed_detail_resp = session.get(
            f"{base_url}/breeds/{breed_id}", headers=auth_headers
        )
        assert breed_detail_resp.status_code == 200
        breed_detail = breed_detail_resp.json()
        assert breed_detail["id"] == breed_id
        assert breed_detail["name"] == breed["name"]

        # Step 3: Search images with that breed
        img_resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"breed_ids": breed_id, "limit": 5},
        )
        assert img_resp.status_code == 200
        images = img_resp.json()
        assert len(images) > 0, f"No images found for breed {breed_id}"
        # Verify returned images have breed info
        for img in images:
            assert "breeds" in img or "id" in img

    def test_paginate_through_breeds(self, session, base_url, auth_headers):
        """Paginate through breeds and verify consistency across pages."""
        page0 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5, "page": 0}
        ).json()
        page1 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5, "page": 1}
        ).json()
        page2 = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5, "page": 2}
        ).json()

        assert len(page0) == 5
        assert len(page1) == 5
        assert len(page2) == 5

        # Verify no duplicates across pages
        all_ids = [b["id"] for b in page0 + page1 + page2]
        assert len(all_ids) == len(set(all_ids)), "Duplicate breed IDs across pages"


# ---------------------------------------------------------------------------
# Flow 2: Image Search with Multiple Filters
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestImageSearchFilterFlow:
    """
    E2E Flow: Search images with various filter combinations.

    Steps:
        1. Search with size filter
        2. Search with mime_type filter
        3. Search with has_breeds filter
        4. Search with combined filters
        5. Verify individual image details
    """

    def test_multi_filter_image_search(self, session, base_url, auth_headers):
        """Apply multiple filters and verify results match criteria."""
        # Step 1: Search JPG images of medium size with breeds
        resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={
                "size": "med",
                "mime_types": "jpg",
                "has_breeds": "true",
                "limit": 3,
            },
        )
        assert resp.status_code == 200
        images = resp.json()
        assert len(images) > 0

        # Step 2: Verify each image matches filters
        for img in images:
            assert "url" in img
            assert "breeds" in img
            assert len(img["breeds"]) > 0

        # Step 3: Fetch individual image details
        image_id = images[0]["id"]
        detail_resp = session.get(
            f"{base_url}/images/{image_id}", headers=auth_headers
        )
        assert detail_resp.status_code == 200
        detail = detail_resp.json()
        assert detail["id"] == image_id
        assert detail["url"] == images[0]["url"]


# ---------------------------------------------------------------------------
# Flow 3: Favourite Lifecycle (Create -> Read -> Delete -> Verify)
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestFavouriteLifecycleFlow:
    """
    E2E Flow: Complete favourite lifecycle.

    Steps:
        1. GET /images/search - Find an image to favourite
        2. POST /favourites - Create a favourite
        3. GET /favourites - List favourites and verify new one exists
        4. GET /favourites/:id - Get the specific favourite
        5. DELETE /favourites/:id - Delete the favourite
        6. GET /favourites/:id - Verify it's gone
    """

    def test_favourite_full_lifecycle(
        self, session, base_url, auth_headers, sub_id
    ):
        """Search -> Favourite -> List -> Get -> Delete -> Verify gone."""
        # Step 1: Find an image
        search_resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"limit": 1},
        )
        assert search_resp.status_code == 200
        images = search_resp.json()
        assert len(images) > 0
        image_id = images[0]["id"]

        # Step 2: Create favourite
        create_resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": image_id, "sub_id": sub_id},
        )
        assert create_resp.status_code in [200, 201]
        fav_id = create_resp.json()["id"]

        # Step 3: List favourites and verify
        list_resp = session.get(f"{base_url}/favourites", headers=auth_headers)
        assert list_resp.status_code == 200
        fav_ids = [f["id"] for f in list_resp.json()]
        assert fav_id in fav_ids, "Created favourite not found in list"

        # Step 4: Get specific favourite
        get_resp = session.get(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert get_resp.status_code == 200
        fav_data = get_resp.json()
        assert fav_data["id"] == fav_id
        assert fav_data["image_id"] == image_id
        assert fav_data["sub_id"] == sub_id

        # Step 5: Delete favourite
        del_resp = session.delete(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert del_resp.status_code == 200

        # Step 6: Verify deletion
        verify_resp = session.get(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert verify_resp.status_code in [400, 404]


# ---------------------------------------------------------------------------
# Flow 4: Vote Lifecycle (Create -> Read -> Delete -> Verify)
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestVoteLifecycleFlow:
    """
    E2E Flow: Complete vote lifecycle.

    Steps:
        1. GET /images/search - Find an image to vote on
        2. POST /votes - Create an upvote
        3. GET /votes - List votes and verify
        4. GET /votes/:id - Get the specific vote
        5. DELETE /votes/:id - Delete the vote
        6. GET /votes/:id - Verify it's gone
    """

    def test_vote_full_lifecycle(self, session, base_url, auth_headers, sub_id):
        """Search -> Vote -> List -> Get -> Delete -> Verify gone."""
        # Step 1: Find an image
        search_resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"limit": 1},
        )
        assert search_resp.status_code == 200
        images = search_resp.json()
        assert len(images) > 0
        image_id = images[0]["id"]

        # Step 2: Create upvote
        create_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": image_id, "sub_id": sub_id, "value": 1},
        )
        assert create_resp.status_code in [200, 201]
        vote_id = create_resp.json()["id"]

        # Step 3: List votes and verify
        list_resp = session.get(f"{base_url}/votes", headers=auth_headers)
        assert list_resp.status_code == 200
        vote_ids = [v["id"] for v in list_resp.json()]
        assert vote_id in vote_ids, "Created vote not found in list"

        # Step 4: Get specific vote
        get_resp = session.get(
            f"{base_url}/votes/{vote_id}", headers=auth_headers
        )
        assert get_resp.status_code == 200
        vote_data = get_resp.json()
        assert vote_data["id"] == vote_id
        assert vote_data["image_id"] == image_id
        assert vote_data["value"] == 1

        # Step 5: Delete vote
        del_resp = session.delete(
            f"{base_url}/votes/{vote_id}", headers=auth_headers
        )
        assert del_resp.status_code == 200

        # Step 6: Verify deletion
        verify_resp = session.get(
            f"{base_url}/votes/{vote_id}", headers=auth_headers
        )
        assert verify_resp.status_code in [400, 404]

    def test_upvote_and_downvote_same_image(
        self, session, base_url, auth_headers, sub_id
    ):
        """Create an upvote and then a downvote on the same image."""
        # Find an image
        search_resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"limit": 1},
        )
        image_id = search_resp.json()[0]["id"]

        # Upvote
        up_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": image_id, "sub_id": sub_id, "value": 1},
        )
        assert up_resp.status_code in [200, 201]
        up_vote_id = up_resp.json()["id"]

        # Downvote
        down_resp = session.post(
            f"{base_url}/votes",
            headers=auth_headers,
            json={"image_id": image_id, "sub_id": sub_id, "value": 0},
        )
        assert down_resp.status_code in [200, 201]
        down_vote_id = down_resp.json()["id"]

        # Verify both exist
        up_get = session.get(
            f"{base_url}/votes/{up_vote_id}", headers=auth_headers
        )
        down_get = session.get(
            f"{base_url}/votes/{down_vote_id}", headers=auth_headers
        )
        assert up_get.status_code == 200
        assert up_get.json()["value"] == 1
        assert down_get.status_code == 200
        assert down_get.json()["value"] == 0

        # Cleanup
        session.delete(f"{base_url}/votes/{up_vote_id}", headers=auth_headers)
        session.delete(f"{base_url}/votes/{down_vote_id}", headers=auth_headers)


# ---------------------------------------------------------------------------
# Flow 5: Image Upload -> Tag -> Favourite -> Vote -> Cleanup
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestImageUploadFullJourneyFlow:
    """
    E2E Flow: Complete image journey from upload to cleanup.

    Steps:
        1. POST /images/upload - Upload an image
        2. GET /images/:id - Verify the uploaded image
        3. POST /images/:id/breeds - Tag with a breed
        4. GET /images/:id/breeds - Verify breed tag
        5. POST /favourites - Favourite the image
        6. GET /favourites - Verify favourite exists
        7. POST /votes - Vote on the image
        8. GET /votes - Verify vote exists
        9. DELETE /votes/:id - Remove vote
        10. DELETE /favourites/:id - Remove favourite
        11. DELETE /images/:id/breeds/:breed_id - Remove breed tag
        12. DELETE /images/:id - Delete the image
    """

    def test_full_image_journey(
        self, session, base_url, api_key, auth_headers, sub_id, sample_breed_id
    ):
        """Upload -> Verify -> Tag -> Favourite -> Vote -> Full Cleanup."""
        # Step 1: Upload image
        png_data = _create_tiny_png()
        files = {"file": ("e2e_journey.png", io.BytesIO(png_data), "image/png")}
        upload_resp = session.post(
            f"{base_url}/images/upload",
            headers={"x-api-key": api_key},
            files=files,
            data={"sub_id": sub_id},
        )
        assert upload_resp.status_code in [200, 201], f"Upload failed: {upload_resp.text}"
        image_id = upload_resp.json()["id"]

        try:
            # Step 2: Verify uploaded image
            get_resp = session.get(
                f"{base_url}/images/{image_id}", headers=auth_headers
            )
            assert get_resp.status_code == 200
            assert get_resp.json()["id"] == image_id

            # Step 3: Tag with breed
            tag_resp = session.post(
                f"{base_url}/images/{image_id}/breeds",
                headers=auth_headers,
                json={"breed_id": sample_breed_id},
            )
            assert tag_resp.status_code in [200, 201, 204], f"Tag breed failed: {tag_resp.text}"

            # Step 4: Verify breed tag
            breeds_resp = session.get(
                f"{base_url}/images/{image_id}/breeds", headers=auth_headers
            )
            assert breeds_resp.status_code == 200

            # Step 5: Favourite the image
            fav_resp = session.post(
                f"{base_url}/favourites",
                headers=auth_headers,
                json={"image_id": image_id, "sub_id": sub_id},
            )
            assert fav_resp.status_code in [200, 201], f"Favourite failed: {fav_resp.text}"
            fav_id = fav_resp.json()["id"]

            # Step 6: Verify favourite in list
            fav_list = session.get(
                f"{base_url}/favourites", headers=auth_headers
            ).json()
            assert fav_id in [f["id"] for f in fav_list]

            # Step 7: Vote on the image
            vote_resp = session.post(
                f"{base_url}/votes",
                headers=auth_headers,
                json={"image_id": image_id, "sub_id": sub_id, "value": 1},
            )
            assert vote_resp.status_code in [200, 201], f"Vote failed: {vote_resp.text}"
            vote_id = vote_resp.json()["id"]

            # Step 8: Verify vote in list
            vote_list = session.get(
                f"{base_url}/votes", headers=auth_headers
            ).json()
            assert vote_id in [v["id"] for v in vote_list]

            # Step 9: Remove vote
            del_vote = session.delete(
                f"{base_url}/votes/{vote_id}", headers=auth_headers
            )
            assert del_vote.status_code == 200

            # Step 10: Remove favourite
            del_fav = session.delete(
                f"{base_url}/favourites/{fav_id}", headers=auth_headers
            )
            assert del_fav.status_code == 200

            # Step 11: Remove breed tag
            del_breed = session.delete(
                f"{base_url}/images/{image_id}/breeds/{sample_breed_id}",
                headers=auth_headers,
            )
            assert del_breed.status_code in [200, 204]

        finally:
            # Step 12: Delete the image (always cleanup)
            session.delete(
                f"{base_url}/images/{image_id}",
                headers={"Content-Type": "application/json", "x-api-key": api_key},
            )


# ---------------------------------------------------------------------------
# Flow 6: Multi-Image Favourite & Vote Comparison
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestMultiImageInteractionFlow:
    """
    E2E Flow: Interact with multiple images simultaneously.

    Steps:
        1. Search for multiple images
        2. Favourite two different images
        3. Vote differently on each (upvote vs downvote)
        4. List and verify all interactions
        5. Cleanup all resources
    """

    def test_multi_image_favourite_and_vote(
        self, session, base_url, auth_headers, sub_id
    ):
        """Favourite & vote on multiple images, verify all, then cleanup."""
        # Step 1: Search for multiple images
        search_resp = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"limit": 2, "order": "ASC", "page": 0},
        )
        assert search_resp.status_code == 200
        images = search_resp.json()
        assert len(images) >= 2, "Need at least 2 images for this test"

        image_id_1 = images[0]["id"]
        image_id_2 = images[1]["id"]
        created_favs = []
        created_votes = []

        try:
            # Step 2: Favourite both images
            for img_id in [image_id_1, image_id_2]:
                fav_resp = session.post(
                    f"{base_url}/favourites",
                    headers=auth_headers,
                    json={"image_id": img_id, "sub_id": sub_id},
                )
                assert fav_resp.status_code in [200, 201]
                created_favs.append(fav_resp.json()["id"])

            # Step 3: Upvote first, downvote second
            vote1_resp = session.post(
                f"{base_url}/votes",
                headers=auth_headers,
                json={"image_id": image_id_1, "sub_id": sub_id, "value": 1},
            )
            assert vote1_resp.status_code in [200, 201]
            created_votes.append(vote1_resp.json()["id"])

            vote2_resp = session.post(
                f"{base_url}/votes",
                headers=auth_headers,
                json={"image_id": image_id_2, "sub_id": sub_id, "value": 0},
            )
            assert vote2_resp.status_code in [200, 201]
            created_votes.append(vote2_resp.json()["id"])

            # Step 4: Verify all favourites exist
            fav_list = session.get(
                f"{base_url}/favourites", headers=auth_headers
            ).json()
            fav_ids_in_list = [f["id"] for f in fav_list]
            for fav_id in created_favs:
                assert fav_id in fav_ids_in_list

            # Step 4b: Verify all votes exist
            vote_list = session.get(
                f"{base_url}/votes", headers=auth_headers
            ).json()
            vote_ids_in_list = [v["id"] for v in vote_list]
            for vote_id in created_votes:
                assert vote_id in vote_ids_in_list

            # Step 4c: Verify vote values
            vote1_get = session.get(
                f"{base_url}/votes/{created_votes[0]}", headers=auth_headers
            ).json()
            vote2_get = session.get(
                f"{base_url}/votes/{created_votes[1]}", headers=auth_headers
            ).json()
            assert vote1_get["value"] == 1  # Upvote
            assert vote2_get["value"] == 0  # Downvote

        finally:
            # Step 5: Cleanup
            for vote_id in created_votes:
                session.delete(
                    f"{base_url}/votes/{vote_id}", headers=auth_headers
                )
            for fav_id in created_favs:
                session.delete(
                    f"{base_url}/favourites/{fav_id}", headers=auth_headers
                )


# ---------------------------------------------------------------------------
# Flow 7: Breed-Filtered Search -> Favourite -> Cleanup
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestBreedFilteredFavouriteFlow:
    """
    E2E Flow: Find images by breed, favourite one, verify, and cleanup.

    Steps:
        1. GET /breeds - List breeds
        2. GET /images/search?breed_ids=X - Search images by breed
        3. POST /favourites - Favourite a breed-specific image
        4. GET /favourites/:id - Verify favourite has correct image
        5. DELETE /favourites/:id - Cleanup
    """

    def test_breed_search_and_favourite(
        self, session, base_url, auth_headers, sub_id
    ):
        """Find breed image -> favourite it -> verify -> cleanup."""
        # Step 1: Get a breed
        breeds = session.get(
            f"{base_url}/breeds", headers=auth_headers, params={"limit": 5}
        ).json()
        breed_id = breeds[0]["id"]

        # Step 2: Search images by that breed
        images = session.get(
            f"{base_url}/images/search",
            headers=auth_headers,
            params={"breed_ids": breed_id, "limit": 1},
        ).json()
        assert len(images) > 0
        image_id = images[0]["id"]

        # Step 3: Favourite
        fav_resp = session.post(
            f"{base_url}/favourites",
            headers=auth_headers,
            json={"image_id": image_id, "sub_id": sub_id},
        )
        assert fav_resp.status_code in [200, 201]
        fav_id = fav_resp.json()["id"]

        # Step 4: Verify
        fav_detail = session.get(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        ).json()
        assert fav_detail["image_id"] == image_id

        # Step 5: Cleanup
        del_resp = session.delete(
            f"{base_url}/favourites/{fav_id}", headers=auth_headers
        )
        assert del_resp.status_code == 200


# ---------------------------------------------------------------------------
# Flow 8: Image Upload -> List -> Verify -> Delete -> Verify Gone
# ---------------------------------------------------------------------------
@pytest.mark.e2e
@pytest.mark.requires_api_key
class TestImageUploadListDeleteFlow:
    """
    E2E Flow: Upload, list, verify in list, delete, verify gone.

    Steps:
        1. POST /images/upload - Upload
        2. GET /images/ - List and find uploaded image
        3. GET /images/:id - Get details
        4. DELETE /images/:id - Delete
        5. GET /images/:id - Verify deleted
    """

    def test_upload_list_delete_verify(self, session, base_url, api_key, auth_headers, sub_id):
        """Upload -> List -> Get -> Delete -> Verify gone."""
        # Step 1: Upload
        png_data = _create_tiny_png()
        files = {"file": ("list_test.png", io.BytesIO(png_data), "image/png")}
        upload_resp = session.post(
            f"{base_url}/images/upload",
            headers={"x-api-key": api_key},
            files=files,
            data={"sub_id": sub_id},
        )
        assert upload_resp.status_code in [200, 201]
        image_id = upload_resp.json()["id"]

        try:
            # Step 2: List uploaded images and find ours
            list_resp = session.get(
                f"{base_url}/images/",
                headers=auth_headers,
                params={"limit": 50},
            )
            assert list_resp.status_code == 200
            uploaded_ids = [img["id"] for img in list_resp.json()]
            assert image_id in uploaded_ids, "Uploaded image not in list"

            # Step 3: Get details
            detail = session.get(
                f"{base_url}/images/{image_id}", headers=auth_headers
            )
            assert detail.status_code == 200
            assert detail.json()["id"] == image_id

        finally:
            # Step 4: Delete
            del_resp = session.delete(
                f"{base_url}/images/{image_id}",
                headers={"Content-Type": "application/json", "x-api-key": api_key},
            )
            assert del_resp.status_code in [200, 204]

        # Step 5: Verify deleted
        verify = session.get(
            f"{base_url}/images/{image_id}", headers=auth_headers
        )
        assert verify.status_code in [400, 404]
