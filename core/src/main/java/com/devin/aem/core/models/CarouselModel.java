package com.devin.aem.core.models;

import org.apache.sling.api.resource.Resource;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.ChildResource;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import javax.annotation.PostConstruct;

@Model(adaptables = Resource.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class CarouselModel {

    @ValueMapValue
    private boolean autoplay;

    @ValueMapValue
    private int autoplayDelay;

    @ValueMapValue
    private boolean showIndicators;

    @ValueMapValue
    private boolean showControls;

    @ValueMapValue
    private String transition;

    @ChildResource
    private List<Resource> slides;

    private List<SlideItem> slideItems;

    @PostConstruct
    protected void init() {
        if (autoplayDelay <= 0) {
            autoplayDelay = 5000;
        }
        if (transition == null || transition.isEmpty()) {
            transition = "slide";
        }
        slideItems = new ArrayList<>();
        if (slides != null) {
            for (Resource slide : slides) {
                SlideItem si = new SlideItem();
                si.setImage(slide.getValueMap().get("slideImage", String.class));
                si.setTitle(slide.getValueMap().get("slideTitle", String.class));
                si.setCaption(slide.getValueMap().get("slideCaption", String.class));
                si.setLink(slide.getValueMap().get("slideLink", String.class));
                slideItems.add(si);
            }
        }
    }

    public boolean isAutoplay() { return autoplay; }
    public int getAutoplayDelay() { return autoplayDelay; }
    public boolean isShowIndicators() { return showIndicators; }
    public boolean isShowControls() { return showControls; }
    public String getTransition() { return transition; }
    public List<SlideItem> getSlideItems() { return Collections.unmodifiableList(slideItems); }

    public static class SlideItem {
        private String image;
        private String title;
        private String caption;
        private String link;

        public String getImage() { return image; }
        public void setImage(String image) { this.image = image; }
        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getCaption() { return caption; }
        public void setCaption(String caption) { this.caption = caption; }
        public String getLink() { return link; }
        public void setLink(String link) { this.link = link; }
    }
}
