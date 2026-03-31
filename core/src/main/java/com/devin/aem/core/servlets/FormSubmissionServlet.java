package com.devin.aem.core.servlets;

import com.google.gson.JsonObject;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.SlingHttpServletResponse;
import org.apache.sling.api.servlets.HttpConstants;
import org.apache.sling.api.servlets.SlingAllMethodsServlet;
import org.osgi.service.component.annotations.Component;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.servlet.Servlet;
import javax.servlet.ServletException;
import java.io.IOException;
import java.util.Enumeration;

@Component(service = Servlet.class, property = {
    "sling.servlet.paths=/bin/devinreactaem/form",
    "sling.servlet.methods=" + HttpConstants.METHOD_POST
})
public class FormSubmissionServlet extends SlingAllMethodsServlet {

    private static final Logger LOG = LoggerFactory.getLogger(FormSubmissionServlet.class);

    @Override
    protected void doPost(SlingHttpServletRequest request, SlingHttpServletResponse response)
            throws ServletException, IOException {

        response.setContentType("application/json");
        response.setCharacterEncoding("UTF-8");

        JsonObject jsonResponse = new JsonObject();

        try {
            JsonObject formData = new JsonObject();
            Enumeration<String> paramNames = request.getParameterNames();

            while (paramNames.hasMoreElements()) {
                String paramName = paramNames.nextElement();
                String paramValue = request.getParameter(paramName);

                if (isValidInput(paramName, paramValue)) {
                    formData.addProperty(paramName, paramValue);
                } else {
                    jsonResponse.addProperty("success", false);
                    jsonResponse.addProperty("error", "Invalid input for field: " + paramName);
                    response.setStatus(SlingHttpServletResponse.SC_BAD_REQUEST);
                    response.getWriter().write(jsonResponse.toString());
                    return;
                }
            }

            LOG.info("Form submission received with {} fields", formData.size());

            jsonResponse.addProperty("success", true);
            jsonResponse.addProperty("message", "Form submitted successfully");
            jsonResponse.add("data", formData);
            response.setStatus(SlingHttpServletResponse.SC_OK);

        } catch (Exception e) {
            LOG.error("Error processing form submission", e);
            jsonResponse.addProperty("success", false);
            jsonResponse.addProperty("error", "Internal server error");
            response.setStatus(SlingHttpServletResponse.SC_INTERNAL_SERVER_ERROR);
        }

        response.getWriter().write(jsonResponse.toString());
    }

    private boolean isValidInput(String name, String value) {
        if (name == null || name.trim().isEmpty()) return false;
        if (value != null && value.length() > 10000) return false;
        if (value != null) {
            String lower = value.toLowerCase();
            if (lower.contains("<script") || lower.contains("javascript:") ||
                lower.contains("<iframe") || lower.contains("<img") ||
                lower.contains("<svg") || lower.contains("<object") ||
                lower.contains("<embed") || lower.contains("<form") ||
                lower.contains("onerror=") || lower.contains("onload=") ||
                lower.contains("onclick=") || lower.contains("onmouseover=")) {
                return false;
            }
        }
        return true;
    }
}
