package com.devin.aem.core.services;

import java.util.List;
import java.util.Map;

/**
 * Service interface for Content Fragment operations.
 */
public interface ContentFragmentService {

    /**
     * Lists content fragments under a given path, optionally filtering by model.
     *
     * @param parentPath the parent DAM path
     * @param modelPath  the CF model path (nullable for all models)
     * @param limit      maximum fragments to return
     * @return list of fragment data maps
     */
    List<Map<String, Object>> listFragments(String parentPath, String modelPath, int limit);

    /**
     * Reads a single content fragment and returns all its elements.
     *
     * @param fragmentPath the JCR path to the content fragment
     * @return map of element name to value
     */
    Map<String, Object> getFragment(String fragmentPath);
}
