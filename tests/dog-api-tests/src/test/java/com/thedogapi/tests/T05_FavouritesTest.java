package com.thedogapi.tests;

import com.thedogapi.config.ApiConfig;
import com.thedogapi.config.BaseTest;
import io.restassured.response.Response;
import org.junit.jupiter.api.*;

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;
import static org.junit.jupiter.api.Assertions.*;

/**
 * Test suite for The Dog API - Favourites endpoints.
 *
 * Endpoints:
 *   POST   /favourites            - Add an image to favourites
 *   GET    /favourites            - List all favourites
 *   GET    /favourites/:favourite_id - Get a specific favourite
 *   DELETE /favourites/:favourite_id - Remove a favourite
 */
@Tag("favourites")
@DisplayName("Favourites API")
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class T05_FavouritesTest extends BaseTest {

    private static int favouriteId;
    private static String imageId;

    @BeforeAll
    void setupFavouritesTests() {
        requireApiKey();

        // Get a sample image ID
        imageId = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 1)
        .when()
            .get("/images/search")
        .then()
            .statusCode(200)
            .extract().jsonPath().getString("[0].id");
    }

    @Test
    @Order(1)
    @DisplayName("Should create a favourite")
    void createFavourite_returns200() {
        String body = String.format(
            "{\"image_id\": \"%s\", \"sub_id\": \"%s\"}",
            imageId, ApiConfig.getSubId()
        );

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/favourites");

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 201,
                "Expected 200 or 201 but got " + statusCode);

        favouriteId = response.jsonPath().getInt("id");
        assertTrue(favouriteId > 0, "Should return a valid favourite ID");
    }

    @Test
    @Order(2)
    @DisplayName("Should list favourites")
    void listFavourites_returns200() {
        given()
            .spec(ApiConfig.authSpec())
            .queryParam("sub_id", ApiConfig.getSubId())
        .when()
            .get("/favourites")
        .then()
            .statusCode(200)
            .body("$", not(empty()));
    }

    @Test
    @Order(3)
    @DisplayName("Should list favourites with limit")
    void listFavourites_respectsLimit() {
        given()
            .spec(ApiConfig.authSpec())
            .queryParam("sub_id", ApiConfig.getSubId())
            .queryParam("limit", 5)
        .when()
            .get("/favourites")
        .then()
            .statusCode(200)
            .body("size()", lessThanOrEqualTo(5));
    }

    @Test
    @Order(4)
    @DisplayName("Should get a specific favourite by ID")
    void getFavourite_returnsDetails() {
        Assumptions.assumeTrue(favouriteId > 0, "No favourite ID available");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/favourites/{id}", favouriteId)
        .then()
            .statusCode(200)
            .body("id", equalTo(favouriteId))
            .body("image_id", equalTo(imageId))
            .body("sub_id", equalTo(ApiConfig.getSubId()));
    }

    @Test
    @Order(5)
    @DisplayName("Should verify favourite contains image data")
    void getFavourite_containsImageData() {
        Assumptions.assumeTrue(favouriteId > 0, "No favourite ID available");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/favourites/{id}", favouriteId)
        .then()
            .statusCode(200)
            .body("image", notNullValue())
            .body("image.id", notNullValue());
    }

    @Test
    @Order(6)
    @DisplayName("Should delete a favourite")
    void deleteFavourite_returns200() {
        Assumptions.assumeTrue(favouriteId > 0, "No favourite ID to delete");

        Response response = given()
            .spec(ApiConfig.authSpec())
        .when()
            .delete("/favourites/{id}", favouriteId);

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 204,
                "Expected 200 or 204 but got " + statusCode);
    }

    @Test
    @Order(7)
    @DisplayName("Should return error when getting deleted favourite")
    void getDeletedFavourite_returnsError() {
        Assumptions.assumeTrue(favouriteId > 0, "No favourite ID to verify");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/favourites/{id}", favouriteId)
        .then()
            .statusCode(anyOf(is(400), is(404)));
    }

    @Test
    @Tag("negative")
    @DisplayName("Should return error for invalid image_id in favourite")
    void createFavourite_invalidImageId() {
        String body = String.format(
            "{\"image_id\": \"nonexistent-xyz\", \"sub_id\": \"%s\"}",
            ApiConfig.getSubId()
        );

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/favourites");

        assertTrue(response.getStatusCode() >= 400,
                "Expected error but got " + response.getStatusCode());
    }

    @Test
    @Tag("negative")
    @DisplayName("Should return error without API key")
    void listFavourites_noApiKey_returns401or403() {
        given()
            .spec(ApiConfig.publicSpec())
        .when()
            .get("/favourites")
        .then()
            .statusCode(anyOf(is(401), is(403)));
    }
}
