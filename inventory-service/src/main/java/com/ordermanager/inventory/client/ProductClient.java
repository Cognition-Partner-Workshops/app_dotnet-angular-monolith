package com.ordermanager.inventory.client;

import org.springframework.stereotype.Component;
import org.springframework.web.reactive.function.client.WebClient;

import java.util.Map;

@Component
public class ProductClient {

    private final WebClient webClient;

    public ProductClient(WebClient.Builder webClientBuilder) {
        this.webClient = webClientBuilder.build();
    }

    @SuppressWarnings("unchecked")
    public Map<String, Object> getProductById(Integer productId) {
        try {
            return webClient.get()
                    .uri("http://product-service/api/products/{id}", productId)
                    .retrieve()
                    .bodyToMono(Map.class)
                    .block();
        } catch (Exception e) {
            return null;
        }
    }
}
