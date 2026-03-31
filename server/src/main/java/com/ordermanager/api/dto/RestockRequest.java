package com.ordermanager.api.dto;

public class RestockRequest {

    private Integer quantity;

    public RestockRequest() {
    }

    public RestockRequest(Integer quantity) {
        this.quantity = quantity;
    }

    public Integer getQuantity() {
        return quantity;
    }

    public void setQuantity(Integer quantity) {
        this.quantity = quantity;
    }
}
