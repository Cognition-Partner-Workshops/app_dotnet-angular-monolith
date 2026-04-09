package com.ordermanager.order.client;

import org.springframework.stereotype.Component;
import org.springframework.web.reactive.function.client.WebClient;

import java.util.Map;

@Component
public class CustomerClient {

    private final WebClient webClient;

    public CustomerClient(WebClient.Builder webClientBuilder) {
        this.webClient = webClientBuilder.build();
    }

    @SuppressWarnings("unchecked")
    public Map<String, Object> getCustomerById(Integer customerId) {
        try {
            return webClient.get()
                    .uri("http://customer-service/api/customers/{id}", customerId)
                    .retrieve()
                    .bodyToMono(Map.class)
                    .block();
        } catch (Exception e) {
            return null;
        }
    }
}
