package com.dogapi.listeners;

import com.aventstack.extentreports.ExtentReports;
import com.aventstack.extentreports.ExtentTest;
import com.aventstack.extentreports.Status;
import com.aventstack.extentreports.markuputils.CodeLanguage;
import com.aventstack.extentreports.markuputils.MarkupHelper;
import com.aventstack.extentreports.reporter.ExtentSparkReporter;
import com.aventstack.extentreports.reporter.configuration.Theme;
import org.testng.ITestContext;
import org.testng.ITestListener;
import org.testng.ITestResult;

import java.io.File;
import java.text.SimpleDateFormat;
import java.util.Date;

/**
 * TestNG listener that generates detailed ExtentReports HTML report
 * with request payload, response, and validation details.
 */
public class ExtentReportListener implements ITestListener {

    private static ExtentReports extentReports;
    private static final ThreadLocal<ExtentTest> extentTest = new ThreadLocal<>();
    private static String reportPath;

    @Override
    public void onStart(ITestContext context) {
        String timestamp = new SimpleDateFormat("yyyy-MM-dd_HH-mm-ss").format(new Date());
        String reportDir = System.getProperty("user.dir") + File.separator + "reports";
        new File(reportDir).mkdirs();
        reportPath = reportDir + File.separator + "TestResult_" + timestamp + ".html";

        ExtentSparkReporter sparkReporter = new ExtentSparkReporter(reportPath);
        sparkReporter.config().setDocumentTitle("The Dog API - Test Automation Report");
        sparkReporter.config().setReportName("API Test Results - The Dog API");
        sparkReporter.config().setTheme(Theme.STANDARD);
        sparkReporter.config().setTimeStampFormat("yyyy-MM-dd HH:mm:ss");
        sparkReporter.config().setEncoding("utf-8");
        sparkReporter.config().setCss(getCustomCss());

        extentReports = new ExtentReports();
        extentReports.attachReporter(sparkReporter);
        extentReports.setSystemInfo("API Base URL", "https://api.thedogapi.com/v1");
        extentReports.setSystemInfo("Framework", "RestAssured + TestNG");
        extentReports.setSystemInfo("Java Version", System.getProperty("java.version"));
        extentReports.setSystemInfo("OS", System.getProperty("os.name"));
    }

    @Override
    public void onTestStart(ITestResult result) {
        String testName = result.getMethod().getMethodName();
        String description = result.getMethod().getDescription();
        String displayName = description != null && !description.isEmpty()
                ? testName + " - " + description
                : testName;

        ExtentTest test = extentReports.createTest(displayName);
        test.assignCategory(result.getTestClass().getRealClass().getSimpleName());
        extentTest.set(test);
    }

    @Override
    public void onTestSuccess(ITestResult result) {
        extentTest.get().log(Status.PASS, "Test PASSED: " + result.getMethod().getMethodName());
    }

    @Override
    public void onTestFailure(ITestResult result) {
        ExtentTest test = extentTest.get();
        test.log(Status.FAIL, "Test FAILED: " + result.getMethod().getMethodName());
        test.log(Status.FAIL, "Failure Reason: " + result.getThrowable().getMessage());
        if (result.getThrowable().getStackTrace().length > 0) {
            StringBuilder stackTrace = new StringBuilder();
            for (StackTraceElement element : result.getThrowable().getStackTrace()) {
                stackTrace.append(element.toString()).append("\n");
                if (stackTrace.length() > 1000) {
                    stackTrace.append("... (truncated)");
                    break;
                }
            }
            test.log(Status.INFO, "Stack Trace:\n" + stackTrace);
        }
    }

    @Override
    public void onTestSkipped(ITestResult result) {
        ExtentTest test = extentTest.get();
        test.log(Status.SKIP, "Test SKIPPED: " + result.getMethod().getMethodName());
        if (result.getThrowable() != null) {
            test.log(Status.SKIP, "Skip Reason: " + result.getThrowable().getMessage());
        }
    }

    @Override
    public void onFinish(ITestContext context) {
        if (extentReports != null) {
            extentReports.flush();
        }
        System.out.println("\n========================================");
        System.out.println("HTML Test Report generated at: " + reportPath);
        System.out.println("========================================\n");
    }

    /**
     * Get the current ExtentTest instance for the running thread.
     */
    public static ExtentTest getTest() {
        return extentTest.get();
    }

