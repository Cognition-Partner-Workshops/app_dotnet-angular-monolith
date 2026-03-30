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

/**
 * TC-048 to TC-075+: Validates footer section links.
 */
public class FooterLinksNavigationTest extends BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(FooterLinksNavigationTest.class);

    @DataProvider(name = "footerLinks")
    public Object[][] footerLinksData() {
        return new Object[][]{
                // Company section
                {"TC-048", "About Us - Overview", "/about.html", "Footer - Company"},
                {"TC-049", "History", "/about/history.html", "Footer - Company"},
                {"TC-050", "ESG", "/about/esg.html", "Footer - Company"},
                {"TC-051", "Management Profiles", "/about/management-profiles.html", "Footer - Company"},
                {"TC-052", "Newsroom", "/newsroom.html", "Footer - Company"},
                {"TC-053", "Contact Us", "/contact.html", "Footer - Company"},
                {"TC-054", "Careers", "/careers/", "Footer - Company"},

                // Subsidiaries
                {"TC-055", "EdgeVerve Systems", "edgeverve.com", "Footer - Subsidiaries"},
                {"TC-056", "Infosys BPM", "infosysbpm.com", "Footer - Subsidiaries"},
                {"TC-057", "Infosys Public Services", "infosyspublicservices.com", "Footer - Subsidiaries"},

                // Foundation links
                {"TC-058", "Infosys Foundation", "infosys.org/infosys-foundation.html", "Footer - Foundation"},
                {"TC-059", "Infosys Foundation USA", "infosys.org/infosys-foundation-usa.html", "Footer - Foundation"},
                {"TC-060", "Infosys Science Foundation", "infosysprize.org", "Footer - Foundation"},

                // Legal links
                {"TC-061", "Terms of Use", "/terms-of-use.html", "Footer - Legal"},
                {"TC-062", "Privacy Statement", "/privacy-statement.html", "Footer - Legal"},
                {"TC-063", "Cookie Policy", "/privacy-statement/cookie-policy.html", "Footer - Legal"},
                {"TC-064", "Safe Harbour Provision", "/safe-harbor-provision.html", "Footer - Legal"},
                {"TC-065", "Site Map", "/sitemap.html", "Footer - Legal"},
                {"TC-066", "Payment Guide for Suppliers", "/payment-information-suppliers.html", "Footer - Legal"},
        };
    }

    @Test(dataProvider = "footerLinks")
    public void testFooterLinkNavigation(String tcId, String linkName, String hrefContains, String category) {
        TestCaseResult result = new TestCaseResult(
                tcId, "Footer: " + linkName, category,
                "Verify footer link '" + linkName + "' navigates to the correct page"
        );
        long startTime = System.currentTimeMillis();
        StepLogger stepLog = new StepLogger(result, driver);

        try {
            stepLog.info("Navigate to Investors page", TestConfig.BASE_URL);
            driver.get(TestConfig.BASE_URL);
            Thread.sleep(3000);

            // Scroll to footer
            ((JavascriptExecutor) driver).executeScript("window.scrollTo(0, document.body.scrollHeight);");
            Thread.sleep(2000);
            stepLog.passWithScreenshot("Scrolled to footer", "Footer section visible");

            // Find link
            WebElement linkElement = null;
            try {
                linkElement = driver.findElement(By.cssSelector("a[href*='" + hrefContains + "']"));
            } catch (Exception e) {
                try {
                    linkElement = driver.findElement(By.partialLinkText(linkName));
                } catch (Exception e2) {
                    logger.warn("Could not find footer link: {}", linkName);
                }
            }

            if (linkElement != null) {
                ((JavascriptExecutor) driver).executeScript(
                        "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", linkElement);
                Thread.sleep(500);
                stepLog.passWithScreenshot("Link found: " + linkName, "Element located in footer");

                // Get href for external link check
                String href = linkElement.getAttribute("href");
                boolean isExternal = href != null && !href.contains("infosys.com/");

                try {
                    linkElement.click();
                } catch (Exception clickEx) {
                    ((JavascriptExecutor) driver).executeScript("arguments[0].click();", linkElement);
                }
                Thread.sleep(3000);

                // Handle new tabs for external links
                if (driver.getWindowHandles().size() > 1) {
                    String originalWindow = driver.getWindowHandles().iterator().next();
                    for (String handle : driver.getWindowHandles()) {
                        if (!handle.equals(originalWindow)) {
                            driver.switchTo().window(handle);
                            break;
                        }
                    }
                }

                String currentUrl = driver.getCurrentUrl();
                String pageTitle = driver.getTitle();

                if (pageTitle != null && !pageTitle.isEmpty()
                        && !pageTitle.toLowerCase().contains("404")
                        && !pageTitle.toLowerCase().contains("not found")) {
                    stepLog.passWithScreenshot("Navigation successful",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                } else {
                    stepLog.failWithScreenshot("Navigation may have failed",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                }

                // Close extra tabs
                if (driver.getWindowHandles().size() > 1) {
                    driver.close();
                    driver.switchTo().window(driver.getWindowHandles().iterator().next());
                }

            } else {
                stepLog.failWithScreenshot("Link not found", "Could not locate '" + linkName + "' in footer");
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
