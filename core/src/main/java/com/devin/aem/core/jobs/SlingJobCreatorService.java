package com.devin.aem.core.jobs;

import org.apache.sling.event.jobs.Job;
import org.apache.sling.event.jobs.JobManager;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Reference;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.HashMap;
import java.util.Map;

@Component(service = SlingJobCreatorService.class, immediate = true)
public class SlingJobCreatorService {

    private static final Logger LOG = LoggerFactory.getLogger(SlingJobCreatorService.class);

    @Reference
    private JobManager jobManager;

    public Job createEmailNotificationJob(String recipientEmail, String subject,
                                          String body, String templatePath) {
        Map<String, Object> properties = new HashMap<>();
        properties.put("recipientEmail", recipientEmail);
        properties.put("subject", subject);
        properties.put("body", body);
        properties.put("templatePath", templatePath);

        LOG.info("Creating email notification job for: {}", recipientEmail);
        return jobManager.addJob(EmailNotificationJobConsumer.JOB_TOPIC, properties);
    }

    public Job createContentSyncJob(String sourcePath, String targetPath, boolean deepSync) {
        Map<String, Object> properties = new HashMap<>();
        properties.put("sourcePath", sourcePath);
        properties.put("targetPath", targetPath);
        properties.put("deepSync", deepSync);

        LOG.info("Creating content sync job: {} -> {}", sourcePath, targetPath);
        return jobManager.addJob(ContentSyncJobConsumer.JOB_TOPIC, properties);
    }
}
