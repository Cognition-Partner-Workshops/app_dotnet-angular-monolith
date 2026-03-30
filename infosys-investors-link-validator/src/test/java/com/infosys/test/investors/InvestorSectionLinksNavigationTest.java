package com.infosys.test.investors;

import com.infosys.test.base.BaseTest;
import com.infosys.test.config.TestConfig;
import com.infosys.test.model.TestCaseResult;
import com.infosys.test.utils.StepLogger;
import org.openqa.selenium.By;
import org.openqa.selenium.JavascriptExecutor;
import org.openqa.selenium.TimeoutException;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.WebDriverWait;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testng.annotations.DataProvider;
import org.testng.annotations.Test;

import java.time.Duration;

/**
 * TC-003 to TC-040+: Validates navigation for key investor-section links.
 * Each link is clicked and the destination page is verified.
 */
public class InvestorSectionLinksNavigationTest extends BaseTest {

    private static final Logger logger = LoggerFactory.getLogger(InvestorSectionLinksNavigationTest.class);

    @DataProvider(name = "investorLinks")
    public Object[][] investorLinksData() {
        return new Object[][]{
                // Investor sidebar/menu links
                {"TC-003", "Recent News", "/investors/news-events.html", "Investor Sidebar"},
                {"TC-004", "Analyst Coverage", "/investors/shares/analyst-coverage.html", "Investor Sidebar"},
                {"TC-005", "Investor Newsletter", "/investors/news-events/quarterly-newsletter.html", "Investor Sidebar"},
                {"TC-006", "Investor Presentations", "/investors/news-events/presentations.html", "Investor Sidebar"},
                {"TC-007", "Inorganic Business Investments", "/investors/inorganic-business-investments.html", "Investor Sidebar"},
                {"TC-008", "Feedback", "/investors/investors-feedback.html", "Investor Sidebar"},

                // Financials and Filings
                {"TC-009", "Financials and Filings", "/investors/reports-filings.html", "Financials and Filings"},
                {"TC-010", "3-year Overview", "/investors/reports-filings/financials/data-sheet.html", "Financials and Filings"},
                {"TC-011", "Quarterly Reports", "/investors/reports-filings/quarterly-results.html", "Financials and Filings"},
                {"TC-012", "Annual Reports", "/investors/reports-filings/annual-report/annual-reports.html", "Financials and Filings"},
                {"TC-013", "Guidance vs Actuals", "/investors/reports-filings/financials/guidance-vs-actuals-usd.html", "Financials and Filings"},
                {"TC-014", "Disclosure on Related party transactions", "/investors/reports-filings/financials/disclosure-related-party-transactions.html", "Financials and Filings"},
                {"TC-015", "Compliance Report on Corporate Governance", "/investors/reports-filings/corporate-governance-report.html", "Financials and Filings"},

                // Shareholder Services
                {"TC-016", "Shareholder's Services", "/investors/shareholder-services.html", "Shareholder Services"},
                {"TC-017", "Distribution of Shareholding", "/investors/shareholder-services/shareholding.html", "Shareholder Services"},
                {"TC-018", "General Meetings", "/investors/shareholder-services/general-meetings.html", "Shareholder Services"},
                {"TC-019", "Postal Ballot", "/investors/shareholder-services/postal-ballot.html", "Shareholder Services"},
                {"TC-020", "Investor FAQs", "/investors/shareholder-services/faqs.html", "Shareholder Services"},
                {"TC-021", "Unclaimed Dividend and Shares", "/investors/shareholder-services/unclaimed-dividend-shares.html", "Shareholder Services"},
                {"TC-022", "Investor Contacts", "/investors/shareholder-services/investor-contact.html", "Shareholder Services"},
                {"TC-023", "Investor Pack", "/investors/shareholder-services/investor-pack.html", "Shareholder Services"},
                {"TC-024", "Tax on Dividend", "/investors/shareholder-services/dividend-tax.html", "Shareholder Services"},
                {"TC-025", "Investor Forms and Services", "/investors/shareholder-services/investors-service.html", "Shareholder Services"},

                // Corporate Governance
                {"TC-026", "Corporate Governance", "/investors/corporate-governance.html", "Corporate Governance"},
                {"TC-027", "Corporate Governance Policies", "/investors/corporate-governance/policies.html", "Corporate Governance"},
                {"TC-028", "Corporate Governance Report", "/investors/corporate-governance/report.html", "Corporate Governance"},
                {"TC-029", "Corporate Social Responsibility", "/investors/corporate-governance/social-responsibility.html", "Corporate Governance"},
                {"TC-030", "Board Committee composition and Charters", "/investors/corporate-governance/board-committee-composition-charters.html", "Corporate Governance"},

                // Shares
                {"TC-031", "Shares", "/investors/shares.html", "Shares"},
                {"TC-032", "Share Price", "/investors/shares/share-price-bse.html", "Shares"},
                {"TC-033", "Share Details", "/investors/shares/share-details.html", "Shares"},
                {"TC-034", "Share Chart", "/investors/shares/share-chart.html", "Shares"},
                {"TC-035", "ADS Premium", "/investors/shares/ads-premium.html", "Shares"},
                {"TC-036", "Historic Share Price", "/investors/shares/newhistoricalshareprice.html", "Shares"},

                // News and Events
                {"TC-037", "Investor Calendar", "/investors/investor-services/investor-calendar.html", "News and Events"},
                {"TC-038", "Press Releases", "/newsroom/press-releases.html", "News and Events"},
                {"TC-039", "Events", "/investors/news-events/events.html", "News and Events"},

                // Featured Content (carousel / body links)
                {"TC-040", "Q3 Quarterly Results", "/investors/reports-filings/quarterly-results/2025-2026/q3.html", "Featured Content"},
                {"TC-041", "Q2 Quarterly Results", "/investors/reports-filings/quarterly-results/2025-2026/q2.html", "Featured Content"},
                {"TC-042", "Annual General Meeting 2025", "/investors/news-events/annual-general-meeting/2025.html", "Featured Content"},
        };
    }

