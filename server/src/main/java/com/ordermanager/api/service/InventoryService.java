package com.ordermanager.api.service;

import com.ordermanager.api.model.InventoryItem;
import com.ordermanager.api.repository.InventoryItemRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Optional;

@Service
@Transactional(readOnly = true)
public class InventoryService {

    private final InventoryItemRepository inventoryItemRepository;

    public InventoryService(InventoryItemRepository inventoryItemRepository) {
        this.inventoryItemRepository = inventoryItemRepository;
    }

    public List<InventoryItem> getAllInventory() {
        return inventoryItemRepository.findAll();
    }

    public Optional<InventoryItem> getInventoryByProductId(Integer productId) {
        return inventoryItemRepository.findByProductId(productId);
    }

    @Transactional
    public InventoryItem restock(Integer productId, Integer quantity) {
        if (quantity == null || quantity <= 0) {
            throw new IllegalArgumentException("Quantity must be a positive number");
        }
        InventoryItem item = inventoryItemRepository.findByProductId(productId)
                .orElseThrow(() -> new IllegalArgumentException(
                        "No inventory record for product " + productId));
        item.setQuantityOnHand(item.getQuantityOnHand() + quantity);
        item.setLastRestocked(LocalDateTime.now(ZoneOffset.UTC));
        return inventoryItemRepository.save(item);
    }

    public List<InventoryItem> getLowStockItems() {
        return inventoryItemRepository.findAll().stream()
                .filter(item -> item.getQuantityOnHand() <= item.getReorderLevel())
                .toList();
    }
}
