package com.ordermanager.repository;

import com.ordermanager.model.InventoryItem;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface InventoryItemRepository extends JpaRepository<InventoryItem, Long> {

    Optional<InventoryItem> findByProductId(Long productId);

    @Query("SELECT i FROM InventoryItem i WHERE i.quantityOnHand <= i.reorderLevel")
    List<InventoryItem> findLowStockItems();
}
