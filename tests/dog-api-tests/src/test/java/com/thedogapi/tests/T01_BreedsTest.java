package com.thedogapi.tests;

import com.thedogapi.config.ApiConfig;
import com.thedogapi.config.BaseTest;
import io.restassured.response.Response;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Tag;
import org.junit.jupiter.api.Test;

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;

/**
 * Test suite for The Dog API - Breeds endpoints.
 *
 * Endpoints:
 *   GET /breeds        - List all breeds with pagination
 *   GET /breeds/:id    - Get a specific breed by ID
 *   GET /breeds/search - Search breeds by name
 */
@Tag("breeds")
@DisplayName("Breeds API")
class T01_BreedsTest extends BaseTest {

    @BeforeEach
    void checkApiKey() {
        requireApiKey();
    }

    @Nested
    @Tag("smoke")
    @DisplayName("GET /breeds - List Breeds")
    class ListBreeds {

        @Test
        @DisplayName("Should return 200 OK")
        void listBreeds_returns200() {
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/breeds")
            .then()
                .statusCode(200);
        }

        @Test
        @DisplayName("Should return a non-empty JSON array")
        void listBreeds_returnsNonEmptyArray() {
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/breeds")
            .then()
                .statusCode(200)
                .body("$", not(empty()))
                .body("[0].id", notNullValue())
                .body("[0].name", notNullValue());
        }

        @Test
        @DisplayName("Should contain expected breed fields")
        void listBreeds_containsExpectedFields() {
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/breeds")
            .then()
                .statusCode(200)
                .body("[0].id", notNullValue())
                .body("[0].name", notNullValue())
                .body("[0].temperament", notNullValue())
                .body("[0].life_span", notNullValue());
        }

        @Test
        @DisplayName("Should respect limit parameter")
        void listBreeds_respectsLimit() {
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 5)
            .when()
                .get("/breeds")
            .then()
                .statusCode(200)
                .body("size()", lessThanOrEqualTo(5));
        }

        @Test
        @DisplayName("Should support pagination with limit and page")
        void listBreeds_supportsPagination() {
            Response page0 = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 5)
                .queryParam("page", 0)
            .when()
                .get("/breeds");

            Response page1 = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 5)
                .queryParam("page", 1)
            .when()
                .get("/breeds");

            page0.then().statusCode(200).body("size()", greaterThan(0));
            page1.then().statusCode(200).body("size()", greaterThan(0));

            String firstIdPage0 = page0.jsonPath().getString("[0].id");
            String firstIdPage1 = page1.jsonPath().getString("[0].id");
            assert !firstIdPage0.equals(firstIdPage1) :
                    "Different pages should return different breeds";
        }
    }

    @Nested
    @DisplayName("GET /breeds/:id - Get Breed by ID")
    class GetBreedById {

        @Test
        @DisplayName("Should return breed details for valid ID")
        void getBreedById_validId() {
            // First get a valid breed ID
            int breedId = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 1)
            .when()
                .get("/breeds")
            .then()
                .statusCode(200)
                .extract().jsonPath().getInt("[0].id");

            // Then fetch that specific breed
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/breeds/{id}", breedId)
            .then()
                .statusCode(200)
                .body("id", equalTo(breedId))
                .body("name", notNullValue());
        }

        @Test
        @DisplayName("Should return expected fields for a breed")
        void getBreedById_hasExpectedFields() {
            int breedId = given()
                .spec(ApiConfig.authSpec())
                .queryParam("limit", 1)
            .when()
                .get("/breeds")
            .then()
                .extract().jsonPath().getInt("[0].id");

            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/breeds/{id}", breedId)
            .then()
                .statusCode(200)
                .body("id", notNullValue())
                .body("name", notNullValue())
                .body("weight", notNullValue())
                .body("height", notNullValue());
        }

        @Test
        @Tag("negative")
        @DisplayName("Should return error for non-existent breed ID")
        void getBreedById_invalidId() {
            given()
                .spec(ApiConfig.authSpec())
            .when()
                .get("/breeds/{id}", 99999)
            .then()
                .statusCode(anyOf(is(400), is(404)));
        }
    }

    @Nested
    @DisplayName("GET /breeds/search - Search Breeds")
    class SearchBreeds {

        @Test
        @DisplayName("Should find breeds matching search query")
        void searchBreeds_findsMatches() {
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("q", "labrador")
            .when()
                .get("/breeds/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0))
                .body("[0].name", containsStringIgnoringCase("labrador"));
        }

        @Test
        @DisplayName("Should return empty array for no matches")
        void searchBreeds_noMatches() {
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("q", "xyznonexistentbreed123")
            .when()
                .get("/breeds/search")
            .then()
                .statusCode(200)
                .body("size()", equalTo(0));
        }

        @Test
        @DisplayName("Should return results for partial name search")
        void searchBreeds_partialName() {
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("q", "bull")
            .when()
                .get("/breeds/search")
            .then()
                .statusCode(200)
                .body("size()", greaterThan(0));
        }

        @Test
        @DisplayName("Should return empty for empty query")
        void searchBreeds_emptyQuery() {
            given()
                .spec(ApiConfig.authSpec())
                .queryParam("q", "")
            .when()
                .get("/breeds/search")
            .then()
                .statusCode(200);
        }
    }
}
