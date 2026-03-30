package com.devin.aem.core.listeners;

import com.day.cq.dam.api.DamEvent;

import org.osgi.service.component.annotations.Component;
import org.osgi.service.event.Event;
import org.osgi.service.event.EventConstants;
import org.osgi.service.event.EventHandler;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

@Component(service = EventHandler.class, immediate = true, property = {
    EventConstants.EVENT_TOPIC + "=" + DamEvent.EVENT_TOPIC
})
public class DAMAssetListener implements EventHandler {

    private static final Logger LOG = LoggerFactory.getLogger(DAMAssetListener.class);

    @Override
    public void handleEvent(Event event) {
        DamEvent damEvent = DamEvent.fromEvent(event);
        if (damEvent == null) {
            return;
        }

        String assetPath = damEvent.getAssetPath();
        DamEvent.Type type = damEvent.getType();

        if (assetPath.startsWith("/content/dam/devinreactaem")) {
            LOG.info("DAM event - Type: {}, Asset: {}", type, assetPath);

            switch (type) {
                case ASSET_CREATED:
                    LOG.info("New asset uploaded: {}", assetPath);
                    onAssetCreated(assetPath);
                    break;
                case ASSET_MODIFIED:
                    LOG.info("Asset modified: {}", assetPath);
                    break;
                case ASSET_REMOVED:
                    LOG.info("Asset removed: {}", assetPath);
                    break;
                case RENDITION_UPDATED:
                    LOG.info("Rendition updated for asset: {}", assetPath);
                    break;
                case METADATA_UPDATED:
                    LOG.info("Metadata updated for asset: {}", assetPath);
                    break;
                default:
                    LOG.debug("DAM event type {} for asset: {}", type, assetPath);
            }
        }
    }

    private void onAssetCreated(String assetPath) {
        LOG.info("Performing post-upload processing for: {}", assetPath);
    }
}
