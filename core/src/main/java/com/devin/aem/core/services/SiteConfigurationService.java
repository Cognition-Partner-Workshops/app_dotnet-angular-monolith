package com.devin.aem.core.services;

/**
 * Service interface for site-wide configuration values.
 */
public interface SiteConfigurationService {

    String getSiteName();

    String getSiteLocale();

    String getAnalyticsId();

    String getHeaderExperienceFragment();

    String getFooterExperienceFragment();

    boolean isSearchEnabled();

    int getSearchResultsPerPage();
}
