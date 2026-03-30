package com.devin.aem.core.services.impl;

import com.devin.aem.core.services.ContentFragmentService;
import com.adobe.cq.dam.cfm.ContentElement;
import com.adobe.cq.dam.cfm.ContentFragment;
import com.adobe.cq.dam.cfm.FragmentData;

import org.apache.sling.api.resource.LoginException;
import org.apache.sling.api.resource.Resource;
import org.apache.sling.api.resource.ResourceResolver;
import org.apache.sling.api.resource.ResourceResolverFactory;
import org.osgi.service.component.annotations.Component;
import org.osgi.service.component.annotations.Reference;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;

@Component(service = ContentFragmentService.class, immediate = true)
public class ContentFragmentServiceImpl implements ContentFragmentService {

    private static final Logger LOG = LoggerFactory.getLogger(ContentFragmentServiceImpl.class);

    private static final String SERVICE_USER = "devinreactaem-service";

    @Reference
    private ResourceResolverFactory resourceResolverFactory;

    @Override
    public List<Map<String, Object>> listFragments(String parentPath, String modelPath, int limit) {
        List<Map<String, Object>> fragmentList = new ArrayList<>();
        Map<String, Object> authInfo = new HashMap<>();
        authInfo.put(ResourceResolverFactory.SUBSERVICE, SERVICE_USER);

        try (ResourceResolver resolver = resourceResolverFactory.getServiceResourceResolver(authInfo)) {
            Resource parentResource = resolver.getResource(parentPath);
            if (parentResource != null) {
                Iterator<Resource> children = parentResource.listChildren();
                int count = 0;
                while (children.hasNext() && count < limit) {
                    Resource child = children.next();
                    ContentFragment cf = child.adaptTo(ContentFragment.class);
                    if (cf != null) {
                        Map<String, Object> data = new HashMap<>();
                        data.put("title", cf.getTitle());
                        data.put("name", cf.getName());
                        data.put("description", cf.getDescription());
                        data.put("path", child.getPath());

                        Map<String, Object> elements = new HashMap<>();
                        Iterator<ContentElement> elementIterator = cf.getElements();
                        while (elementIterator.hasNext()) {
                            ContentElement element = elementIterator.next();
                            FragmentData fragmentData = element.getValue();
                            if (fragmentData != null) {
                                elements.put(element.getName(), fragmentData.getValue(String.class));
                            }
                        }
                        data.put("elements", elements);
                        fragmentList.add(data);
                        count++;
                    }
                }
            }
        } catch (LoginException e) {
            LOG.error("Failed to obtain service resource resolver", e);
        }

        return fragmentList;
    }

    @Override
    public Map<String, Object> getFragment(String fragmentPath) {
        Map<String, Object> authInfo = new HashMap<>();
        authInfo.put(ResourceResolverFactory.SUBSERVICE, SERVICE_USER);

        try (ResourceResolver resolver = resourceResolverFactory.getServiceResourceResolver(authInfo)) {
            Resource resource = resolver.getResource(fragmentPath);
            if (resource != null) {
                ContentFragment cf = resource.adaptTo(ContentFragment.class);
                if (cf != null) {
                    Map<String, Object> data = new HashMap<>();
                    data.put("title", cf.getTitle());
                    data.put("name", cf.getName());
                    data.put("description", cf.getDescription());
                    data.put("path", fragmentPath);

                    Map<String, Object> elements = new HashMap<>();
                    Iterator<ContentElement> elementIterator = cf.getElements();
                    while (elementIterator.hasNext()) {
                        ContentElement element = elementIterator.next();
                        FragmentData fragmentData = element.getValue();
                        if (fragmentData != null) {
                            elements.put(element.getName(), fragmentData.getValue(String.class));
                        }
                    }
                    data.put("elements", elements);
                    return data;
                }
            }
        } catch (LoginException e) {
            LOG.error("Failed to obtain service resource resolver", e);
        }

        return Collections.emptyMap();
    }
}
