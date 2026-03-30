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
public class AccordionModel {

    @ValueMapValue
    private String heading;

    @ValueMapValue
    private boolean singleExpansion;

    @ValueMapValue
    private String expandedItems;

    @ChildResource
    private List<Resource> items;

    private List<AccordionItem> accordionItems;

    @PostConstruct
    protected void init() {
        accordionItems = new ArrayList<>();
        if (items != null) {
            for (Resource item : items) {
                AccordionItem ai = new AccordionItem();
                ai.setTitle(item.getValueMap().get("itemTitle", String.class));
                ai.setContent(item.getValueMap().get("itemContent", String.class));
                ai.setIcon(item.getValueMap().get("itemIcon", String.class));
                accordionItems.add(ai);
            }
        }
    }

    public String getHeading() {
        return heading;
    }

    public boolean isSingleExpansion() {
        return singleExpansion;
    }

    public String getExpandedItems() {
        return expandedItems;
    }

    public List<AccordionItem> getAccordionItems() {
        return Collections.unmodifiableList(accordionItems);
    }

    public static class AccordionItem {
        private String title;
        private String content;
        private String icon;

        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getContent() { return content; }
        public void setContent(String content) { this.content = content; }
        public String getIcon() { return icon; }
        public void setIcon(String icon) { this.icon = icon; }
    }
}
