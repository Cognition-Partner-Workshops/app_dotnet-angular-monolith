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
 * Test suite for The Dog API - Image Upload endpoints.
 *
 * Endpoints:
 *   POST   /images/upload   - Upload an image
 *   GET    /images          - List uploaded images
 *   DELETE /images/:image_id - Delete an uploaded image
 */
@Tag("upload")
@DisplayName("Image Upload API")
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class T03_ImageUploadTest extends BaseTest {

    private static String uploadedImageId;
    private static Path tempImagePath;

    @BeforeAll
    void setupUploadTests() {
        requireApiKey();
    }

    /**
     * Creates a minimal valid PNG file for upload testing.
     */
    private File createTinyPng() throws IOException {
        tempImagePath = Files.createTempFile("dog-api-test-", ".png");
        // Minimal valid PNG: 1x1 pixel
        byte[] pngBytes = new byte[]{
            (byte) 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,         // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,         // 1x1
            0x08, 0x02, 0x00, 0x00, 0x00, (byte) 0x90, 0x77, 0x53,
            (byte) 0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,  // IDAT chunk
            0x54, 0x08, (byte) 0xD7, 0x63, (byte) 0xF8, (byte) 0xCF,
            (byte) 0xC0, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01,
            (byte) 0xE2, 0x21, (byte) 0xBC, 0x33, 0x00, 0x00, 0x00,
            0x00, 0x49, 0x45, 0x4E, 0x44, (byte) 0xAE, 0x42, 0x60,  // IEND chunk
            (byte) 0x82
        };
        try (FileOutputStream fos = new FileOutputStream(tempImagePath.toFile())) {
            fos.write(pngBytes);
        }
        return tempImagePath.toFile();
    }

    @Test
    @Order(1)
    @DisplayName("Should upload an image successfully")
    void uploadImage_returns201() throws IOException {
        File imageFile = createTinyPng();

        Response response = given()
            .header("x-api-key", ApiConfig.getApiKey())
            .contentType(ContentType.MULTIPART)
            .multiPart("file", imageFile, "image/png")
            .formParam("sub_id", ApiConfig.getSubId())
        .when()
            .post(ApiConfig.getBaseUrl() + "/images/upload");

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 201 || statusCode == 200,
                "Expected 200 or 201 but got " + statusCode);

        uploadedImageId = response.jsonPath().getString("id");
        assertNotNull(uploadedImageId, "Upload should return an image ID");
    }

    @Test
    @Order(2)
    @DisplayName("Should list uploaded images")
    void listUploadedImages_returns200() {
        given()
            .spec(ApiConfig.authSpec())
            .queryParam("sub_id", ApiConfig.getSubId())
            .queryParam("limit", 10)
        .when()
            .get("/images")
        .then()
            .statusCode(200)
            .body("$", instanceOf(java.util.List.class));
    }

    @Test
    @Order(3)
    @DisplayName("Should get uploaded image by ID")
    void getUploadedImage_returnsDetails() {
        Assumptions.assumeTrue(uploadedImageId != null, "No uploaded image to fetch");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/images/{id}", uploadedImageId)
        .then()
            .statusCode(200)
            .body("id", equalTo(uploadedImageId))
            .body("url", notNullValue());
    }

    @Test
    @Order(4)
    @DisplayName("Should list uploaded images with pagination")
    void listUploadedImages_pagination() {
        given()
            .spec(ApiConfig.authSpec())
            .queryParam("sub_id", ApiConfig.getSubId())
            .queryParam("limit", 5)
            .queryParam("page", 0)
        .when()
            .get("/images")
        .then()
            .statusCode(200);
    }

    @Test
    @Order(5)
    @DisplayName("Should delete an uploaded image")
    void deleteImage_returns200or204() {
        Assumptions.assumeTrue(uploadedImageId != null, "No uploaded image to delete");

        Response response = given()
            .spec(ApiConfig.authSpec())
        .when()
            .delete("/images/{id}", uploadedImageId);

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 204,
                "Expected 200 or 204 but got " + statusCode);
    }

    @Test
    @Order(6)
    @DisplayName("Should return error when getting deleted image")
    void getDeletedImage_returnsError() {
        Assumptions.assumeTrue(uploadedImageId != null, "No image ID to verify");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/images/{id}", uploadedImageId)
        .then()
            .statusCode(anyOf(is(400), is(404)));
    }

    @Test
    @Tag("negative")
    @DisplayName("Should reject upload without file")
    void uploadImage_noFile_returnsError() {
        Response response = given()
            .header("x-api-key", ApiConfig.getApiKey())
            .contentType(ContentType.MULTIPART)
            .formParam("sub_id", ApiConfig.getSubId())
        .when()
            .post(ApiConfig.getBaseUrl() + "/images/upload");

        assertTrue(response.getStatusCode() >= 400,
                "Expected error status but got " + response.getStatusCode());
    }

    @Test
    @Tag("negative")
    @DisplayName("Should reject upload without API key")
    void uploadImage_noApiKey_returns401or403() {
        given()
            .contentType(ContentType.MULTIPART)
            .formParam("sub_id", ApiConfig.getSubId())
        .when()
            .post(ApiConfig.getBaseUrl() + "/images/upload")
        .then()
            .statusCode(anyOf(is(401), is(403)));
    }

    @AfterAll
    void cleanup() throws IOException {
        if (tempImagePath != null) {
            Files.deleteIfExists(tempImagePath);
        }
    }
}
