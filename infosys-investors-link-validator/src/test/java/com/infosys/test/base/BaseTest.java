package com.infosys.test.base;

import com.infosys.test.config.TestConfig;
import com.infosys.test.driver.DriverManager;
import com.infosys.test.model.TestCaseResult;
import com.infosys.test.report.HtmlReportGenerator;
import org.openqa.selenium.WebDriver;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testng.annotations.AfterSuite;
import org.testng.annotations.BeforeMethod;
import org.testng.annotations.BeforeSuite;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Base test class that provides WebDriver lifecycle management
 * and collects test results for reporting.
 */
public abstract class BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(BaseTest.class);

    protected static final List<TestCaseResult> allResults =
            Collections.synchronizedList(new ArrayList<>());

    protected WebDriver driver;

    @BeforeSuite
    public void suiteSetup() {
        logger.info("=== Test Suite Started ===");
        logger.info("Target URL: {}", TestConfig.BASE_URL);
        logger.info("Browser: {}", TestConfig.getBrowser());
        logger.info("Headless: {}", TestConfig.isHeadless());
        logger.info("Output Directory: {}", TestConfig.OUTPUT_DIR);
    }

    @BeforeMethod
    public void setUp() {
        driver = DriverManager.createDriver();
    }

    @org.testng.annotations.AfterMethod
    public void tearDown() {
        DriverManager.quitDriver();
    }

    @AfterSuite
    public void suiteTearDown() {
        logger.info("=== Generating Final Report ===");
        HtmlReportGenerator.generate(allResults);
        logger.info("=== Test Suite Completed: {} test cases ===", allResults.size());
        logger.info("Report location: {}", TestConfig.REPORT_FILE);
    }

    protected void addResult(TestCaseResult result) {
        allResults.add(result);
    }
}
