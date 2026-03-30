package com.infosys.test.driver;

import com.infosys.test.config.TestConfig;
import io.github.bonigarcia.wdm.WebDriverManager;
import org.openqa.selenium.WebDriver;
import org.openqa.selenium.chrome.ChromeDriver;
import org.openqa.selenium.chrome.ChromeOptions;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.time.Duration;

/**
 * Manages WebDriver lifecycle (creation and teardown).
 */
public class DriverManager {

    private static final Logger logger = LoggerFactory.getLogger(DriverManager.class);
    private static final ThreadLocal<WebDriver> driverThreadLocal = new ThreadLocal<>();

    public static WebDriver getDriver() {
        return driverThreadLocal.get();
    }

    public static WebDriver createDriver() {
        logger.info("Setting up ChromeDriver...");

        ChromeOptions options = new ChromeOptions();

        // Allow overriding Chrome binary via system property or env var
        String chromeBinary = System.getProperty("chrome.binary",
                System.getenv("CHROME_BINARY") != null ? System.getenv("CHROME_BINARY") : "");
        String resolvedBinaryPath = null;
        if (!chromeBinary.isEmpty()) {
            logger.info("Using custom Chrome binary: {}", chromeBinary);
            options.setBinary(chromeBinary);
            resolvedBinaryPath = chromeBinary;
        } else {
            // Auto-detect Chrome at common non-standard paths
            String[] candidates = {
                    "/opt/google/chrome/chrome",
                    "/usr/bin/google-chrome-stable",
                    "/usr/bin/google-chrome",
                    "/usr/bin/chromium-browser"
            };
            for (String path : candidates) {
                if (new java.io.File(path).exists()) {
                    logger.info("Auto-detected Chrome binary: {}", path);
                    options.setBinary(path);
                    resolvedBinaryPath = path;
                    break;
                }
            }
        }

        // Let WebDriverManager resolve a compatible ChromeDriver
        WebDriverManager wdm = WebDriverManager.chromedriver();
        if (resolvedBinaryPath != null) {
            try {
                // Detect Chrome version from the binary and match driver
                String binaryPath = resolvedBinaryPath;
                ProcessBuilder pb = new ProcessBuilder(binaryPath, "--version");
                pb.redirectErrorStream(true);
                Process proc = pb.start();
                String versionOutput = new String(proc.getInputStream().readAllBytes()).trim();
                proc.waitFor();
                // Extract major version, e.g. "Google Chrome for Testing 137.0.7118.2" -> "137"
                java.util.regex.Matcher m = java.util.regex.Pattern.compile("(\\d+)\\.").matcher(versionOutput);
                if (m.find()) {
                    String majorVersion = m.group(1);
                    logger.info("Detected Chrome major version: {}", majorVersion);
                    wdm = wdm.browserVersion(majorVersion);
                }
            } catch (Exception e) {
                logger.warn("Could not detect Chrome version from binary: {}", e.getMessage());
            }
        }
        wdm.setup();

        if (TestConfig.isHeadless()) {
            options.addArguments("--headless=new");
        }
        options.addArguments("--no-sandbox");
        options.addArguments("--disable-dev-shm-usage");
        options.addArguments("--window-size=1920,1080");
        options.addArguments("--disable-gpu");
        options.addArguments("--remote-allow-origins=*");
        options.addArguments("--user-data-dir=" + System.getProperty("java.io.tmpdir") + "/chrome-test-profile");
        options.addArguments("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        WebDriver driver = new ChromeDriver(options);
        driver.manage().timeouts().pageLoadTimeout(Duration.ofSeconds(TestConfig.PAGE_LOAD_TIMEOUT_SECONDS));
        driver.manage().timeouts().implicitlyWait(Duration.ofSeconds(TestConfig.IMPLICIT_WAIT_SECONDS));
        driver.manage().window().maximize();

        driverThreadLocal.set(driver);
        logger.info("ChromeDriver created successfully.");
        return driver;
    }

    public static void quitDriver() {
        WebDriver driver = driverThreadLocal.get();
        if (driver != null) {
            try {
                driver.quit();
                logger.info("ChromeDriver quit successfully.");
            } catch (Exception e) {
                logger.warn("Error quitting driver: {}", e.getMessage());
            } finally {
                driverThreadLocal.remove();
            }
        }
    }
}
