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
 * Test class for Images API endpoints.
 * Covers: GET /images/search, GET /images/:image_id, GET /images/
 */
public class ImagesApiTest extends BaseTest {

    private String capturedImageId;

    // ==================== GET /images/search ====================

    @Test(description = "Verify GET /images/search returns random images", priority = 1)
    public void testSearchImagesDefault() {
        ExtentReportListener.logStep("GET /images/search - Default search");
        ExtentReportListener.logRequest("GET", "/images/search", null);

        Response response = apiHelper.get("/images/search");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean hasImages = images != null && !images.isEmpty();
        ExtentReportListener.logValidation("Response contains images", "Non-empty array",
                images != null ? images.size() + " images" : "null", hasImages);
        Assert.assertTrue(hasImages, "Search should return at least one image");

        // Validate image structure
        String imageId = response.jsonPath().getString("[0].id");
        boolean hasId = imageId != null && !imageId.isEmpty();
        ExtentReportListener.logValidation("Image has 'id' field", "Non-empty string", imageId, hasId);
        Assert.assertTrue(hasId, "Image should have an id");

        String url = response.jsonPath().getString("[0].url");
        boolean hasUrl = url != null && url.startsWith("http");
        ExtentReportListener.logValidation("Image has valid 'url' field", "URL starting with http", url, hasUrl);
        Assert.assertTrue(hasUrl, "Image should have a valid URL");

        // Store for later tests
        capturedImageId = imageId;
    }

