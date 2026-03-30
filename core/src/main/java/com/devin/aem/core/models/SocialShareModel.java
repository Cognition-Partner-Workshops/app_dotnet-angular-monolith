package com.devin.aem.core.models;

import org.apache.sling.api.SlingHttpServletRequest;
import org.apache.sling.api.resource.Resource;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import javax.annotation.PostConstruct;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

@Model(adaptables = Resource.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class SocialShareModel {

    @ValueMapValue
    private boolean enableFacebook;

    @ValueMapValue
    private boolean enableTwitter;

    @ValueMapValue
    private boolean enableLinkedIn;

    @ValueMapValue
    private boolean enableEmail;

    @ValueMapValue
    private String shareUrl;

    @ValueMapValue
    private String shareTitle;

    private List<SocialPlatform> platforms;

    @PostConstruct
    protected void init() {
        platforms = new ArrayList<>();
        if (enableFacebook) {
            platforms.add(new SocialPlatform("facebook", "Facebook",
                "https://www.facebook.com/sharer/sharer.php?u=" + encodeUrl(shareUrl), "fab fa-facebook-f"));
        }
        if (enableTwitter) {
            platforms.add(new SocialPlatform("twitter", "Twitter",
                "https://twitter.com/intent/tweet?url=" + encodeUrl(shareUrl) + "&text=" + encodeUrl(shareTitle), "fab fa-twitter"));
        }
        if (enableLinkedIn) {
            platforms.add(new SocialPlatform("linkedin", "LinkedIn",
                "https://www.linkedin.com/sharing/share-offsite/?url=" + encodeUrl(shareUrl), "fab fa-linkedin-in"));
        }
        if (enableEmail) {
            platforms.add(new SocialPlatform("email", "Email",
                "mailto:?subject=" + encodeUrl(shareTitle) + "&body=" + encodeUrl(shareUrl), "fas fa-envelope"));
        }
    }

    private String encodeUrl(String url) {
        if (url == null) return "";
        try {
            return java.net.URLEncoder.encode(url, "UTF-8");
        } catch (Exception e) {
            return url;
        }
    }

    public List<SocialPlatform> getPlatforms() { return Collections.unmodifiableList(platforms); }
    public String getShareUrl() { return shareUrl; }
    public String getShareTitle() { return shareTitle; }

    public static class SocialPlatform {
        private final String id;
        private final String name;
        private final String url;
        private final String icon;

        public SocialPlatform(String id, String name, String url, String icon) {
            this.id = id;
            this.name = name;
            this.url = url;
            this.icon = icon;
        }

        public String getId() { return id; }
        public String getName() { return name; }
        public String getUrl() { return url; }
        public String getIcon() { return icon; }
    }
}
