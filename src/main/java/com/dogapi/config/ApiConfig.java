package com.dogapi.config;

import java.io.IOException;
import java.io.InputStream;
import java.util.Properties;

/**
 * Configuration loader for API test properties.
 */
public class ApiConfig {

    private static final Properties properties = new Properties();
    private static ApiConfig instance;

    private ApiConfig() {
        loadProperties();
    }

    public static synchronized ApiConfig getInstance() {
        if (instance == null) {
            instance = new ApiConfig();
        }
        return instance;
    }

    private void loadProperties() {
        try (InputStream input = getClass().getClassLoader().getResourceAsStream("config.properties")) {
            if (input == null) {
                throw new RuntimeException("Unable to find config.properties");
            }
            properties.load(input);
        } catch (IOException e) {
            throw new RuntimeException("Failed to load config.properties", e);
        }
    }

    public String getBaseUrl() {
        return properties.getProperty("base.url", "https://api.thedogapi.com/v1");
    }

    public String getApiKey() {
        String envKey = System.getenv("DOG_API_KEY");
        if (envKey != null && !envKey.isEmpty()) {
            return envKey;
        }
        return properties.getProperty("api.key", "DEMO-API-KEY");
    }

    public int getConnectionTimeout() {
        return Integer.parseInt(properties.getProperty("connection.timeout", "10"));
    }

    public int getSocketTimeout() {
        return Integer.parseInt(properties.getProperty("socket.timeout", "10"));
    }
}
