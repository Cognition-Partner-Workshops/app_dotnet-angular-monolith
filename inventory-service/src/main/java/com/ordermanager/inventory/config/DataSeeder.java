package com.ordermanager.inventory.config;

import com.ordermanager.inventory.model.InventoryItem;
import com.ordermanager.inventory.repository.InventoryRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.util.List;

@Component
@ConditionalOnProperty(name = "app.seed-data.enabled", havingValue = "true", matchIfMissing = true)
public class DataSeeder implements CommandLineRunner {

    private final InventoryRepository inventoryRepository;

    public DataSeeder(InventoryRepository inventoryRepository) {
        this.inventoryRepository = inventoryRepository;
    }

    @Override
    @Transactional
    public void run(String... args) {
        if (inventoryRepository.count() > 0) {
            return;
        }

        LocalDateTime now = LocalDateTime.now(ZoneOffset.UTC);
        List<InventoryItem> items = List.of(
                createItem(1, "Widget A", 100, 10, "Warehouse A - Shelf 1", now.minusDays(5)),
                createItem(2, "Widget B", 50, 15, "Warehouse A - Shelf 2", now.minusDays(3)),
                createItem(3, "Gadget X", 75, 10, "Warehouse B - Shelf 1", now.minusDays(7)),
                createItem(4, "Gadget Y", 30, 20, "Warehouse B - Shelf 2", now.minusDays(1)),
                createItem(5, "Thingamajig", 200, 25, "Warehouse C - Shelf 1", now.minusDays(10))
        );
        inventoryRepository.saveAll(items);
    }

    private InventoryItem createItem(Integer productId, String productName, Integer quantity,
                                     Integer reorderLevel, String location, LocalDateTime lastRestocked) {
        InventoryItem item = new InventoryItem();
        item.setProductId(productId);
        item.setProductName(productName);
        item.setQuantityOnHand(quantity);
        item.setReorderLevel(reorderLevel);
        item.setWarehouseLocation(location);
        item.setLastRestocked(lastRestocked);
        return item;
    }
}
