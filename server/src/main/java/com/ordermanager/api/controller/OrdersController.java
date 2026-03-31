package com.ordermanager.api.controller;

import com.ordermanager.api.dto.CreateOrderRequest;
import com.ordermanager.api.dto.UpdateStatusRequest;
import com.ordermanager.api.model.CustomerOrder;
import com.ordermanager.api.service.OrderService;
import com.ordermanager.api.service.OrderService.OrderItemData;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.net.URI;
import java.util.List;

@RestController
@RequestMapping("/api/orders")
public class OrdersController {

    private final OrderService orderService;

    public OrdersController(OrderService orderService) {
        this.orderService = orderService;
    }

    @GetMapping
    public ResponseEntity<List<CustomerOrder>> getAll() {
        return ResponseEntity.ok(orderService.getAllOrders());
    }

    @GetMapping("/{id}")
    public ResponseEntity<CustomerOrder> getById(@PathVariable Integer id) {
        CustomerOrder order = orderService.getOrderById(id);
        if (order == null) {
            return ResponseEntity.notFound().build();
        }
        return ResponseEntity.ok(order);
    }

    @PostMapping
    public ResponseEntity<CustomerOrder> create(
            @RequestBody CreateOrderRequest request) {
        List<OrderItemData> items = request.getItems().stream()
                .map(i -> new OrderItemData(i.getProductId(), i.getQuantity()))
                .toList();
        CustomerOrder order = orderService.createOrder(
                request.getCustomerId(), items);
        return ResponseEntity
                .created(URI.create("/api/orders/" + order.getId()))
                .body(order);
    }

    @PatchMapping("/{id}/status")
    public ResponseEntity<CustomerOrder> updateStatus(
            @PathVariable Integer id,
            @RequestBody UpdateStatusRequest request) {
        CustomerOrder order = orderService.updateOrderStatus(
                id, request.getStatus());
        return ResponseEntity.ok(order);
    }
}
