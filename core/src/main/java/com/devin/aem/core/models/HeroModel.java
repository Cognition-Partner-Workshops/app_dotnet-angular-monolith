package com.devin.aem.core.models;

import org.apache.sling.api.resource.Resource;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import javax.annotation.PostConstruct;

@Model(adaptables = Resource.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class HeroModel {

    @ValueMapValue
    private String title;

    @ValueMapValue
    private String subtitle;

    @ValueMapValue
    private String backgroundImage;

    @ValueMapValue
    private String ctaText;

    @ValueMapValue
    private String ctaLink;

    @ValueMapValue
    private String alignment;

    @ValueMapValue
    private boolean overlayEnabled;

    private String heroClass;

    @PostConstruct
    protected void init() {
        if (alignment == null || alignment.isEmpty()) {
            alignment = "center";
        }
        heroClass = "hero hero--" + alignment;
        if (overlayEnabled) {
            heroClass += " hero--overlay";
        }
    }

    public String getTitle() {
        return title;
    }

    public String getSubtitle() {
        return subtitle;
    }

    public String getBackgroundImage() {
        return backgroundImage;
    }

    public String getCtaText() {
        return ctaText;
    }

    public String getCtaLink() {
        return ctaLink;
    }

    public String getAlignment() {
        return alignment;
    }

    public boolean isOverlayEnabled() {
        return overlayEnabled;
    }

    public String getHeroClass() {
        return heroClass;
    }
}
