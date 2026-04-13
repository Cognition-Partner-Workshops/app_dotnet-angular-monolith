package com.ordermanager.controller;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.boot.test.web.server.LocalServerPort;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.http.client.HttpComponentsClientHttpRequestFactory;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.web.client.RestTemplate;

import java.time.format.DateTimeFormatter;

import static org.junit.jupiter.api.Assertions.*;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ActiveProfiles("test")
class OrdersControllerIntegrationTest {

    @Autowired
    private TestRestTemplate restTemplate;

    @LocalServerPort
    private int port;

    @Autowired
    private ObjectMapper objectMapper;

    private long customerId;
    private long productId;

    @BeforeEach
    void setUp() {
        // Get a valid customer ID from seed data
        ResponseEntity<String> customersResponse = restTemplate.getForEntity("/api/customers", String.class);
        JsonNode customers = parseJson(customersResponse.getBody());
        customerId = customers.get(0).get("id").asLong();

        // Get a valid product ID from seed data
        ResponseEntity<String> productsResponse = restTemplate.getForEntity("/api/products", String.class);
        JsonNode products = parseJson(productsResponse.getBody());
        productId = products.get(0).get("id").asLong();
    }

    @Test
    void getAllOrders_returnsList() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/orders", String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode json = parseJson(response.getBody());
        assertTrue(json.isArray());
    }

    @Test
    void createOrder_returnsCreatedOrderWithCorrectShape() {
        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 2 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> response = restTemplate.postForEntity("/api/orders", request, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());
        assertNotNull(response.getHeaders().getLocation(), "Should include Location header");

        JsonNode order = parseJson(response.getBody());
        assertNotNull(order.get("id"), "Order should have 'id'");
        assertNotNull(order.get("customerId"), "Order should have 'customerId'");
        assertNotNull(order.get("orderDate"), "Order should have 'orderDate'");
        assertNotNull(order.get("status"), "Order should have 'status'");
        assertNotNull(order.get("totalAmount"), "Order should have 'totalAmount'");
        assertNotNull(order.get("shippingAddress"), "Order should have 'shippingAddress'");
        assertEquals("Pending", order.get("status").asText());
    }

    @Test
    void createOrder_orderDateIsIso8601() {
        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 1 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> response = restTemplate.postForEntity("/api/orders", request, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());

        JsonNode order = parseJson(response.getBody());
        String orderDate = order.get("orderDate").asText();

        // Verify it parses as ISO 8601 date-time
        assertDoesNotThrow(() -> {
            // Should be parsable as ISO 8601
            DateTimeFormatter.ISO_DATE_TIME.parse(orderDate);
        }, "orderDate should be ISO 8601 format, got: " + orderDate);
    }

    @Test
    void createOrder_totalAmountIsCorrect() {
        // Get the product price first
        ResponseEntity<String> productResponse = restTemplate.getForEntity("/api/products/" + productId, String.class);
        JsonNode product = parseJson(productResponse.getBody());
        double unitPrice = product.get("price").asDouble();

        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 3 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> response = restTemplate.postForEntity("/api/orders", request, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());

        JsonNode order = parseJson(response.getBody());
        double totalAmount = order.get("totalAmount").asDouble();
        assertEquals(unitPrice * 3, totalAmount, 0.01, "totalAmount should be unitPrice * quantity");
    }

    @Test
    void createOrder_includesNestedCustomer() {
        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 1 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> response = restTemplate.postForEntity("/api/orders", request, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());

        JsonNode order = parseJson(response.getBody());

        // Angular uses o.customer?.name
        JsonNode customer = order.get("customer");
        assertNotNull(customer, "Order should include nested 'customer' object");
        assertNotNull(customer.get("name"), "Customer should have 'name'");
        assertNotNull(customer.get("email"), "Customer should have 'email'");
    }

    @Test
    void createOrder_includesNestedItemsWithProduct() {
        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 1 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> response = restTemplate.postForEntity("/api/orders", request, String.class);
        assertEquals(HttpStatus.CREATED, response.getStatusCode());

        JsonNode order = parseJson(response.getBody());

        // Verify order.items[].product.name
        JsonNode items = order.get("items");
        assertNotNull(items, "Order should include 'items' array");
        assertTrue(items.isArray());
        assertTrue(items.size() > 0);

        JsonNode firstItem = items.get(0);
        assertNotNull(firstItem.get("id"), "OrderItem should have 'id'");
        assertNotNull(firstItem.get("productId"), "OrderItem should have 'productId'");
        assertNotNull(firstItem.get("quantity"), "OrderItem should have 'quantity'");
        assertNotNull(firstItem.get("unitPrice"), "OrderItem should have 'unitPrice'");

        JsonNode itemProduct = firstItem.get("product");
        assertNotNull(itemProduct, "OrderItem should include nested 'product'");
        assertNotNull(itemProduct.get("name"), "Nested product should have 'name'");
    }

    @Test
    void getOrderById_returnsOrderWithFullGraph() {
        // Create an order first
        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 1 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> request = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> createResponse = restTemplate.postForEntity("/api/orders", request, String.class);
        JsonNode createdOrder = parseJson(createResponse.getBody());
        long orderId = createdOrder.get("id").asLong();

        // Now GET the order by ID
        ResponseEntity<String> response = restTemplate.getForEntity("/api/orders/" + orderId, String.class);
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode order = parseJson(response.getBody());
        assertNotNull(order.get("customer"), "GET order by ID should include nested customer");
        assertNotNull(order.get("items"), "GET order by ID should include items");
    }

    @Test
    void getOrderById_returns404ForMissingOrder() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/orders/99999", String.class);
        assertEquals(HttpStatus.NOT_FOUND, response.getStatusCode());
    }

    @Test
    void updateOrderStatus_updatesAndReturnsOrder() {
        // Create an order first
        String orderJson = """
                {
                    "customerId": %d,
                    "items": [
                        { "productId": %d, "quantity": 1 }
                    ]
                }
                """.formatted(customerId, productId);

        HttpHeaders headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_JSON);
        HttpEntity<String> createRequest = new HttpEntity<>(orderJson, headers);

        ResponseEntity<String> createResponse = restTemplate.postForEntity("/api/orders", createRequest, String.class);
        JsonNode createdOrder = parseJson(createResponse.getBody());
        long orderId = createdOrder.get("id").asLong();

        // Use RestTemplate with HttpComponentsClientHttpRequestFactory for PATCH support
        RestTemplate patchTemplate = new RestTemplate(new HttpComponentsClientHttpRequestFactory());

        String statusJson = """
                { "status": "Shipped" }
                """;
        HttpEntity<String> patchRequest = new HttpEntity<>(statusJson, headers);

        ResponseEntity<String> response = patchTemplate.exchange(
                "http://localhost:" + port + "/api/orders/" + orderId + "/status",
                HttpMethod.PATCH,
                patchRequest,
                String.class
        );
        assertEquals(HttpStatus.OK, response.getStatusCode());

        JsonNode order = parseJson(response.getBody());
        assertEquals("Shipped", order.get("status").asText());
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
