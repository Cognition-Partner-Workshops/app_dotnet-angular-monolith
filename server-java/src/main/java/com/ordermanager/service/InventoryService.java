package com.ordermanager.service;

import com.ordermanager.model.InventoryItem;
import com.ordermanager.repository.InventoryItemRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.List;
import java.util.Optional;

@Service
public class InventoryService {

    private static final Logger log = LoggerFactory.getLogger(InventoryService.class);

    @Autowired
    private InventoryItemRepository inventoryItemRepository;

    public List<InventoryItem> getAllInventory() {
        return inventoryItemRepository.findAll();
    }

    public Optional<InventoryItem> getInventoryByProductId(Long productId) {
        return inventoryItemRepository.findByProductId(productId);
    }

    public InventoryItem restock(Long productId, int quantity) {
        log.info("Restocking product {} with {} units", productId, quantity);
        InventoryItem item = inventoryItemRepository.findByProductId(productId)
                .orElseThrow(() -> {
                    log.warn("No inventory record found for product {}", productId);
                    return new IllegalArgumentException("No inventory record for product " + productId);
                });
        item.setQuantityOnHand(item.getQuantityOnHand() + quantity);
        item.setLastRestocked(LocalDateTime.now());
        InventoryItem saved = inventoryItemRepository.save(item);
        log.info("Product {} restocked. New quantity: {}", productId, saved.getQuantityOnHand());
        return saved;
    }

    public List<InventoryItem> getLowStockItems() {
        return inventoryItemRepository.findLowStockItems();
    }
}
