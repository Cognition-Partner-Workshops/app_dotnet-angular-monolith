package com.devin.aem.core.models;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.SlingObject;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import com.day.cq.search.PredicateGroup;
import com.day.cq.search.Query;
import com.day.cq.search.QueryBuilder;
import com.day.cq.search.result.Hit;
import com.day.cq.search.result.SearchResult;

import javax.annotation.PostConstruct;
import javax.inject.Inject;
import javax.jcr.Session;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Model(adaptables = SlingHttpServletRequest.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class SearchResultsModel {

    @ValueMapValue
    private String searchRoot;

    @ValueMapValue
    private int resultsPerPage;

    @SlingObject
    private ResourceResolver resourceResolver;

    @Inject
    private QueryBuilder queryBuilder;

    private List<SearchResultItem> results;
    private long totalResults;

    @PostConstruct
    protected void init() {
        results = new ArrayList<>();
        if (searchRoot == null || searchRoot.isEmpty()) {
            searchRoot = "/content/devinreactaem";
        }
        if (resultsPerPage <= 0) {
            resultsPerPage = 10;
        }
    }

    public List<SearchResultItem> search(String queryText) {
        results = new ArrayList<>();
        if (queryText == null || queryText.trim().isEmpty()) {
            return results;
        }

        Map<String, String> predicateMap = new HashMap<>();
        predicateMap.put("path", searchRoot);
        predicateMap.put("type", "cq:Page");
        predicateMap.put("fulltext", queryText);
        predicateMap.put("p.limit", String.valueOf(resultsPerPage));
        predicateMap.put("orderby", "@jcr:score");
        predicateMap.put("orderby.sort", "desc");

        try {
            Session session = resourceResolver.adaptTo(Session.class);
            if (session != null && queryBuilder != null) {
                Query query = queryBuilder.createQuery(PredicateGroup.create(predicateMap), session);
                SearchResult result = query.getResult();
                totalResults = result.getTotalMatches();

                for (Hit hit : result.getHits()) {
                    SearchResultItem item = new SearchResultItem();
                    item.setPath(hit.getPath());
                    item.setTitle(hit.getTitle());
                    item.setExcerpt(hit.getExcerpt());
                    results.add(item);
                }
            }
        } catch (Exception e) {
            totalResults = 0;
        }
        return results;
    }

    public String getSearchRoot() { return searchRoot; }
    public int getResultsPerPage() { return resultsPerPage; }
    public List<SearchResultItem> getResults() { return Collections.unmodifiableList(results); }
    public long getTotalResults() { return totalResults; }

    public static class SearchResultItem {
        private String path;
        private String title;
        private String excerpt;

        public String getPath() { return path; }
        public void setPath(String path) { this.path = path; }
        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getExcerpt() { return excerpt; }
        public void setExcerpt(String excerpt) { this.excerpt = excerpt; }
    }
}
