package com.infosys.test.model;

import java.util.ArrayList;
import java.util.List;

/**
 * Represents the result of a single test case.
 */
public class TestCaseResult {

    private String testCaseId;
    private String testCaseName;
    private String category;
    private String description;
    private String status; // PASS, FAIL, SKIP
    private long totalDurationMs;
    private String errorMessage;
    private List<TestStep> steps;

    public TestCaseResult() {
        this.steps = new ArrayList<>();
    }

    public TestCaseResult(String testCaseId, String testCaseName, String category, String description) {
        this.testCaseId = testCaseId;
        this.testCaseName = testCaseName;
        this.category = category;
        this.description = description;
        this.status = "PASS";
        this.steps = new ArrayList<>();
    }

    public void addStep(TestStep step) {
        this.steps.add(step);
        if ("FAIL".equals(step.getStatus())) {
            this.status = "FAIL";
        }
    }

    public String getTestCaseId() { return testCaseId; }
    public void setTestCaseId(String testCaseId) { this.testCaseId = testCaseId; }

    public String getTestCaseName() { return testCaseName; }
    public void setTestCaseName(String testCaseName) { this.testCaseName = testCaseName; }

    public String getCategory() { return category; }
    public void setCategory(String category) { this.category = category; }

    public String getDescription() { return description; }
    public void setDescription(String description) { this.description = description; }

    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }

    public long getTotalDurationMs() { return totalDurationMs; }
    public void setTotalDurationMs(long totalDurationMs) { this.totalDurationMs = totalDurationMs; }

    public String getErrorMessage() { return errorMessage; }
    public void setErrorMessage(String errorMessage) { this.errorMessage = errorMessage; }

    public List<TestStep> getSteps() { return steps; }
    public void setSteps(List<TestStep> steps) { this.steps = steps; }
}
