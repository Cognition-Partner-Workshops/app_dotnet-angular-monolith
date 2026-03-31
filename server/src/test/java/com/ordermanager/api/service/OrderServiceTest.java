package com.ordermanager.api.service;

import com.ordermanager.api.model.Customer;
import com.ordermanager.api.model.CustomerOrder;
import com.ordermanager.api.model.InventoryItem;
import com.ordermanager.api.model.Product;
import com.ordermanager.api.repository.CustomerRepository;
import com.ordermanager.api.repository.InventoryItemRepository;
import com.ordermanager.api.repository.OrderRepository;
import com.ordermanager.api.repository.ProductRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

@SpringBootTest
@ActiveProfiles("test")
@Transactional
class OrderServiceTest {

    @Autowired
    private OrderService orderService;

    @Autowired
    private OrderRepository orderRepository;

    @Autowired
    private CustomerRepository customerRepository;

    @Autowired
    private ProductRepository productRepository;

    @Autowired
    private InventoryItemRepository inventoryItemRepository;

    private Customer testCustomer;
    private Product testProduct;

    @BeforeEach
    void setUp() {
        orderRepository.deleteAll();
        inventoryItemRepository.deleteAll();
        productRepository.deleteAll();
        customerRepository.deleteAll();

        testCustomer = new Customer();
        testCustomer.setName("Acme Corp");
        testCustomer.setEmail("orders@acme.com");
        testCustomer.setPhone("555-0100");
        testCustomer.setAddress("123 Main St");
        testCustomer.setCity("Springfield");
        testCustomer.setState("IL");
        testCustomer.setZipCode("62701");
        testCustomer = customerRepository.save(testCustomer);

        testProduct = new Product();
        testProduct.setName("Widget A");
        testProduct.setDescription("Standard widget");
        testProduct.setCategory("Widgets");
        testProduct.setPrice(new BigDecimal("9.99"));
        testProduct.setSku("WGT-001");
        testProduct = productRepository.save(testProduct);

        InventoryItem inventory = new InventoryItem();
        inventory.setProduct(testProduct);
        inventory.setQuantityOnHand(50);
        inventory.setReorderLevel(10);
        inventory.setWarehouseLocation("A-01");
        inventoryItemRepository.save(inventory);
    }

    @Test
    void getAllOrders_returnsEmptyList_whenNoOrders() {
        List<CustomerOrder> orders = orderService.getAllOrders();
        assertTrue(orders.isEmpty());
    }

    @Test
    void createOrder_deductsInventory() {
        int qtyBefore = inventoryItemRepository
                .findByProductId(testProduct.getId())
                .orElseThrow()
                .getQuantityOnHand();

        orderService.createOrder(testCustomer.getId(),
                List.of(new OrderService.OrderItemData(
                        testProduct.getId(), 5)));

        int qtyAfter = inventoryItemRepository
                .findByProductId(testProduct.getId())
                .orElseThrow()
                .getQuantityOnHand();

        assertEquals(qtyBefore - 5, qtyAfter);
    }

    @Test
    void createOrder_throwsOnInsufficientStock() {
        assertThrows(IllegalStateException.class, () ->
                orderService.createOrder(testCustomer.getId(),
                        List.of(new OrderService.OrderItemData(
                                testProduct.getId(), 99999))));
    }
}
