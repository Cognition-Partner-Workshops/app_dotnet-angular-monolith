package com.ordermanager.data;

import com.ordermanager.model.Customer;
import com.ordermanager.model.InventoryItem;
import com.ordermanager.model.Product;
import com.ordermanager.repository.CustomerRepository;
import com.ordermanager.repository.InventoryItemRepository;
import com.ordermanager.repository.ProductRepository;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.CommandLineRunner;
import org.springframework.stereotype.Component;

import java.math.BigDecimal;
import java.util.List;

@Component
public class DataSeeder implements CommandLineRunner {

    @Autowired
    private ProductRepository productRepository;

    @Autowired
    private CustomerRepository customerRepository;

    @Autowired
    private InventoryItemRepository inventoryItemRepository;

    @Override
    public void run(String... args) {
        if (productRepository.count() > 0) {
            return;
        }

        // Seed customers
        Customer c1 = new Customer();
        c1.setName("Acme Corp");
        c1.setEmail("orders@acme.com");
        c1.setPhone("555-0100");
        c1.setAddress("123 Main St");
        c1.setCity("Springfield");
        c1.setState("IL");
        c1.setZipCode("62701");

        Customer c2 = new Customer();
        c2.setName("Globex Inc");
        c2.setEmail("purchasing@globex.com");
        c2.setPhone("555-0200");
        c2.setAddress("456 Oak Ave");
        c2.setCity("Shelbyville");
        c2.setState("IL");
        c2.setZipCode("62565");

        Customer c3 = new Customer();
        c3.setName("Initech LLC");
        c3.setEmail("supplies@initech.com");
        c3.setPhone("555-0300");
        c3.setAddress("789 Pine Rd");
        c3.setCity("Capital City");
        c3.setState("IL");
        c3.setZipCode("62702");

        customerRepository.saveAll(List.of(c1, c2, c3));

        // Seed products
        Product p1 = createProduct("Widget A", "Standard widget", "Widgets", "9.99", "WGT-001");
        Product p2 = createProduct("Widget B", "Premium widget", "Widgets", "19.99", "WGT-002");
        Product p3 = createProduct("Gadget X", "Basic gadget", "Gadgets", "29.99", "GDG-001");
        Product p4 = createProduct("Gadget Y", "Advanced gadget", "Gadgets", "49.99", "GDG-002");
        Product p5 = createProduct("Thingamajig", "Multi-purpose thingamajig", "Misc", "14.99", "THG-001");

        List<Product> products = productRepository.saveAll(List.of(p1, p2, p3, p4, p5));

        // Seed inventory items
        int[] quantities = {50, 100, 150, 200, 250};
        String[] locations = {"A-01", "A-02", "A-03", "A-04", "A-05"};

        for (int i = 0; i < products.size(); i++) {
            InventoryItem item = new InventoryItem();
            item.setProduct(products.get(i));
            item.setQuantityOnHand(quantities[i]);
            item.setReorderLevel(10);
            item.setWarehouseLocation(locations[i]);
            inventoryItemRepository.save(item);
        }
    }

    private Product createProduct(String name, String description, String category, String price, String sku) {
        Product product = new Product();
        product.setName(name);
        product.setDescription(description);
        product.setCategory(category);
        product.setPrice(new BigDecimal(price));
        product.setSku(sku);
        return product;
    }
}
