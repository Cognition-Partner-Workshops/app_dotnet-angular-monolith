package com.ordermanager.controller;

import com.ordermanager.dto.RestockRequest;
import com.ordermanager.model.InventoryItem;
import com.ordermanager.service.InventoryService;
import jakarta.validation.Valid;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;

@RestController
@RequestMapping("/api/inventory")
public class InventoryController {

    @Autowired
    private InventoryService inventoryService;

    @GetMapping
    public List<InventoryItem> getAll() {
        return inventoryService.getAllInventory();
    }

    @GetMapping("/product/{productId}")
    public ResponseEntity<InventoryItem> getByProductId(@PathVariable Long productId) {
        return inventoryService.getInventoryByProductId(productId)
                .map(ResponseEntity::ok)
                .orElse(ResponseEntity.notFound().build());
    }

    @PostMapping("/product/{productId}/restock")
    public ResponseEntity<InventoryItem> restock(@PathVariable Long productId, @Valid @RequestBody RestockRequest request) {
        InventoryItem restocked = inventoryService.restock(productId, request.quantity());
        return ResponseEntity.ok(restocked);
    }

    @GetMapping("/low-stock")
    public List<InventoryItem> getLowStock() {
        return inventoryService.getLowStockItems();
    }
}
