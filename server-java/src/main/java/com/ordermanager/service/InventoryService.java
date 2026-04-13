package com.ordermanager.service;

import com.ordermanager.model.InventoryItem;
import com.ordermanager.repository.InventoryItemRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

@Service
public class InventoryService {

    @Autowired
    private InventoryItemRepository inventoryItemRepository;

    public List<InventoryItem> getAllInventory() {
        return inventoryItemRepository.findAll();
    }

    public Optional<InventoryItem> getInventoryByProductId(Long productId) {
        return inventoryItemRepository.findByProductId(productId);
    }

    public InventoryItem restock(Long productId, int quantity) {
        InventoryItem item = inventoryItemRepository.findByProductId(productId)
                .orElseThrow(() -> new IllegalArgumentException("No inventory record for product " + productId));
        item.setQuantityOnHand(item.getQuantityOnHand() + quantity);
        item.setLastRestocked(LocalDateTime.now());
        return inventoryItemRepository.save(item);
    }

    public List<InventoryItem> getLowStockItems() {
        return inventoryItemRepository.findLowStockItems();
    }
}
