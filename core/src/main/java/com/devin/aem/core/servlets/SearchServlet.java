package com.devin.aem.core.servlets;

import com.devin.aem.core.services.SearchService;
import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonObject;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.SlingHttpServletResponse;
import org.apache.sling.api.servlets.HttpConstants;
import org.apache.sling.api.servlets.SlingSafeMethodsServlet;
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
    "sling.servlet.paths=/bin/devinreactaem/search",
    "sling.servlet.methods=" + HttpConstants.METHOD_GET
})
public class SearchServlet extends SlingSafeMethodsServlet {

    private static final Logger LOG = LoggerFactory.getLogger(SearchServlet.class);

    @Reference
    private SearchService searchService;

    @Override
    protected void doGet(SlingHttpServletRequest request, SlingHttpServletResponse response)
            throws ServletException, IOException {

        response.setContentType("application/json");
        response.setCharacterEncoding("UTF-8");

        String query = request.getParameter("q");
        String searchRoot = request.getParameter("root");
        String limitParam = request.getParameter("limit");

        if (searchRoot == null || searchRoot.isEmpty()) {
            searchRoot = "/content/devinreactaem";
        }

        int limit = 10;
        if (limitParam != null) {
            try {
                limit = Integer.parseInt(limitParam);
            } catch (NumberFormatException e) {
                LOG.warn("Invalid limit parameter: {}", limitParam);
            }
        }

        JsonObject jsonResponse = new JsonObject();

        if (query == null || query.trim().isEmpty()) {
            jsonResponse.addProperty("error", "Query parameter 'q' is required");
            jsonResponse.add("results", new JsonArray());
            jsonResponse.addProperty("total", 0);
            response.getWriter().write(jsonResponse.toString());
            return;
        }

        List<Map<String, String>> results = searchService.search(query, searchRoot, limit);

        JsonArray jsonResults = new JsonArray();
        for (Map<String, String> result : results) {
            JsonObject item = new JsonObject();
            item.addProperty("path", result.get("path"));
            item.addProperty("title", result.get("title"));
            item.addProperty("excerpt", result.get("excerpt"));
            jsonResults.add(item);
        }

        jsonResponse.add("results", jsonResults);
        jsonResponse.addProperty("total", results.size());
        jsonResponse.addProperty("query", query);

        response.getWriter().write(jsonResponse.toString());
    }
}
