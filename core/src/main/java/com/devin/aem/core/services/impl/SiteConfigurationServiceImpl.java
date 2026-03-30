package com.devin.aem.core.services.impl;

import com.devin.aem.core.services.SiteConfigurationService;

import org.osgi.service.component.annotations.Activate;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Modified;
import org.osgi.service.metatype.annotations.AttributeDefinition;
import org.osgi.service.metatype.annotations.Designate;
import org.osgi.service.metatype.annotations.ObjectClassDefinition;

@Component(service = SiteConfigurationService.class, immediate = true)
@Designate(ocd = SiteConfigurationServiceImpl.Config.class)
public class SiteConfigurationServiceImpl implements SiteConfigurationService {

    @ObjectClassDefinition(name = "DevinReactAEM - Site Configuration",
                           description = "Configuration for site-wide settings")
    public @interface Config {

        @AttributeDefinition(name = "Site Name", description = "The name of the site")
        String siteName() default "DevinReactAEM";

        @AttributeDefinition(name = "Site Locale", description = "Default locale for the site")
        String siteLocale() default "en-US";

        @AttributeDefinition(name = "Analytics ID", description = "Google Analytics or Adobe Analytics ID")
        String analyticsId() default "";

        @AttributeDefinition(name = "Header Experience Fragment",
                             description = "Path to the header experience fragment")
        String headerExperienceFragment() default "/content/experience-fragments/devinreactaem/header/master";

        @AttributeDefinition(name = "Footer Experience Fragment",
                             description = "Path to the footer experience fragment")
        String footerExperienceFragment() default "/content/experience-fragments/devinreactaem/footer/master";

        @AttributeDefinition(name = "Enable Search", description = "Enable or disable site search")
        boolean searchEnabled() default true;

        @AttributeDefinition(name = "Search Results Per Page",
                             description = "Number of search results per page")
        int searchResultsPerPage() default 10;
    }

    private String siteName;
    private String siteLocale;
    private String analyticsId;
    private String headerExperienceFragment;
    private String footerExperienceFragment;
    private boolean searchEnabled;
    private int searchResultsPerPage;

    @Activate
    @Modified
    protected void activate(Config config) {
        this.siteName = config.siteName();
        this.siteLocale = config.siteLocale();
        this.analyticsId = config.analyticsId();
        this.headerExperienceFragment = config.headerExperienceFragment();
        this.footerExperienceFragment = config.footerExperienceFragment();
        this.searchEnabled = config.searchEnabled();
        this.searchResultsPerPage = config.searchResultsPerPage();
    }

    @Override
    public String getSiteName() { return siteName; }

    @Override
    public String getSiteLocale() { return siteLocale; }

    @Override
    public String getAnalyticsId() { return analyticsId; }

    @Override
    public String getHeaderExperienceFragment() { return headerExperienceFragment; }

    @Override
    public String getFooterExperienceFragment() { return footerExperienceFragment; }

    @Override
    public boolean isSearchEnabled() { return searchEnabled; }

    @Override
    public int getSearchResultsPerPage() { return searchResultsPerPage; }
}
