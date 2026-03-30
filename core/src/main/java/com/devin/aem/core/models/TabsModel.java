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
public class TabsModel {

    @ValueMapValue
    private String activeTab;

    @ValueMapValue
    private String orientation;

    @ChildResource
    private List<Resource> tabItems;

    private List<TabItem> tabs;

    @PostConstruct
    protected void init() {
        if (orientation == null || orientation.isEmpty()) {
            orientation = "horizontal";
        }
        tabs = new ArrayList<>();
        if (tabItems != null) {
            for (Resource tab : tabItems) {
                TabItem ti = new TabItem();
                ti.setId(tab.getName());
                ti.setTitle(tab.getValueMap().get("tabTitle", String.class));
                ti.setContent(tab.getValueMap().get("tabContent", String.class));
                ti.setIcon(tab.getValueMap().get("tabIcon", String.class));
                tabs.add(ti);
            }
        }
    }

    public String getActiveTab() {
        return activeTab;
    }

    public String getOrientation() {
        return orientation;
    }

    public List<TabItem> getTabs() {
        return Collections.unmodifiableList(tabs);
    }

    public static class TabItem {
        private String id;
        private String title;
        private String content;
        private String icon;

        public String getId() { return id; }
        public void setId(String id) { this.id = id; }
        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getContent() { return content; }
        public void setContent(String content) { this.content = content; }
        public String getIcon() { return icon; }
        public void setIcon(String icon) { this.icon = icon; }
    }
}
