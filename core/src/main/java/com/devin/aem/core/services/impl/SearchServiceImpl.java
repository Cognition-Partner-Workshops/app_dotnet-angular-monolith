package com.devin.aem.core.services.impl;

import com.devin.aem.core.services.SearchService;
import com.day.cq.search.PredicateGroup;
import com.day.cq.search.Query;
import com.day.cq.search.QueryBuilder;
import com.day.cq.search.result.Hit;
import com.day.cq.search.result.SearchResult;

import org.apache.sling.api.resource.LoginException;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.api.resource.ResourceResolverFactory;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Reference;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.jcr.Session;
import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Component(service = SearchService.class, immediate = true)
public class SearchServiceImpl implements SearchService {

    private static final Logger LOG = LoggerFactory.getLogger(SearchServiceImpl.class);

    private static final String SERVICE_USER = "devinreactaem-service";

    @Reference
    private QueryBuilder queryBuilder;

    @Reference
    private ResourceResolverFactory resourceResolverFactory;

    @Override
    public List<Map<String, String>> search(String queryText, String searchRoot, int limit) {
        if (queryText == null || queryText.trim().isEmpty()) {
            return Collections.emptyList();
        }

        List<Map<String, String>> results = new ArrayList<>();
        Map<String, Object> authInfo = new HashMap<>();
        authInfo.put(ResourceResolverFactory.SUBSERVICE, SERVICE_USER);

        try (ResourceResolver resolver = resourceResolverFactory.getServiceResourceResolver(authInfo)) {
            Map<String, String> predicateMap = new HashMap<>();
            predicateMap.put("path", searchRoot);
            predicateMap.put("type", "cq:Page");
            predicateMap.put("fulltext", queryText);
            predicateMap.put("p.limit", String.valueOf(limit));
            predicateMap.put("orderby", "@jcr:score");
            predicateMap.put("orderby.sort", "desc");

            Session session = resolver.adaptTo(Session.class);
            if (session != null) {
                Query query = queryBuilder.createQuery(PredicateGroup.create(predicateMap), session);
                SearchResult searchResult = query.getResult();

                for (Hit hit : searchResult.getHits()) {
                    Map<String, String> item = new HashMap<>();
                    item.put("path", hit.getPath());
                    item.put("title", hit.getTitle());
                    item.put("excerpt", hit.getExcerpt());
                    results.add(item);
                }
            }
        } catch (LoginException e) {
            LOG.error("Failed to obtain service resource resolver for search", e);
        } catch (Exception e) {
            LOG.error("Error executing search query: {}", queryText, e);
        }

        return results;
    }
}
