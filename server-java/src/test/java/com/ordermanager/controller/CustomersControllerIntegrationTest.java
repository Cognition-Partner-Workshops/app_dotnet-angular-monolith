package com.ordermanager.controller;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ordermanager.model.Customer;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.test.context.ActiveProfiles;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
class CustomersControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @Autowired
    private ObjectMapper objectMapper;

    @Test
    void getAllCustomers_returnsListWithCamelCaseFields() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/customers", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());
        assertTrue(json.size() >= 3, "Seed data should have at least 3 customers");

        JsonNode first = json.get(0);
        assertNotNull(first.get("id"), "Should have 'id' field");
        assertNotNull(first.get("name"), "Should have 'name' field");
        assertNotNull(first.get("email"), "Should have 'email' field");
        assertNotNull(first.get("phone"), "Should have 'phone' field");
        assertNotNull(first.get("address"), "Should have 'address' field");
        assertNotNull(first.get("city"), "Should have 'city' field");
        assertNotNull(first.get("state"), "Should have 'state' field");
        assertNotNull(first.get("zipCode"), "Should have 'zipCode' field (camelCase)");

        // Verify no PascalCase fields leak through
        assertNull(first.get("ZipCode"), "Should NOT have PascalCase 'ZipCode'");
        assertNull(first.get("Name"), "Should NOT have PascalCase 'Name'");
    }

    @Test
    void getCustomerById_returnsCustomerWithCorrectShape() {
        // First get the list to find a valid ID
        ResponseEntity<String> listResponse = restTemplate.getForEntity("/api/customers", String.class);
        JsonNode list = parseJson(listResponse.getBody());
        long customerId = list.get(0).get("id").asLong();

        ResponseEntity<String> response = restTemplate.getForEntity("/api/customers/" + customerId, String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode customer = parseJson(response.getBody());
        assertTrue(customer.get("id").isNumber());
        assertTrue(customer.get("name").isTextual());
        assertTrue(customer.get("email").isTextual());
    }

    @Test
    void getCustomerById_returns404ForMissingCustomer() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/customers/99999", String.class);
        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void createCustomer_returnsCreatedWithLocationHeader() {
        Customer newCustomer = new Customer();
        newCustomer.setName("Test Customer");
        newCustomer.setEmail("test-" + System.currentTimeMillis() + "@example.com");
        newCustomer.setPhone("555-9999");
        newCustomer.setAddress("999 Test St");
        newCustomer.setCity("Testville");
        newCustomer.setState("TX");
        newCustomer.setZipCode("99999");

        ResponseEntity<String> response = restTemplate.postForEntity("/api/customers", newCustomer, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());
        assertNotNull(response.getHeaders().getLocation(), "Should include Location header");

        JsonNode created = parseJson(response.getBody());
        assertNotNull(created.get("id"), "Created customer should have 'id'");
        assertEquals("Test Customer", created.get("name").asText());
        assertEquals("555-9999", created.get("phone").asText());
        assertEquals("Testville", created.get("city").asText());
        assertEquals("TX", created.get("state").asText());
        assertEquals("99999", created.get("zipCode").asText());
    }

    @Test
    void createCustomer_returnsValidationErrorForMissingName() {
        Customer invalid = new Customer();
        invalid.setEmail("invalid@example.com");
        // name is missing (required)

        ResponseEntity<String> response = restTemplate.postForEntity("/api/customers", invalid, String.class);
        assertTrue(
                response.getStatusCode().is4xxClientError(),
                "Should return 4xx for invalid customer, got: " + response.getStatusCode()
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
