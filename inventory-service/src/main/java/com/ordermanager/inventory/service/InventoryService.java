package com.ordermanager.inventory.service;

import com.ordermanager.inventory.model.InventoryItem;
import com.ordermanager.inventory.repository.InventoryRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.util.List;
import java.util.Optional;

@Service
@Transactional(readOnly = true)
public class InventoryService {

    private final InventoryRepository inventoryRepository;

    public InventoryService(InventoryRepository inventoryRepository) {
        this.inventoryRepository = inventoryRepository;
    }

    public List<InventoryItem> getAllItems() {
        return inventoryRepository.findAll();
    }

    public Optional<InventoryItem> getItemByProductId(Integer productId) {
        return inventoryRepository.findByProductId(productId);
    }

    public List<InventoryItem> getLowStockItems() {
        return inventoryRepository.findAll().stream()
                .filter(item -> item.getQuantityOnHand() <= item.getReorderLevel())
                .toList();
    }

    @Transactional
    public Optional<InventoryItem> restock(Integer productId, Integer quantity) {
        if (quantity == null || quantity <= 0) {
            throw new IllegalArgumentException("Quantity must be positive");
        }

        return inventoryRepository.findByProductId(productId)
                .map(item -> {
                    item.setQuantityOnHand(item.getQuantityOnHand() + quantity);
                    item.setLastRestocked(LocalDateTime.now(ZoneOffset.UTC));
                    return inventoryRepository.save(item);
                });
    }
}
