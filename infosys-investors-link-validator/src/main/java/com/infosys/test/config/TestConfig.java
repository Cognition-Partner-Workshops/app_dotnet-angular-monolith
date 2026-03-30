package com.infosys.test.config;

import java.io.File;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

/**
 * Central configuration for the test suite.
 */
public class TestConfig {

    public static final String BASE_URL = "https://www.infosys.com/investors.html";
    public static final int PAGE_LOAD_TIMEOUT_SECONDS = 30;
    public static final int IMPLICIT_WAIT_SECONDS = 10;
    public static final int EXPLICIT_WAIT_SECONDS = 15;
    public static final int HTTP_CONNECT_TIMEOUT_MS = 10000;
    public static final int HTTP_READ_TIMEOUT_MS = 15000;

    private static final String TIMESTAMP = LocalDateTime.now()
            .format(DateTimeFormatter.ofPattern("yyyyMMdd_HHmmss"));

    public static final String OUTPUT_DIR = "test-output" + File.separator + "run_" + TIMESTAMP;
    public static final String SCREENSHOT_DIR = OUTPUT_DIR + File.separator + "screenshots";
    public static final String REPORT_FILE = OUTPUT_DIR + File.separator + "test-report.html";

    public static boolean isHeadless() {
        String headless = System.getProperty("headless", "true");
        return Boolean.parseBoolean(headless);
    }

    public static String getBrowser() {
        return System.getProperty("browser", "chrome");
    }

    static {
        new File(SCREENSHOT_DIR).mkdirs();
    }
}
