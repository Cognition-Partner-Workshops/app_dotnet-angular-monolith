package com.ordermanager.product.config;

import com.ordermanager.product.model.Product;
import com.ordermanager.product.repository.ProductRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;

@Component
@ConditionalOnProperty(name = "app.seed-data.enabled", havingValue = "true", matchIfMissing = true)
public class DataSeeder implements CommandLineRunner {

    private final ProductRepository productRepository;

    public DataSeeder(ProductRepository productRepository) {
        this.productRepository = productRepository;
    }

    @Override
    @Transactional
    public void run(String... args) {
        if (productRepository.count() > 0) {
            return;
        }

        List<Product> products = List.of(
                createProduct("Widget A", "Standard widget", "Widgets", new BigDecimal("9.99"), "WGT-001"),
                createProduct("Widget B", "Premium widget", "Widgets", new BigDecimal("19.99"), "WGT-002"),
                createProduct("Gadget X", "Basic gadget", "Gadgets", new BigDecimal("29.99"), "GDG-001"),
                createProduct("Gadget Y", "Advanced gadget", "Gadgets", new BigDecimal("49.99"), "GDG-002"),
                createProduct("Thingamajig", "Multi-purpose thingamajig", "Misc", new BigDecimal("14.99"), "THG-001")
        );
        productRepository.saveAll(products);
    }

    private Product createProduct(String name, String description, String category,
                                  BigDecimal price, String sku) {
        Product product = new Product();
        product.setName(name);
        product.setDescription(description);
        product.setCategory(category);
        product.setPrice(price);
        product.setSku(sku);
        return product;
    }
}
