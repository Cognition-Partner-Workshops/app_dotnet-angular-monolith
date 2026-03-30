package com.infosys.test.utils;

import com.infosys.test.model.TestCaseResult;
import com.infosys.test.model.TestStep;
import org.openqa.selenium.WebDriver;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Logs test steps with screenshots and builds the TestCaseResult.
 */
public class StepLogger {

    private static final Logger logger = LoggerFactory.getLogger(StepLogger.class);

    private final TestCaseResult testCaseResult;
    private final WebDriver driver;
    private int stepCounter = 0;

    public StepLogger(TestCaseResult testCaseResult, WebDriver driver) {
        this.testCaseResult = testCaseResult;
        this.driver = driver;
    }

    /**
     * Log an informational step (no screenshot).
     */
    public void info(String description, String details) {
        stepCounter++;
        TestStep step = new TestStep(stepCounter, description, "INFO", details, null, 0);
        testCaseResult.addStep(step);
        logger.info("[STEP {}] {} - {}", stepCounter, description, details);
    }

    /**
     * Log a passing step with a screenshot.
     */
    public void pass(String description, String details, String screenshotName) {
        stepCounter++;
        long start = System.currentTimeMillis();
        String screenshotPath = ScreenshotUtil.capture(driver, screenshotName);
        long duration = System.currentTimeMillis() - start;

        TestStep step = new TestStep(stepCounter, description, "PASS", details, screenshotPath, duration);
        testCaseResult.addStep(step);
        logger.info("[STEP {} PASS] {} - {}", stepCounter, description, details);
    }

    /**
     * Log a passing step with a screenshot (providing explicit screenshot path).
     */
    public void passWithScreenshot(String description, String details) {
        stepCounter++;
        String screenshotPath = ScreenshotUtil.capture(driver,
                "step_" + stepCounter + "_" + description.replaceAll("\\s+", "_"));

        TestStep step = new TestStep(stepCounter, description, "PASS", details, screenshotPath, 0);
        testCaseResult.addStep(step);
        logger.info("[STEP {} PASS] {} - {}", stepCounter, description, details);
    }

    /**
     * Log a failing step with a screenshot.
     */
    public void fail(String description, String details, String screenshotName) {
        stepCounter++;
        String screenshotPath = ScreenshotUtil.capture(driver, screenshotName);

        TestStep step = new TestStep(stepCounter, description, "FAIL", details, screenshotPath, 0);
        testCaseResult.addStep(step);
        testCaseResult.setStatus("FAIL");
        logger.error("[STEP {} FAIL] {} - {}", stepCounter, description, details);
    }

    /**
     * Log a failing step with a screenshot (auto-named).
     */
    public void failWithScreenshot(String description, String details) {
        stepCounter++;
        String screenshotPath = ScreenshotUtil.capture(driver,
                "step_" + stepCounter + "_FAIL_" + description.replaceAll("\\s+", "_"));

        TestStep step = new TestStep(stepCounter, description, "FAIL", details, screenshotPath, 0);
        testCaseResult.addStep(step);
        testCaseResult.setStatus("FAIL");
        logger.error("[STEP {} FAIL] {} - {}", stepCounter, description, details);
    }

    /**
     * Log a step without a screenshot.
     */
    public void passNoScreenshot(String description, String details) {
        stepCounter++;
        TestStep step = new TestStep(stepCounter, description, "PASS", details, null, 0);
        testCaseResult.addStep(step);
        logger.info("[STEP {} PASS] {} - {}", stepCounter, description, details);
    }

    /**
     * Log a failing step without a screenshot.
     */
    public void failNoScreenshot(String description, String details) {
        stepCounter++;
        TestStep step = new TestStep(stepCounter, description, "FAIL", details, null, 0);
        testCaseResult.addStep(step);
        testCaseResult.setStatus("FAIL");
        logger.error("[STEP {} FAIL] {} - {}", stepCounter, description, details);
    }

    public TestCaseResult getResult() {
        return testCaseResult;
    }
}
