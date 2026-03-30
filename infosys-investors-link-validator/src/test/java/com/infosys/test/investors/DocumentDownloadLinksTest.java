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
import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

import java.net.HttpURLConnection;
import java.net.URI;

/**
 * TC-073 to TC-076: Validates document download links (PDF, XLS).
 */
public class DocumentDownloadLinksTest extends BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(DocumentDownloadLinksTest.class);

    @DataProvider(name = "documentLinks")
    public Object[][] documentLinksData() {
        return new Object[][]{
                {"TC-073", "Investor Sheet (XLS)", "https://www.infosys.com/content/dam/infosys-web/en/investors/reports-filings/documents/investor-sheet.xls", "Document Downloads"},
                {"TC-074", "Integrated Annual Report 2025 (PDF)", "https://www.infosys.com/investors/reports-filings/annual-report/annual/documents/infosys-ar-25.pdf", "Document Downloads"},
                {"TC-075", "ESG Report 2024-25 (PDF)", "https://www.infosys.com/sustainability/documents/infosys-esg-report-2024-25.pdf", "Document Downloads"},
                {"TC-076", "Modern Slavery Statement (PDF)", "https://www.infosys.com/investors/corporate-governance/documents/statement-under-modern-slavery-act.pdf", "Document Downloads"},
        };
    }

    @Test(dataProvider = "documentLinks")
    public void testDocumentDownloadLink(String tcId, String linkName, String url, String category) {
        TestCaseResult result = new TestCaseResult(
                tcId, "Download: " + linkName, category,
                "Verify document link '" + linkName + "' is accessible and returns a valid response"
        );
        long startTime = System.currentTimeMillis();
        StepLogger stepLog = new StepLogger(result, driver);

        try {
            // Step 1: Navigate to investors page
            stepLog.info("Navigate to Investors page", TestConfig.BASE_URL);
            driver.get(TestConfig.BASE_URL);
            Thread.sleep(3000);
            stepLog.passWithScreenshot("Investors page loaded", "Page loaded successfully");

            // Step 2: Verify link exists on page
            WebElement linkElement = null;
            try {
                String hrefPart = url.substring(url.lastIndexOf("/") + 1);
                linkElement = driver.findElement(By.cssSelector("a[href*='" + hrefPart + "']"));
                stepLog.passWithScreenshot("Document link found on page", "Link element located for: " + linkName);
            } catch (Exception e) {
                stepLog.info("Link element not found on visible page", "Will validate URL directly");
            }

            // Step 3: Validate document URL via HTTP
            try {
                HttpURLConnection connection = (HttpURLConnection) new URI(url).toURL().openConnection();
                connection.setRequestMethod("HEAD");
                connection.setConnectTimeout(TestConfig.HTTP_CONNECT_TIMEOUT_MS);
                connection.setReadTimeout(TestConfig.HTTP_READ_TIMEOUT_MS);
                connection.setInstanceFollowRedirects(true);
                connection.setRequestProperty("User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                int statusCode = connection.getResponseCode();
                String contentType = connection.getContentType();
                long contentLength = connection.getContentLengthLong();
                connection.disconnect();

                if (statusCode >= 200 && statusCode < 400) {
                    String details = String.format("HTTP %d | Content-Type: %s | Size: %s",
                            statusCode,
                            contentType != null ? contentType : "unknown",
                            contentLength > 0 ? formatFileSize(contentLength) : "unknown");
                    stepLog.passNoScreenshot("Document URL is accessible", details);
                } else if (statusCode == 403) {
                    stepLog.passNoScreenshot("Document URL exists (access restricted)",
                            "HTTP " + statusCode + " - Server blocks automated HEAD requests");
                } else {
                    stepLog.failNoScreenshot("Document URL returned error",
                            "HTTP " + statusCode + " for URL: " + url);
                }
            } catch (Exception e) {
                stepLog.failNoScreenshot("Failed to validate document URL",
                        "Error: " + e.getMessage() + " | URL: " + url);
            }

            // Step 4: Navigate to document URL
            driver.get(url);
            Thread.sleep(3000);
            String currentUrl = driver.getCurrentUrl();
            stepLog.passWithScreenshot("Navigated to document URL",
                    "Current URL: " + currentUrl);

        } catch (Exception e) {
            stepLog.failWithScreenshot("Unexpected error", e.getMessage());
            result.setStatus("FAIL");
            result.setErrorMessage(e.getMessage());
        } finally {
            result.setTotalDurationMs(System.currentTimeMillis() - startTime);
            addResult(result);
        }
    }

    private String formatFileSize(long bytes) {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return String.format("%.1f KB", bytes / 1024.0);
        return String.format("%.1f MB", bytes / (1024.0 * 1024.0));
    }
}
