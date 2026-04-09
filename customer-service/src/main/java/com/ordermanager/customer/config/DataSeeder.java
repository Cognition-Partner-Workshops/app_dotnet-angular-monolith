package com.ordermanager.customer.config;

import com.ordermanager.customer.model.Customer;
import com.ordermanager.customer.repository.CustomerRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;
import org.springframework.transaction.annotation.Transactional;

import java.util.List;

@Component
@ConditionalOnProperty(name = "app.seed-data.enabled", havingValue = "true", matchIfMissing = true)
public class DataSeeder implements CommandLineRunner {

    private final CustomerRepository customerRepository;

    public DataSeeder(CustomerRepository customerRepository) {
        this.customerRepository = customerRepository;
    }

    @Override
    @Transactional
    public void run(String... args) {
        if (customerRepository.count() > 0) {
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
}
