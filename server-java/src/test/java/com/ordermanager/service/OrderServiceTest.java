package com.ordermanager.service;

import com.ordermanager.data.DataSeeder;
import com.ordermanager.dto.OrderItemRequest;
import com.ordermanager.model.InventoryItem;
import com.ordermanager.model.Product;
import com.ordermanager.repository.*;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.context.annotation.Import;
import org.springframework.test.context.ActiveProfiles;

import java.util.List;

import static org.junit.jupiter.api.Assertions.*;

@DataJpaTest
@ActiveProfiles("test")
@Import({OrderService.class, DataSeeder.class})
class OrderServiceTest {

    @Autowired
    private OrderService orderService;

    @Autowired
    private ProductRepository productRepository;

    @Autowired
    private CustomerRepository customerRepository;

    @Autowired
    private InventoryItemRepository inventoryItemRepository;

    @Autowired
    private DataSeeder dataSeeder;

    @BeforeEach
    void setUp() {
        dataSeeder.run();
    }

    @Test
    void getAllOrders_returnsEmptyList_whenNoOrders() {
        var orders = orderService.getAllOrders();
        assertTrue(orders.isEmpty());
    }

    @Test
    void createOrder_deductsInventory() {
        Product product = productRepository.findAll().get(0);
        var customer = customerRepository.findAll().get(0);
        InventoryItem inventoryBefore = inventoryItemRepository.findByProductId(product.getId()).orElseThrow();
        int qtyBefore = inventoryBefore.getQuantityOnHand();

        orderService.createOrder(customer.getId(), List.of(new OrderItemRequest(product.getId(), 5)));

        InventoryItem inventoryAfter = inventoryItemRepository.findByProductId(product.getId()).orElseThrow();
        assertEquals(qtyBefore - 5, inventoryAfter.getQuantityOnHand());
    }

    @Test
    void createOrder_throwsOnInsufficientStock() {
        Product product = productRepository.findAll().get(0);
        var customer = customerRepository.findAll().get(0);

        assertThrows(IllegalStateException.class, () ->
                orderService.createOrder(customer.getId(), List.of(new OrderItemRequest(product.getId(), 99999)))
        );
    }
}
