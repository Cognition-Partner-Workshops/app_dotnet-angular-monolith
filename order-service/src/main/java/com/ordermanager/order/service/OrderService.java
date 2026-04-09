package com.ordermanager.order.service;

import com.ordermanager.order.client.CustomerClient;
import com.ordermanager.order.client.ProductClient;
import com.ordermanager.order.dto.CreateOrderRequest;
import com.ordermanager.order.dto.OrderItemRequest;
import com.ordermanager.order.model.CustomerOrder;
import com.ordermanager.order.model.OrderItem;
import com.ordermanager.order.repository.OrderRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Optional;

@Service
@Transactional(readOnly = true)
public class OrderService {

    private final OrderRepository orderRepository;
    private final CustomerClient customerClient;
    private final ProductClient productClient;

    public OrderService(OrderRepository orderRepository,
                        CustomerClient customerClient,
                        ProductClient productClient) {
        this.orderRepository = orderRepository;
        this.customerClient = customerClient;
        this.productClient = productClient;
    }

    public List<CustomerOrder> getAllOrders() {
        return orderRepository.findAll();
    }

    public Optional<CustomerOrder> getOrderById(Integer id) {
        return orderRepository.findById(id);
    }

    @Transactional
    public CustomerOrder createOrder(CreateOrderRequest request) {
        // Validate customer exists via customer-service
        Map<String, Object> customer = customerClient.getCustomerById(request.getCustomerId());
        if (customer == null) {
            throw new IllegalArgumentException("Customer not found: " + request.getCustomerId());
        }

        // Build shipping address from customer data
        String shippingAddress = Objects.toString(customer.get("address"), "")
                + ", " + Objects.toString(customer.get("city"), "")
                + ", " + Objects.toString(customer.get("state"), "")
                + " " + Objects.toString(customer.get("zipCode"), "");

        CustomerOrder order = new CustomerOrder();
        order.setCustomerId(request.getCustomerId());
        order.setOrderDate(LocalDateTime.now(ZoneOffset.UTC));
        order.setStatus("Pending");
        order.setShippingAddress(shippingAddress);

        BigDecimal totalAmount = BigDecimal.ZERO;

        if (request.getItems() != null) {
            for (OrderItemRequest itemRequest : request.getItems()) {
                if (itemRequest.getQuantity() == null || itemRequest.getQuantity() <= 0) {
                    throw new IllegalArgumentException("Quantity must be positive");
                }

                // Get product price from product-service
                Map<String, Object> product = productClient.getProductById(itemRequest.getProductId());
                if (product == null) {
                    throw new IllegalArgumentException("Product not found: " + itemRequest.getProductId());
                }

                BigDecimal unitPrice;
                Object priceObj = product.get("price");
                if (priceObj instanceof Number) {
                    unitPrice = BigDecimal.valueOf(((Number) priceObj).doubleValue());
                } else {
                    unitPrice = new BigDecimal(priceObj.toString());
                }

                OrderItem item = new OrderItem();
                item.setProductId(itemRequest.getProductId());
                item.setQuantity(itemRequest.getQuantity());
                item.setUnitPrice(unitPrice);
                item.setOrder(order);
                order.getItems().add(item);

                totalAmount = totalAmount.add(unitPrice.multiply(BigDecimal.valueOf(itemRequest.getQuantity())));
            }
        }

        order.setTotalAmount(totalAmount);
        CustomerOrder savedOrder = orderRepository.save(order);

        // Set FK IDs for response
        for (OrderItem item : savedOrder.getItems()) {
            item.setOrderId(savedOrder.getId());
        }

        return savedOrder;
    }

    @Transactional
    public Optional<CustomerOrder> updateStatus(Integer id, String status) {
        return orderRepository.findById(id)
                .map(order -> {
                    order.setStatus(status);
                    return orderRepository.save(order);
                });
    }
}
