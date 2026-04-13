package com.ordermanager.productservice.config;

import com.ordermanager.productservice.model.Product;
import com.ordermanager.productservice.repository.ProductRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;

@Component
public class DataSeeder implements CommandLineRunner {

    private final ProductRepository productRepository;

    public DataSeeder(ProductRepository productRepository) {
        this.productRepository = productRepository;
    }

    @Override
    public void run(String... args) {
        if (productRepository.count() > 0) {
            return;
        }

        productRepository.save(new Product("Widget A", "Standard widget", "Widgets", new BigDecimal("9.99"), "WGT-001"));
        productRepository.save(new Product("Widget B", "Premium widget", "Widgets", new BigDecimal("19.99"), "WGT-002"));
        productRepository.save(new Product("Gadget X", "Basic gadget", "Gadgets", new BigDecimal("29.99"), "GDG-001"));
        productRepository.save(new Product("Gadget Y", "Advanced gadget", "Gadgets", new BigDecimal("49.99"), "GDG-002"));
        productRepository.save(new Product("Thingamajig", "Multi-purpose thingamajig", "Misc", new BigDecimal("14.99"), "THG-001"));
    }
}
