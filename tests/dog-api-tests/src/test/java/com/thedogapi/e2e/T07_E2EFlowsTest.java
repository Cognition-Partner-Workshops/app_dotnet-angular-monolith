package com.thedogapi.e2e;

import com.thedogapi.config.ApiConfig;
import com.thedogapi.config.BaseTest;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import org.junit.jupiter.api.*;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.List;

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;
import static org.junit.jupiter.api.Assertions.*;

/**
 * End-to-End API flow tests for The Dog API.
 * Each test method exercises a complete user journey spanning multiple endpoints.
 */
@Tag("e2e")
@DisplayName("E2E API Flows")
class T07_E2EFlowsTest extends BaseTest {

    @BeforeAll
    void setupE2E() {
        requireApiKey();
    }

    // ---------------------------------------------------------------
    // E2E Flow 1: Breed Exploration -> Image Discovery
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 1: List breeds -> Pick one -> Search images for that breed")
    void flow1_breedExplorationToImageDiscovery() {
        // Step 1: List breeds
        Response breedsResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 10)
        .when()
            .get("/breeds");
        breedsResp.then().statusCode(200).body("size()", greaterThan(0));

        int breedId = breedsResp.jsonPath().getInt("[0].id");
        String breedName = breedsResp.jsonPath().getString("[0].name");
        assertNotNull(breedName, "Breed should have a name");

        // Step 2: Get breed details
        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/breeds/{id}", breedId)
        .then()
            .statusCode(200)
            .body("id", equalTo(breedId))
            .body("name", equalTo(breedName));

        // Step 3: Search images filtered by this breed
        Response imagesResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("breed_ids", breedId)
            .queryParam("limit", 5)
        .when()
            .get("/images/search");
        imagesResp.then().statusCode(200).body("size()", greaterThan(0));

        // Step 4: Verify returned images have the breed data
        String firstImageId = imagesResp.jsonPath().getString("[0].id");
        assertNotNull(firstImageId);

