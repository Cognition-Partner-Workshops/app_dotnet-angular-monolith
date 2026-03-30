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
public class FormContainerModel {

    @ValueMapValue
    private String formTitle;

    @ValueMapValue
    private String actionUrl;

    @ValueMapValue
    private String method;

    @ValueMapValue
    private String redirectUrl;

    @ValueMapValue
    private String submitLabel;

    @ValueMapValue
    private boolean enableValidation;

    @ChildResource
    private List<Resource> fields;

    private List<FormField> formFields;

    @PostConstruct
    protected void init() {
        if (method == null || method.isEmpty()) {
            method = "POST";
        }
        if (submitLabel == null || submitLabel.isEmpty()) {
            submitLabel = "Submit";
        }
        formFields = new ArrayList<>();
        if (fields != null) {
            for (Resource field : fields) {
                FormField ff = new FormField();
                ff.setName(field.getValueMap().get("fieldName", String.class));
                ff.setLabel(field.getValueMap().get("fieldLabel", String.class));
                ff.setType(field.getValueMap().get("fieldType", "text"));
                ff.setPlaceholder(field.getValueMap().get("fieldPlaceholder", String.class));
                ff.setRequired(field.getValueMap().get("fieldRequired", false));
                ff.setPattern(field.getValueMap().get("fieldPattern", String.class));
                ff.setErrorMessage(field.getValueMap().get("fieldErrorMessage", String.class));
                formFields.add(ff);
            }
        }
    }

    public String getFormTitle() { return formTitle; }
    public String getActionUrl() { return actionUrl; }
    public String getMethod() { return method; }
    public String getRedirectUrl() { return redirectUrl; }
    public String getSubmitLabel() { return submitLabel; }
    public boolean isEnableValidation() { return enableValidation; }
    public List<FormField> getFormFields() { return Collections.unmodifiableList(formFields); }

    public static class FormField {
        private String name;
        private String label;
        private String type;
        private String placeholder;
        private boolean required;
        private String pattern;
        private String errorMessage;

        public String getName() { return name; }
        public void setName(String name) { this.name = name; }
        public String getLabel() { return label; }
        public void setLabel(String label) { this.label = label; }
        public String getType() { return type; }
        public void setType(String type) { this.type = type; }
        public String getPlaceholder() { return placeholder; }
        public void setPlaceholder(String placeholder) { this.placeholder = placeholder; }
        public boolean isRequired() { return required; }
        public void setRequired(boolean required) { this.required = required; }
        public String getPattern() { return pattern; }
        public void setPattern(String pattern) { this.pattern = pattern; }
        public String getErrorMessage() { return errorMessage; }
        public void setErrorMessage(String errorMessage) { this.errorMessage = errorMessage; }
    }
}
