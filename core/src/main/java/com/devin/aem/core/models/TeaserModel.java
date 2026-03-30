package com.devin.aem.core.models;

import org.apache.sling.api.resource.Resource;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import javax.annotation.PostConstruct;

@Model(adaptables = Resource.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class TeaserModel {

    @ValueMapValue
    private String title;

    @ValueMapValue
    private String description;

    @ValueMapValue
    private String image;

    @ValueMapValue
    private String linkURL;

    @ValueMapValue
    private String linkText;

    @ValueMapValue
    private String pretitle;

    @ValueMapValue
    private String style;

    private String teaserClass;

    @PostConstruct
    protected void init() {
        if (style == null || style.isEmpty()) {
            style = "default";
        }
        teaserClass = "cmp-teaser cmp-teaser--" + style;
    }

    public String getTitle() {
        return title;
    }

    public String getDescription() {
        return description;
    }

    public String getImage() {
        return image;
    }

    public String getLinkURL() {
        return linkURL;
    }

    public String getLinkText() {
        return linkText;
    }

    public String getPretitle() {
        return pretitle;
    }

    public String getStyle() {
        return style;
    }

    public String getTeaserClass() {
        return teaserClass;
    }
}
