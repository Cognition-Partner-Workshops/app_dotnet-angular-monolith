package com.devin.aem.core.jobs;

import org.apache.sling.api.resource.LoginException;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.api.resource.ResourceResolverFactory;
import org.apache.sling.event.jobs.Job;
import org.apache.sling.event.jobs.consumer.JobConsumer;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Reference;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.HashMap;
import java.util.Map;

@Component(service = JobConsumer.class, immediate = true, property = {
    JobConsumer.PROPERTY_TOPICS + "=com/devin/aem/content/sync"
})
public class ContentSyncJobConsumer implements JobConsumer {

    private static final Logger LOG = LoggerFactory.getLogger(ContentSyncJobConsumer.class);

    public static final String JOB_TOPIC = "com/devin/aem/content/sync";

    @Reference
    private ResourceResolverFactory resourceResolverFactory;

    @Override
    public JobResult process(Job job) {
        String sourcePath = job.getProperty("sourcePath", String.class);
        String targetPath = job.getProperty("targetPath", String.class);
        boolean deepSync = job.getProperty("deepSync", false);

        LOG.info("Processing content sync job - Source: {}, Target: {}, Deep: {}",
                 sourcePath, targetPath, deepSync);

        if (sourcePath == null || targetPath == null) {
            LOG.error("Source and target paths are required for content sync");
            return JobResult.CANCEL;
        }

        Map<String, Object> authInfo = new HashMap<>();
        authInfo.put(ResourceResolverFactory.SUBSERVICE, "devinreactaem-service");

        try (ResourceResolver resolver = resourceResolverFactory.getServiceResourceResolver(authInfo)) {
            syncContent(resolver, sourcePath, targetPath, deepSync);
            resolver.commit();

            LOG.info("Content sync completed: {} -> {}", sourcePath, targetPath);
            return JobResult.OK;

        } catch (LoginException e) {
            LOG.error("Failed to obtain service resource resolver for content sync", e);
            return JobResult.CANCEL;
        } catch (Exception e) {
            LOG.error("Error during content sync from {} to {}", sourcePath, targetPath, e);
            return JobResult.FAILED;
        }
    }

    private void syncContent(ResourceResolver resolver, String source, String target, boolean deep) {
        LOG.info("Syncing content from {} to {} (deep: {})", source, target, deep);
    }
}
