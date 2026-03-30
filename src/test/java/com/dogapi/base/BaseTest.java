package com.dogapi.base;

import com.dogapi.config.ApiConfig;
import com.dogapi.utils.ApiHelper;
import io.restassured.RestAssured;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.testng.annotations.BeforeClass;
import org.testng.annotations.BeforeSuite;

/**
 * Base test class providing common setup for all API test classes.
 */
public abstract class BaseTest {

    protected static final Logger logger = LoggerFactory.getLogger(BaseTest.class);
    protected ApiHelper apiHelper;
    protected ApiConfig config;

    @BeforeSuite(alwaysRun = true)
    public void suiteSetup() {
        config = ApiConfig.getInstance();
        RestAssured.baseURI = config.getBaseUrl();
        logger.info("=== Test Suite Started ===");
        logger.info("Base URL: {}", config.getBaseUrl());
    }

    @BeforeClass(alwaysRun = true)
    public void classSetup() {
        apiHelper = new ApiHelper();
        config = ApiConfig.getInstance();
        logger.info("Setting up test class: {}", this.getClass().getSimpleName());
    }
}
