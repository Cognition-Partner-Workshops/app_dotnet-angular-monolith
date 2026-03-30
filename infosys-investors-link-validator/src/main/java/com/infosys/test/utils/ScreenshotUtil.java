package com.infosys.test.utils;

import com.infosys.test.config.TestConfig;
import org.openqa.selenium.OutputType;
import org.openqa.selenium.TakesScreenshot;
import org.openqa.selenium.WebDriver;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.concurrent.atomic.AtomicInteger;

/**
 * Utility class for capturing screenshots.
 */
public class ScreenshotUtil {

    private static final Logger logger = LoggerFactory.getLogger(ScreenshotUtil.class);
    private static final AtomicInteger counter = new AtomicInteger(0);

    /**
     * Captures a screenshot and saves it to the screenshot directory.
     *
     * @param driver the WebDriver instance
     * @param name   a descriptive name for the screenshot
     * @return the relative path to the screenshot file (relative to the output dir), or null if failed
     */
    public static String capture(WebDriver driver, String name) {
        if (driver == null) {
            logger.warn("Driver is null, cannot take screenshot.");
            return null;
        }

        try {
            File srcFile = ((TakesScreenshot) driver).getScreenshotAs(OutputType.FILE);
            String sanitizedName = name.replaceAll("[^a-zA-Z0-9_-]", "_");
            if (sanitizedName.length() > 80) {
                sanitizedName = sanitizedName.substring(0, 80);
            }
            String fileName = String.format("%04d_%s.png", counter.incrementAndGet(), sanitizedName);
            Path destPath = Path.of(TestConfig.SCREENSHOT_DIR, fileName);

            Files.copy(srcFile.toPath(), destPath, StandardCopyOption.REPLACE_EXISTING);
            logger.debug("Screenshot saved: {}", destPath);

            // Return path relative to the output directory (for HTML report)
            return "screenshots" + File.separator + fileName;
        } catch (IOException e) {
            logger.error("Failed to save screenshot '{}': {}", name, e.getMessage());
            return null;
        } catch (Exception e) {
            logger.error("Failed to capture screenshot '{}': {}", name, e.getMessage());
            return null;
        }
    }
}
