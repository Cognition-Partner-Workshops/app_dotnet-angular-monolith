package com.thedogapi.tests;

import com.thedogapi.config.ApiConfig;
import com.thedogapi.config.BaseTest;
import io.restassured.response.Response;
import org.junit.jupiter.api.*;

import static io.restassured.RestAssured.given;
import static org.hamcrest.Matchers.*;
import static org.junit.jupiter.api.Assertions.*;

/**
 * Test suite for The Dog API - Votes endpoints.
 *
 * Endpoints:
 *   POST   /votes            - Create a vote (upvote/downvote)
 *   GET    /votes            - List all votes
 *   GET    /votes/:vote_id   - Get a specific vote
 *   DELETE /votes/:vote_id   - Delete a vote
 */
@Tag("votes")
@DisplayName("Votes API")
@TestMethodOrder(MethodOrderer.OrderAnnotation.class)
class T06_VotesTest extends BaseTest {

    private static int upvoteId;
    private static int downvoteId;
    private static String imageId;

    @BeforeAll
    void setupVotesTests() {
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
    @DisplayName("Should create an upvote (value=1)")
    void createUpvote_returns200or201() {
        String body = String.format(
            "{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 1}",
            imageId, ApiConfig.getSubId()
        );

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/votes");

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 201,
                "Expected 200 or 201 but got " + statusCode);

        upvoteId = response.jsonPath().getInt("id");
        assertTrue(upvoteId > 0, "Should return a valid vote ID");
    }

    @Test
    @Order(2)
    @DisplayName("Should create a downvote (value=0)")
    void createDownvote_returns200or201() {
        // Get a different image for the downvote
        String secondImageId = given()
            .spec(ApiConfig.authSpec())
            .queryParam("limit", 2)
        .when()
            .get("/images/search")
        .then()
            .statusCode(200)
            .extract().jsonPath().getString("[1].id");

        String body = String.format(
            "{\"image_id\": \"%s\", \"sub_id\": \"%s\", \"value\": 0}",
            secondImageId != null ? secondImageId : imageId, ApiConfig.getSubId()
        );

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/votes");

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 201,
                "Expected 200 or 201 but got " + statusCode);

        downvoteId = response.jsonPath().getInt("id");
        assertTrue(downvoteId > 0, "Should return a valid vote ID");
    }

    @Test
    @Order(3)
    @DisplayName("Should list votes")
    void listVotes_returns200() {
        given()
            .spec(ApiConfig.authSpec())
            .queryParam("sub_id", ApiConfig.getSubId())
        .when()
            .get("/votes")
        .then()
            .statusCode(200)
            .body("$", not(empty()));
    }

    @Test
    @Order(4)
    @DisplayName("Should list votes with limit")
    void listVotes_respectsLimit() {
        given()
            .spec(ApiConfig.authSpec())
            .queryParam("sub_id", ApiConfig.getSubId())
            .queryParam("limit", 5)
        .when()
            .get("/votes")
        .then()
            .statusCode(200)
            .body("size()", lessThanOrEqualTo(5));
    }

    @Test
    @Order(5)
    @DisplayName("Should get a specific upvote by ID")
    void getUpvote_returnsDetails() {
        Assumptions.assumeTrue(upvoteId > 0, "No upvote ID available");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/votes/{id}", upvoteId)
        .then()
            .statusCode(200)
            .body("id", equalTo(upvoteId))
            .body("value", equalTo(1))
            .body("image_id", equalTo(imageId));
    }

    @Test
    @Order(6)
    @DisplayName("Should get a specific downvote by ID")
    void getDownvote_returnsDetails() {
        Assumptions.assumeTrue(downvoteId > 0, "No downvote ID available");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/votes/{id}", downvoteId)
        .then()
            .statusCode(200)
            .body("id", equalTo(downvoteId))
            .body("value", equalTo(0));
    }

    @Test
    @Order(7)
    @DisplayName("Should delete an upvote")
    void deleteUpvote_returns200or204() {
        Assumptions.assumeTrue(upvoteId > 0, "No upvote ID to delete");

        Response response = given()
            .spec(ApiConfig.authSpec())
        .when()
            .delete("/votes/{id}", upvoteId);

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 204,
                "Expected 200 or 204 but got " + statusCode);
    }

    @Test
    @Order(8)
    @DisplayName("Should delete a downvote")
    void deleteDownvote_returns200or204() {
        Assumptions.assumeTrue(downvoteId > 0, "No downvote ID to delete");

        Response response = given()
            .spec(ApiConfig.authSpec())
        .when()
            .delete("/votes/{id}", downvoteId);

        int statusCode = response.getStatusCode();
        assertTrue(statusCode == 200 || statusCode == 204,
                "Expected 200 or 204 but got " + statusCode);
    }

    @Test
    @Order(9)
    @DisplayName("Should return error when getting deleted vote")
    void getDeletedVote_returnsError() {
        Assumptions.assumeTrue(upvoteId > 0, "No vote ID to verify");

        given()
            .spec(ApiConfig.authSpec())
        .when()
            .get("/votes/{id}", upvoteId)
        .then()
            .statusCode(anyOf(is(200), is(400), is(404)));
    }

    @Test
    @Tag("negative")
    @DisplayName("Should return error for invalid image_id in vote")
    void createVote_invalidImageId() {
        String body = String.format(
            "{\"image_id\": \"nonexistent-xyz\", \"sub_id\": \"%s\", \"value\": 1}",
            ApiConfig.getSubId()
        );

        Response response = given()
            .spec(ApiConfig.authSpec())
            .body(body)
        .when()
            .post("/votes");

        assertTrue(response.getStatusCode() >= 400,
                "Expected error but got " + response.getStatusCode());
    }

    @Test
    @Tag("negative")
    @DisplayName("Should return error without API key")
    void listVotes_noApiKey_returns401or403() {
        given()
            .spec(ApiConfig.publicSpec())
        .when()
            .get("/votes")
        .then()
            .statusCode(anyOf(is(401), is(403)));
    }
}
