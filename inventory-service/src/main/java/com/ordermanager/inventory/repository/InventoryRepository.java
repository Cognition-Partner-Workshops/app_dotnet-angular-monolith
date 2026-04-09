package com.ordermanager.inventory.repository;

import com.ordermanager.inventory.model.InventoryItem;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Optional;

@Repository
public interface InventoryRepository extends JpaRepository<InventoryItem, Integer> {

    Optional<InventoryItem> findByProductId(Integer productId);

    List<InventoryItem> findByQuantityOnHandLessThanEqual(Integer threshold);
}
