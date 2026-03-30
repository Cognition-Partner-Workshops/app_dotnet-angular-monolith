package com.devin.aem.core.listeners;

import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Reference;
import org.osgi.service.event.Event;
import org.osgi.service.event.EventConstants;
import org.osgi.service.event.EventHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.day.cq.wcm.api.PageEvent;
import com.day.cq.wcm.api.PageModification;

import java.util.Iterator;

@Component(service = EventHandler.class, immediate = true, property = {
    EventConstants.EVENT_TOPIC + "=" + PageEvent.EVENT_TOPIC
})
public class PageEventListener implements EventHandler {

    private static final Logger LOG = LoggerFactory.getLogger(PageEventListener.class);

    @Override
    public void handleEvent(Event event) {
        PageEvent pageEvent = PageEvent.fromEvent(event);
        if (pageEvent == null) {
            return;
        }

        Iterator<PageModification> modifications = pageEvent.getModifications();
        while (modifications.hasNext()) {
            PageModification modification = modifications.next();
            String path = modification.getPath();
            PageModification.ModificationType type = modification.getType();

            switch (type) {
                case CREATED:
                    LOG.info("Page created: {}", path);
                    onPageCreated(path);
                    break;
                case MODIFIED:
                    LOG.info("Page modified: {}", path);
                    onPageModified(path);
                    break;
                case DELETED:
                    LOG.info("Page deleted: {}", path);
                    onPageDeleted(path);
                    break;
                case MOVED:
                    LOG.info("Page moved: {}", path);
                    break;
                case VERSION_CREATED:
                    LOG.info("Page version created: {}", path);
                    break;
                default:
                    LOG.debug("Page event of type {} for path: {}", type, path);
            }
        }
    }

    private void onPageCreated(String path) {
        if (path.startsWith("/content/devinreactaem")) {
            LOG.info("DevinReactAEM page created, triggering post-creation tasks for: {}", path);
        }
    }

    private void onPageModified(String path) {
        if (path.startsWith("/content/devinreactaem")) {
            LOG.info("DevinReactAEM page modified, invalidating cache for: {}", path);
        }
    }

    private void onPageDeleted(String path) {
        if (path.startsWith("/content/devinreactaem")) {
            LOG.info("DevinReactAEM page deleted, cleaning up references for: {}", path);
        }
    }
}