    @Test(description = "Verify GET /images/search with size parameter", priority = 2)
    public void testSearchImagesWithSizeParam() {
        ExtentReportListener.logStep("GET /images/search with size=small");

        Map<String, Object> params = new HashMap<>();
        params.put("size", "small");
        params.put("limit", 3);
        ExtentReportListener.logRequest("GET", "/images/search?size=small&limit=3", null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean withinLimit = images != null && images.size() <= 3;
        ExtentReportListener.logValidation("Result count <= limit(3)", "<= 3",
                images != null ? String.valueOf(images.size()) : "null", withinLimit);
        Assert.assertTrue(withinLimit, "Number of images should be <= limit");
    }

    @Test(description = "Verify GET /images/search with mime_types filter", priority = 3)
    public void testSearchImagesWithMimeType() {
        ExtentReportListener.logStep("GET /images/search with mime_types=jpg");

        Map<String, Object> params = new HashMap<>();
        params.put("mime_types", "jpg");
        params.put("limit", 5);
        ExtentReportListener.logRequest("GET", "/images/search?mime_types=jpg&limit=5", null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Check that results have image/jpeg mime type or jpg URLs
        List<?> images = response.jsonPath().getList("$");
        boolean hasImages = images != null && !images.isEmpty();
        ExtentReportListener.logValidation("Response contains images", "Non-empty array",
                images != null ? images.size() + " images" : "null", hasImages);
        Assert.assertTrue(hasImages, "Search should return images for jpg mime type");
    }

    @Test(description = "Verify GET /images/search with has_breeds=true returns images with breed data", priority = 4)
    public void testSearchImagesWithBreeds() {
        ExtentReportListener.logStep("GET /images/search with has_breeds=true");

        Map<String, Object> params = new HashMap<>();
        params.put("has_breeds", true);
        params.put("limit", 3);
        ExtentReportListener.logRequest("GET", "/images/search?has_breeds=true&limit=3", null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean hasImages = images != null && !images.isEmpty();
        ExtentReportListener.logValidation("Response contains images", "Non-empty array",
                images != null ? images.size() + " images" : "null", hasImages);
        Assert.assertTrue(hasImages, "Search should return images with breeds");

        // Validate breeds array is present and non-empty
        List<?> breeds = response.jsonPath().getList("[0].breeds");
        boolean hasBreeds = breeds != null && !breeds.isEmpty();
        ExtentReportListener.logValidation("First image has breeds data", "Non-empty breeds array",
                breeds != null ? breeds.size() + " breeds" : "null", hasBreeds);
        Assert.assertTrue(hasBreeds, "Images should contain breed data when has_breeds=true");
    }

    @Test(description = "Verify GET /images/search with limit parameter", priority = 5)
    public void testSearchImagesWithLimit() {
        int limitValue = 10;
        ExtentReportListener.logStep("GET /images/search with limit=" + limitValue);

        Map<String, Object> params = new HashMap<>();
        params.put("limit", limitValue);
        ExtentReportListener.logRequest("GET", "/images/search?limit=" + limitValue, null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean correctCount = images != null && images.size() <= limitValue;
        ExtentReportListener.logValidation("Number of images <= limit", "<= " + limitValue,
                images != null ? String.valueOf(images.size()) : "null", correctCount);
        Assert.assertTrue(correctCount, "Image count should be <= " + limitValue);
    }

    @Test(description = "Verify GET /images/search with order=ASC", priority = 6)
    public void testSearchImagesOrderAsc() {
        ExtentReportListener.logStep("GET /images/search with order=ASC");

        Map<String, Object> params = new HashMap<>();
        params.put("order", "ASC");
        params.put("limit", 5);
        params.put("page", 0);
        ExtentReportListener.logRequest("GET", "/images/search?order=ASC&limit=5&page=0", null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean hasImages = images != null && !images.isEmpty();
        ExtentReportListener.logValidation("Response contains images", "Non-empty array",
                images != null ? images.size() + " images" : "null", hasImages);
        Assert.assertTrue(hasImages, "Search should return images");
    }

    // ==================== GET /images/:image_id ====================

    @Test(description = "Verify GET /images/:image_id returns specific image", priority = 7,
            dependsOnMethods = "testSearchImagesDefault")
    public void testGetImageById() {
        ExtentReportListener.logStep("GET /images/:image_id - Get specific image by ID");

        // Use image from search if available, otherwise do a new search
        if (capturedImageId == null) {
            Response searchResponse = apiHelper.get("/images/search");
            capturedImageId = searchResponse.jsonPath().getString("[0].id");
        }

        ExtentReportListener.logInfo("Using image_id: " + capturedImageId);
        ExtentReportListener.logRequest("GET", "/images/" + capturedImageId, null);

        Response response = apiHelper.get("/images/" + capturedImageId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Validate image id matches
        String returnedId = response.jsonPath().getString("id");
        boolean idMatches = capturedImageId.equals(returnedId);
        ExtentReportListener.logValidation("Image ID matches", capturedImageId, returnedId, idMatches);
        Assert.assertEquals(returnedId, capturedImageId, "Returned image ID should match requested");

        // Validate has URL
        String url = response.jsonPath().getString("url");
        boolean hasUrl = url != null && url.startsWith("http");
        ExtentReportListener.logValidation("Image has valid URL", "URL starting with http", url, hasUrl);
        Assert.assertTrue(hasUrl, "Image should have a valid URL");

        // Validate width and height
        Object width = response.jsonPath().get("width");
        Object height = response.jsonPath().get("height");
        boolean hasDimensions = width != null && height != null;
        ExtentReportListener.logValidation("Image has dimensions", "width and height present",
                "width=" + width + ", height=" + height, hasDimensions);
    }

    @Test(description = "Verify GET /images/:image_id with invalid ID", priority = 8)
    public void testGetImageByInvalidId() {
        String invalidId = "INVALID_IMAGE_ID_XYZ";
        ExtentReportListener.logStep("GET /images/" + invalidId + " - Invalid image ID");
        ExtentReportListener.logRequest("GET", "/images/" + invalidId, null);

        Response response = apiHelper.get("/images/" + invalidId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean isErrorResponse = response.getStatusCode() == 400 || response.getStatusCode() == 404;
        ExtentReportListener.logValidation("Returns error for invalid image ID", "400 or 404",
                String.valueOf(response.getStatusCode()), isErrorResponse);
        Assert.assertTrue(isErrorResponse, "Invalid image ID should return 400 or 404");
    }
}
