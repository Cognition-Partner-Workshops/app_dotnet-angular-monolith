package com.thedogapi.config;

import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.TestInstance;

import static org.junit.jupiter.api.Assumptions.assumeTrue;

/**
 * Base test class providing common setup and utility methods.
 */
@TestInstance(TestInstance.Lifecycle.PER_CLASS)
public abstract class BaseTest {

    @BeforeAll
    void setupAll() {
        ApiConfig.setup();
    }

    /**
     * Skip test if API key is not configured.
     * Call this at the start of tests that require authentication.
     */
    protected void requireApiKey() {
        assumeTrue(ApiConfig.hasApiKey(),
                "Skipped: DOG_API_KEY not set. Get one at https://thedogapi.com");
    }
}
