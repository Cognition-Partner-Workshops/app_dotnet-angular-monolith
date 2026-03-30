package com.devin.aem.core.models;

import org.apache.sling.api.resource.Resource;
import org.apache.sling.models.annotations.DefaultInjectionStrategy;
import org.apache.sling.models.annotations.Model;
import org.apache.sling.models.annotations.injectorspecific.ChildResource;
import org.apache.sling.models.annotations.injectorspecific.ValueMapValue;

import javax.annotation.PostConstruct;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

@Model(adaptables = Resource.class,
       defaultInjectionStrategy = DefaultInjectionStrategy.OPTIONAL)
public class CardListModel {

    @ValueMapValue
    private String title;

    @ValueMapValue
    private String layout;

    @ValueMapValue
    private int maxItems;

    @ChildResource
    private List<Resource> cards;

    private List<CardItem> cardItems;

    @PostConstruct
    protected void init() {
        if (layout == null || layout.isEmpty()) {
            layout = "grid";
        }
        if (maxItems <= 0) {
            maxItems = 12;
        }
        cardItems = new ArrayList<>();
        if (cards != null) {
            int count = 0;
            for (Resource card : cards) {
                if (count >= maxItems) break;
                CardItem item = new CardItem();
                item.setTitle(card.getValueMap().get("cardTitle", String.class));
                item.setDescription(card.getValueMap().get("cardDescription", String.class));
                item.setImage(card.getValueMap().get("cardImage", String.class));
                item.setLink(card.getValueMap().get("cardLink", String.class));
                cardItems.add(item);
                count++;
            }
        }
    }

    public String getTitle() {
        return title;
    }

    public String getLayout() {
        return layout;
    }

    public int getMaxItems() {
        return maxItems;
    }

    public List<CardItem> getCardItems() {
        return Collections.unmodifiableList(cardItems);
    }

    public static class CardItem {
        private String title;
        private String description;
        private String image;
        private String link;

        public String getTitle() { return title; }
        public void setTitle(String title) { this.title = title; }
        public String getDescription() { return description; }
        public void setDescription(String description) { this.description = description; }
        public String getImage() { return image; }
        public void setImage(String image) { this.image = image; }
        public String getLink() { return link; }
        public void setLink(String link) { this.link = link; }
    }
}
