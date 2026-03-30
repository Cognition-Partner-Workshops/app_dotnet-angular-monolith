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
    "process.label=DevinReactAEM - Content Approval Process"
})
public class ContentApprovalWorkflowProcess implements WorkflowProcess {

    private static final Logger LOG = LoggerFactory.getLogger(ContentApprovalWorkflowProcess.class);

    @Override
    public void execute(WorkItem workItem, WorkflowSession workflowSession, MetaDataMap metaDataMap)
            throws WorkflowException {

        String payloadPath = workItem.getWorkflowData().getPayload().toString();
        LOG.info("Content Approval Workflow - Processing payload: {}", payloadPath);

        try {
            String approvalStatus = metaDataMap.get("approvalStatus", "pending");
            String approverGroup = metaDataMap.get("approverGroup", "content-approvers");

            LOG.info("Approval Status: {}, Approver Group: {}", approvalStatus, approverGroup);

            if ("approved".equals(approvalStatus)) {
                LOG.info("Content approved for path: {}", payloadPath);
                handleApproval(payloadPath, workflowSession);
            } else if ("rejected".equals(approvalStatus)) {
                LOG.info("Content rejected for path: {}", payloadPath);
                handleRejection(payloadPath, workflowSession);
            } else {
                LOG.info("Content pending approval for path: {}", payloadPath);
                handlePendingApproval(payloadPath, approverGroup, workflowSession);
            }

        } catch (Exception e) {
            LOG.error("Error in Content Approval Workflow for path: {}", payloadPath, e);
            throw new WorkflowException("Content approval workflow failed", e);
        }
    }

    private void handleApproval(String path, WorkflowSession session) {
        LOG.info("Setting content status to 'approved' for: {}", path);
    }

    private void handleRejection(String path, WorkflowSession session) {
        LOG.info("Setting content status to 'rejected' for: {}", path);
    }

    private void handlePendingApproval(String path, String approverGroup, WorkflowSession session) {
        LOG.info("Sending approval notification to group '{}' for path: {}", approverGroup, path);
    }
}
