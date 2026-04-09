package com.ordermanager.inventory.controller;

import com.ordermanager.inventory.dto.RestockRequest;
import com.ordermanager.inventory.model.InventoryItem;
import com.ordermanager.inventory.service.InventoryService;
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
        return ResponseEntity.ok(inventoryService.getAllItems());
    }

    @GetMapping("/product/{productId}")
    public ResponseEntity<InventoryItem> getByProductId(@PathVariable Integer productId) {
        return inventoryService.getItemByProductId(productId)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @GetMapping("/low-stock")
    public ResponseEntity<List<InventoryItem>> getLowStock() {
        return ResponseEntity.ok(inventoryService.getLowStockItems());
    }

    @PostMapping("/restock/{productId}")
    public ResponseEntity<InventoryItem> restock(@PathVariable Integer productId,
                                                 @RequestBody RestockRequest request) {
        try {
            return inventoryService.restock(productId, request.getQuantity())
                    .map(ResponseEntity::ok)
                    .orElse(ResponseEntity.notFound().build());
        } catch (IllegalArgumentException e) {
            return ResponseEntity.badRequest().build();
        }
    }
}
