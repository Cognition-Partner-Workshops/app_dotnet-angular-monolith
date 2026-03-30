package com.devin.aem.core.workflows;

import com.adobe.granite.workflow.WorkflowException;
import com.adobe.granite.workflow.WorkflowSession;
import com.adobe.granite.workflow.exec.WorkItem;
import com.adobe.granite.workflow.exec.WorkflowProcess;
import com.adobe.granite.workflow.metadata.MetaDataMap;

import org.osgi.service.component.annotations.Component;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

@Component(service = WorkflowProcess.class, immediate = true, property = {
    "process.label=DevinReactAEM - Notification Process"
})
public class NotificationWorkflowProcess implements WorkflowProcess {

    private static final Logger LOG = LoggerFactory.getLogger(NotificationWorkflowProcess.class);

    @Override
    public void execute(WorkItem workItem, WorkflowSession workflowSession, MetaDataMap metaDataMap)
            throws WorkflowException {

        String payloadPath = workItem.getWorkflowData().getPayload().toString();
        LOG.info("Notification Workflow - Processing payload: {}", payloadPath);

        try {
            String notificationType = metaDataMap.get("notificationType", "email");
            String recipientGroup = metaDataMap.get("recipientGroup", "content-authors");
            String subject = metaDataMap.get("subject", "Content Update Notification");
            String templatePath = metaDataMap.get("templatePath",
                "/etc/notification/email/devinreactaem/content-update.html");

            LOG.info("Sending {} notification to group '{}' for payload: {}",
                     notificationType, recipientGroup, payloadPath);

            switch (notificationType) {
                case "email":
                    sendEmailNotification(recipientGroup, subject, payloadPath, templatePath);
                    break;
                case "inbox":
                    sendInboxNotification(recipientGroup, subject, payloadPath, workflowSession);
                    break;
                case "both":
                    sendEmailNotification(recipientGroup, subject, payloadPath, templatePath);
                    sendInboxNotification(recipientGroup, subject, payloadPath, workflowSession);
                    break;
                default:
                    LOG.warn("Unknown notification type: {}", notificationType);
            }

        } catch (Exception e) {
            LOG.error("Error in Notification Workflow for path: {}", payloadPath, e);
            throw new WorkflowException("Notification workflow failed", e);
        }
    }

    private void sendEmailNotification(String group, String subject, String path, String template) {
        LOG.info("Email notification sent to group '{}' - Subject: '{}', Path: {}, Template: {}",
                 group, subject, path, template);
    }

    private void sendInboxNotification(String group, String subject, String path,
                                       WorkflowSession session) {
        LOG.info("Inbox notification sent to group '{}' - Subject: '{}', Path: {}",
                 group, subject, path);
    }
}
