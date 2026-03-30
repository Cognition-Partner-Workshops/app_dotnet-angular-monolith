package com.dogapi.tests;

import com.dogapi.base.BaseTest;
import com.dogapi.listeners.ExtentReportListener;
import io.restassured.response.Response;
import org.testng.Assert;
import org.testng.annotations.Test;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * Test class for Breeds API endpoints.
 * Covers: GET /breeds, GET /breeds/:breed_id
 */
public class BreedsApiTest extends BaseTest {

    // ==================== GET /breeds ====================

    @Test(description = "Verify GET /breeds returns list of breeds with default pagination", priority = 1)
    public void testGetBreedsDefaultPagination() {
        ExtentReportListener.logStep("GET /breeds - Default pagination");
        ExtentReportListener.logRequest("GET", "/breeds", null);

        Response response = apiHelper.get("/breeds");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        // Validate status code
        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200, "Expected status code 200");

        // Validate response is a non-empty array
        List<?> breeds = response.jsonPath().getList("$");
        boolean hasBreeds = breeds != null && !breeds.isEmpty();
        ExtentReportListener.logValidation("Response contains breeds", "Non-empty array", 
                breeds != null ? breeds.size() + " breeds" : "null", hasBreeds);
        Assert.assertTrue(hasBreeds, "Breeds list should not be empty");

        // Validate breed object structure
        String firstName = response.jsonPath().getString("[0].name");
        boolean hasName = firstName != null && !firstName.isEmpty();
        ExtentReportListener.logValidation("First breed has 'name' field", "Non-empty string", firstName, hasName);
        Assert.assertTrue(hasName, "First breed should have a name");

        Object firstId = response.jsonPath().get("[0].id");
        boolean hasId = firstId != null;
        ExtentReportListener.logValidation("First breed has 'id' field", "Non-null value", String.valueOf(firstId), hasId);
        Assert.assertNotNull(firstId, "First breed should have an id");
    }

    @Test(description = "Verify GET /breeds supports limit parameter", priority = 2)
    public void testGetBreedsWithLimit() {
        int limitValue = 5;
        ExtentReportListener.logStep("GET /breeds with limit=" + limitValue);

        Map<String, Object> params = new HashMap<>();
        params.put("limit", limitValue);
        params.put("page", 0);
        ExtentReportListener.logRequest("GET", "/breeds?limit=" + limitValue + "&page=0", null);

        Response response = apiHelper.get("/breeds", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> breeds = response.jsonPath().getList("$");
        boolean correctLimit = breeds != null && breeds.size() <= limitValue;
        ExtentReportListener.logValidation("Number of breeds <= limit", "<= " + limitValue,
                breeds != null ? String.valueOf(breeds.size()) : "null", correctLimit);
        Assert.assertTrue(correctLimit, "Number of breeds should be <= " + limitValue);
    }

    @Test(description = "Verify GET /breeds supports pagination (page parameter)", priority = 3)
    public void testGetBreedsWithPagination() {
        ExtentReportListener.logStep("GET /breeds with pagination - Page 0 vs Page 1");

        Map<String, Object> page0Params = new HashMap<>();
        page0Params.put("limit", 5);
        page0Params.put("page", 0);
        ExtentReportListener.logRequest("GET", "/breeds?limit=5&page=0", null);

        Response page0Response = apiHelper.get("/breeds", page0Params);
        ExtentReportListener.logResponse(page0Response.getStatusCode(), page0Response.getBody().asString());

        Map<String, Object> page1Params = new HashMap<>();
        page1Params.put("limit", 5);
        page1Params.put("page", 1);
        ExtentReportListener.logRequest("GET", "/breeds?limit=5&page=1", null);

        Response page1Response = apiHelper.get("/breeds", page1Params);
        ExtentReportListener.logResponse(page1Response.getStatusCode(), page1Response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean bothOk = page0Response.getStatusCode() == 200 && page1Response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Both pages return 200", "200", 
                page0Response.getStatusCode() + " & " + page1Response.getStatusCode(), bothOk);
        Assert.assertTrue(bothOk, "Both page requests should return 200");

        String page0FirstName = page0Response.jsonPath().getString("[0].name");
        String page1FirstName = page1Response.jsonPath().getString("[0].name");
        boolean differentPages = page0FirstName != null && page1FirstName != null && !page0FirstName.equals(page1FirstName);
        ExtentReportListener.logValidation("Pages return different breeds", "Different first breed names",
                "Page0: " + page0FirstName + ", Page1: " + page1FirstName, differentPages);
        Assert.assertTrue(differentPages, "Different pages should return different breeds");
    }

    // ==================== GET /breeds/:breed_id ====================

    @Test(description = "Verify GET /breeds/:breed_id returns specific breed details", priority = 4)
    public void testGetBreedById() {
        ExtentReportListener.logStep("GET /breeds/:breed_id - Fetch specific breed");

        // First get a valid breed id
        Response listResponse = apiHelper.get("/breeds");
        int breedId = listResponse.jsonPath().getInt("[0].id");
        String expectedName = listResponse.jsonPath().getString("[0].name");
        ExtentReportListener.logInfo("Using breed_id: " + breedId + " (expected name: " + expectedName + ")");

        ExtentReportListener.logRequest("GET", "/breeds/" + breedId, null);
        Response response = apiHelper.get("/breeds/" + breedId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Validate returned breed id matches requested
        int returnedId = response.jsonPath().getInt("id");
        boolean idMatches = returnedId == breedId;
        ExtentReportListener.logValidation("Breed ID matches", breedId, returnedId, idMatches);
        Assert.assertEquals(returnedId, breedId, "Returned breed id should match requested id");

        // Validate breed name
        String returnedName = response.jsonPath().getString("name");
        boolean nameMatches = returnedName != null && returnedName.equals(expectedName);
        ExtentReportListener.logValidation("Breed name matches", expectedName, returnedName, nameMatches);
        Assert.assertEquals(returnedName, expectedName, "Breed name should match");

        // Validate breed has expected fields
        String weight = response.jsonPath().getString("weight");
        boolean hasWeight = weight != null;
        ExtentReportListener.logValidation("Breed has 'weight' field", "Non-null", String.valueOf(weight), hasWeight);
    }

    @Test(description = "Verify GET /breeds/:breed_id with invalid ID returns proper error", priority = 5)
    public void testGetBreedByInvalidId() {
        int invalidId = 999999;
        ExtentReportListener.logStep("GET /breeds/" + invalidId + " - Invalid breed ID");
        ExtentReportListener.logRequest("GET", "/breeds/" + invalidId, null);

        Response response = apiHelper.get("/breeds/" + invalidId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        // API should return 400 or empty/error response for invalid breed id
        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 404
                || response.getBody().asString().isEmpty() || response.getBody().asString().equals("{}");
        ExtentReportListener.logValidation("Returns error/empty for invalid ID", "400/404 or empty body",
                "Status: " + response.getStatusCode() + ", Body: " + response.getBody().asString(), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Invalid breed ID should return error or empty response");
    }
}
