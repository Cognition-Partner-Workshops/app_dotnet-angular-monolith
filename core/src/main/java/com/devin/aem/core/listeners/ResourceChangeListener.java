package com.devin.aem.core.listeners;

import org.apache.sling.api.resource.observation.ResourceChange;
import org.osgi.service.component.annotations.Component;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;

@Component(service = org.apache.sling.api.resource.observation.ResourceChangeListener.class, immediate = true, property = {
    org.apache.sling.api.resource.observation.ResourceChangeListener.PATHS + "=/content/devinreactaem",
    org.apache.sling.api.resource.observation.ResourceChangeListener.CHANGES + "=ADDED",
    org.apache.sling.api.resource.observation.ResourceChangeListener.CHANGES + "=CHANGED",
    org.apache.sling.api.resource.observation.ResourceChangeListener.CHANGES + "=REMOVED"
})
public class ResourceChangeListener implements org.apache.sling.api.resource.observation.ResourceChangeListener {

    private static final Logger LOG = LoggerFactory.getLogger(ResourceChangeListener.class);

    @Override
    public void onChange(List<ResourceChange> changes) {
        for (ResourceChange change : changes) {
            String path = change.getPath();
            ResourceChange.ChangeType type = change.getType();

            LOG.debug("Resource change detected - Type: {}, Path: {}", type, path);

            switch (type) {
                case ADDED:
                    onResourceAdded(path);
                    break;
                case CHANGED:
                    onResourceChanged(path);
                    break;
                case REMOVED:
                    onResourceRemoved(path);
                    break;
                default:
                    break;
            }
        }
    }

    private void onResourceAdded(String path) {
        LOG.info("Resource added under devinreactaem: {}", path);
    }

    private void onResourceChanged(String path) {
        LOG.info("Resource changed under devinreactaem: {}", path);
    }

    private void onResourceRemoved(String path) {
        LOG.info("Resource removed under devinreactaem: {}", path);
    }
}
