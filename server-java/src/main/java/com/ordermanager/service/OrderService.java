package com.ordermanager.service;

import com.ordermanager.dto.OrderItemRequest;
import com.ordermanager.model.*;
import com.ordermanager.repository.*;
import jakarta.persistence.EntityNotFoundException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;

@Service
public class OrderService {

    private static final Logger log = LoggerFactory.getLogger(OrderService.class);

    @Autowired
    private OrderRepository orderRepository;

    @Autowired
    private CustomerRepository customerRepository;

    @Autowired
    private ProductRepository productRepository;

    @Autowired
    private InventoryItemRepository inventoryItemRepository;

    public List<Order> getAllOrders() {
        return orderRepository.findAllByOrderByOrderDateDesc();
    }

    public Optional<Order> getOrderById(Long id) {
        return orderRepository.findById(id);
    }

    @Transactional
    public Order createOrder(Long customerId, List<OrderItemRequest> items) {
        log.info("Creating order for customer {} with {} item(s)", customerId, items.size());

        Customer customer = customerRepository.findById(customerId)
                .orElseThrow(() -> {
                    log.warn("Customer {} not found", customerId);
                    return new EntityNotFoundException("Customer " + customerId + " not found");
                });

        Order order = new Order();
        order.setCustomer(customer);
        order.setShippingAddress(
                customer.getAddress() + ", " + customer.getCity() + ", " + customer.getState() + " " + customer.getZipCode()
        );

        for (OrderItemRequest itemRequest : items) {
            Product product = productRepository.findById(itemRequest.productId())
                    .orElseThrow(() -> {
                        log.warn("Product {} not found", itemRequest.productId());
                        return new EntityNotFoundException("Product " + itemRequest.productId() + " not found");
                    });

            InventoryItem inventory = inventoryItemRepository.findByProductId(itemRequest.productId())
                    .orElseThrow(() -> {
                        log.warn("No inventory record for product {}", itemRequest.productId());
                        return new IllegalStateException("No inventory record for product " + itemRequest.productId());
                    });

            if (inventory.getQuantityOnHand() < itemRequest.quantity()) {
                log.warn("Insufficient stock for product {} ({}). Requested: {}, Available: {}",
                        product.getId(), product.getName(), itemRequest.quantity(), inventory.getQuantityOnHand());
                throw new IllegalStateException(
                        "Insufficient stock for " + product.getName() + ". Available: " + inventory.getQuantityOnHand()
                );
            }

            inventory.setQuantityOnHand(inventory.getQuantityOnHand() - itemRequest.quantity());
            inventoryItemRepository.save(inventory);

            OrderItem orderItem = new OrderItem();
            orderItem.setOrder(order);
            orderItem.setProduct(product);
            orderItem.setQuantity(itemRequest.quantity());
            orderItem.setUnitPrice(product.getPrice());
            order.getItems().add(orderItem);
        }

        BigDecimal totalAmount = order.getItems().stream()
                .map(item -> item.getUnitPrice().multiply(BigDecimal.valueOf(item.getQuantity())))
                .reduce(BigDecimal.ZERO, BigDecimal::add);
        order.setTotalAmount(totalAmount);

        Order saved = orderRepository.save(order);
        log.info("Order {} created successfully. Total: {}", saved.getId(), saved.getTotalAmount());
        return saved;
    }

    public Order updateOrderStatus(Long id, String status) {
        log.info("Updating order {} status to '{}'", id, status);
        Order order = orderRepository.findById(id)
                .orElseThrow(() -> {
                    log.warn("Order {} not found for status update", id);
                    return new EntityNotFoundException("Order " + id + " not found");
                });
        order.setStatus(status);
        Order saved = orderRepository.save(order);
        log.info("Order {} status updated to '{}'", id, status);
        return saved;
    }
}