    /**
     * Log request details to the HTML report.
     */
    public static void logRequest(String method, String endpoint, String requestBody) {
        ExtentTest test = getTest();
        if (test != null) {
            test.log(Status.INFO, "<b>Request Method:</b> " + method);
            test.log(Status.INFO, "<b>Request Endpoint:</b> " + endpoint);
            if (requestBody != null && !requestBody.isEmpty()) {
                test.log(Status.INFO, "<b>Request Payload:</b>");
                test.log(Status.INFO, MarkupHelper.createCodeBlock(requestBody, CodeLanguage.JSON));
            }
        }
    }

    /**
     * Log response details to the HTML report.
     */
    public static void logResponse(int statusCode, String responseBody) {
        ExtentTest test = getTest();
        if (test != null) {
            test.log(Status.INFO, "<b>Response Status Code:</b> " + statusCode);
            if (responseBody != null && !responseBody.isEmpty()) {
                String truncatedBody = responseBody.length() > 5000
                        ? responseBody.substring(0, 5000) + "\n... (truncated)"
                        : responseBody;
                test.log(Status.INFO, "<b>Response Body:</b>");
                test.log(Status.INFO, MarkupHelper.createCodeBlock(truncatedBody, CodeLanguage.JSON));
            }
        }
    }

    /**
     * Log full request/response capture from RestAssured filters.
     */
    public static void logRequestResponse(String requestLog, String responseLog) {
        ExtentTest test = getTest();
        if (test != null) {
            if (requestLog != null && !requestLog.isEmpty()) {
                test.log(Status.INFO, "<b>Full Request Log:</b>");
                test.log(Status.INFO, MarkupHelper.createCodeBlock(requestLog));
            }
            if (responseLog != null && !responseLog.isEmpty()) {
                String truncatedLog = responseLog.length() > 5000
                        ? responseLog.substring(0, 5000) + "\n... (truncated)"
                        : responseLog;
                test.log(Status.INFO, "<b>Full Response Log:</b>");
                test.log(Status.INFO, MarkupHelper.createCodeBlock(truncatedLog));
            }
        }
    }

    /**
     * Log a validation step with expected and actual values.
     */
    public static void logValidation(String validationName, Object expectedValue, Object actualValue, boolean passed) {
        ExtentTest test = getTest();
        if (test != null) {
            Status status = passed ? Status.PASS : Status.FAIL;
            String result = passed ? "PASSED" : "FAILED";
            String html = String.format(
                    "<table class='validation-table'>" +
                    "<tr><th>Validation</th><th>Expected</th><th>Actual</th><th>Result</th></tr>" +
                    "<tr><td>%s</td><td>%s</td><td>%s</td><td class='%s'>%s</td></tr>" +
                    "</table>",
                    escapeHtml(validationName),
                    escapeHtml(String.valueOf(expectedValue)),
                    escapeHtml(String.valueOf(actualValue)),
                    passed ? "pass-cell" : "fail-cell",
                    result
            );
            test.log(status, html);
        }
    }

    /**
     * Log an informational message to the HTML report.
     */
    public static void logInfo(String message) {
        ExtentTest test = getTest();
        if (test != null) {
            test.log(Status.INFO, message);
        }
    }

    /**
     * Log a step/section header.
     */
    public static void logStep(String stepDescription) {
        ExtentTest test = getTest();
        if (test != null) {
            test.log(Status.INFO, "<b style='color:#1a73e8;font-size:14px;'>Step: " + escapeHtml(stepDescription) + "</b>");
        }
    }

    private static String escapeHtml(String text) {
        if (text == null) return "null";
        return text.replace("&", "&amp;")
                   .replace("<", "&lt;")
                   .replace(">", "&gt;")
                   .replace("\"", "&quot;");
    }

    private String getCustomCss() {
        return ".validation-table { border-collapse: collapse; width: 100%; margin: 5px 0; }" +
               ".validation-table th, .validation-table td { border: 1px solid #ddd; padding: 8px; text-align: left; }" +
               ".validation-table th { background-color: #4472C4; color: white; }" +
               ".pass-cell { color: #28a745; font-weight: bold; }" +
               ".fail-cell { color: #dc3545; font-weight: bold; }" +
               ".validation-table tr:nth-child(even) { background-color: #f2f2f2; }";
    }
}
