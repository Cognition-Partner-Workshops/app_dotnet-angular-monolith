package com.ordermanager.controller;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ordermanager.model.Product;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

import java.math.BigDecimal;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
class ProductsControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Autowired
    private ObjectMapper objectMapper;

    @Test
    void getAllProducts_returnsListWithCamelCaseFields() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/products", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());
        assertTrue(json.size() >= 5, "Seed data should have at least 5 products");

        JsonNode first = json.get(0);
        assertNotNull(first.get("id"), "Should have 'id' field");
        assertNotNull(first.get("name"), "Should have 'name' field");
        assertNotNull(first.get("description"), "Should have 'description' field");
        assertNotNull(first.get("category"), "Should have 'category' field");
        assertNotNull(first.get("price"), "Should have 'price' field");
        assertNotNull(first.get("sku"), "Should have 'sku' field");
    }

    @Test
    void getAllProducts_priceSerializesAsNumber() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/products", String.class);
        JsonNode json = parseJson(response.getBody());

        JsonNode first = json.get(0);
        assertTrue(first.get("price").isNumber(), "Price should be a number, not a string");

        // Verify BigDecimal doesn't have trailing zeros issues
        double price = first.get("price").asDouble();
        assertTrue(price > 0, "Price should be positive");
    }

    @Test
    void getAllProducts_includesNestedInventory() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/products", String.class);
        JsonNode json = parseJson(response.getBody());

        // Angular uses p.inventory?.quantityOnHand
        JsonNode firstProduct = json.get(0);
        JsonNode inventory = firstProduct.get("inventory");
        assertNotNull(inventory, "Product should include nested 'inventory' object");
        assertNotNull(inventory.get("quantityOnHand"), "Inventory should have 'quantityOnHand'");
        assertNotNull(inventory.get("reorderLevel"), "Inventory should have 'reorderLevel'");
        assertNotNull(inventory.get("warehouseLocation"), "Inventory should have 'warehouseLocation'");
    }

    @Test
    void getProductById_returnsProductWithCorrectShape() {
        ResponseEntity<String> listResponse = restTemplate.getForEntity("/api/products", String.class);
        JsonNode list = parseJson(listResponse.getBody());
        long productId = list.get(0).get("id").asLong();

        ResponseEntity<String> response = restTemplate.getForEntity("/api/products/" + productId, String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode product = parseJson(response.getBody());
        assertTrue(product.get("id").isNumber());
        assertTrue(product.get("name").isTextual());
        assertTrue(product.get("price").isNumber());
        assertTrue(product.get("sku").isTextual());
    }

    @Test
    void getProductById_returns404ForMissingProduct() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/products/99999", String.class);
        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void getProductsByCategory_returnsFilteredList() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/products/category/Widgets", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());
        assertTrue(json.size() >= 2, "Should have at least 2 widgets from seed data");

        for (JsonNode product : json) {
            assertEquals("Widgets", product.get("category").asText());
        }
    }

    @Test
    void getProductsByCategory_returnsEmptyListForUnknownCategory() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/products/category/NonExistent", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());
        assertEquals(0, json.size());
    }

    @Test
    void createProduct_returnsCreatedWithCorrectShape() {
        Product newProduct = new Product();
        newProduct.setName("Test Product");
        newProduct.setDescription("A test product");
        newProduct.setCategory("TestCategory");
        newProduct.setPrice(new BigDecimal("25.50"));
        newProduct.setSku("TST-" + System.currentTimeMillis());

        ResponseEntity<String> response = restTemplate.postForEntity("/api/products", newProduct, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());
        assertNotNull(response.getHeaders().getLocation(), "Should include Location header");

        JsonNode created = parseJson(response.getBody());
        assertNotNull(created.get("id"), "Created product should have 'id'");
        assertEquals("Test Product", created.get("name").asText());
        assertEquals("A test product", created.get("description").asText());
        assertEquals("TestCategory", created.get("category").asText());
        assertEquals(25.50, created.get("price").asDouble(), 0.01);
    }

    @Test
    void createProduct_returnsValidationErrorForMissingName() {
        Product invalid = new Product();
        invalid.setSku("INVALID-" + System.currentTimeMillis());
        // name is missing (required)

        ResponseEntity<String> response = restTemplate.postForEntity("/api/products", invalid, String.class);
        assertTrue(
                response.getStatusCode().is4xxClientError(),
                "Should return 4xx for invalid product, got: " + response.getStatusCode()
        );
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
