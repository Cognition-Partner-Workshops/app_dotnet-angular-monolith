package com.infosys.test.investors;

import com.infosys.test.base.BaseTest;
import com.infosys.test.config.TestConfig;
import com.infosys.test.model.TestCaseResult;
import com.infosys.test.utils.StepLogger;
import org.openqa.selenium.By;
import org.openqa.selenium.WebElement;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testng.annotations.Test;

import java.util.List;

/**
 * TC-001: Verifies that the Investors page loads correctly and discovers all links.
 */
public class InvestorPageLinkExistenceTest extends BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(InvestorPageLinkExistenceTest.class);

    @Test
    public void testInvestorsPageLoadsAndLinksExist() {
        TestCaseResult result = new TestCaseResult(
                "TC-001",
                "Verify Investors Page Loads and Links Exist",
                "Page Load",
                "Validate that https://www.infosys.com/investors.html loads successfully and contains links"
        );
        long startTime = System.currentTimeMillis();
        StepLogger stepLog = new StepLogger(result, driver);

        try {
            // Step 1: Navigate to investors page
            stepLog.info("Navigate to Investors page", TestConfig.BASE_URL);
            driver.get(TestConfig.BASE_URL);
            Thread.sleep(3000);

            // Step 2: Verify page title
            String title = driver.getTitle();
            if (title != null && !title.isEmpty()) {
                stepLog.pass("Verify page title is present", "Title: " + title, "page_title_verify");
            } else {
                stepLog.fail("Verify page title is present", "Page title is empty or null", "page_title_fail");
            }

            // Step 3: Verify URL
            String currentUrl = driver.getCurrentUrl();
            if (currentUrl.contains("investors")) {
                stepLog.passWithScreenshot("Verify URL contains 'investors'", "Current URL: " + currentUrl);
            } else {
                stepLog.failWithScreenshot("Verify URL contains 'investors'", "URL does not contain 'investors': " + currentUrl);
            }

            // Step 4: Count all links
            List<WebElement> allLinks = driver.findElements(By.tagName("a"));
            int totalLinks = allLinks.size();
            stepLog.passWithScreenshot("Count all links on the page",
                    "Total <a> elements found: " + totalLinks);

            // Step 5: Count links with valid href
            long validHrefCount = allLinks.stream()
                    .filter(link -> {
                        String href = link.getAttribute("href");
                        return href != null && !href.isEmpty() && !href.equals("#")
                                && !href.startsWith("javascript:") && !href.startsWith("mailto:")
                                && !href.startsWith("tel:");
                    })
                    .count();
            stepLog.passNoScreenshot("Count links with valid href",
                    "Links with valid href: " + validHrefCount + " out of " + totalLinks);

            // Step 6: Verify page has substantial content
            String bodyText = driver.findElement(By.tagName("body")).getText();
            if (bodyText.length() > 100) {
                stepLog.passNoScreenshot("Verify page has content",
                        "Page body text length: " + bodyText.length() + " characters");
            } else {
                stepLog.failWithScreenshot("Verify page has content",
                        "Page appears to have insufficient content: " + bodyText.length() + " characters");
            }

            result.setStatus(result.getSteps().stream().anyMatch(s -> "FAIL".equals(s.getStatus())) ? "FAIL" : "PASS");
        } catch (Exception e) {
            stepLog.failWithScreenshot("Unexpected error", e.getMessage());
            result.setStatus("FAIL");
            result.setErrorMessage(e.getMessage());
            logger.error("Test failed with exception", e);
        } finally {
            result.setTotalDurationMs(System.currentTimeMillis() - startTime);
            addResult(result);
        }
    }
}
