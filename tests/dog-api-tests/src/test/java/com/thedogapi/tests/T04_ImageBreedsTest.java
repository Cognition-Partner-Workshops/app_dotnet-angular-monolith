package com.thedogapi.tests;

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

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;
import static org.junit.jupiter.api.Assertions.*;

/**
 * Test suite for The Dog API - Image Breed Tagging endpoints.
 *
 * Endpoints:
 *   POST   /images/:image_id/breeds  - Add a breed tag to an image
 *   DELETE /images/:image_id/breeds   - Remove a breed tag from an image
 */
@Tag("imageBreeds")
@DisplayName("Image Breed Tagging API")
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class T04_ImageBreedsTest extends BaseTest {

    private static String uploadedImageId;
    private static int breedId;
    private static Path tempImagePath;

    @BeforeAll
    void setupImageBreedTests() throws IOException {
        requireApiKey();

        // Upload a test image
        tempImagePath = Files.createTempFile("dog-api-breed-test-", ".png");
        byte[] pngBytes = new byte[]{
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
        try (FileOutputStream fos = new FileOutputStream(tempImagePath.toFile())) {
            fos.write(pngBytes);
        }

        Response uploadResp = given()
            .header("x-api-key", ApiConfig.getApiKey())
            .contentType(ContentType.MULTIPART)
            .multiPart("file", tempImagePath.toFile(), "image/png")
            .formParam("sub_id", ApiConfig.getSubId())
        .when()
            .post(ApiConfig.getBaseUrl() + "/images/upload");

        if (uploadResp.getStatusCode() == 201 || uploadResp.getStatusCode() == 200) {
            uploadedImageId = uploadResp.jsonPath().getString("id");
        }

        // Get a breed ID
        breedId = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 1)
        .when()
            .get("/breeds")
        .then()
            .extract().jsonPath().getInt("[0].id");
    }

    @Test
    @Order(1)
    @DisplayName("Should add a breed tag to an uploaded image")
    void addBreedToImage_returnsSuccess() {
        Assumptions.assumeTrue(uploadedImageId != null, "No uploaded image available");

        String body = String.format("{\"breed_id\": %d}", breedId);

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/images/{imageId}/breeds", uploadedImageId);

        assertTrue(response.getStatusCode() >= 200 && response.getStatusCode() < 300,
                "Expected success but got " + response.getStatusCode());
    }

    @Test
    @Order(2)
    @DisplayName("Should verify breed tag is present on image")
    void getImage_showsBreedTag() {
        Assumptions.assumeTrue(uploadedImageId != null, "No uploaded image available");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/images/{id}", uploadedImageId)
        .then()
            .statusCode(200)
            .body("breeds", notNullValue());
    }

    @Test
    @Order(3)
    @DisplayName("Should remove a breed tag from an image")
    void removeBreedFromImage_returnsSuccess() {
        Assumptions.assumeTrue(uploadedImageId != null, "No uploaded image available");

        Response response = given()
            .spec(ApiConfig.authSpec())
        .when()
            .delete("/images/{imageId}/breeds/{breedId}", uploadedImageId, breedId);

        assertTrue(response.getStatusCode() >= 200 && response.getStatusCode() < 300,
                "Expected success but got " + response.getStatusCode());
    }

    @Test
    @Tag("negative")
    @Order(4)
    @DisplayName("Should return error for invalid image ID")
    void addBreedToInvalidImage_returnsError() {
        String body = String.format("{\"breed_id\": %d}", breedId);

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/images/{imageId}/breeds", "nonexistent-image-xyz");

        assertTrue(response.getStatusCode() >= 400,
                "Expected error but got " + response.getStatusCode());
    }

    @Test
    @Tag("negative")
    @Order(5)
    @DisplayName("Should return error for invalid breed ID")
    void addInvalidBreedToImage_returnsError() {
        Assumptions.assumeTrue(uploadedImageId != null, "No uploaded image available");

        String body = "{\"breed_id\": 999999}";

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/images/{imageId}/breeds", uploadedImageId);

        assertTrue(response.getStatusCode() >= 400,
                "Expected error but got " + response.getStatusCode());
    }

    @AfterAll
    void cleanup() throws IOException {
        // Delete uploaded image
        if (uploadedImageId != null) {
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .delete("/images/{id}", uploadedImageId);
        }
        if (tempImagePath != null) {
            Files.deleteIfExists(tempImagePath);
        }
    }
}
