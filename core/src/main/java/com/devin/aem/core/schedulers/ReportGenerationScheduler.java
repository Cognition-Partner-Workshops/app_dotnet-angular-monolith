package com.devin.aem.core.schedulers;

import org.apache.sling.api.resource.LoginException;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.api.resource.ResourceResolverFactory;
import org.osgi.service.component.annotations.Activate;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Deactivate;
import org.osgi.service.component.annotations.Modified;
import org.osgi.service.component.annotations.Reference;
import org.osgi.service.metatype.annotations.AttributeDefinition;
import org.osgi.service.metatype.annotations.Designate;
import org.osgi.service.metatype.annotations.ObjectClassDefinition;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.HashMap;
import java.util.Map;

@Component(service = Runnable.class, immediate = true)
@Designate(ocd = ReportGenerationScheduler.Config.class)
public class ReportGenerationScheduler implements Runnable {

    private static final Logger LOG = LoggerFactory.getLogger(ReportGenerationScheduler.class);

    @ObjectClassDefinition(name = "DevinReactAEM - Report Generation Scheduler",
                           description = "Scheduler to generate site reports (page count, asset count, etc.)")
    public @interface Config {

        @AttributeDefinition(name = "Cron Expression",
                             description = "Cron expression (default: weekly on Sunday at 3 AM)")
        String scheduler_expression() default "0 0 3 ? * SUN";

        @AttributeDefinition(name = "Concurrent Execution")
        boolean scheduler_concurrent() default false;

        @AttributeDefinition(name = "Enabled")
        boolean enabled() default true;

        @AttributeDefinition(name = "Report Path", description = "JCR path to store generated reports")
        String reportPath() default "/content/devinreactaem/reports";

        @AttributeDefinition(name = "Content Root", description = "Root path for content analysis")
        String contentRoot() default "/content/devinreactaem";
    }

    @Reference
    private ResourceResolverFactory resourceResolverFactory;

    private boolean enabled;
    private String reportPath;
    private String contentRoot;

    @Activate
    @Modified
    protected void activate(Config config) {
        this.enabled = config.enabled();
        this.reportPath = config.reportPath();
        this.contentRoot = config.contentRoot();
        LOG.info("Report Generation Scheduler activated. Enabled: {}", enabled);
    }

    @Deactivate
    protected void deactivate() {
        LOG.info("Report Generation Scheduler deactivated");
    }

    @Override
    public void run() {
        if (!enabled) {
            LOG.debug("Report Generation Scheduler is disabled");
            return;
        }

        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd_HH-mm-ss");
        String reportName = "report_" + sdf.format(new Date());

        LOG.info("Report Generation started: {}", reportName);

        Map<String, Object> authInfo = new HashMap<>();
        authInfo.put(ResourceResolverFactory.SUBSERVICE, "devinreactaem-service");

        try (ResourceResolver resolver = resourceResolverFactory.getServiceResourceResolver(authInfo)) {
            int pageCount = countResources(resolver, contentRoot, "cq:Page");
            int assetCount = countResources(resolver, "/content/dam/devinreactaem", "dam:Asset");

            LOG.info("Report '{}' generated: {} pages, {} assets", reportName, pageCount, assetCount);

        } catch (LoginException e) {
            LOG.error("Failed to obtain service resource resolver for report generation", e);
        } catch (Exception e) {
            LOG.error("Error during report generation", e);
        }
    }

    private int countResources(ResourceResolver resolver, String path, String resourceType) {
        int count = 0;
        try {
            org.apache.sling.api.resource.Resource root = resolver.getResource(path);
            if (root != null) {
                java.util.Iterator<org.apache.sling.api.resource.Resource> children = root.listChildren();
                while (children.hasNext()) {
                    org.apache.sling.api.resource.Resource child = children.next();
                    if (child.isResourceType(resourceType)) {
                        count++;
                    }
                    count += countResources(resolver, child.getPath(), resourceType);
                }
            }
        } catch (Exception e) {
            LOG.warn("Error counting resources at path: {}", path);
        }
        return count;
    }
}
