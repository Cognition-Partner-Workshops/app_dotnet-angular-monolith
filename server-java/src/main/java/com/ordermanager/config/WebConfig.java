package com.ordermanager.config;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.boot.web.servlet.error.ErrorController;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;

import jakarta.servlet.http.HttpServletResponse;

/**
 * SPA fallback configuration: forward any non-API, non-static-resource 404
 * to /index.html (matching .NET's MapFallbackToFile("index.html")).
 */
@Controller
public class WebConfig implements ErrorController {

    @RequestMapping("/error")
    public String handleError(HttpServletRequest request, HttpServletResponse response) {
        Integer statusCode = (Integer) request.getAttribute("jakarta.servlet.error.status_code");
        String uri = (String) request.getAttribute("jakarta.servlet.error.request_uri");
        if (statusCode != null && statusCode == HttpStatus.NOT_FOUND.value()
                && uri != null && !uri.startsWith("/api/") && !uri.startsWith("/swagger") && !uri.startsWith("/v3/api-docs")) {
            response.setStatus(HttpStatus.OK.value());
            return "forward:/index.html";
        }
        return "forward:/error-page";
    }
}
