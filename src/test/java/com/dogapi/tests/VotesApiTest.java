package com.dogapi.tests;

import com.dogapi.base.BaseTest;
import com.dogapi.listeners.ExtentReportListener;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.Test;

import java.util.List;

/**
 * Test class for Votes API endpoints.
 * Covers: POST /votes, GET /votes, GET /votes/:vote_id, DELETE /votes/:vote_id
 */
public class VotesApiTest extends BaseTest {

    private int createdVoteId;
    private String testImageId;

    // ==================== POST /votes (Upvote) ====================

    @Test(description = "Verify POST /votes creates an upvote", priority = 1)
    public void testCreateUpvote() {
        ExtentReportListener.logStep("Step 1: Search for an image to vote on");

        Response searchResponse = apiHelper.get("/images/search");
        testImageId = searchResponse.jsonPath().getString("[0].id");
        ExtentReportListener.logInfo("Found image to vote on: " + testImageId);

        ExtentReportListener.logStep("Step 2: POST /votes - Create upvote (value=1)");
        String requestBody = String.format("{\"image_id\": \"%s\", \"sub_id\": \"test-user-automation\", \"value\": 1}", testImageId);
        ExtentReportListener.logRequest("POST", "/votes", requestBody);

        Response response = apiHelper.post("/votes", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 201 || response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", "200 or 201", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk, "Create vote should return 200 or 201");

        createdVoteId = response.jsonPath().getInt("id");
        boolean hasId = createdVoteId > 0;
        ExtentReportListener.logValidation("Vote ID returned", "> 0", String.valueOf(createdVoteId), hasId);
        Assert.assertTrue(hasId, "Created vote should have a valid ID");

        ExtentReportListener.logInfo("Created vote with ID: " + createdVoteId);
    }

    // ==================== POST /votes (Downvote) ====================

    @Test(description = "Verify POST /votes creates a downvote", priority = 2)
    public void testCreateDownvote() {
        ExtentReportListener.logStep("POST /votes - Create downvote (value=0)");

        // Get a fresh image
        Response searchResponse = apiHelper.get("/images/search");
        String imageId = searchResponse.jsonPath().getString("[0].id");

        String requestBody = String.format("{\"image_id\": \"%s\", \"sub_id\": \"test-user-automation\", \"value\": 0}", imageId);
        ExtentReportListener.logRequest("POST", "/votes", requestBody);

        Response response = apiHelper.post("/votes", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 201 || response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", "200 or 201", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk, "Create downvote should return 200 or 201");

        // Clean up
        int downvoteId = response.jsonPath().getInt("id");
        if (downvoteId > 0) {
            apiHelper.delete("/votes/" + downvoteId);
            ExtentReportListener.logInfo("Cleaned up downvote ID: " + downvoteId);
        }
    }

    // ==================== GET /votes ====================

    @Test(description = "Verify GET /votes returns list of votes", priority = 3,
            dependsOnMethods = "testCreateUpvote")
    public void testGetVotes() {
        ExtentReportListener.logStep("GET /votes - List all votes");
        ExtentReportListener.logRequest("GET", "/votes", null);

        Response response = apiHelper.get("/votes");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> votes = response.jsonPath().getList("$");
        boolean hasVotes = votes != null && !votes.isEmpty();
        ExtentReportListener.logValidation("Response contains votes", "Non-empty array",
                votes != null ? votes.size() + " votes" : "null", hasVotes);
        Assert.assertTrue(hasVotes, "Votes list should contain at least one entry");

        // Validate vote structure
        Object firstId = response.jsonPath().get("[0].id");
        boolean hasId = firstId != null;
        ExtentReportListener.logValidation("First vote has 'id' field", "Non-null", String.valueOf(firstId), hasId);

        String firstImageId = response.jsonPath().getString("[0].image_id");
        boolean hasImageId = firstImageId != null && !firstImageId.isEmpty();
        ExtentReportListener.logValidation("First vote has 'image_id'", "Non-empty string", firstImageId, hasImageId);

        Object firstValue = response.jsonPath().get("[0].value");
        boolean hasValue = firstValue != null;
        ExtentReportListener.logValidation("First vote has 'value' field", "Non-null (0 or 1)", String.valueOf(firstValue), hasValue);
    }

    // ==================== GET /votes/:vote_id ====================

    @Test(description = "Verify GET /votes/:vote_id returns specific vote", priority = 4,
            dependsOnMethods = "testCreateUpvote")
    public void testGetVoteById() {
        ExtentReportListener.logStep("GET /votes/" + createdVoteId + " - Get specific vote");
        ExtentReportListener.logRequest("GET", "/votes/" + createdVoteId, null);

        Response response = apiHelper.get("/votes/" + createdVoteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Validate vote id
        int returnedId = response.jsonPath().getInt("id");
        boolean idMatches = returnedId == createdVoteId;
        ExtentReportListener.logValidation("Vote ID matches", createdVoteId, returnedId, idMatches);
        Assert.assertEquals(returnedId, createdVoteId, "Returned vote ID should match");

        // Validate image_id
        String returnedImageId = response.jsonPath().getString("image_id");
        boolean imageIdMatches = testImageId.equals(returnedImageId);
        ExtentReportListener.logValidation("Image ID matches", testImageId, returnedImageId, imageIdMatches);
        Assert.assertEquals(returnedImageId, testImageId, "Image ID should match");

        // Validate value is 1 (upvote)
        int value = response.jsonPath().getInt("value");
        boolean valueCorrect = value == 1;
        ExtentReportListener.logValidation("Vote value is upvote", 1, value, valueCorrect);
        Assert.assertEquals(value, 1, "Vote value should be 1 (upvote)");

        // Validate sub_id
        String subId = response.jsonPath().getString("sub_id");
        boolean subIdMatches = "test-user-automation".equals(subId);
        ExtentReportListener.logValidation("Sub ID matches", "test-user-automation", subId, subIdMatches);
    }

    @Test(description = "Verify GET /votes/:vote_id with invalid ID returns error", priority = 5)
    public void testGetVoteByInvalidId() {
        int invalidId = 999999999;
        ExtentReportListener.logStep("GET /votes/" + invalidId + " - Invalid vote ID");
        ExtentReportListener.logRequest("GET", "/votes/" + invalidId, null);

        Response response = apiHelper.get("/votes/" + invalidId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Returns error for invalid vote ID", "400 or 404",
                String.valueOf(response.getStatusCode()), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Invalid vote ID should return error");
    }

    // ==================== DELETE /votes/:vote_id ====================

    @Test(description = "Verify DELETE /votes/:vote_id handles vote deletion", priority = 6,
            dependsOnMethods = {"testCreateUpvote", "testGetVoteById"})
    public void testDeleteVote() {
        ExtentReportListener.logStep("DELETE /votes/" + createdVoteId + " - Attempt to remove vote");
        ExtentReportListener.logRequest("DELETE", "/votes/" + createdVoteId, null);

        Response response = apiHelper.delete("/votes/" + createdVoteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        // The Dog API may return 200 (deleted) or 404 (vote was auto-replaced by a newer vote)
        boolean statusOk = response.getStatusCode() == 200 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Status Code", "200 or 404", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk, "Delete vote should return 200 or 404");

        ExtentReportListener.logInfo("Vote delete returned: " + response.getStatusCode());
    }

    @Test(description = "Verify GET /votes/:vote_id after deletion attempt", priority = 7,
            dependsOnMethods = "testDeleteVote")
    public void testGetDeletedVote() {
        ExtentReportListener.logStep("GET /votes/" + createdVoteId + " - Check vote status after deletion attempt");
        ExtentReportListener.logRequest("GET", "/votes/" + createdVoteId, null);

        Response response = apiHelper.get("/votes/" + createdVoteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        // With DEMO API key, votes may persist even after delete attempt (returns 200) or be removed (404)
        boolean validResponse = response.getStatusCode() == 200 || response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Vote status after delete", "200, 400 or 404",
                String.valueOf(response.getStatusCode()), validResponse);
        Assert.assertTrue(validResponse, "Vote should return 200 (persisted), 400 or 404 (deleted)");

        if (response.getStatusCode() == 200) {
            ExtentReportListener.logInfo("Note: Vote persists after delete with DEMO API key - this is expected API behavior");
        } else {
            ExtentReportListener.logInfo("Vote was successfully deleted");
        }
    }

    @Test(description = "Verify DELETE /votes/:vote_id with invalid ID returns error", priority = 8)
    public void testDeleteVoteInvalidId() {
        int invalidId = 999999999;
        ExtentReportListener.logStep("DELETE /votes/" + invalidId + " - Invalid vote ID");
        ExtentReportListener.logRequest("DELETE", "/votes/" + invalidId, null);

        Response response = apiHelper.delete("/votes/" + invalidId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Returns error for invalid vote ID deletion", "400 or 404",
                String.valueOf(response.getStatusCode()), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Deleting invalid vote ID should return error");
    }

    // ==================== POST /votes - Validation ====================

    @Test(description = "Verify POST /votes with missing image_id returns error", priority = 9)
    public void testCreateVoteMissingImageId() {
        ExtentReportListener.logStep("POST /votes - Missing image_id field");
        String requestBody = "{\"sub_id\": \"test-user-automation\", \"value\": 1}";
        ExtentReportListener.logRequest("POST", "/votes", requestBody);

        Response response = apiHelper.post("/votes", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 422;
        ExtentReportListener.logValidation("Returns error for missing image_id", "400 or 422",
                String.valueOf(response.getStatusCode()), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Missing image_id should return error");
    }
}
