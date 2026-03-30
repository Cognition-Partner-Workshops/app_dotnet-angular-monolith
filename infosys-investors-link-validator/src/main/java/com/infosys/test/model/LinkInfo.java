package com.infosys.test.model;

/**
 * Represents a link found on the page.
 */
public class LinkInfo {

    private String text;
    private String href;
    private String category;

    public LinkInfo() {}

    public LinkInfo(String text, String href, String category) {
        this.text = text;
        this.href = href;
        this.category = category;
    }

    public String getText() {
        return text;
    }

    public void setText(String text) {
        this.text = text;
    }

    public String getHref() {
        return href;
    }

    public void setHref(String href) {
        this.href = href;
    }

    public String getCategory() {
        return category;
    }

    public void setCategory(String category) {
        this.category = category;
    }

    @Override
    public String toString() {
        return String.format("[%s] %s -> %s", category, text, href);
    }
}
