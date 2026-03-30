package com.devin.aem.core.models;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.resource.Resource;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.SlingObject;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import com.adobe.cq.dam.cfm.ContentFragment;

import javax.annotation.PostConstruct;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Iterator;
import java.util.List;

@Model(adaptables = SlingHttpServletRequest.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class ContentFragmentListModel {

    @ValueMapValue
    private String parentPath;

    @ValueMapValue
    private String modelPath;

    @ValueMapValue
    private int maxItems;

    @ValueMapValue
    private String orderBy;

    @ValueMapValue
    private String sortOrder;

    @SlingObject
    private ResourceResolver resourceResolver;

    private List<ContentFragmentItem> fragments;

    @PostConstruct
    protected void init() {
        fragments = new ArrayList<>();
        if (maxItems <= 0) {
            maxItems = 10;
        }
        if (parentPath != null && resourceResolver != null) {
            Resource parentResource = resourceResolver.getResource(parentPath);
            if (parentResource != null) {
                Iterator<Resource> children = parentResource.listChildren();
                int count = 0;
                while (children.hasNext() && count < maxItems) {
                    Resource child = children.next();
                    ContentFragment cf = child.adaptTo(ContentFragment.class);
                    if (cf != null) {
                        ContentFragmentItem item = new ContentFragmentItem();
                        item.setTitle(cf.getTitle());
                        item.setName(cf.getName());
                        item.setDescription(cf.getDescription());
                        item.setPath(child.getPath());
                        fragments.add(item);
                        count++;
                    }
                }
            }
        }
    }

    public String getParentPath() { return parentPath; }
    public String getModelPath() { return modelPath; }
    public int getMaxItems() { return maxItems; }
    public List<ContentFragmentItem> getFragments() { return Collections.unmodifiableList(fragments); }

    public static class ContentFragmentItem {
        private String title;
        private String name;
        private String description;
        private String path;

        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
        public String getPath() { return path; }
        public void setPath(String path) { this.path = path; }
    }
}
