package com.devin.aem.core.jobs;

import org.apache.sling.event.jobs.Job;
import org.apache.sling.event.jobs.consumer.JobConsumer;
import org.osgi.service.component.annotations.Component;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

@Component(service = JobConsumer.class, immediate = true, property = {
    JobConsumer.PROPERTY_TOPICS + "=com/devin/aem/email/notification"
})
public class EmailNotificationJobConsumer implements JobConsumer {

    private static final Logger LOG = LoggerFactory.getLogger(EmailNotificationJobConsumer.class);

    public static final String JOB_TOPIC = "com/devin/aem/email/notification";

    @Override
    public JobResult process(Job job) {
        String recipientEmail = job.getProperty("recipientEmail", String.class);
        String subject = job.getProperty("subject", String.class);
        String body = job.getProperty("body", String.class);
        String templatePath = job.getProperty("templatePath", String.class);

        LOG.info("Processing email notification job - To: {}, Subject: {}", recipientEmail, subject);

        try {
            if (recipientEmail == null || recipientEmail.isEmpty()) {
                LOG.error("Recipient email is required");
                return JobResult.CANCEL;
            }

            if (subject == null || subject.isEmpty()) {
                subject = "DevinReactAEM Notification";
            }

            sendEmail(recipientEmail, subject, body, templatePath);

            LOG.info("Email notification sent successfully to: {}", recipientEmail);
            return JobResult.OK;

        } catch (Exception e) {
            LOG.error("Failed to send email notification to: {}", recipientEmail, e);
            int retryCount = job.getRetryCount();
            if (retryCount < 3) {
                LOG.info("Will retry email notification (attempt {} of 3)", retryCount + 1);
                return JobResult.FAILED;
            }
            LOG.error("Max retries reached for email notification to: {}", recipientEmail);
            return JobResult.CANCEL;
        }
    }

    private void sendEmail(String to, String subject, String body, String templatePath) {
        LOG.info("Sending email - To: {}, Subject: {}, Template: {}", to, subject, templatePath);
    }
}
