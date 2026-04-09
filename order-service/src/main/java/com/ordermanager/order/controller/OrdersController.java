package com.ordermanager.order.controller;

import com.ordermanager.order.dto.CreateOrderRequest;
import com.ordermanager.order.dto.UpdateStatusRequest;
import com.ordermanager.order.model.CustomerOrder;
import com.ordermanager.order.service.OrderService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
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
        return orderService.getOrderById(id)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @PostMapping
    public ResponseEntity<CustomerOrder> create(@RequestBody CreateOrderRequest request) {
        if (request.getItems() == null || request.getItems().isEmpty()) {
            return ResponseEntity.badRequest().build();
        }

        try {
            CustomerOrder created = orderService.createOrder(request);
            return ResponseEntity
                    .created(URI.create("/api/orders/" + created.getId()))
                    .body(created);
        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest().build();
        }
    }

    @PutMapping("/{id}/status")
    public ResponseEntity<CustomerOrder> updateStatus(@PathVariable Integer id,
                                                      @RequestBody UpdateStatusRequest request) {
        return orderService.updateStatus(id, request.getStatus())
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }
}
