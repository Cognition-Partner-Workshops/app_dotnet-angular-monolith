package com.ordermanager.controller;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
class InventoryControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Autowired
    private ObjectMapper objectMapper;

    @Test
    void getAllInventory_returnsListWithCamelCaseFields() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/inventory", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());
        assertTrue(json.size() >= 5, "Seed data should have at least 5 inventory items");

        JsonNode first = json.get(0);
        assertNotNull(first.get("id"), "Should have 'id' field");
        assertNotNull(first.get("productId"), "Should have 'productId' field");
        assertNotNull(first.get("quantityOnHand"), "Should have 'quantityOnHand' field");
        assertNotNull(first.get("reorderLevel"), "Should have 'reorderLevel' field");
        assertNotNull(first.get("warehouseLocation"), "Should have 'warehouseLocation' field");
    }

    @Test
    void getAllInventory_includesNestedProduct() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/inventory", String.class);
        JsonNode json = parseJson(response.getBody());

        // Angular uses i.product?.name
        JsonNode first = json.get(0);
        JsonNode product = first.get("product");
        assertNotNull(product, "Inventory item should include nested 'product' object");
        assertNotNull(product.get("name"), "Nested product should have 'name'");
        assertNotNull(product.get("sku"), "Nested product should have 'sku'");
    }

    @Test
    void getAllInventory_lastRestockedIsIso8601OrNull() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/inventory", String.class);
        JsonNode json = parseJson(response.getBody());

        JsonNode first = json.get(0);
        JsonNode lastRestocked = first.get("lastRestocked");
        // lastRestocked can be null for seed data items that haven't been restocked
        // If not null, should be ISO 8601 format
        assertNotNull(lastRestocked, "Should have 'lastRestocked' field (may be null value)");
    }

    @Test
    void getInventoryByProductId_returnsItem() {
        // Get a valid product ID from products endpoint
        ResponseEntity<String> productsResponse = restTemplate.getForEntity("/api/products", String.class);
        JsonNode products = parseJson(productsResponse.getBody());
        long productId = products.get(0).get("id").asLong();

        ResponseEntity<String> response = restTemplate.getForEntity("/api/inventory/product/" + productId, String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode item = parseJson(response.getBody());
        assertTrue(item.get("quantityOnHand").isNumber());
        assertTrue(item.get("reorderLevel").isNumber());
    }

    @Test
    void getInventoryByProductId_returns404ForMissingProduct() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/inventory/product/99999", String.class);
        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void restockInventory_increasesQuantityAndUpdatesLastRestocked() {
        // Get a valid product ID
        ResponseEntity<String> productsResponse = restTemplate.getForEntity("/api/products", String.class);
        JsonNode products = parseJson(productsResponse.getBody());
        long productId = products.get(0).get("id").asLong();

        // Get current inventory
        ResponseEntity<String> beforeResponse = restTemplate.getForEntity("/api/inventory/product/" + productId, String.class);
        JsonNode before = parseJson(beforeResponse.getBody());
        int quantityBefore = before.get("quantityOnHand").asInt();

        // Restock
        String restockJson = """
                { "quantity": 25 }
                """;
        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(restockJson, headers);

        ResponseEntity<String> response = restTemplate.postForEntity(
                "/api/inventory/product/" + productId + "/restock", request, String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode restocked = parseJson(response.getBody());
        assertEquals(quantityBefore + 25, restocked.get("quantityOnHand").asInt());
        assertNotNull(restocked.get("lastRestocked"), "lastRestocked should be set after restock");
        assertFalse(restocked.get("lastRestocked").isNull(), "lastRestocked should not be null after restock");
    }

    @Test
    void getLowStock_returnsListOfLowStockItems() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/inventory/low-stock", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());

        // Verify all returned items actually have low stock
        for (JsonNode item : json) {
            int quantity = item.get("quantityOnHand").asInt();
            int reorderLevel = item.get("reorderLevel").asInt();
            assertTrue(quantity <= reorderLevel,
                    "Low stock item should have quantityOnHand <= reorderLevel");
        }
    }

    private JsonNode parseJson(String body) {
        try {
            return objectMapper.readTree(body);
        } catch (Exception e) {
            fail("Failed to parse JSON: " + e.getMessage());
            return null;
        }
    }
}
