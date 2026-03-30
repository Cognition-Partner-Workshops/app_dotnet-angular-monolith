# Infosys Investors Page - Link Validation Test Suite

Automated Selenium WebDriver test suite that validates all links on the [Infosys Investors page](https://www.infosys.com/investors.html).

## Features

- **Comprehensive Link Validation**: Tests all 248+ links found on the Investors page
- **HTTP Status Validation**: Verifies each link returns a valid HTTP response (not 404/500)
- **Click-Through Navigation**: Clicks key investor links and verifies destination pages
- **Categorized Test Cases**: Links grouped by Header, Investor Section, Footer, Social Media, Documents
- **Step-by-Step Logging**: Every test step is logged with details
- **Screenshots**: Screenshots captured at each validation step
- **HTML Report**: Rich, interactive HTML report with expandable test cases, inline screenshots, and filters

## Prerequisites

- **Java 17** or later
- **Maven 3.6+**
- **Google Chrome** browser installed
- ChromeDriver is managed automatically by WebDriverManager

## Project Structure

```
infosys-investors-link-validator/
├── pom.xml
├── README.md
├── src/
│   ├── main/java/com/infosys/test/
│   │   ├── config/TestConfig.java          # Central configuration
│   │   ├── driver/DriverManager.java       # WebDriver lifecycle management
│   │   ├── model/
│   │   │   ├── LinkInfo.java               # Link data model
│   │   │   ├── TestStep.java               # Test step data model
│   │   │   └── TestCaseResult.java         # Test case result model
│   │   ├── report/HtmlReportGenerator.java # HTML report generator
│   │   └── utils/
│   │       ├── ScreenshotUtil.java         # Screenshot capture utility
│   │       └── StepLogger.java             # Step logging with screenshots
│   ├── main/resources/logback.xml          # Logging configuration
│   └── test/
│       ├── java/com/infosys/test/
│       │   ├── base/BaseTest.java                          # Base test class
│       │   └── investors/
│       │       ├── InvestorPageLinkExistenceTest.java      # TC-001: Page load & link discovery
│       │       ├── LinkHttpValidationTest.java             # TC-002: HTTP validation of all links
│       │       ├── InvestorSectionLinksNavigationTest.java  # TC-003 to TC-042: Investor section nav
│       │       ├── HeaderNavigationLinksTest.java           # TC-043 to TC-047: Header nav links
│       │       ├── FooterLinksNavigationTest.java           # TC-048 to TC-066: Footer links
│       │       ├── SocialMediaLinksTest.java                # TC-067 to TC-072: Social media links
│       │       └── DocumentDownloadLinksTest.java           # TC-073 to TC-076: Document downloads
│       └── resources/testng.xml            # TestNG suite configuration
└── test-output/                            # Generated after test run
    └── run_<timestamp>/
        ├── test-report.html                # Interactive HTML report
        └── screenshots/                    # All captured screenshots
```

## How to Run

### Run all tests (headless mode - default)
```bash
mvn clean test
```

### Run with browser visible (non-headless)
```bash
mvn clean test -Dheadless=false
```

### Run a specific test class
```bash
mvn clean test -Dtest=InvestorSectionLinksNavigationTest
```

### Run with custom TestNG suite
```bash
mvn clean test -DsuiteXmlFile=src/test/resources/testng.xml
```

## Test Cases Summary

| Test ID | Test Name | Category |
|---------|-----------|----------|
| TC-001 | Page Load & Link Discovery | Page Load |
| TC-002 | HTTP Validation of All Links | HTTP Validation |
| TC-003 to TC-042 | Investor Section Link Navigation | Various Investor Sections |
| TC-043 to TC-047 | Header Navigation Links | Header Navigation |
| TC-048 to TC-066 | Footer Links Navigation | Footer Sections |
| TC-067 to TC-072 | Social Media Links | Social Media |
| TC-073 to TC-076 | Document Download Links | Document Downloads |

## Test Report

After running the tests, open the HTML report at:
```
test-output/run_<timestamp>/test-report.html
```

The report includes:
- **Summary Dashboard**: Total tests, pass/fail counts, duration
- **Progress Bar**: Visual pass/fail ratio
- **Filter Buttons**: Filter by passed/failed tests
- **Expandable Test Cases**: Click to see detailed steps
- **Inline Screenshots**: Thumbnails with click-to-zoom
- **Categorized View**: Tests grouped by category

## Configuration

Edit `src/main/java/com/infosys/test/config/TestConfig.java` to customize:
- `BASE_URL` - Target page URL
- `PAGE_LOAD_TIMEOUT_SECONDS` - Page load timeout
- `IMPLICIT_WAIT_SECONDS` - Implicit wait time
- `HTTP_CONNECT_TIMEOUT_MS` - HTTP connection timeout
- `HTTP_READ_TIMEOUT_MS` - HTTP read timeout