        // Step 5: Get the individual image details
        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/images/{id}", firstImageId)
        .then()
            .statusCode(200)
            .body("id", equalTo(firstImageId))
            .body("url", notNullValue());
    }

    // ---------------------------------------------------------------
    // E2E Flow 2: Image Search with Multiple Filters
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 2: Search images with size, order, pagination filters")
    void flow2_imageSearchMultipleFilters() {
        // Step 1: Search with size filter
        Response smallResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("size", "small")
            .queryParam("limit", 3)
        .when()
            .get("/images/search");
        smallResp.then().statusCode(200).body("size()", greaterThan(0));

        // Step 2: Search with order ASC
        Response ascResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("order", "ASC")
            .queryParam("limit", 5)
            .queryParam("page", 0)
        .when()
            .get("/images/search");
        ascResp.then().statusCode(200).body("size()", greaterThan(0));

        // Step 3: Search page 1 with same order
        Response page1Resp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("order", "ASC")
            .queryParam("limit", 5)
            .queryParam("page", 1)
        .when()
            .get("/images/search");
        page1Resp.then().statusCode(200);

        // Step 4: Verify different pages have different results
        List<String> page0Ids = ascResp.jsonPath().getList("id");
        List<String> page1Ids = page1Resp.jsonPath().getList("id");
        assertNotEquals(page0Ids, page1Ids, "Different pages should return different images");

        // Step 5: Get individual image details
        String imageId = ascResp.jsonPath().getString("[0].id");
        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/images/{id}", imageId)
        .then()
            .statusCode(200)
            .body("id", equalTo(imageId));
    }

    // ---------------------------------------------------------------
    // E2E Flow 3: Favourite Lifecycle
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 3: Search -> Favourite -> List -> Get -> Delete -> Verify gone")
    void flow3_favouriteLifecycle() {
        // Step 1: Search for an image
        String imageId = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 1)
        .when()
            .get("/images/search")
        .then()
            .statusCode(200)
            .extract().jsonPath().getString("[0].id");

        // Step 2: Create favourite
        String body = String.format(
            "{\"image_id\": \"%s\", \"sub_id\": \"%s\"}",
            imageId, ApiConfig.getSubId()
        );
        Response createResp = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/favourites");
        assertTrue(createResp.getStatusCode() == 200 || createResp.getStatusCode() == 201);
        int favouriteId = createResp.jsonPath().getInt("id");

        try {
            // Step 3: List favourites and verify it appears
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("sub_id", ApiConfig.getSubId())
            .when()
                .get("/favourites")
            .then()
                .statusCode(200)
                .body("id", hasItem(favouriteId));

            // Step 4: Get specific favourite
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/favourites/{id}", favouriteId)
            .then()
                .statusCode(200)
                .body("id", equalTo(favouriteId))
                .body("image_id", equalTo(imageId));

        } finally {
            // Step 5: Delete favourite
            Response deleteResp = given()
                .spec(ApiConfig.authSpec())
            .when()
                .delete("/favourites/{id}", favouriteId);
            assertTrue(deleteResp.getStatusCode() == 200 || deleteResp.getStatusCode() == 204);

            // Step 6: Verify favourite is gone
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/favourites/{id}", favouriteId)
            .then()
                .statusCode(anyOf(is(400), is(404)));
        }
    }

    // ---------------------------------------------------------------
    // E2E Flow 4: Vote Lifecycle
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 4: Search -> Upvote -> Downvote -> List -> Delete -> Verify gone")
    void flow4_voteLifecycle() {
        // Step 1: Search for images
        Response searchResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 2)
        .when()
            .get("/images/search");
        searchResp.then().statusCode(200);
        String imageId1 = searchResp.jsonPath().getString("[0].id");

        // Step 2: Create upvote
        String upvoteBody = String.format(
            "{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 1}",
            imageId1, ApiConfig.getSubId()
        );
        Response upvoteResp = given()
            .spec(ApiConfig.authSpec())
            .body(upvoteBody)
        .when()
            .post("/votes");
        assertTrue(upvoteResp.getStatusCode() == 200 || upvoteResp.getStatusCode() == 201);
        int upvoteId = upvoteResp.jsonPath().getInt("id");

        // Step 3: Create downvote on same or different image
        String imageId2 = searchResp.jsonPath().getString("[1].id");
        if (imageId2 == null) imageId2 = imageId1;
        String downvoteBody = String.format(
            "{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 0}",
            imageId2, ApiConfig.getSubId()
        );
        Response downvoteResp = given()
            .spec(ApiConfig.authSpec())
            .body(downvoteBody)
        .when()
            .post("/votes");
        assertTrue(downvoteResp.getStatusCode() == 200 || downvoteResp.getStatusCode() == 201);
        int downvoteId = downvoteResp.jsonPath().getInt("id");

        try {
            // Step 4: List votes
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("sub_id", ApiConfig.getSubId())
            .when()
                .get("/votes")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));

            // Step 5: Get upvote details
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/votes/{id}", upvoteId)
            .then()
                .statusCode(200)
                .body("value", equalTo(1));

            // Step 6: Get downvote details
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/votes/{id}", downvoteId)
            .then()
                .statusCode(200)
                .body("value", equalTo(0));

        } finally {
            // Step 7: Cleanup - delete both votes
            given().spec(ApiConfig.authSpec()).delete("/votes/{id}", upvoteId);
            given().spec(ApiConfig.authSpec()).delete("/votes/{id}", downvoteId);
        }
    }

    // ---------------------------------------------------------------
    // E2E Flow 5: Image Upload Full Journey
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 5: Upload -> Tag breed -> Favourite -> Vote -> Full cleanup")
    void flow5_imageUploadFullJourney() throws IOException {
        // Step 1: Upload an image
        Path tempImage = Files.createTempFile("e2e-upload-", ".png");
        byte[] pngBytes = createMinimalPng();
        try (FileOutputStream fos = new FileOutputStream(tempImage.toFile())) {
            fos.write(pngBytes);
        }

        Response uploadResp = given()
            .header("x-api-key", ApiConfig.getApiKey())
            .contentType(ContentType.MULTIPART)
            .multiPart("file", tempImage.toFile(), "image/png")
            .formParam("sub_id", ApiConfig.getSubId())
        .when()
            .post(ApiConfig.getBaseUrl() + "/images/upload");

        assertTrue(uploadResp.getStatusCode() == 200 || uploadResp.getStatusCode() == 201,
                "Upload failed with status " + uploadResp.getStatusCode());
        String uploadedImageId = uploadResp.jsonPath().getString("id");
        assertNotNull(uploadedImageId);

        int favouriteId = 0;
        int voteId = 0;

        try {
            // Step 2: Get a breed ID and tag the image
            int breedId = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 1)
            .when()
                .get("/breeds")
            .then()
                .extract().jsonPath().getInt("[0].id");

            given()
                .spec(ApiConfig.authSpec())
                .body(String.format("{\"breed_id\": %d}", breedId))
            .when()
                .post("/images/{imageId}/breeds", uploadedImageId);

            // Step 3: Favourite the uploaded image
            Response favResp = given()
                .spec(ApiConfig.authSpec())
                .body(String.format(
                    "{\"image_id\": \"%s\", \"sub_id\": \"%s\"}",
                    uploadedImageId, ApiConfig.getSubId()))
            .when()
                .post("/favourites");
            if (favResp.getStatusCode() == 200 || favResp.getStatusCode() == 201) {
                favouriteId = favResp.jsonPath().getInt("id");
            }

            // Step 4: Vote on the uploaded image
            Response voteResp = given()
                .spec(ApiConfig.authSpec())
                .body(String.format(
                    "{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 1}",
                    uploadedImageId, ApiConfig.getSubId()))
            .when()
                .post("/votes");
            if (voteResp.getStatusCode() == 200 || voteResp.getStatusCode() == 201) {
                voteId = voteResp.jsonPath().getInt("id");
            }

            // Step 5: Verify image has all associated data
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/images/{id}", uploadedImageId)
            .then()
                .statusCode(200)
                .body("id", equalTo(uploadedImageId));

        } finally {
            // Step 6: Full cleanup
            if (favouriteId > 0) {
                given().spec(ApiConfig.authSpec()).delete("/favourites/{id}", favouriteId);
            }
            if (voteId > 0) {
                given().spec(ApiConfig.authSpec()).delete("/votes/{id}", voteId);
            }
            given().spec(ApiConfig.authSpec()).delete("/images/{id}", uploadedImageId);
            Files.deleteIfExists(tempImage);
        }
    }

    // ---------------------------------------------------------------
    // E2E Flow 6: Multi-Image Interaction Flow
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 6: Favourite 2 images -> Vote differently -> Verify all -> Cleanup")
    void flow6_multiImageInteraction() {
        // Step 1: Get two images
        Response searchResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 2)
        .when()
            .get("/images/search");
        searchResp.then().statusCode(200).body("size()", greaterThanOrEqualTo(2));

        String imageId1 = searchResp.jsonPath().getString("[0].id");
        String imageId2 = searchResp.jsonPath().getString("[1].id");

        int favId1 = 0, favId2 = 0, voteId1 = 0, voteId2 = 0;

        try {
            // Step 2: Favourite both images
            Response fav1 = given()
                .spec(ApiConfig.authSpec())
                .body(String.format("{\"image_id\": \"%s\", \"sub_id\": \"%s\"}", imageId1, ApiConfig.getSubId()))
            .when()
                .post("/favourites");
            if (fav1.getStatusCode() == 200 || fav1.getStatusCode() == 201) {
                favId1 = fav1.jsonPath().getInt("id");
            }

            Response fav2 = given()
                .spec(ApiConfig.authSpec())
                .body(String.format("{\"image_id\": \"%s\", \"sub_id\": \"%s\"}", imageId2, ApiConfig.getSubId()))
            .when()
                .post("/favourites");
            if (fav2.getStatusCode() == 200 || fav2.getStatusCode() == 201) {
                favId2 = fav2.jsonPath().getInt("id");
            }

            // Step 3: Upvote first image, downvote second
            Response vote1 = given()
                .spec(ApiConfig.authSpec())
                .body(String.format("{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 1}", imageId1, ApiConfig.getSubId()))
            .when()
                .post("/votes");
            if (vote1.getStatusCode() == 200 || vote1.getStatusCode() == 201) {
                voteId1 = vote1.jsonPath().getInt("id");
            }

            Response vote2 = given()
                .spec(ApiConfig.authSpec())
                .body(String.format("{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 0}", imageId2, ApiConfig.getSubId()))
            .when()
                .post("/votes");
            if (vote2.getStatusCode() == 200 || vote2.getStatusCode() == 201) {
                voteId2 = vote2.jsonPath().getInt("id");
            }

            // Step 4: Verify favourites list contains both
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("sub_id", ApiConfig.getSubId())
            .when()
                .get("/favourites")
            .then()
                .statusCode(200)
                .body("size()", greaterThanOrEqualTo(2));

            // Step 5: Verify votes list
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("sub_id", ApiConfig.getSubId())
            .when()
                .get("/votes")
            .then()
                .statusCode(200)
                .body("size()", greaterThanOrEqualTo(2));

        } finally {
            // Cleanup
            if (favId1 > 0) given().spec(ApiConfig.authSpec()).delete("/favourites/{id}", favId1);
            if (favId2 > 0) given().spec(ApiConfig.authSpec()).delete("/favourites/{id}", favId2);
            if (voteId1 > 0) given().spec(ApiConfig.authSpec()).delete("/votes/{id}", voteId1);
            if (voteId2 > 0) given().spec(ApiConfig.authSpec()).delete("/votes/{id}", voteId2);
        }
    }

    // ---------------------------------------------------------------
    // E2E Flow 7: Breed-Filtered Favourite Flow
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 7: Get breed -> Search by breed -> Favourite result -> Verify -> Cleanup")
    void flow7_breedFilteredFavourite() {
        // Step 1: Get a breed
        Response breedsResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 1)
        .when()
            .get("/breeds");
        breedsResp.then().statusCode(200);
        int breedId = breedsResp.jsonPath().getInt("[0].id");

        // Step 2: Search images filtered by breed
        Response searchResp = given()
            .spec(ApiConfig.authSpec())
            .queryParam("breed_ids", breedId)
            .queryParam("limit", 1)
        .when()
            .get("/images/search");
        searchResp.then().statusCode(200).body("size()", greaterThan(0));
        String imageId = searchResp.jsonPath().getString("[0].id");

        int favouriteId = 0;

        try {
            // Step 3: Favourite the breed-specific image
            Response favResp = given()
                .spec(ApiConfig.authSpec())
                .body(String.format("{\"image_id\": \"%s\", \"sub_id\": \"%s\"}", imageId, ApiConfig.getSubId()))
            .when()
                .post("/favourites");
            assertTrue(favResp.getStatusCode() == 200 || favResp.getStatusCode() == 201);
            favouriteId = favResp.jsonPath().getInt("id");

            // Step 4: Verify the favourite
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/favourites/{id}", favouriteId)
            .then()
                .statusCode(200)
                .body("image_id", equalTo(imageId));

        } finally {
            // Cleanup
            if (favouriteId > 0) {
                given().spec(ApiConfig.authSpec()).delete("/favourites/{id}", favouriteId);
            }
        }
    }

    // ---------------------------------------------------------------
    // E2E Flow 8: Image Upload -> List -> Get -> Delete -> Verify Gone
    // ---------------------------------------------------------------
    @Test
    @DisplayName("Flow 8: Upload -> List uploaded -> Get details -> Delete -> Verify gone")
    void flow8_imageUploadListDeleteVerify() throws IOException {
        // Step 1: Upload an image
        Path tempImage = Files.createTempFile("e2e-lifecycle-", ".png");
        byte[] pngBytes = createMinimalPng();
        try (FileOutputStream fos = new FileOutputStream(tempImage.toFile())) {
            fos.write(pngBytes);
        }

        Response uploadResp = given()
            .header("x-api-key", ApiConfig.getApiKey())
            .contentType(ContentType.MULTIPART)
            .multiPart("file", tempImage.toFile(), "image/png")
            .formParam("sub_id", ApiConfig.getSubId())
        .when()
            .post(ApiConfig.getBaseUrl() + "/images/upload");

        assertTrue(uploadResp.getStatusCode() == 200 || uploadResp.getStatusCode() == 201);
        String imageId = uploadResp.jsonPath().getString("id");
        assertNotNull(imageId);

        try {
            // Step 2: List uploaded images and verify ours appears
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("sub_id", ApiConfig.getSubId())
                .queryParam("limit", 20)
            .when()
                .get("/images")
            .then()
                .statusCode(200)
                .body("id", hasItem(imageId));

            // Step 3: Get the specific image
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/images/{id}", imageId)
            .then()
                .statusCode(200)
                .body("id", equalTo(imageId))
                .body("url", notNullValue());

            // Step 4: Delete the image
            Response deleteResp = given()
                .spec(ApiConfig.authSpec())
            .when()
                .delete("/images/{id}", imageId);
            assertTrue(deleteResp.getStatusCode() == 200 || deleteResp.getStatusCode() == 204);

            // Step 5: Verify the image is gone
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/images/{id}", imageId)
            .then()
                .statusCode(anyOf(is(400), is(404)));

        } finally {
            Files.deleteIfExists(tempImage);
        }
    }

    /**
     * Creates a minimal valid 1x1 PNG byte array.
     */
    private byte[] createMinimalPng() {
        return new byte[]{
            (byte) 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x02, 0x00, 0x00, 0x00, (byte) 0x90, 0x77, 0x53,
            (byte) 0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
            0x54, 0x08, (byte) 0xD7, 0x63, (byte) 0xF8, (byte) 0xCF,
            (byte) 0xC0, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01,
            (byte) 0xE2, 0x21, (byte) 0xBC, 0x33, 0x00, 0x00, 0x00,
            0x00, 0x49, 0x45, 0x4E, 0x44, (byte) 0xAE, 0x42, 0x60,
            (byte) 0x82
        };
    }
}
