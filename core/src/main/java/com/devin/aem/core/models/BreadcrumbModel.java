package com.devin.aem.core.models;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.SlingObject;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import com.day.cq.wcm.api.Page;
import com.day.cq.wcm.api.PageManager;

import javax.annotation.PostConstruct;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

@Model(adaptables = SlingHttpServletRequest.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class BreadcrumbModel {

    @ValueMapValue
    private int startLevel;

    @ValueMapValue
    private boolean hideCurrent;

    @SlingObject
    private ResourceResolver resourceResolver;

    @SlingObject
    private SlingHttpServletRequest request;

    private List<BreadcrumbItem> breadcrumbItems;

    @PostConstruct
    protected void init() {
        breadcrumbItems = new ArrayList<>();
        if (startLevel <= 0) {
            startLevel = 2;
        }
        if (resourceResolver != null) {
            PageManager pageManager = resourceResolver.adaptTo(PageManager.class);
            if (pageManager != null) {
                Page currentPage = pageManager.getContainingPage(request.getResource());
                if (currentPage != null) {
                    buildBreadcrumb(currentPage);
                }
            }
        }
    }

    private void buildBreadcrumb(Page currentPage) {
        int depth = currentPage.getDepth();
        List<BreadcrumbItem> items = new ArrayList<>();

        for (int i = startLevel; i < depth; i++) {
            Page ancestor = currentPage.getAbsoluteParent(i);
            if (ancestor != null) {
                BreadcrumbItem item = new BreadcrumbItem();
                item.setTitle(ancestor.getTitle() != null ? ancestor.getTitle() : ancestor.getName());
                item.setPath(ancestor.getPath() + ".html");
                item.setActive(false);
                items.add(item);
            }
        }

        if (!hideCurrent) {
            BreadcrumbItem current = new BreadcrumbItem();
            current.setTitle(currentPage.getTitle() != null ? currentPage.getTitle() : currentPage.getName());
            current.setPath(currentPage.getPath() + ".html");
            current.setActive(true);
            items.add(current);
        }

        breadcrumbItems = items;
    }

    public List<BreadcrumbItem> getBreadcrumbItems() {
        return Collections.unmodifiableList(breadcrumbItems);
    }

    public static class BreadcrumbItem {
        private String title;
        private String path;
        private boolean active;

        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getPath() { return path; }
        public void setPath(String path) { this.path = path; }
        public boolean isActive() { return active; }
        public void setActive(boolean active) { this.active = active; }
    }
}
