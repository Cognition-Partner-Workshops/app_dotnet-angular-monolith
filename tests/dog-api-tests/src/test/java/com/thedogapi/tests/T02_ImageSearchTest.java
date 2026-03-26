package com.thedogapi.tests;

import com.thedogapi.config.ApiConfig;
import com.thedogapi.config.BaseTest;
import io.restassured.response.Response;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import java.util.List;

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;
import static org.junit.jupiter.api.Assertions.*;

/**
 * Test suite for The Dog API - Image Search endpoints.
 *
 * Endpoints:
 *   GET /images/search      - Search approved images with filters
 *   GET /images/:image_id   - Get a specific image by ID
 */
@Tag("images")
@DisplayName("Image Search API")
class T02_ImageSearchTest extends BaseTest {

    @Nested
    @Tag("smoke")
    @DisplayName("GET /images/search - Search Images")
    class SearchImages {

        @Test
        @DisplayName("Should return 200 OK")
        void searchImages_returns200() {
            given()
                .spec(ApiConfig.publicSpec())
            .when()
                .get("/images/search")
            .then()
                .statusCode(200);
        }

        @Test
        @DisplayName("Should return a JSON array")
        void searchImages_returnsArray() {
            given()
                .spec(ApiConfig.publicSpec())
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("$", not(empty()));
        }

        @Test
        @DisplayName("Should contain expected image fields")
        void searchImages_hasExpectedFields() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("limit", 1)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("[0].id", notNullValue())
                .body("[0].url", notNullValue())
                .body("[0].width", notNullValue())
                .body("[0].height", notNullValue());
        }

        @Test
        @DisplayName("Should respect limit parameter")
        void searchImages_respectsLimit() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("limit", 3)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", lessThanOrEqualTo(3));
        }

        @Test
        @DisplayName("Should return exactly one image when limit=1")
        void searchImages_limitOne() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("limit", 1)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", equalTo(1));
        }

        @Test
        @DisplayName("Should filter by has_breeds=true (requires API key)")
        void searchImages_hasBreedsFilter() {
            requireApiKey();
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("has_breeds", "true")
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0))
                .body("[0].breeds", not(empty()));
        }

        @Test
        @DisplayName("Should filter by mime_types=jpg")
        void searchImages_mimeTypeJpg() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("mime_types", "jpg")
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should filter by mime_types=png")
        void searchImages_mimeTypePng() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("mime_types", "png")
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should filter by size=small")
        void searchImages_sizeSmall() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("size", "small")
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should filter by size=med")
        void searchImages_sizeMed() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("size", "med")
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should filter by size=full")
        void searchImages_sizeFull() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("size", "full")
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should support order=ASC")
        void searchImages_orderAsc() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("order", "ASC")
                .queryParam("limit", 5)
                .queryParam("page", 0)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("$", instanceOf(List.class));
        }

        @Test
        @DisplayName("Should support order=DESC")
        void searchImages_orderDesc() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("order", "DESC")
                .queryParam("limit", 5)
                .queryParam("page", 0)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("$", instanceOf(List.class));
        }

        @Test
        @DisplayName("Should return results for page 0")
        void searchImages_page0() {
            given()
                .spec(ApiConfig.publicSpec())
                .queryParam("limit", 5)
                .queryParam("page", 0)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should return different results for different pages")
        void searchImages_differentPages() {
            Response page0 = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 5)
                .queryParam("page", 0)
                .queryParam("order", "ASC")
            .when()
                .get("/images/search");

            Response page1 = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 5)
                .queryParam("page", 1)
                .queryParam("order", "ASC")
            .when()
                .get("/images/search");

            List<String> idsPage0 = page0.jsonPath().getList("id");
            List<String> idsPage1 = page1.jsonPath().getList("id");
            assertNotEquals(idsPage0, idsPage1, "Different pages should return different images");
        }

        @Test
        @DisplayName("Should filter by breed_ids (requires API key)")
        void searchImages_breedIdFilter() {
            requireApiKey();

            // Get a breed ID first
            int breedId = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 1)
            .when()
                .get("/breeds")
            .then()
                .extract().jsonPath().getInt("[0].id");

            given()
                .spec(ApiConfig.authSpec())
                .queryParam("breed_ids", breedId)
                .queryParam("limit", 5)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }
    }

    @Nested
    @DisplayName("GET /images/:image_id - Get Image by ID")
    class GetImageById {

        @Test
        @DisplayName("Should return image details for valid ID")
        void getImageById_validId() {
            // First search for an image to get a valid ID
            String imageId = given()
                .spec(ApiConfig.publicSpec())
                .queryParam("limit", 1)
            .when()
                .get("/images/search")
            .then()
                .statusCode(200)
                .extract().jsonPath().getString("[0].id");

            given()
                .spec(ApiConfig.publicSpec())
            .when()
                .get("/images/{id}", imageId)
            .then()
                .statusCode(200)
                .body("id", equalTo(imageId))
                .body("url", notNullValue());
        }

        @Test
        @DisplayName("Should contain all expected response fields")
        void getImageById_hasExpectedFields() {
            String imageId = given()
                .spec(ApiConfig.publicSpec())
                .queryParam("limit", 1)
            .when()
                .get("/images/search")
            .then()
                .extract().jsonPath().getString("[0].id");

            given()
                .spec(ApiConfig.publicSpec())
            .when()
                .get("/images/{id}", imageId)
            .then()
                .statusCode(200)
                .body("id", notNullValue())
                .body("url", notNullValue())
                .body("width", notNullValue())
                .body("height", notNullValue());
        }

        @Test
        @Tag("negative")
        @DisplayName("Should return error for non-existent image ID")
        void getImageById_invalidId() {
            given()
                .spec(ApiConfig.publicSpec())
            .when()
                .get("/images/{id}", "nonexistent-id-12345")
            .then()
                .statusCode(anyOf(is(400), is(404)));
        }

        @Test
        @Tag("negative")
        @DisplayName("Should return appropriate response for empty image ID")
        void getImageById_emptyId() {
            given()
                .spec(ApiConfig.publicSpec())
            .when()
                .get("/images/")
            .then()
                .statusCode(anyOf(is(200), is(400), is(401)));
        }
    }
}
