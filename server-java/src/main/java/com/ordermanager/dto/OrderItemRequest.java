package com.ordermanager.dto;

public record OrderItemRequest(Long productId, int quantity) {
}
