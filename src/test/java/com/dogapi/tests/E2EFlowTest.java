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
 * End-to-End API flow tests that exercise multiple endpoints in logical sequences.
 * These tests simulate real user workflows across different API resources.
 */
public class E2EFlowTest extends BaseTest {

    // ==========================================
    // E2E Flow 1: Browse Breeds → Search Images → Favourite → Cleanup
    // ==========================================

    private String e2eImageId;
    private int e2eFavouriteId;
    private int e2eVoteId;
    private int e2eBreedId;
    private String e2eBreedName;

    @Test(description = "E2E Flow 1 - Step 1: Browse available dog breeds", priority = 1)
    public void e2eFlow1_Step1_BrowseBreeds() {
        ExtentReportListener.logStep("E2E Flow 1: Browse Breeds → Search Images by Breed → Favourite → Vote → Cleanup");
        ExtentReportListener.logStep("Step 1: Browse available breeds");
        ExtentReportListener.logRequest("GET", "/breeds?limit=10&page=0", null);

        Map<String, Object> params = new HashMap<>();
        params.put("limit", 10);
        params.put("page", 0);

        Response response = apiHelper.get("/breeds", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> breeds = response.jsonPath().getList("$");
        boolean hasBreeds = breeds != null && !breeds.isEmpty();
        ExtentReportListener.logValidation("Breeds list returned", "Non-empty", 
                breeds != null ? breeds.size() + " breeds" : "null", hasBreeds);
        Assert.assertTrue(hasBreeds, "Should receive breeds list");

        // Pick first breed for next steps
        e2eBreedId = response.jsonPath().getInt("[0].id");
        e2eBreedName = response.jsonPath().getString("[0].name");
        ExtentReportListener.logInfo("Selected breed: " + e2eBreedName + " (ID: " + e2eBreedId + ")");
    }

    @Test(description = "E2E Flow 1 - Step 2: Get detailed breed info", priority = 2,
            dependsOnMethods = "e2eFlow1_Step1_BrowseBreeds")
    public void e2eFlow1_Step2_GetBreedDetails() {
        ExtentReportListener.logStep("Step 2: Get detailed info for breed: " + e2eBreedName);
        ExtentReportListener.logRequest("GET", "/breeds/" + e2eBreedId, null);

        Response response = apiHelper.get("/breeds/" + e2eBreedId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        String name = response.jsonPath().getString("name");
        boolean nameMatches = e2eBreedName.equals(name);
        ExtentReportListener.logValidation("Breed name matches", e2eBreedName, name, nameMatches);
        Assert.assertEquals(name, e2eBreedName);

        int id = response.jsonPath().getInt("id");
        boolean idMatches = id == e2eBreedId;
        ExtentReportListener.logValidation("Breed ID matches", e2eBreedId, id, idMatches);
        Assert.assertEquals(id, e2eBreedId);
    }

    @Test(description = "E2E Flow 1 - Step 3: Search images with breed info", priority = 3,
            dependsOnMethods = "e2eFlow1_Step2_GetBreedDetails")
    public void e2eFlow1_Step3_SearchImagesWithBreed() {
        ExtentReportListener.logStep("Step 3: Search images that include breed information");

        Map<String, Object> params = new HashMap<>();
        params.put("has_breeds", true);
        params.put("limit", 5);
        params.put("size", "med");
        params.put("mime_types", "jpg");
        ExtentReportListener.logRequest("GET", "/images/search?has_breeds=true&limit=5&size=med&mime_types=jpg", null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean hasImages = images != null && !images.isEmpty();
        ExtentReportListener.logValidation("Images returned", "Non-empty array",
                images != null ? images.size() + " images" : "null", hasImages);
        Assert.assertTrue(hasImages, "Should return images");

        // Pick first image for favouriting and voting
        e2eImageId = response.jsonPath().getString("[0].id");
        String imageUrl = response.jsonPath().getString("[0].url");
        ExtentReportListener.logInfo("Selected image: " + e2eImageId + " (URL: " + imageUrl + ")");

        // Validate breeds data is present
        List<?> breeds = response.jsonPath().getList("[0].breeds");
        boolean hasBreedData = breeds != null && !breeds.isEmpty();
        ExtentReportListener.logValidation("Image has breed data", "Non-empty breeds array",
                breeds != null ? breeds.size() + " breeds" : "null", hasBreedData);
        Assert.assertTrue(hasBreedData, "Image should have breed data");
    }

    @Test(description = "E2E Flow 1 - Step 4: Get image details by ID", priority = 4,
            dependsOnMethods = "e2eFlow1_Step3_SearchImagesWithBreed")
    public void e2eFlow1_Step4_GetImageDetails() {
        ExtentReportListener.logStep("Step 4: Get detailed image info for: " + e2eImageId);
        ExtentReportListener.logRequest("GET", "/images/" + e2eImageId, null);

        Response response = apiHelper.get("/images/" + e2eImageId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        String returnedId = response.jsonPath().getString("id");
        boolean idMatches = e2eImageId.equals(returnedId);
        ExtentReportListener.logValidation("Image ID matches", e2eImageId, returnedId, idMatches);
        Assert.assertEquals(returnedId, e2eImageId);

        String url = response.jsonPath().getString("url");
        boolean hasUrl = url != null && url.startsWith("http");
        ExtentReportListener.logValidation("Image has valid URL", "URL starting with http", url, hasUrl);
        Assert.assertTrue(hasUrl);
    }

    @Test(description = "E2E Flow 1 - Step 5: Favourite the image", priority = 5,
            dependsOnMethods = "e2eFlow1_Step4_GetImageDetails")
    public void e2eFlow1_Step5_FavouriteImage() {
        ExtentReportListener.logStep("Step 5: Add image " + e2eImageId + " to favourites");

        String requestBody = String.format("{\"image_id\": \"%s\", \"sub_id\": \"e2e-test-user\"}", e2eImageId);
        ExtentReportListener.logRequest("POST", "/favourites", requestBody);

        Response response = apiHelper.post("/favourites", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        String message = response.jsonPath().getString("message");
        boolean messageOk = "SUCCESS".equals(message);
        ExtentReportListener.logValidation("Message", "SUCCESS", message, messageOk);
        Assert.assertEquals(message, "SUCCESS");

        e2eFavouriteId = response.jsonPath().getInt("id");
        ExtentReportListener.logInfo("Created favourite ID: " + e2eFavouriteId);
    }

    @Test(description = "E2E Flow 1 - Step 6: Verify favourite appears in list", priority = 6,
            dependsOnMethods = "e2eFlow1_Step5_FavouriteImage")
    public void e2eFlow1_Step6_VerifyFavouriteInList() {
        ExtentReportListener.logStep("Step 6: Verify favourite appears in favourites list");
        ExtentReportListener.logRequest("GET", "/favourites", null);

        Response response = apiHelper.get("/favourites");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        // Check if our favourite is in the list
        List<Integer> ids = response.jsonPath().getList("id");
        boolean containsFavourite = ids != null && ids.contains(e2eFavouriteId);
        ExtentReportListener.logValidation("Favourite ID in list", "Contains " + e2eFavouriteId,
                ids != null ? "List of " + ids.size() + " IDs" : "null", containsFavourite);
        Assert.assertTrue(containsFavourite, "Created favourite should appear in favourites list");
    }

    @Test(description = "E2E Flow 1 - Step 7: Upvote the same image", priority = 7,
            dependsOnMethods = "e2eFlow1_Step5_FavouriteImage")
    public void e2eFlow1_Step7_VoteOnImage() {
        ExtentReportListener.logStep("Step 7: Upvote image " + e2eImageId);

        String requestBody = String.format("{\"image_id\": \"%s\", \"sub_id\": \"e2e-test-user\", \"value\": 1}", e2eImageId);
        ExtentReportListener.logRequest("POST", "/votes", requestBody);

        Response response = apiHelper.post("/votes", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200 || response.getStatusCode() == 201;
        ExtentReportListener.logValidation("Status Code", "200 or 201", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk, "Create vote should return success");

        String message = response.jsonPath().getString("message");
        boolean messageOk = "SUCCESS".equals(message);
        ExtentReportListener.logValidation("Message", "SUCCESS", message, messageOk);

        e2eVoteId = response.jsonPath().getInt("id");
        ExtentReportListener.logInfo("Created vote ID: " + e2eVoteId);
    }

    @Test(description = "E2E Flow 1 - Step 8: Verify vote details", priority = 8,
            dependsOnMethods = "e2eFlow1_Step7_VoteOnImage")
    public void e2eFlow1_Step8_VerifyVoteDetails() {
        ExtentReportListener.logStep("Step 8: Verify vote details for vote ID: " + e2eVoteId);
        ExtentReportListener.logRequest("GET", "/votes/" + e2eVoteId, null);

        Response response = apiHelper.get("/votes/" + e2eVoteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        String imageId = response.jsonPath().getString("image_id");
        boolean imageIdMatches = e2eImageId.equals(imageId);
        ExtentReportListener.logValidation("Vote image_id matches", e2eImageId, imageId, imageIdMatches);
        Assert.assertEquals(imageId, e2eImageId);

        int value = response.jsonPath().getInt("value");
        boolean valueOk = value == 1;
        ExtentReportListener.logValidation("Vote value is upvote", 1, value, valueOk);
        Assert.assertEquals(value, 1);
    }

    @Test(description = "E2E Flow 1 - Step 9: Cleanup - Delete vote", priority = 9,
            dependsOnMethods = "e2eFlow1_Step8_VerifyVoteDetails")
    public void e2eFlow1_Step9_DeleteVote() {
        ExtentReportListener.logStep("Step 9: Cleanup - Delete vote " + e2eVoteId);
        ExtentReportListener.logRequest("DELETE", "/votes/" + e2eVoteId, null);

        Response response = apiHelper.delete("/votes/" + e2eVoteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        String message = response.jsonPath().getString("message");
        boolean messageOk = "SUCCESS".equals(message);
        ExtentReportListener.logValidation("Delete message", "SUCCESS", message, messageOk);
        Assert.assertEquals(message, "SUCCESS");
    }

    @Test(description = "E2E Flow 1 - Step 10: Cleanup - Delete favourite", priority = 10,
            dependsOnMethods = "e2eFlow1_Step9_DeleteVote")
    public void e2eFlow1_Step10_DeleteFavourite() {
        ExtentReportListener.logStep("Step 10: Cleanup - Delete favourite " + e2eFavouriteId);
        ExtentReportListener.logRequest("DELETE", "/favourites/" + e2eFavouriteId, null);

        Response response = apiHelper.delete("/favourites/" + e2eFavouriteId);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        String message = response.jsonPath().getString("message");
        boolean messageOk = "SUCCESS".equals(message);
        ExtentReportListener.logValidation("Delete message", "SUCCESS", message, messageOk);
        Assert.assertEquals(message, "SUCCESS");
    }

    @Test(description = "E2E Flow 1 - Step 11: Verify cleanup - favourite deleted", priority = 11,
            dependsOnMethods = "e2eFlow1_Step10_DeleteFavourite")
    public void e2eFlow1_Step11_VerifyCleanup() {
        ExtentReportListener.logStep("Step 11: Verify cleanup - favourite and vote are deleted");

        // Verify favourite is deleted
        ExtentReportListener.logRequest("GET", "/favourites/" + e2eFavouriteId, null);
        Response favResponse = apiHelper.get("/favourites/" + e2eFavouriteId);
        ExtentReportListener.logResponse(favResponse.getStatusCode(), favResponse.getBody().asString());

        boolean favDeleted = favResponse.getStatusCode() == 400 || favResponse.getStatusCode() == 404;
        ExtentReportListener.logValidation("Favourite deleted", "400 or 404", 
                String.valueOf(favResponse.getStatusCode()), favDeleted);
        Assert.assertTrue(favDeleted, "Favourite should be deleted");

        // Verify vote is deleted
        ExtentReportListener.logRequest("GET", "/votes/" + e2eVoteId, null);
        Response voteResponse = apiHelper.get("/votes/" + e2eVoteId);
        ExtentReportListener.logResponse(voteResponse.getStatusCode(), voteResponse.getBody().asString());

        boolean voteDeleted = voteResponse.getStatusCode() == 400 || voteResponse.getStatusCode() == 404;
        ExtentReportListener.logValidation("Vote deleted", "400 or 404",
                String.valueOf(voteResponse.getStatusCode()), voteDeleted);
        Assert.assertTrue(voteDeleted, "Vote should be deleted");

        ExtentReportListener.logInfo("E2E Flow 1 completed successfully - all resources cleaned up!");
    }

    // ==========================================
    // E2E Flow 2: Image Search Variations → Vote → Change Vote → Cleanup
    // ==========================================

    private String e2eFlow2ImageId;
    private int e2eFlow2VoteId1;
    private int e2eFlow2VoteId2;

    @Test(description = "E2E Flow 2 - Step 1: Search images with different size params", priority = 12)
    public void e2eFlow2_Step1_SearchWithSizes() {
        ExtentReportListener.logStep("E2E Flow 2: Search Variations → Vote → Change Vote → Cleanup");
        ExtentReportListener.logStep("Step 1: Search images with size=thumb");

        Map<String, Object> params = new HashMap<>();
        params.put("size", "thumb");
        params.put("limit", 3);
        ExtentReportListener.logRequest("GET", "/images/search?size=thumb&limit=3", null);

        Response thumbResponse = apiHelper.get("/images/search", params);
        ExtentReportListener.logResponse(thumbResponse.getStatusCode(), thumbResponse.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = thumbResponse.getStatusCode() == 200;
        ExtentReportListener.logValidation("Thumb search status", 200, thumbResponse.getStatusCode(), statusOk);
        Assert.assertEquals(thumbResponse.getStatusCode(), 200);

        // Now search with full size
        params.put("size", "full");
        ExtentReportListener.logRequest("GET", "/images/search?size=full&limit=3", null);

        Response fullResponse = apiHelper.get("/images/search", params);
        ExtentReportListener.logResponse(fullResponse.getStatusCode(), fullResponse.getBody().asString());

        boolean fullStatusOk = fullResponse.getStatusCode() == 200;
        ExtentReportListener.logValidation("Full search status", 200, fullResponse.getStatusCode(), fullStatusOk);
        Assert.assertEquals(fullResponse.getStatusCode(), 200);

        // Pick an image for subsequent steps
        e2eFlow2ImageId = thumbResponse.jsonPath().getString("[0].id");
        ExtentReportListener.logInfo("Selected image for voting: " + e2eFlow2ImageId);
    }

    @Test(description = "E2E Flow 2 - Step 2: Upvote the image", priority = 13,
            dependsOnMethods = "e2eFlow2_Step1_SearchWithSizes")
    public void e2eFlow2_Step2_UpvoteImage() {
        ExtentReportListener.logStep("Step 2: Upvote image " + e2eFlow2ImageId);

        String requestBody = String.format(
                "{\"image_id\": \"%s\", \"sub_id\": \"e2e-flow2-user\", \"value\": 1}", e2eFlow2ImageId);
        ExtentReportListener.logRequest("POST", "/votes", requestBody);

        Response response = apiHelper.post("/votes", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200 || response.getStatusCode() == 201;
        ExtentReportListener.logValidation("Status Code", "200 or 201", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk);

        e2eFlow2VoteId1 = response.jsonPath().getInt("id");
        ExtentReportListener.logInfo("Upvote created with ID: " + e2eFlow2VoteId1);
    }

    @Test(description = "E2E Flow 2 - Step 3: Verify upvote in votes list", priority = 14,
            dependsOnMethods = "e2eFlow2_Step2_UpvoteImage")
    public void e2eFlow2_Step3_VerifyUpvoteInList() {
        ExtentReportListener.logStep("Step 3: Verify upvote appears in votes list");
        ExtentReportListener.logRequest("GET", "/votes", null);

        Response response = apiHelper.get("/votes");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<Integer> ids = response.jsonPath().getList("id");
        boolean containsVote = ids != null && ids.contains(e2eFlow2VoteId1);
        ExtentReportListener.logValidation("Vote ID in list", "Contains " + e2eFlow2VoteId1,
                ids != null ? "List of " + ids.size() + " vote IDs" : "null", containsVote);
        Assert.assertTrue(containsVote, "Upvote should appear in votes list");
    }

    @Test(description = "E2E Flow 2 - Step 4: Change vote to downvote (new vote)", priority = 15,
            dependsOnMethods = "e2eFlow2_Step3_VerifyUpvoteInList")
    public void e2eFlow2_Step4_ChangeToDownvote() {
        ExtentReportListener.logStep("Step 4: Cast a downvote on same image (simulating vote change)");

        String requestBody = String.format(
                "{\"image_id\": \"%s\", \"sub_id\": \"e2e-flow2-user\", \"value\": 0}", e2eFlow2ImageId);
        ExtentReportListener.logRequest("POST", "/votes", requestBody);

        Response response = apiHelper.post("/votes", requestBody);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200 || response.getStatusCode() == 201;
        ExtentReportListener.logValidation("Status Code", "200 or 201", response.getStatusCode(), statusOk);
        Assert.assertTrue(statusOk);

        e2eFlow2VoteId2 = response.jsonPath().getInt("id");
        ExtentReportListener.logInfo("Downvote created with ID: " + e2eFlow2VoteId2);
    }

    @Test(description = "E2E Flow 2 - Step 5: Verify downvote details", priority = 16,
            dependsOnMethods = "e2eFlow2_Step4_ChangeToDownvote")
    public void e2eFlow2_Step5_VerifyDownvote() {
        ExtentReportListener.logStep("Step 5: Verify downvote details");
        ExtentReportListener.logRequest("GET", "/votes/" + e2eFlow2VoteId2, null);

        Response response = apiHelper.get("/votes/" + e2eFlow2VoteId2);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        int value = response.jsonPath().getInt("value");
        boolean isDownvote = value == 0;
        ExtentReportListener.logValidation("Vote value is downvote", 0, value, isDownvote);
        Assert.assertEquals(value, 0, "Vote should be a downvote (value=0)");

        String imageId = response.jsonPath().getString("image_id");
        boolean imageMatches = e2eFlow2ImageId.equals(imageId);
        ExtentReportListener.logValidation("Image ID matches", e2eFlow2ImageId, imageId, imageMatches);
        Assert.assertEquals(imageId, e2eFlow2ImageId);
    }

    @Test(description = "E2E Flow 2 - Step 6: Cleanup - Delete both votes", priority = 17,
            dependsOnMethods = "e2eFlow2_Step5_VerifyDownvote")
    public void e2eFlow2_Step6_Cleanup() {
        ExtentReportListener.logStep("Step 6: Cleanup - Delete both upvote and downvote");

        // Delete upvote
        ExtentReportListener.logRequest("DELETE", "/votes/" + e2eFlow2VoteId1, null);
        Response deleteUpvote = apiHelper.delete("/votes/" + e2eFlow2VoteId1);
        ExtentReportListener.logResponse(deleteUpvote.getStatusCode(), deleteUpvote.getBody().asString());

        boolean upvoteDeleted = deleteUpvote.getStatusCode() == 200;
        ExtentReportListener.logValidation("Upvote deleted", 200, deleteUpvote.getStatusCode(), upvoteDeleted);
        Assert.assertEquals(deleteUpvote.getStatusCode(), 200);

        // Delete downvote
        ExtentReportListener.logRequest("DELETE", "/votes/" + e2eFlow2VoteId2, null);
        Response deleteDownvote = apiHelper.delete("/votes/" + e2eFlow2VoteId2);
        ExtentReportListener.logResponse(deleteDownvote.getStatusCode(), deleteDownvote.getBody().asString());

        boolean downvoteDeleted = deleteDownvote.getStatusCode() == 200;
        ExtentReportListener.logValidation("Downvote deleted", 200, deleteDownvote.getStatusCode(), downvoteDeleted);
        Assert.assertEquals(deleteDownvote.getStatusCode(), 200);

        ExtentReportListener.logInfo("E2E Flow 2 completed - all votes cleaned up!");
    }

    // ==========================================
    // E2E Flow 3: Multi-Favourite → List → Paginate → Cleanup
    // ==========================================

    private int[] multipleE2eFavouriteIds = new int[3];
    private String[] multipleE2eImageIds = new String[3];

    @Test(description = "E2E Flow 3 - Step 1: Search multiple images", priority = 18)
    public void e2eFlow3_Step1_SearchMultipleImages() {
        ExtentReportListener.logStep("E2E Flow 3: Multi-Favourite → List → Verify → Cleanup");
        ExtentReportListener.logStep("Step 1: Search for multiple images");

        Map<String, Object> params = new HashMap<>();
        params.put("limit", 3);
        params.put("order", "RANDOM");
        ExtentReportListener.logRequest("GET", "/images/search?limit=3&order=RANDOM", null);

        Response response = apiHelper.get("/images/search", params);

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<?> images = response.jsonPath().getList("$");
        boolean hasEnoughImages = images != null && images.size() >= 3;
        ExtentReportListener.logValidation("Got 3+ images", ">= 3",
                images != null ? String.valueOf(images.size()) : "0", hasEnoughImages);
        Assert.assertTrue(hasEnoughImages, "Should get at least 3 images");

        for (int i = 0; i < 3; i++) {
            multipleE2eImageIds[i] = response.jsonPath().getString("[" + i + "].id");
        }
        ExtentReportListener.logInfo("Selected images: " + String.join(", ", multipleE2eImageIds));
    }

    @Test(description = "E2E Flow 3 - Step 2: Create 3 favourites", priority = 19,
            dependsOnMethods = "e2eFlow3_Step1_SearchMultipleImages")
    public void e2eFlow3_Step2_CreateMultipleFavourites() {
        ExtentReportListener.logStep("Step 2: Create 3 favourites for different images");

        for (int i = 0; i < 3; i++) {
            String requestBody = String.format(
                    "{\"image_id\": \"%s\", \"sub_id\": \"e2e-flow3-user\"}", multipleE2eImageIds[i]);
            ExtentReportListener.logRequest("POST", "/favourites", requestBody);

            Response response = apiHelper.post("/favourites", requestBody);
            ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());

            boolean statusOk = response.getStatusCode() == 200;
            ExtentReportListener.logValidation("Favourite #" + (i + 1) + " created", 200,
                    response.getStatusCode(), statusOk);
            Assert.assertEquals(response.getStatusCode(), 200);

            multipleE2eFavouriteIds[i] = response.jsonPath().getInt("id");
            ExtentReportListener.logInfo("Created favourite #" + (i + 1) + " with ID: " + multipleE2eFavouriteIds[i]);
        }
    }

    @Test(description = "E2E Flow 3 - Step 3: Verify all favourites in list", priority = 20,
            dependsOnMethods = "e2eFlow3_Step2_CreateMultipleFavourites")
    public void e2eFlow3_Step3_VerifyAllFavouritesInList() {
        ExtentReportListener.logStep("Step 3: Verify all 3 favourites appear in list");
        ExtentReportListener.logRequest("GET", "/favourites", null);

        Response response = apiHelper.get("/favourites");

        ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, response.getStatusCode(), statusOk);
        Assert.assertEquals(response.getStatusCode(), 200);

        List<Integer> ids = response.jsonPath().getList("id");
        for (int i = 0; i < 3; i++) {
            boolean found = ids != null && ids.contains(multipleE2eFavouriteIds[i]);
            ExtentReportListener.logValidation("Favourite #" + (i + 1) + " in list",
                    "Contains " + multipleE2eFavouriteIds[i],
                    found ? "Found" : "Not found", found);
            Assert.assertTrue(found, "Favourite #" + (i + 1) + " should be in the list");
        }
    }

    @Test(description = "E2E Flow 3 - Step 4: Get each favourite by ID and verify", priority = 21,
            dependsOnMethods = "e2eFlow3_Step2_CreateMultipleFavourites")
    public void e2eFlow3_Step4_VerifyEachFavourite() {
        ExtentReportListener.logStep("Step 4: Verify each favourite individually");

        for (int i = 0; i < 3; i++) {
            ExtentReportListener.logRequest("GET", "/favourites/" + multipleE2eFavouriteIds[i], null);

            Response response = apiHelper.get("/favourites/" + multipleE2eFavouriteIds[i]);
            ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());

            boolean statusOk = response.getStatusCode() == 200;
            ExtentReportListener.logValidation("Favourite #" + (i + 1) + " status", 200,
                    response.getStatusCode(), statusOk);
            Assert.assertEquals(response.getStatusCode(), 200);

            String imageId = response.jsonPath().getString("image_id");
            boolean imageMatches = multipleE2eImageIds[i].equals(imageId);
            ExtentReportListener.logValidation("Favourite #" + (i + 1) + " image_id",
                    multipleE2eImageIds[i], imageId, imageMatches);
            Assert.assertEquals(imageId, multipleE2eImageIds[i]);
        }
    }

    @Test(description = "E2E Flow 3 - Step 5: Cleanup - Delete all 3 favourites", priority = 22,
            dependsOnMethods = {"e2eFlow3_Step3_VerifyAllFavouritesInList", "e2eFlow3_Step4_VerifyEachFavourite"})
    public void e2eFlow3_Step5_CleanupAll() {
        ExtentReportListener.logStep("Step 5: Cleanup - Delete all 3 favourites");

        for (int i = 0; i < 3; i++) {
            ExtentReportListener.logRequest("DELETE", "/favourites/" + multipleE2eFavouriteIds[i], null);

            Response response = apiHelper.delete("/favourites/" + multipleE2eFavouriteIds[i]);
            ExtentReportListener.logResponse(response.getStatusCode(), response.getBody().asString());

            boolean deleted = response.getStatusCode() == 200;
            ExtentReportListener.logValidation("Favourite #" + (i + 1) + " deleted", 200,
                    response.getStatusCode(), deleted);
            Assert.assertEquals(response.getStatusCode(), 200);
        }

        // Verify all are deleted
        for (int i = 0; i < 3; i++) {
            Response verifyResponse = apiHelper.get("/favourites/" + multipleE2eFavouriteIds[i]);
            boolean isDeleted = verifyResponse.getStatusCode() == 400 || verifyResponse.getStatusCode() == 404;
            ExtentReportListener.logValidation("Favourite #" + (i + 1) + " confirmed deleted",
                    "400 or 404", String.valueOf(verifyResponse.getStatusCode()), isDeleted);
            Assert.assertTrue(isDeleted);
        }

        ExtentReportListener.logInfo("E2E Flow 3 completed - all favourites cleaned up!");
    }

    // ==========================================
    // E2E Flow 4: Breed Pagination → Image Search by Breed → Breed Details Cross-Validation
    // ==========================================

    @Test(description = "E2E Flow 4 - Step 1: Paginate through breeds", priority = 23)
    public void e2eFlow4_Step1_PaginateBreeds() {
        ExtentReportListener.logStep("E2E Flow 4: Breed Pagination → Image Search → Cross-Validation");
        ExtentReportListener.logStep("Step 1: Paginate through breeds (page 0 and page 1)");

        Map<String, Object> page0 = new HashMap<>();
        page0.put("limit", 5);
        page0.put("page", 0);
        ExtentReportListener.logRequest("GET", "/breeds?limit=5&page=0", null);

        Response page0Response = apiHelper.get("/breeds", page0);
        ExtentReportListener.logResponse(page0Response.getStatusCode(), page0Response.getBody().asString());

        Map<String, Object> page1 = new HashMap<>();
        page1.put("limit", 5);
        page1.put("page", 1);
        ExtentReportListener.logRequest("GET", "/breeds?limit=5&page=1", null);

        Response page1Response = apiHelper.get("/breeds", page1);
        ExtentReportListener.logResponse(page1Response.getStatusCode(), page1Response.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean bothOk = page0Response.getStatusCode() == 200 && page1Response.getStatusCode() == 200;
        ExtentReportListener.logValidation("Both pages return 200", "200 & 200",
                page0Response.getStatusCode() + " & " + page1Response.getStatusCode(), bothOk);
        Assert.assertTrue(bothOk);

        // Ensure page 0 and page 1 don't overlap
        List<Integer> page0Ids = page0Response.jsonPath().getList("id");
        List<Integer> page1Ids = page1Response.jsonPath().getList("id");
        boolean noOverlap = true;
        if (page0Ids != null && page1Ids != null) {
            for (Integer id : page0Ids) {
                if (page1Ids.contains(id)) {
                    noOverlap = false;
                    break;
                }
            }
        }
        ExtentReportListener.logValidation("No overlap between pages", "No common IDs",
                "Page0: " + page0Ids + ", Page1: " + page1Ids, noOverlap);
        Assert.assertTrue(noOverlap, "Pages should not have overlapping breed IDs");
    }

    @Test(description = "E2E Flow 4 - Step 2: Search images with breed data and cross-validate", priority = 24)
    public void e2eFlow4_Step2_CrossValidateBreedAndImage() {
        ExtentReportListener.logStep("Step 2: Search images with breed data and cross-validate breed info");

        Map<String, Object> params = new HashMap<>();
        params.put("has_breeds", true);
        params.put("limit", 1);
        ExtentReportListener.logRequest("GET", "/images/search?has_breeds=true&limit=1", null);

        Response imageResponse = apiHelper.get("/images/search", params);
        ExtentReportListener.logResponse(imageResponse.getStatusCode(), imageResponse.getBody().asString());
        ExtentReportListener.logRequestResponse(apiHelper.getLastRequestLog(), apiHelper.getLastResponseLog());

        boolean statusOk = imageResponse.getStatusCode() == 200;
        ExtentReportListener.logValidation("Status Code", 200, imageResponse.getStatusCode(), statusOk);
        Assert.assertEquals(imageResponse.getStatusCode(), 200);

        // Extract breed from image
        int breedIdFromImage = imageResponse.jsonPath().getInt("[0].breeds[0].id");
        String breedNameFromImage = imageResponse.jsonPath().getString("[0].breeds[0].name");
        ExtentReportListener.logInfo("Breed from image: " + breedNameFromImage + " (ID: " + breedIdFromImage + ")");

        // Now fetch breed directly and compare
        ExtentReportListener.logRequest("GET", "/breeds/" + breedIdFromImage, null);
        Response breedResponse = apiHelper.get("/breeds/" + breedIdFromImage);
        ExtentReportListener.logResponse(breedResponse.getStatusCode(), breedResponse.getBody().asString());

        boolean breedStatusOk = breedResponse.getStatusCode() == 200;
        ExtentReportListener.logValidation("Breed API status", 200, breedResponse.getStatusCode(), breedStatusOk);
        Assert.assertEquals(breedResponse.getStatusCode(), 200);

        String breedNameDirect = breedResponse.jsonPath().getString("name");
        boolean namesMatch = breedNameFromImage.equals(breedNameDirect);
        ExtentReportListener.logValidation("Breed names match (image vs breed API)",
                breedNameFromImage, breedNameDirect, namesMatch);
        Assert.assertEquals(breedNameDirect, breedNameFromImage,
                "Breed name from image and breed API should match");

        ExtentReportListener.logInfo("E2E Flow 4 completed - breed data is consistent across APIs!");
    }
}
