package com.ordermanager.service;

import com.ordermanager.dto.OrderItemRequest;
import com.ordermanager.model.*;
import com.ordermanager.repository.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;
import java.util.Optional;

@Service
public class OrderService {

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
        Customer customer = customerRepository.findById(customerId)
                .orElseThrow(() -> new IllegalArgumentException("Customer " + customerId + " not found"));

        Order order = new Order();
        order.setCustomer(customer);
        order.setShippingAddress(
                customer.getAddress() + ", " + customer.getCity() + ", " + customer.getState() + " " + customer.getZipCode()
        );

        for (OrderItemRequest itemRequest : items) {
            Product product = productRepository.findById(itemRequest.productId())
                    .orElseThrow(() -> new IllegalArgumentException("Product " + itemRequest.productId() + " not found"));

            InventoryItem inventory = inventoryItemRepository.findByProductId(itemRequest.productId())
                    .orElseThrow(() -> new IllegalStateException("No inventory record for product " + itemRequest.productId()));

            if (inventory.getQuantityOnHand() < itemRequest.quantity()) {
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

        return orderRepository.save(order);
    }

    public Order updateOrderStatus(Long id, String status) {
        Order order = orderRepository.findById(id)
                .orElseThrow(() -> new IllegalArgumentException("Order " + id + " not found"));
        order.setStatus(status);
        return orderRepository.save(order);
    }
}
