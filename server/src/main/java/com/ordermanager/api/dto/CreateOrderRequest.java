package com.ordermanager.api.dto;

import java.util.List;

public class CreateOrderRequest {

    private Integer customerId;
    private List<OrderItemRequest> items;

    public CreateOrderRequest() {
    }

    public CreateOrderRequest(Integer customerId, List<OrderItemRequest> items) {
        this.customerId = customerId;
        this.items = items;
    }

    public Integer getCustomerId() {
        return customerId;
    }

    public void setCustomerId(Integer customerId) {
        this.customerId = customerId;
    }

    public List<OrderItemRequest> getItems() {
        return items;
    }

    public void setItems(List<OrderItemRequest> items) {
        this.items = items;
    }
}
