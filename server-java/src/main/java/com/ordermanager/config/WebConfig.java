package com.ordermanager.config;

import jakarta.servlet.http.HttpServletRequest;
import org.springframework.boot.web.servlet.error.ErrorController;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestMapping;

/**
 * SPA fallback configuration: forward any non-API, non-static-resource 404
 * to /index.html (matching .NET's MapFallbackToFile("index.html")).
 */
@Controller
public class WebConfig implements ErrorController {

    @RequestMapping("/error")
    public String handleError(HttpServletRequest request) {
        String uri = (String) request.getAttribute("jakarta.servlet.error.request_uri");
        if (uri != null && !uri.startsWith("/api/") && !uri.startsWith("/swagger") && !uri.startsWith("/v3/api-docs")) {
            return "forward:/index.html";
        }
        return "forward:/error-page";
    }
}
