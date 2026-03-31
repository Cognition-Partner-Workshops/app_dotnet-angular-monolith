package com.ordermanager.api.service;

import com.ordermanager.api.model.Customer;
import com.ordermanager.api.model.CustomerOrder;
import com.ordermanager.api.model.InventoryItem;
import com.ordermanager.api.model.OrderItem;
import com.ordermanager.api.model.Product;
import com.ordermanager.api.repository.CustomerRepository;
import com.ordermanager.api.repository.InventoryItemRepository;
import com.ordermanager.api.repository.OrderRepository;
import com.ordermanager.api.repository.ProductRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;
import java.util.Objects;

@Service
@Transactional(readOnly = true)
public class OrderService {

    private final OrderRepository orderRepository;
    private final CustomerRepository customerRepository;
    private final ProductRepository productRepository;
    private final InventoryItemRepository inventoryItemRepository;

    public OrderService(OrderRepository orderRepository,
                        CustomerRepository customerRepository,
                        ProductRepository productRepository,
                        InventoryItemRepository inventoryItemRepository) {
        this.orderRepository = orderRepository;
        this.customerRepository = customerRepository;
        this.productRepository = productRepository;
        this.inventoryItemRepository = inventoryItemRepository;
    }

    public List<CustomerOrder> getAllOrders() {
        return orderRepository.findAllByOrderByOrderDateDesc();
    }

    public CustomerOrder getOrderById(Integer id) {
        return orderRepository.findById(id).orElse(null);
    }

    @Transactional
    public CustomerOrder createOrder(Integer customerId,
                                     List<OrderItemData> items) {
        Customer customer = customerRepository.findById(customerId)
                .orElseThrow(() -> new IllegalArgumentException(
                        "Customer " + customerId + " not found"));

        CustomerOrder order = new CustomerOrder();
        order.setCustomer(customer);
        order.setCustomerId(customer.getId());
        order.setShippingAddress(String.format("%s, %s, %s %s",
                Objects.toString(customer.getAddress(), ""),
                Objects.toString(customer.getCity(), ""),
                Objects.toString(customer.getState(), ""),
                Objects.toString(customer.getZipCode(), "")));

        for (OrderItemData itemData : items) {
            if (itemData.productId() == null) {
                throw new IllegalArgumentException("Product ID must not be null");
            }
            if (itemData.quantity() == null || itemData.quantity() <= 0) {
                throw new IllegalArgumentException("Quantity must be a positive number");
            }
            Product product = productRepository.findById(itemData.productId())
                    .orElseThrow(() -> new IllegalArgumentException(
                            "Product " + itemData.productId() + " not found"));

            InventoryItem inventory = inventoryItemRepository
                    .findByProductId(itemData.productId())
                    .orElseThrow(() -> new IllegalStateException(
                            "No inventory record for product " + itemData.productId()));

            if (inventory.getQuantityOnHand() < itemData.quantity()) {
                throw new IllegalStateException(String.format(
                        "Insufficient stock for %s. Available: %d",
                        product.getName(), inventory.getQuantityOnHand()));
            }

            inventory.setQuantityOnHand(
                    inventory.getQuantityOnHand() - itemData.quantity());
            inventoryItemRepository.save(inventory);

            OrderItem orderItem = new OrderItem();
            orderItem.setOrder(order);
            orderItem.setProduct(product);
            orderItem.setProductId(product.getId());
            orderItem.setQuantity(itemData.quantity());
            orderItem.setUnitPrice(product.getPrice());
            order.getItems().add(orderItem);
        }

        BigDecimal total = order.getItems().stream()
                .map(item -> item.getUnitPrice()
                        .multiply(BigDecimal.valueOf(item.getQuantity())))
                .reduce(BigDecimal.ZERO, BigDecimal::add);
        order.setTotalAmount(total);

        CustomerOrder saved = orderRepository.save(order);
        saved.getItems().forEach(item -> item.setOrderId(saved.getId()));
        return saved;
    }

    @Transactional
    public CustomerOrder updateOrderStatus(Integer orderId, String status) {
        CustomerOrder order = orderRepository.findById(orderId)
                .orElseThrow(() -> new IllegalArgumentException(
                        "Order " + orderId + " not found"));
        order.setStatus(status);
        return orderRepository.save(order);
    }

    public record OrderItemData(Integer productId, Integer quantity) {
    }
}