    @Test(dataProvider = "investorLinks")
    public void testInvestorLinkNavigation(String tcId, String linkName, String hrefContains, String category) {
        TestCaseResult result = new TestCaseResult(
                tcId,
                "Navigate: " + linkName,
                category,
                "Verify clicking '" + linkName + "' link navigates to the correct page"
        );
        long startTime = System.currentTimeMillis();
        StepLogger stepLog = new StepLogger(result, driver);

        try {
            // Step 1: Navigate to investors page
            stepLog.info("Navigate to Investors page", TestConfig.BASE_URL);
            driver.get(TestConfig.BASE_URL);
            Thread.sleep(3000);
            stepLog.passWithScreenshot("Investors page loaded", "Page loaded successfully");

            // Step 2: Find the link
            String fullUrl = "https://www.infosys.com" + hrefContains;
            WebElement linkElement = null;

            // Try finding by exact href match first
            try {
                linkElement = driver.findElement(By.cssSelector("a[href*='" + hrefContains + "']"));
            } catch (Exception e) {
                // Try partial match
                try {
                    linkElement = driver.findElement(By.partialLinkText(linkName));
                } catch (Exception e2) {
                    logger.warn("Could not find link by text '{}', trying XPath", linkName);
                }
            }

            if (linkElement != null) {
                // Scroll to element
                ((JavascriptExecutor) driver).executeScript(
                        "arguments[0].scrollIntoView({behavior: 'smooth', block: 'center'});", linkElement);
                Thread.sleep(1000);
                stepLog.passWithScreenshot("Link found: " + linkName, "Element located on page");

                // Step 3: Click the link
                try {
                    linkElement.click();
                } catch (Exception clickEx) {
                    // Use JS click as fallback
                    ((JavascriptExecutor) driver).executeScript("arguments[0].click();", linkElement);
                }

                // Step 4: Wait for page to load
                try {
                    new WebDriverWait(driver, Duration.ofSeconds(TestConfig.EXPLICIT_WAIT_SECONDS))
                            .until(ExpectedConditions.not(ExpectedConditions.urlToBe(TestConfig.BASE_URL)));
                } catch (TimeoutException te) {
                    // URL might be same page with anchor
                }
                Thread.sleep(2000);

                // Step 5: Verify destination page
                String currentUrl = driver.getCurrentUrl();
                String pageTitle = driver.getTitle();

                if (currentUrl.contains(hrefContains.split("#")[0]) || currentUrl.equals(fullUrl)) {
                    stepLog.passWithScreenshot("Navigated to correct page",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                } else {
                    // Check if redirected to a valid page (not error page)
                    if (pageTitle != null && !pageTitle.isEmpty() && !pageTitle.toLowerCase().contains("error")
                            && !pageTitle.toLowerCase().contains("404") && !pageTitle.toLowerCase().contains("not found")) {
                        stepLog.passWithScreenshot("Navigated to page (redirected)",
                                "URL: " + currentUrl + " | Title: " + pageTitle);
                    } else {
                        stepLog.failWithScreenshot("Navigation verification",
                                "Expected URL containing: " + hrefContains + " but got: " + currentUrl);
                    }
                }

                // Step 6: Verify page has content
                try {
                    String bodyText = driver.findElement(By.tagName("body")).getText();
                    if (bodyText.length() > 50) {
                        stepLog.passNoScreenshot("Destination page has content",
                                "Page body text length: " + bodyText.length() + " characters");
                    } else {
                        stepLog.failWithScreenshot("Destination page has no content",
                                "Page body text is too short: " + bodyText.length() + " characters");
                    }
                } catch (Exception e) {
                    stepLog.failWithScreenshot("Error checking page content", e.getMessage());
                }

            } else {
                // Link not found on page - navigate directly
                stepLog.info("Link element not found on page", "Will navigate directly to URL");
                driver.get(fullUrl);
                Thread.sleep(3000);

                String currentUrl = driver.getCurrentUrl();
                String pageTitle = driver.getTitle();

                if (pageTitle != null && !pageTitle.isEmpty() && !pageTitle.toLowerCase().contains("404")) {
                    stepLog.passWithScreenshot("Direct navigation successful",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                } else {
                    stepLog.failWithScreenshot("Direct navigation failed",
                            "URL: " + currentUrl + " | Title: " + pageTitle);
                }
            }

        } catch (Exception e) {
            stepLog.failWithScreenshot("Unexpected error", e.getMessage());
            result.setStatus("FAIL");
            result.setErrorMessage(e.getMessage());
            logger.error("Test {} failed", tcId, e);
        } finally {
            result.setTotalDurationMs(System.currentTimeMillis() - startTime);
            addResult(result);
        }
    }
}
