package com.devin.aem.core.models;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.resource.Resource;
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
import java.util.Iterator;
import java.util.List;

@Model(adaptables = SlingHttpServletRequest.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class NavigationModel {

    @ValueMapValue
    private String navigationRoot;

    @ValueMapValue
    private int structureDepth;

    @ValueMapValue
    private boolean skipNavigationRoot;

    @SlingObject
    private ResourceResolver resourceResolver;

    private List<NavigationItem> navigationItems;

    @PostConstruct
    protected void init() {
        navigationItems = new ArrayList<>();
        if (structureDepth <= 0) {
            structureDepth = 2;
        }
        if (navigationRoot != null && resourceResolver != null) {
            PageManager pageManager = resourceResolver.adaptTo(PageManager.class);
            if (pageManager != null) {
                Page rootPage = pageManager.getPage(navigationRoot);
                if (rootPage != null) {
                    buildNavigation(rootPage, 0);
                }
            }
        }
    }

    private void buildNavigation(Page page, int currentDepth) {
        if (currentDepth >= structureDepth) return;

        Iterator<Page> children = page.listChildren();
        while (children.hasNext()) {
            Page child = children.next();
            if (!child.isHideInNav()) {
                NavigationItem item = new NavigationItem();
                item.setTitle(child.getTitle() != null ? child.getTitle() : child.getName());
                item.setPath(child.getPath() + ".html");
                item.setLevel(currentDepth);
                item.setActive(false);

                List<NavigationItem> subItems = new ArrayList<>();
                if (currentDepth + 1 < structureDepth) {
                    Iterator<Page> grandChildren = child.listChildren();
                    while (grandChildren.hasNext()) {
                        Page grandChild = grandChildren.next();
                        if (!grandChild.isHideInNav()) {
                            NavigationItem subItem = new NavigationItem();
                            subItem.setTitle(grandChild.getTitle() != null ? grandChild.getTitle() : grandChild.getName());
                            subItem.setPath(grandChild.getPath() + ".html");
                            subItem.setLevel(currentDepth + 1);
                            subItems.add(subItem);
                        }
                    }
                }
                item.setChildren(subItems);
                navigationItems.add(item);
            }
        }
    }

    public String getNavigationRoot() { return navigationRoot; }
    public int getStructureDepth() { return structureDepth; }
    public boolean isSkipNavigationRoot() { return skipNavigationRoot; }
    public List<NavigationItem> getNavigationItems() { return Collections.unmodifiableList(navigationItems); }

    public static class NavigationItem {
        private String title;
        private String path;
        private int level;
        private boolean active;
        private List<NavigationItem> children = new ArrayList<>();

        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getPath() { return path; }
        public void setPath(String path) { this.path = path; }
        public int getLevel() { return level; }
        public void setLevel(int level) { this.level = level; }
        public boolean isActive() { return active; }
        public void setActive(boolean active) { this.active = active; }
        public List<NavigationItem> getChildren() { return children; }
        public void setChildren(List<NavigationItem> children) { this.children = children; }
        public boolean hasChildren() { return children != null && !children.isEmpty(); }
    }
}
