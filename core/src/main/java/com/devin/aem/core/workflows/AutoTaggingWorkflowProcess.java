package com.devin.aem.core.workflows;

import com.adobe.granite.workflow.WorkflowException;
import com.adobe.granite.workflow.WorkflowSession;
import com.adobe.granite.workflow.exec.WorkItem;
import com.adobe.granite.workflow.exec.WorkflowProcess;
import com.adobe.granite.workflow.metadata.MetaDataMap;

import org.apache.sling.api.resource.ModifiableValueMap;
import org.apache.sling.api.resource.Resource;
import org.apache.sling.api.resource.ResourceResolver;
import org.osgi.service.component.annotations.Component;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.List;

@Component(service = WorkflowProcess.class, immediate = true, property = {
    "process.label=DevinReactAEM - Auto Tagging Process"
})
public class AutoTaggingWorkflowProcess implements WorkflowProcess {

    private static final Logger LOG = LoggerFactory.getLogger(AutoTaggingWorkflowProcess.class);

    @Override
    public void execute(WorkItem workItem, WorkflowSession workflowSession, MetaDataMap metaDataMap)
            throws WorkflowException {

        String payloadPath = workItem.getWorkflowData().getPayload().toString();
        LOG.info("Auto Tagging Workflow - Processing payload: {}", payloadPath);

        try {
            ResourceResolver resolver = workflowSession.adaptTo(ResourceResolver.class);
            if (resolver == null) {
                throw new WorkflowException("Unable to obtain ResourceResolver");
            }

            String tagNamespace = metaDataMap.get("tagNamespace", "devinreactaem:");
            boolean overwriteExisting = metaDataMap.get("overwriteExisting", false);

            Resource resource = resolver.getResource(payloadPath + "/jcr:content");
            if (resource != null) {
                ModifiableValueMap properties = resource.adaptTo(ModifiableValueMap.class);
                if (properties != null) {
                    String title = properties.get("jcr:title", "");
                    String description = properties.get("jcr:description", "");

                    List<String> tags = generateTags(title, description, tagNamespace);

                    if (!tags.isEmpty()) {
                        String[] existingTags = properties.get("cq:tags", String[].class);
                        if (overwriteExisting || existingTags == null || existingTags.length == 0) {
                            properties.put("cq:tags", tags.toArray(new String[0]));
                            resolver.commit();
                            LOG.info("Auto-tagged page '{}' with {} tags", payloadPath, tags.size());
                        } else {
                            LOG.info("Skipping auto-tagging for '{}' - existing tags found", payloadPath);
                        }
                    }
                }
            }

        } catch (Exception e) {
            LOG.error("Error in Auto Tagging Workflow for path: {}", payloadPath, e);
            throw new WorkflowException("Auto tagging workflow failed", e);
        }
    }

    private List<String> generateTags(String title, String description, String namespace) {
        List<String> tags = new ArrayList<>();
        String combined = (title + " " + description).toLowerCase();

        String[][] tagKeywords = {
            {"product", namespace + "category/product"},
            {"service", namespace + "category/service"},
            {"news", namespace + "type/news"},
            {"blog", namespace + "type/blog"},
            {"tutorial", namespace + "type/tutorial"},
            {"announcement", namespace + "type/announcement"},
            {"event", namespace + "type/event"},
            {"faq", namespace + "type/faq"}
        };

        for (String[] entry : tagKeywords) {
            if (combined.contains(entry[0])) {
                tags.add(entry[1]);
            }
        }

        return tags;
    }
}
