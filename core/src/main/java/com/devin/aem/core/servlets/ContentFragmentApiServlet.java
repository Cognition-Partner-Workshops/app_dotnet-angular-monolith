package com.devin.aem.core.servlets;

import com.devin.aem.core.services.ContentFragmentService;
import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonObject;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.SlingHttpServletResponse;
import org.apache.sling.api.servlets.HttpConstants;
import org.apache.sling.api.servlets.SlingSafeMethodServlet;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Reference;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.servlet.Servlet;
import javax.servlet.ServletException;
import java.io.IOException;
import java.util.List;
import java.util.Map;

@Component(service = Servlet.class, property = {
    "sling.servlet.paths=/bin/devinreactaem/contentfragments",
    "sling.servlet.methods=" + HttpConstants.METHOD_GET
})
public class ContentFragmentApiServlet extends SlingSafeMethodServlet {

    private static final Logger LOG = LoggerFactory.getLogger(ContentFragmentApiServlet.class);

    @Reference
    private ContentFragmentService contentFragmentService;

    @Override
    protected void doGet(SlingHttpServletRequest request, SlingHttpServletResponse response)
            throws ServletException, IOException {

        response.setContentType("application/json");
        response.setCharacterEncoding("UTF-8");

        String path = request.getParameter("path");
        String modelPath = request.getParameter("model");
        String limitParam = request.getParameter("limit");
        String action = request.getParameter("action");

        int limit = 10;
        if (limitParam != null) {
            try {
                limit = Integer.parseInt(limitParam);
            } catch (NumberFormatException e) {
                LOG.warn("Invalid limit parameter: {}", limitParam);
            }
        }

        JsonObject jsonResponse = new JsonObject();

        if (path == null || path.isEmpty()) {
            jsonResponse.addProperty("error", "Path parameter is required");
            response.setStatus(SlingHttpServletResponse.SC_BAD_REQUEST);
            response.getWriter().write(jsonResponse.toString());
            return;
        }

        try {
            if ("single".equals(action)) {
                Map<String, Object> fragment = contentFragmentService.getFragment(path);
                Gson gson = new Gson();
                response.getWriter().write(gson.toJson(fragment));
            } else {
                List<Map<String, Object>> fragments = contentFragmentService.listFragments(path, modelPath, limit);
                Gson gson = new Gson();
                jsonResponse.addProperty("total", fragments.size());
                jsonResponse.add("fragments", gson.toJsonTree(fragments));
                response.getWriter().write(jsonResponse.toString());
            }
        } catch (Exception e) {
            LOG.error("Error fetching content fragments", e);
            jsonResponse.addProperty("error", "Failed to retrieve content fragments");
            response.setStatus(SlingHttpServletResponse.SC_INTERNAL_SERVER_ERROR);
            response.getWriter().write(jsonResponse.toString());
        }
    }
}
