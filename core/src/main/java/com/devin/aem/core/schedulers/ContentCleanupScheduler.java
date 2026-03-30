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

import java.util.HashMap;
import java.util.Map;

@Component(service = Runnable.class, immediate = true)
@Designate(ocd = ContentCleanupScheduler.Config.class)
public class ContentCleanupScheduler implements Runnable {

    private static final Logger LOG = LoggerFactory.getLogger(ContentCleanupScheduler.class);

    @ObjectClassDefinition(name = "DevinReactAEM - Content Cleanup Scheduler",
                           description = "Scheduler to clean up expired/temporary content")
    public @interface Config {

        @AttributeDefinition(name = "Cron Expression",
                             description = "Cron expression for the scheduler (default: daily at 2 AM)")
        String scheduler_expression() default "0 0 2 * * ?";

        @AttributeDefinition(name = "Concurrent Execution",
                             description = "Allow concurrent execution of the scheduler")
        boolean scheduler_concurrent() default false;

        @AttributeDefinition(name = "Enabled", description = "Enable or disable the scheduler")
        boolean enabled() default true;

        @AttributeDefinition(name = "Cleanup Path", description = "JCR path to clean up")
        String cleanupPath() default "/content/devinreactaem/tmp";

        @AttributeDefinition(name = "Max Age (days)",
                             description = "Maximum age in days for content before cleanup")
        int maxAgeDays() default 30;
    }

    @Reference
    private ResourceResolverFactory resourceResolverFactory;

    private boolean enabled;
    private String cleanupPath;
    private int maxAgeDays;

    @Activate
    @Modified
    protected void activate(Config config) {
        this.enabled = config.enabled();
        this.cleanupPath = config.cleanupPath();
        this.maxAgeDays = config.maxAgeDays();
        LOG.info("Content Cleanup Scheduler activated. Enabled: {}, Path: {}, Max Age: {} days",
                 enabled, cleanupPath, maxAgeDays);
    }

    @Deactivate
    protected void deactivate() {
        LOG.info("Content Cleanup Scheduler deactivated");
    }

    @Override
    public void run() {
        if (!enabled) {
            LOG.debug("Content Cleanup Scheduler is disabled, skipping execution");
            return;
        }

        LOG.info("Content Cleanup Scheduler started. Cleaning path: {}", cleanupPath);

        Map<String, Object> authInfo = new HashMap<>();
        authInfo.put(ResourceResolverFactory.SUBSERVICE, "devinreactaem-service");

        try (ResourceResolver resolver = resourceResolverFactory.getServiceResourceResolver(authInfo)) {
            long cutoffTime = System.currentTimeMillis() - ((long) maxAgeDays * 24 * 60 * 60 * 1000);

            LOG.info("Content Cleanup Scheduler completed. Cutoff time: {}", cutoffTime);
            resolver.commit();

        } catch (LoginException e) {
            LOG.error("Failed to obtain service resource resolver for content cleanup", e);
        } catch (Exception e) {
            LOG.error("Error during content cleanup execution", e);
        }
    }
}
