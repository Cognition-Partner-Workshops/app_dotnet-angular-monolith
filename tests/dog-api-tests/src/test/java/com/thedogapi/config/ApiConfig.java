package com.thedogapi.config;

import io.restassured.RestAssured;
import io.restassured.builder.RequestSpecBuilder;
import io.restassured.filter.log.LogDetail;
import io.restassured.http.ContentType;
import io.restassured.specification.RequestSpecification;

import java.io.IOException;
import java.io.InputStream;
import java.util.Properties;

/**
 * Centralized configuration for The Dog API test suite.
 * Provides base URL, API key, and reusable request specifications.
 */
public final class ApiConfig {

    private static final Properties properties = new Properties();
    private static final String BASE_URL;
    private static final String API_KEY;
    private static final String SUB_ID;

    static {
        try (InputStream input = ApiConfig.class.getClassLoader()
                .getResourceAsStream("config.properties")) {
            if (input != null) {
                properties.load(input);
            }
        } catch (IOException e) {
            throw new RuntimeException("Failed to load config.properties", e);
        }

        BASE_URL = properties.getProperty("base.url", "https://api.thedogapi.com/v1");

        // API key: system property > environment variable > config file
        String key = System.getProperty("dog.api.key");
        if (key == null || key.isEmpty() || key.equals("${DOG_API_KEY}")) {
            key = System.getenv("DOG_API_KEY");
        }
        if (key == null || key.isEmpty() || key.equals("${DOG_API_KEY}")) {
            key = properties.getProperty("api.key", "");
        }
        if (key != null && key.equals("${DOG_API_KEY}")) {
            key = "";
        }
        API_KEY = key != null ? key : "";

        SUB_ID = properties.getProperty("sub.id", "test-user-dogapi");
    }

    private ApiConfig() {
    }

    public static String getBaseUrl() {
        return BASE_URL;
    }

    public static String getApiKey() {
        return API_KEY;
    }

    public static String getSubId() {
        return SUB_ID;
    }

    public static boolean hasApiKey() {
        return API_KEY != null && !API_KEY.isEmpty();
    }

    /**
     * Request specification without authentication (public endpoints).
     */
    public static RequestSpecification publicSpec() {
        return new RequestSpecBuilder()
                .setBaseUri(BASE_URL)
                .setContentType(ContentType.JSON)
                .log(LogDetail.URI)
                .build();
    }

    /**
     * Request specification with API key authentication.
     */
    public static RequestSpecification authSpec() {
        return new RequestSpecBuilder()
                .setBaseUri(BASE_URL)
                .setContentType(ContentType.JSON)
                .addHeader("x-api-key", API_KEY)
                .log(LogDetail.URI)
                .build();
    }

    /**
     * Configure RestAssured defaults.
     */
    public static void setup() {
        RestAssured.baseURI = BASE_URL;
        RestAssured.enableLoggingOfRequestAndResponseIfValidationFails();
    }
}
