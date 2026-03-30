package com.dogapi.tests;

import com.dogapi.base.BaseTest;
import com.dogapi.listeners.ExtentReportListener;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.Test;

import java.util.List;

/**
 * Test class for Favourites API endpoints.
 * Covers: POST /favourites, GET /favourites, GET /favourites/:favourite_id, DELETE /favourites/:favourite_id
 */
public class FavouritesApiTest extends BaseTest {

    private int createdFavouriteId;
    private String testImageId;

    // ==================== POST /favourites ====================

    @Test(description = "Verify POST /favourites creates a new favourite", priority = 1)
    public void testCreateFavourite() {
        ExtentReportListener.logStep("Step 1: Search for an image to favourite");

        // First, get a valid image ID
        Response searchResponse = apiHelper.get("/images/search");
        testImageId = searchResponse.jsonPath().getString("[0].id");
        ExtentReportListener.logInfo("Found image to favourite: " + testImageId);

        ExtentReportListener.logStep("Step 2: POST /favourites - Create favourite");
        String requestBody = String.format("{\"image_id\": \"%s\", \"sub_id\": \"test-user-automation\"}", testImageId);
        ExtentReportListener.logRequest("POST", "/favourites", requestBody);

        Response response = apiHelper.post("/favourites", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200 || response.getStatusCode() == 201;
        ExtentReportListener.logValidation("Status Code", "200 or 201", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk, "Create favourite should return 200 or 201");

        createdFavouriteId = response.jsonPath().getInt("id");
        boolean hasId = createdFavouriteId > 0;
        ExtentReportListener.logValidation("Favourite ID returned", "> 0", String.valueOf(createdFavouriteId), hasId);
        Assert.assertTrue(hasId, "Created favourite should have a valid ID");

        ExtentReportListener.logInfo("Created favourite with ID: " + createdFavouriteId);
    }

    // ==================== GET /favourites ====================

    @Test(description = "Verify GET /favourites returns list of favourites", priority = 2,
            dependsOnMethods = "testCreateFavourite")
    public void testGetFavourites() {
        ExtentReportListener.logStep("GET /favourites - List all favourites");
        ExtentReportListener.logRequest("GET", "/favourites", null);

        Response response = apiHelper.get("/favourites");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Validate response is an array
        List<?> favourites = response.jsonPath().getList("$");
        boolean hasFavourites = favourites != null && !favourites.isEmpty();
        ExtentReportListener.logValidation("Response contains favourites", "Non-empty array",
                favourites != null ? favourites.size() + " favourites" : "null", hasFavourites);
        Assert.assertTrue(hasFavourites, "Favourites list should contain at least one entry");

        // Validate favourite object structure
        Object firstId = response.jsonPath().get("[0].id");
        boolean hasId = firstId != null;
        ExtentReportListener.logValidation("First favourite has 'id' field", "Non-null", String.valueOf(firstId), hasId);

        String firstImageId = response.jsonPath().getString("[0].image_id");
        boolean hasImageId = firstImageId != null && !firstImageId.isEmpty();
        ExtentReportListener.logValidation("First favourite has 'image_id'", "Non-empty string", firstImageId, hasImageId);
    }

    // ==================== GET /favourites/:favourite_id ====================

    @Test(description = "Verify GET /favourites/:favourite_id returns specific favourite", priority = 3,
            dependsOnMethods = "testCreateFavourite")
    public void testGetFavouriteById() {
        ExtentReportListener.logStep("GET /favourites/" + createdFavouriteId + " - Get specific favourite");
        ExtentReportListener.logRequest("GET", "/favourites/" + createdFavouriteId, null);

        Response response = apiHelper.get("/favourites/" + createdFavouriteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Validate favourite id
        int returnedId = response.jsonPath().getInt("id");
        boolean idMatches = returnedId == createdFavouriteId;
        ExtentReportListener.logValidation("Favourite ID matches", createdFavouriteId, returnedId, idMatches);
        Assert.assertEquals(returnedId, createdFavouriteId, "Returned favourite ID should match");

        // Validate image_id
        String returnedImageId = response.jsonPath().getString("image_id");
        boolean imageIdMatches = testImageId.equals(returnedImageId);
        ExtentReportListener.logValidation("Image ID matches", testImageId, returnedImageId, imageIdMatches);
        Assert.assertEquals(returnedImageId, testImageId, "Image ID should match the one we favourited");

        // Validate sub_id
        String subId = response.jsonPath().getString("sub_id");
        boolean subIdMatches = "test-user-automation".equals(subId);
        ExtentReportListener.logValidation("Sub ID matches", "test-user-automation", subId, subIdMatches);
        Assert.assertEquals(subId, "test-user-automation", "Sub ID should match");
    }

    @Test(description = "Verify GET /favourites/:favourite_id with invalid ID returns error", priority = 4)
    public void testGetFavouriteByInvalidId() {
        int invalidId = 999999999;
        ExtentReportListener.logStep("GET /favourites/" + invalidId + " - Invalid favourite ID");
        ExtentReportListener.logRequest("GET", "/favourites/" + invalidId, null);

        Response response = apiHelper.get("/favourites/" + invalidId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Returns error for invalid favourite ID", "400 or 404",
                String.valueOf(response.getStatusCode()), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Invalid favourite ID should return error");
    }

    // ==================== POST /favourites - Duplicate check ====================

    @Test(description = "Verify POST /favourites with same image returns duplicate error", priority = 5,
            dependsOnMethods = "testCreateFavourite")
    public void testCreateDuplicateFavourite() {
        ExtentReportListener.logStep("POST /favourites - Attempt duplicate favourite with same image and sub_id");
        String requestBody = String.format("{\"image_id\": \"%s\", \"sub_id\": \"test-user-automation\"}", testImageId);
        ExtentReportListener.logRequest("POST", "/favourites", requestBody);

        Response response = apiHelper.post("/favourites", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        // The API rejects duplicate favourites for the same account + image_id + sub_id
        boolean isDuplicate = response.getStatusCode() == 400;
        ExtentReportListener.logValidation("Status Code", 400, response.getStatusCode(), isDuplicate);
        Assert.assertEquals(response.getStatusCode(), 400, "Duplicate favourite should return 400");

        String message = response.jsonPath().getString("message");
        boolean hasDuplicateMsg = message != null && message.contains("DUPLICATE_FAVOURITE");
        ExtentReportListener.logValidation("Error message contains DUPLICATE_FAVOURITE", "DUPLICATE_FAVOURITE", message, hasDuplicateMsg);
        Assert.assertTrue(hasDuplicateMsg, "Should indicate duplicate favourite error");
    }

    // ==================== DELETE /favourites/:favourite_id ====================

    @Test(description = "Verify DELETE /favourites/:favourite_id removes a favourite", priority = 6,
            dependsOnMethods = {"testCreateFavourite", "testGetFavouriteById"})
    public void testDeleteFavourite() {
        ExtentReportListener.logStep("DELETE /favourites/" + createdFavouriteId + " - Remove favourite");
        ExtentReportListener.logRequest("DELETE", "/favourites/" + createdFavouriteId, null);

        Response response = apiHelper.delete("/favourites/" + createdFavouriteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200, "Delete favourite should return 200");
    }

    @Test(description = "Verify GET /favourites/:favourite_id after deletion returns error", priority = 7,
            dependsOnMethods = "testDeleteFavourite")
    public void testGetDeletedFavourite() {
        ExtentReportListener.logStep("GET /favourites/" + createdFavouriteId + " - Verify deleted favourite not found");
        ExtentReportListener.logRequest("GET", "/favourites/" + createdFavouriteId, null);

        Response response = apiHelper.get("/favourites/" + createdFavouriteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isNotFound = response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Deleted favourite returns error", "400 or 404",
                String.valueOf(response.getStatusCode()), isNotFound);
        Assert.assertTrue(isNotFound, "Deleted favourite should not be found");
    }

    @Test(description = "Verify DELETE /favourites/:favourite_id with invalid ID returns error", priority = 8)
    public void testDeleteFavouriteInvalidId() {
        int invalidId = 999999999;
        ExtentReportListener.logStep("DELETE /favourites/" + invalidId + " - Invalid favourite ID");
        ExtentReportListener.logRequest("DELETE", "/favourites/" + invalidId, null);

        Response response = apiHelper.delete("/favourites/" + invalidId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Returns error for invalid favourite ID deletion", "400 or 404",
                String.valueOf(response.getStatusCode()), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Deleting invalid favourite ID should return error");
    }
}
