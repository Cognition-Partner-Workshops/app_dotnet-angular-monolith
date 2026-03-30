package com.infosys.test.investors;

import com.infosys.test.base.BaseTest;
import com.infosys.test.config.TestConfig;
import com.infosys.test.model.TestCaseResult;
import com.infosys.test.utils.StepLogger;
import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.WebDriverWait;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

import java.time.Duration;

/**
 * TC-043 to TC-050: Validates header/navigation bar links.
 */
public class HeaderNavigationLinksTest extends BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(HeaderNavigationLinksTest.class);

    @DataProvider(name = "headerLinks")
    public Object[][] headerLinksData() {
        return new Object[][]{
                {"TC-043", "Infosys Home Logo", "/", "Header Navigation"},
                {"TC-044", "Navigate your next", "/navigate-your-next.html", "Header Navigation"},
                {"TC-045", "Infosys Knowledge Institute", "/iki.html", "Header Navigation"},
                {"TC-046", "Investors", "/investors.html", "Header Navigation"},
                {"TC-047", "Careers", "/careers/", "Header Navigation"},
        };
    }

    @Test(dataProvider = "headerLinks")
    public void testHeaderLinkNavigation(String tcId, String linkName, String hrefContains, String category) {
        TestCaseResult result = new TestCaseResult(
                tcId, "Header: " + linkName, category,
                "Verify header link '" + linkName + "' navigates correctly"
        );
        long startTime = System.currentTimeMillis();
        StepLogger stepLog = new StepLogger(result, driver);

        try {
            stepLog.info("Navigate to Investors page", TestConfig.BASE_URL);
            driver.get(TestConfig.BASE_URL);
            Thread.sleep(3000);
            stepLog.passWithScreenshot("Investors page loaded", "Page loaded successfully");

            // Find link
            WebElement linkElement = null;
            try {
                if ("/".equals(hrefContains)) {
                    linkElement = driver.findElement(By.cssSelector("a[href='https://www.infosys.com/']"));
                } else {
                    linkElement = driver.findElement(By.cssSelector("a[href*='" + hrefContains + "']"));
                }
            } catch (Exception e) {
                try {
                    linkElement = driver.findElement(By.partialLinkText(linkName));
                } catch (Exception e2) {
                    logger.warn("Could not find header link: {}", linkName);
                }
            }

            if (linkElement != null) {
                ((JavascriptExecutor) driver).executeScript(
                        "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", linkElement);
                Thread.sleep(500);
                stepLog.passWithScreenshot("Link found: " + linkName, "Element located in header");

                try {
                    linkElement.click();
                } catch (Exception clickEx) {
                    ((JavascriptExecutor) driver).executeScript("arguments[0].click();", linkElement);
                }
                Thread.sleep(3000);

                String currentUrl = driver.getCurrentUrl();
                String pageTitle = driver.getTitle();

                if (pageTitle != null && !pageTitle.isEmpty() && !pageTitle.toLowerCase().contains("404")) {
                    stepLog.passWithScreenshot("Navigation successful",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                } else {
                    stepLog.failWithScreenshot("Navigation failed",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                }
            } else {
                stepLog.failWithScreenshot("Link not found", "Could not locate '" + linkName + "' in header");
            }

        } catch (Exception e) {
            stepLog.failWithScreenshot("Unexpected error", e.getMessage());
            result.setStatus("FAIL");
            result.setErrorMessage(e.getMessage());
        } finally {
            result.setTotalDurationMs(System.currentTimeMillis() - startTime);
            addResult(result);
        }
    }
}
