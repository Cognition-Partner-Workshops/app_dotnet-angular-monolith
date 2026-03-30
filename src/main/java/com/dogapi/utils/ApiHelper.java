package com.dogapi.utils;

import com.dogapi.config.ApiConfig;
import io.restassured.RestAssured;
import io.restassured.filter.log.RequestLoggingFilter;
import io.restassured.filter.log.ResponseLoggingFilter;
import io.restassured.http.ContentType;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;

import java.io.ByteArrayOutputStream;
import java.io.PrintStream;
import java.util.Map;

/**
 * Helper utility for making API calls with detailed request/response capture.
 */
public class ApiHelper {

    private final ApiConfig config;
    private String lastRequestLog;
    private String lastResponseLog;

    public ApiHelper() {
        this.config = ApiConfig.getInstance();
    }

    public String getLastRequestLog() {
        return lastRequestLog;
    }

    public String getLastResponseLog() {
        return lastResponseLog;
    }

    private RequestSpecification baseRequest() {
        ByteArrayOutputStream requestCapture = new ByteArrayOutputStream();
        ByteArrayOutputStream responseCapture = new ByteArrayOutputStream();

        return RestAssured.given()
                .baseUri(config.getBaseUrl())
                .contentType(ContentType.JSON)
                .header("x-api-key", config.getApiKey())
                .filter(new RequestLoggingFilter(new PrintStream(requestCapture)))
                .filter(new ResponseLoggingFilter(new PrintStream(responseCapture)))
                .filter((requestSpec, responseSpec, ctx) -> {
                    Response response = ctx.next(requestSpec, responseSpec);
                    lastRequestLog = requestCapture.toString();
                    lastResponseLog = responseCapture.toString();
                    return response;
                });
    }

    public Response get(String endpoint) {
        return baseRequest()
                .when()
                .get(endpoint);
    }

    public Response get(String endpoint, Map<String, Object> queryParams) {
        return baseRequest()
                .queryParams(queryParams)
                .when()
                .get(endpoint);
    }

    public Response post(String endpoint, Object body) {
        return baseRequest()
                .body(body)
                .when()
                .post(endpoint);
    }

    public Response post(String endpoint, String body) {
        return baseRequest()
                .body(body)
                .when()
                .post(endpoint);
    }

    public Response delete(String endpoint) {
        return baseRequest()
                .when()
                .delete(endpoint);
    }

    public Response postMultipart(String endpoint, String filePath, String subId) {
        ByteArrayOutputStream requestCapture = new ByteArrayOutputStream();
        ByteArrayOutputStream responseCapture = new ByteArrayOutputStream();

        return RestAssured.given()
                .baseUri(config.getBaseUrl())
                .header("x-api-key", config.getApiKey())
                .multiPart("file", new java.io.File(filePath))
                .multiPart("sub_id", subId)
                .filter(new RequestLoggingFilter(new PrintStream(requestCapture)))
                .filter(new ResponseLoggingFilter(new PrintStream(responseCapture)))
                .filter((requestSpec, responseSpec, ctx) -> {
                    Response response = ctx.next(requestSpec, responseSpec);
                    lastRequestLog = requestCapture.toString();
                    lastResponseLog = responseCapture.toString();
                    return response;
                })
                .when()
                .post(endpoint);
    }
}
