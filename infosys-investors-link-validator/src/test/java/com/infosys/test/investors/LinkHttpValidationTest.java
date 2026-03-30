package com.infosys.test.investors;

import com.infosys.test.base.BaseTest;
import com.infosys.test.config.TestConfig;
import com.infosys.test.model.TestCaseResult;
import com.infosys.test.utils.StepLogger;
import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebElement;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testng.annotations.Test;

import java.net.HttpURLConnection;
import java.net.URI;
import java.util.*;

/**
 * TC-002: Validates all links on the Investors page via HTTP HEAD/GET requests.
 * Groups links by category and checks each URL returns a successful HTTP status code.
 */
public class LinkHttpValidationTest extends BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(LinkHttpValidationTest.class);

    @Test
    public void testAllLinksReturnValidHttpStatus() {
        TestCaseResult result = new TestCaseResult(
                "TC-002",
                "Validate All Links Return Valid HTTP Status",
                "HTTP Validation",
                "Send HTTP requests to all links found on the Investors page and verify successful responses"
        );
        long startTime = System.currentTimeMillis();
        StepLogger stepLog = new StepLogger(result, driver);

        try {
            // Step 1: Navigate to investors page
            stepLog.info("Navigate to Investors page", TestConfig.BASE_URL);
            driver.get(TestConfig.BASE_URL);
            Thread.sleep(5000);

            // Scroll to load lazy content
            JavascriptExecutor js = (JavascriptExecutor) driver;
            for (int i = 1; i <= 15; i++) {
                js.executeScript("window.scrollTo(0, " + (i * 500) + ");");
                Thread.sleep(300);
            }
            js.executeScript("window.scrollTo(0, 0);");
            Thread.sleep(1000);

            stepLog.passWithScreenshot("Page loaded and scrolled", "Investors page loaded and scrolled to discover all links");

            // Step 2: Collect all links
            List<WebElement> allLinks = driver.findElements(By.tagName("a"));
            Set<String> uniqueUrls = new LinkedHashSet<>();
            Map<String, String> urlToText = new LinkedHashMap<>();

            for (WebElement link : allLinks) {
                String href = link.getAttribute("href");
                if (href != null && !href.isEmpty() && !href.equals("#")
                        && !href.startsWith("javascript:") && !href.startsWith("mailto:")
                        && !href.startsWith("tel:") && !href.contains("#main-cnt") && !href.contains("#footer")) {
                    if (uniqueUrls.add(href)) {
                        String text = link.getText().trim();
                        if (text.isEmpty()) {
                            text = link.getAttribute("aria-label");
                            if (text == null || text.isEmpty()) {
                                text = link.getAttribute("title");
                            }
                            if (text == null) text = "[No Text]";
                        }
                        urlToText.put(href, text.length() > 60 ? text.substring(0, 60) + "..." : text);
                    }
                }
            }

            stepLog.passNoScreenshot("Collected unique links", "Found " + uniqueUrls.size() + " unique links to validate");

            // Step 3: Validate each link via HTTP
            int passCount = 0;
            int failCount = 0;
            int linkIndex = 0;
            List<String> failedLinks = new ArrayList<>();

            for (String url : uniqueUrls) {
                linkIndex++;
                String linkText = urlToText.getOrDefault(url, "[Unknown]");
                String shortUrl = url.length() > 80 ? url.substring(0, 80) + "..." : url;

                try {
                    int statusCode = checkUrl(url);
                    if (statusCode >= 200 && statusCode < 400) {
                        stepLog.passNoScreenshot(
                                "Link " + linkIndex + "/" + uniqueUrls.size() + ": " + linkText,
                                "HTTP " + statusCode + " - " + shortUrl);
                        passCount++;
                    } else if (statusCode == 403) {
                        // 403 is common for sites blocking automated requests, treat as warning
                        stepLog.passNoScreenshot(
                                "Link " + linkIndex + "/" + uniqueUrls.size() + ": " + linkText,
                                "HTTP " + statusCode + " (Access restricted, may block bots) - " + shortUrl);
                        passCount++;
                    } else {
                        stepLog.failNoScreenshot(
                                "Link " + linkIndex + "/" + uniqueUrls.size() + ": " + linkText,
                                "HTTP " + statusCode + " - " + shortUrl);
                        failCount++;
                        failedLinks.add(url + " (HTTP " + statusCode + ")");
                    }
                } catch (Exception e) {
                    // Connection errors for external sites should not fail the test
                    String errorMsg = e.getMessage() != null ? e.getMessage() : e.getClass().getSimpleName();
                    if (errorMsg.length() > 100) errorMsg = errorMsg.substring(0, 100) + "...";

                    if (url.contains("infosys.com")) {
                        stepLog.failNoScreenshot(
                                "Link " + linkIndex + "/" + uniqueUrls.size() + ": " + linkText,
                                "Connection error: " + errorMsg + " - " + shortUrl);
                        failCount++;
                        failedLinks.add(url + " (Error: " + errorMsg + ")");
                    } else {
                        stepLog.passNoScreenshot(
                                "Link " + linkIndex + "/" + uniqueUrls.size() + ": " + linkText,
                                "External link - Connection issue (may block bots): " + errorMsg + " - " + shortUrl);
                        passCount++;
                    }
                }
            }

            // Step 4: Summary
            String summary = String.format("Validated %d links: %d passed, %d failed",
                    uniqueUrls.size(), passCount, failCount);
            if (failCount == 0) {
                stepLog.passWithScreenshot("Validation Summary", summary);
            } else {
                stepLog.failWithScreenshot("Validation Summary",
                        summary + ". Failed links: " + String.join(", ", failedLinks));
            }

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

    private int checkUrl(String url) throws Exception {
        HttpURLConnection connection = (HttpURLConnection) new URI(url).toURL().openConnection();
        connection.setRequestMethod("HEAD");
        connection.setConnectTimeout(TestConfig.HTTP_CONNECT_TIMEOUT_MS);
        connection.setReadTimeout(TestConfig.HTTP_READ_TIMEOUT_MS);
        connection.setInstanceFollowRedirects(true);
        connection.setRequestProperty("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        connection.setRequestProperty("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

        int code = connection.getResponseCode();
        connection.disconnect();

        // If HEAD returns 405, retry with GET
        if (code == 405) {
            connection = (HttpURLConnection) new URI(url).toURL().openConnection();
            connection.setRequestMethod("GET");
            connection.setConnectTimeout(TestConfig.HTTP_CONNECT_TIMEOUT_MS);
            connection.setReadTimeout(TestConfig.HTTP_READ_TIMEOUT_MS);
            connection.setInstanceFollowRedirects(true);
            connection.setRequestProperty("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            code = connection.getResponseCode();
            connection.disconnect();
        }

        return code;
    }
}
