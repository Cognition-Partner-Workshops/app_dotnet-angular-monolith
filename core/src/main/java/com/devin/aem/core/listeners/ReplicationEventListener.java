package com.devin.aem.core.listeners;

import org.osgi.service.component.annotations.Component;
import org.osgi.service.event.Event;
import org.osgi.service.event.EventConstants;
import org.osgi.service.event.EventHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.day.cq.replication.ReplicationAction;
import com.day.cq.replication.ReplicationActionType;

@Component(service = EventHandler.class, immediate = true, property = {
    EventConstants.EVENT_TOPIC + "=" + ReplicationAction.EVENT_TOPIC
})
public class ReplicationEventListener implements EventHandler {

    private static final Logger LOG = LoggerFactory.getLogger(ReplicationEventListener.class);

    @Override
    public void handleEvent(Event event) {
        ReplicationAction action = ReplicationAction.fromEvent(event);
        if (action == null) {
            return;
        }

        String path = action.getPath();
        ReplicationActionType type = action.getType();
        String userId = action.getUserId();

        LOG.info("Replication event - Type: {}, Path: {}, User: {}", type, path, userId);

        if (path.startsWith("/content/devinreactaem")) {
            handleDevinReactAEMReplication(path, type, userId);
        }
    }

    private void handleDevinReactAEMReplication(String path, ReplicationActionType type, String userId) {
        if (ReplicationActionType.ACTIVATE.equals(type)) {
            LOG.info("Content activated: {} by user: {}", path, userId);
            onContentActivated(path);
        } else if (ReplicationActionType.DEACTIVATE.equals(type)) {
            LOG.info("Content deactivated: {} by user: {}", path, userId);
            onContentDeactivated(path);
        } else if (ReplicationActionType.DELETE.equals(type)) {
            LOG.info("Content deleted via replication: {} by user: {}", path, userId);
        }
    }

    private void onContentActivated(String path) {
        LOG.info("Post-activation tasks for: {}", path);
    }

    private void onContentDeactivated(String path) {
        LOG.info("Post-deactivation tasks for: {}", path);
    }
}
