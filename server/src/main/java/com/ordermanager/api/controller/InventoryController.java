package com.ordermanager.api.controller;

import com.ordermanager.api.dto.RestockRequest;
import com.ordermanager.api.model.InventoryItem;
import com.ordermanager.api.service.InventoryService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

@RestController
@RequestMapping("/api/inventory")
public class InventoryController {

    private final InventoryService inventoryService;

    public InventoryController(InventoryService inventoryService) {
        this.inventoryService = inventoryService;
    }

    @GetMapping
    public ResponseEntity<List<InventoryItem>> getAll() {
        return ResponseEntity.ok(inventoryService.getAllInventory());
    }

    @GetMapping("/product/{productId}")
    public ResponseEntity<InventoryItem> getByProduct(
            @PathVariable Integer productId) {
        return inventoryService.getInventoryByProductId(productId)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @PostMapping("/product/{productId}/restock")
    public ResponseEntity<InventoryItem> restock(
            @PathVariable Integer productId,
            @RequestBody RestockRequest request) {
        InventoryItem item = inventoryService.restock(
                productId, request.getQuantity());
        return ResponseEntity.ok(item);
    }

    @GetMapping("/low-stock")
    public ResponseEntity<List<InventoryItem>> getLowStock() {
        return ResponseEntity.ok(inventoryService.getLowStockItems());
    }
}
