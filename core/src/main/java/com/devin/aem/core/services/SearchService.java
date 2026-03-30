package com.devin.aem.core.services;

import java.util.List;
import java.util.Map;

/**
 * Service interface for full-text search operations across AEM content.
 */
public interface SearchService {

    /**
     * Performs a full-text search under the given root path.
     *
     * @param queryText   the search query
     * @param searchRoot  the JCR path to search under
     * @param limit       maximum results to return
     * @return list of result maps containing path, title, and excerpt
     */
    List<Map<String, String>> search(String queryText, String searchRoot, int limit);
}
