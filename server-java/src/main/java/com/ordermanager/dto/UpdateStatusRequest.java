package com.ordermanager.dto;

import jakarta.validation.constraints.NotBlank;

public record UpdateStatusRequest(
        @NotBlank(message = "Status is required")
        String status
) {
}
