package com.ordermanager.api.config;

import com.ordermanager.api.model.Customer;
import com.ordermanager.api.model.InventoryItem;
import com.ordermanager.api.model.Product;
import com.ordermanager.api.repository.CustomerRepository;
import com.ordermanager.api.repository.InventoryItemRepository;
import com.ordermanager.api.repository.ProductRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.util.List;

@Component
@ConditionalOnProperty(name = "app.seed-data.enabled", havingValue = "true", matchIfMissing = true)
public class DataSeeder implements CommandLineRunner {

    private final CustomerRepository customerRepository;
    private final ProductRepository productRepository;
    private final InventoryItemRepository inventoryItemRepository;

    public DataSeeder(CustomerRepository customerRepository,
                      ProductRepository productRepository,
                      InventoryItemRepository inventoryItemRepository) {
        this.customerRepository = customerRepository;
        this.productRepository = productRepository;
        this.inventoryItemRepository = inventoryItemRepository;
    }

    @Override
    @Transactional
    public void run(String... args) {
        if (productRepository.count() > 0) {
            return;
        }

        List<Customer> customers = List.of(
                createCustomer("Acme Corp", "orders@acme.com", "555-0100",
                        "123 Main St", "Springfield", "IL", "62701"),
                createCustomer("Globex Inc", "purchasing@globex.com", "555-0200",
                        "456 Oak Ave", "Shelbyville", "IL", "62565"),
                createCustomer("Initech LLC", "supplies@initech.com", "555-0300",
                        "789 Pine Rd", "Capital City", "IL", "62702")
        );
        customerRepository.saveAll(customers);

        List<Product> products = List.of(
                createProduct("Widget A", "Standard widget", "Widgets",
                        new BigDecimal("9.99"), "WGT-001"),
                createProduct("Widget B", "Premium widget", "Widgets",
                        new BigDecimal("19.99"), "WGT-002"),
                createProduct("Gadget X", "Basic gadget", "Gadgets",
                        new BigDecimal("29.99"), "GDG-001"),
                createProduct("Gadget Y", "Advanced gadget", "Gadgets",
                        new BigDecimal("49.99"), "GDG-002"),
                createProduct("Thingamajig", "Multi-purpose thingamajig", "Misc",
                        new BigDecimal("14.99"), "THG-001")
        );
        productRepository.saveAll(products);

        for (int i = 0; i < products.size(); i++) {
            InventoryItem inventory = new InventoryItem();
            inventory.setProduct(products.get(i));
            inventory.setQuantityOnHand((i + 1) * 50);
            inventory.setReorderLevel(10);
            inventory.setWarehouseLocation(String.format("A-%02d", i + 1));
            inventoryItemRepository.save(inventory);
        }
    }

    private Customer createCustomer(String name, String email, String phone,
                                    String address, String city, String state,
                                    String zipCode) {
        Customer customer = new Customer();
        customer.setName(name);
        customer.setEmail(email);
        customer.setPhone(phone);
        customer.setAddress(address);
        customer.setCity(city);
        customer.setState(state);
        customer.setZipCode(zipCode);
        return customer;
    }

    private Product createProduct(String name, String description,
                                  String category, BigDecimal price,
                                  String sku) {
        Product product = new Product();
        product.setName(name);
        product.setDescription(description);
        product.setCategory(category);
        product.setPrice(price);
        product.setSku(sku);
        return product;
    }
}
