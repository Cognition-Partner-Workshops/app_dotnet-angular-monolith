package com.infosys.test.model;

/**
 * Represents a single step within a test case.
 */
public class TestStep {

    private int stepNumber;
    private String description;
    private String status; // PASS, FAIL, INFO
    private String details;
    private String screenshotPath; // relative path to screenshot
    private long durationMs;

    public TestStep() {}

    public TestStep(int stepNumber, String description, String status, String details, String screenshotPath, long durationMs) {
        this.stepNumber = stepNumber;
        this.description = description;
        this.status = status;
        this.details = details;
        this.screenshotPath = screenshotPath;
        this.durationMs = durationMs;
    }

    public int getStepNumber() { return stepNumber; }
    public void setStepNumber(int stepNumber) { this.stepNumber = stepNumber; }

    public String getDescription() { return description; }
    public void setDescription(String description) { this.description = description; }

    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }

    public String getDetails() { return details; }
    public void setDetails(String details) { this.details = details; }

    public String getScreenshotPath() { return screenshotPath; }
    public void setScreenshotPath(String screenshotPath) { this.screenshotPath = screenshotPath; }

    public long getDurationMs() { return durationMs; }
    public void setDurationMs(long durationMs) { this.durationMs = durationMs; }
}
