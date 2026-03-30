package com.devin.aem.core.filters;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.SlingHttpServletResponse;
import org.apache.sling.api.servlets.HttpConstants;
import org.apache.sling.servlets.annotations.SlingServletFilter;
import org.apache.sling.servlets.annotations.SlingServletFilterScope;
import org.osgi.service.component.annotations.Component;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.servlet.Filter;
import javax.servlet.FilterChain;
import javax.servlet.FilterConfig;
import javax.servlet.ServletException;
import javax.servlet.ServletRequest;
import javax.servlet.ServletResponse;
import java.io.IOException;

@Component(service = Filter.class)
@SlingServletFilter(scope = SlingServletFilterScope.REQUEST,
                    pattern = "/content/devinreactaem/.*",
                    methods = {HttpConstants.METHOD_GET, HttpConstants.METHOD_POST})
public class LoggingFilter implements Filter {

    private static final Logger LOG = LoggerFactory.getLogger(LoggingFilter.class);

    @Override
    public void init(FilterConfig filterConfig) throws ServletException {
        LOG.info("DevinReactAEM Logging Filter initialized");
    }

    @Override
    public void doFilter(ServletRequest request, ServletResponse response, FilterChain chain)
            throws IOException, ServletException {

        long startTime = System.currentTimeMillis();

        SlingHttpServletRequest slingRequest = (SlingHttpServletRequest) request;
        String method = slingRequest.getMethod();
        String path = slingRequest.getPathInfo();
        String userAgent = slingRequest.getHeader("User-Agent");

        LOG.debug("Request: {} {} - User-Agent: {}", method, path, userAgent);

        chain.doFilter(request, response);

        long duration = System.currentTimeMillis() - startTime;

        SlingHttpServletResponse slingResponse = (SlingHttpServletResponse) response;
        int status = slingResponse.getStatus();

        LOG.debug("Response: {} {} - Status: {} - Duration: {}ms", method, path, status, duration);

        if (duration > 3000) {
            LOG.warn("Slow request detected: {} {} took {}ms", method, path, duration);
        }
    }

    @Override
    public void destroy() {
        LOG.info("DevinReactAEM Logging Filter destroyed");
    }
}
