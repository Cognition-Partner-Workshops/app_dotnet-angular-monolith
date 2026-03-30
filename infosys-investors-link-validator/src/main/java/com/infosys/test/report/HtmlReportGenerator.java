package com.infosys.test.report;

import com.infosys.test.config.TestConfig;
import com.infosys.test.model.TestCaseResult;
import com.infosys.test.model.TestStep;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

/**
 * Generates a comprehensive HTML report with test cases, steps, and embedded screenshots.
 */
public class HtmlReportGenerator {

    private static final Logger logger = LoggerFactory.getLogger(HtmlReportGenerator.class);

    public static void generate(List<TestCaseResult> results) {
        String reportPath = TestConfig.REPORT_FILE;
        logger.info("Generating HTML report at: {}", reportPath);

        long totalTests = results.size();
        long passCount = results.stream().filter(r -> "PASS".equals(r.getStatus())).count();
        long failCount = results.stream().filter(r -> "FAIL".equals(r.getStatus())).count();
        long skipCount = results.stream().filter(r -> "SKIP".equals(r.getStatus())).count();
        long totalDuration = results.stream().mapToLong(TestCaseResult::getTotalDurationMs).sum();

        Map<String, List<TestCaseResult>> byCategory = results.stream()
                .collect(Collectors.groupingBy(r -> r.getCategory() != null ? r.getCategory() : "Uncategorized"));

        try (PrintWriter pw = new PrintWriter(new FileWriter(reportPath))) {
            pw.println("<!DOCTYPE html>");
            pw.println("<html lang=\"en\">");
            pw.println("<head>");
            pw.println("<meta charset=\"UTF-8\">");
            pw.println("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            pw.println("<title>Infosys Investors Page - Link Validation Test Report</title>");
            pw.println("<style>");
            pw.println(getCSS());
            pw.println("</style>");
            pw.println("<script>");
            pw.println(getJavaScript());
            pw.println("</script>");
            pw.println("</head>");
            pw.println("<body>");

            // Header
            pw.println("<div class=\"header\">");
            pw.println("<h1>Infosys Investors Page - Link Validation Test Report</h1>");
            pw.println("<p class=\"timestamp\">Generated: " +
                    LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss")) + "</p>");
            pw.println("<p class=\"url\">Target URL: <a href=\"" + TestConfig.BASE_URL + "\" target=\"_blank\">" +
                    TestConfig.BASE_URL + "</a></p>");
            pw.println("</div>");

            // Summary Dashboard
            pw.println("<div class=\"summary\">");
            pw.println("<div class=\"summary-card total\"><div class=\"count\">" + totalTests + "</div><div class=\"label\">Total Tests</div></div>");
            pw.println("<div class=\"summary-card pass\"><div class=\"count\">" + passCount + "</div><div class=\"label\">Passed</div></div>");
            pw.println("<div class=\"summary-card fail\"><div class=\"count\">" + failCount + "</div><div class=\"label\">Failed</div></div>");
            pw.println("<div class=\"summary-card skip\"><div class=\"count\">" + skipCount + "</div><div class=\"label\">Skipped</div></div>");
            pw.println("<div class=\"summary-card duration\"><div class=\"count\">" + formatDuration(totalDuration) + "</div><div class=\"label\">Total Duration</div></div>");
            pw.println("</div>");

            // Progress Bar
            double passPercent = totalTests > 0 ? (passCount * 100.0 / totalTests) : 0;
            double failPercent = totalTests > 0 ? (failCount * 100.0 / totalTests) : 0;
            pw.println("<div class=\"progress-bar\">");
            pw.printf("<div class=\"progress-pass\" style=\"width:%.1f%%\">%.1f%%</div>%n", passPercent, passPercent);
            if (failPercent > 0) {
                pw.printf("<div class=\"progress-fail\" style=\"width:%.1f%%\">%.1f%%</div>%n", failPercent, failPercent);
            }
            pw.println("</div>");

            // Filter buttons
            pw.println("<div class=\"filters\">");
            pw.println("<button class=\"filter-btn active\" onclick=\"filterTests('all')\">All</button>");
            pw.println("<button class=\"filter-btn pass-btn\" onclick=\"filterTests('PASS')\">Passed</button>");
            pw.println("<button class=\"filter-btn fail-btn\" onclick=\"filterTests('FAIL')\">Failed</button>");
            pw.println("<button class=\"filter-btn\" onclick=\"toggleAll(true)\">Expand All</button>");
            pw.println("<button class=\"filter-btn\" onclick=\"toggleAll(false)\">Collapse All</button>");
            pw.println("</div>");

            // Test Cases by Category
            for (Map.Entry<String, List<TestCaseResult>> entry : byCategory.entrySet()) {
                String category = entry.getKey();
                List<TestCaseResult> catResults = entry.getValue();
                long catPass = catResults.stream().filter(r -> "PASS".equals(r.getStatus())).count();
                long catFail = catResults.stream().filter(r -> "FAIL".equals(r.getStatus())).count();

                pw.println("<div class=\"category\">");
                pw.println("<h2 class=\"category-title\" onclick=\"toggleCategory(this)\">");
                pw.println("<span class=\"toggle-icon\">&#9660;</span> " + escapeHtml(category));
                pw.printf(" <span class=\"category-summary\">(%d tests: <span class=\"pass-text\">%d passed</span>, <span class=\"fail-text\">%d failed</span>)</span>%n",
                        catResults.size(), catPass, catFail);
                pw.println("</h2>");
                pw.println("<div class=\"category-content\">");

                for (TestCaseResult tc : catResults) {
                    String statusClass = tc.getStatus().toLowerCase();
                    pw.println("<div class=\"test-case " + statusClass + "\" data-status=\"" + tc.getStatus() + "\">");
                    pw.println("<div class=\"test-case-header\" onclick=\"toggleTestCase(this)\">");
                    pw.println("<span class=\"status-badge " + statusClass + "\">" + tc.getStatus() + "</span>");
                    pw.println("<span class=\"test-id\">" + escapeHtml(tc.getTestCaseId()) + "</span>");
                    pw.println("<span class=\"test-name\">" + escapeHtml(tc.getTestCaseName()) + "</span>");
                    pw.println("<span class=\"test-duration\">" + formatDuration(tc.getTotalDurationMs()) + "</span>");
                    pw.println("<span class=\"toggle-icon\">&#9654;</span>");
                    pw.println("</div>");

                    pw.println("<div class=\"test-case-body\" style=\"display:none;\">");
                    pw.println("<p class=\"test-description\">" + escapeHtml(tc.getDescription()) + "</p>");

                    if (tc.getErrorMessage() != null && !tc.getErrorMessage().isEmpty()) {
                        pw.println("<div class=\"error-message\">" + escapeHtml(tc.getErrorMessage()) + "</div>");
                    }

                    // Steps table
                    pw.println("<table class=\"steps-table\">");
                    pw.println("<thead><tr><th>#</th><th>Step</th><th>Status</th><th>Details</th><th>Screenshot</th></tr></thead>");
                    pw.println("<tbody>");

                    for (TestStep step : tc.getSteps()) {
                        String stepStatusClass = step.getStatus().toLowerCase();
                        pw.println("<tr class=\"step-row " + stepStatusClass + "\">");
                        pw.println("<td>" + step.getStepNumber() + "</td>");
                        pw.println("<td>" + escapeHtml(step.getDescription()) + "</td>");
                        pw.println("<td><span class=\"step-status " + stepStatusClass + "\">" + step.getStatus() + "</span></td>");
                        pw.println("<td class=\"step-details\">" + escapeHtml(step.getDetails()) + "</td>");
                        pw.println("<td>");
                        if (step.getScreenshotPath() != null) {
                            pw.println("<a href=\"" + step.getScreenshotPath() + "\" target=\"_blank\">");
                            pw.println("<img class=\"screenshot-thumb\" src=\"" + step.getScreenshotPath() +
                                    "\" alt=\"Screenshot\" onclick=\"showModal(this.src); event.preventDefault();\" />");
                            pw.println("</a>");
                        } else {
                            pw.println("-");
                        }
                        pw.println("</td>");
                        pw.println("</tr>");
                    }

                    pw.println("</tbody></table>");
                    pw.println("</div>"); // test-case-body
                    pw.println("</div>"); // test-case
                }

                pw.println("</div>"); // category-content
                pw.println("</div>"); // category
            }

            // Modal for screenshot zoom
            pw.println("<div id=\"screenshotModal\" class=\"modal\" onclick=\"closeModal()\">");
            pw.println("<img id=\"modalImg\" class=\"modal-content\" />");
            pw.println("</div>");

            pw.println("</body>");
            pw.println("</html>");

            logger.info("HTML report generated successfully: {}", reportPath);
        } catch (IOException e) {
            logger.error("Failed to generate HTML report: {}", e.getMessage(), e);
        }
    }

    private static String formatDuration(long ms) {
        if (ms < 1000) return ms + "ms";
        long seconds = ms / 1000;
        long minutes = seconds / 60;
        seconds = seconds % 60;
        if (minutes > 0) return minutes + "m " + seconds + "s";
        return seconds + "s";
    }

    private static String escapeHtml(String text) {
        if (text == null) return "";
        return text.replace("&", "&amp;")
                   .replace("<", "&lt;")
                   .replace(">", "&gt;")
                   .replace("\"", "&quot;")
                   .replace("'", "&#39;");
    }

    private static String getCSS() {
        return """
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f7fa; color: #333; padding: 20px; }
            .header { background: linear-gradient(135deg, #0056b3, #003d82); color: white; padding: 30px; border-radius: 10px; margin-bottom: 20px; }
            .header h1 { font-size: 24px; margin-bottom: 10px; }
            .header .timestamp { font-size: 14px; opacity: 0.9; }
            .header .url { font-size: 14px; margin-top: 5px; }
            .header .url a { color: #8ec8f8; }
            .summary { display: flex; gap: 15px; margin-bottom: 20px; flex-wrap: wrap; }
            .summary-card { flex: 1; min-width: 120px; background: white; border-radius: 10px; padding: 20px; text-align: center; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
            .summary-card .count { font-size: 32px; font-weight: bold; }
            .summary-card .label { font-size: 13px; color: #666; margin-top: 5px; text-transform: uppercase; }
            .summary-card.total .count { color: #0056b3; }
            .summary-card.pass .count { color: #28a745; }
            .summary-card.fail .count { color: #dc3545; }
            .summary-card.skip .count { color: #ffc107; }
            .summary-card.duration .count { color: #6f42c1; font-size: 22px; }
            .progress-bar { display: flex; height: 24px; border-radius: 12px; overflow: hidden; margin-bottom: 20px; background: #e9ecef; }
            .progress-pass { background: #28a745; color: white; text-align: center; font-size: 12px; line-height: 24px; }
            .progress-fail { background: #dc3545; color: white; text-align: center; font-size: 12px; line-height: 24px; }
            .filters { margin-bottom: 20px; display: flex; gap: 10px; flex-wrap: wrap; }
            .filter-btn { padding: 8px 16px; border: 1px solid #ddd; background: white; border-radius: 6px; cursor: pointer; font-size: 13px; }
            .filter-btn:hover { background: #e9ecef; }
            .filter-btn.active { background: #0056b3; color: white; border-color: #0056b3; }
            .filter-btn.pass-btn { border-color: #28a745; color: #28a745; }
            .filter-btn.fail-btn { border-color: #dc3545; color: #dc3545; }
            .category { background: white; border-radius: 10px; margin-bottom: 15px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); overflow: hidden; }
            .category-title { padding: 15px 20px; background: #f8f9fa; cursor: pointer; font-size: 18px; border-bottom: 1px solid #eee; }
            .category-title:hover { background: #e9ecef; }
            .category-summary { font-size: 14px; font-weight: normal; color: #666; }
            .pass-text { color: #28a745; }
            .fail-text { color: #dc3545; }
            .toggle-icon { font-size: 12px; margin-right: 5px; }
            .category-content { padding: 10px; }
            .test-case { border: 1px solid #eee; border-radius: 8px; margin: 8px 10px; overflow: hidden; }
            .test-case.pass { border-left: 4px solid #28a745; }
            .test-case.fail { border-left: 4px solid #dc3545; }
            .test-case-header { display: flex; align-items: center; padding: 12px 15px; cursor: pointer; gap: 10px; background: #fafafa; }
            .test-case-header:hover { background: #f0f0f0; }
            .status-badge { padding: 3px 10px; border-radius: 4px; font-size: 11px; font-weight: bold; color: white; }
            .status-badge.pass { background: #28a745; }
            .status-badge.fail { background: #dc3545; }
            .status-badge.skip { background: #ffc107; color: #333; }
            .test-id { font-size: 12px; color: #888; min-width: 80px; }
            .test-name { flex: 1; font-size: 14px; font-weight: 500; }
            .test-duration { font-size: 12px; color: #888; }
            .test-case-body { padding: 15px; background: white; }
            .test-description { font-size: 13px; color: #555; margin-bottom: 12px; padding: 8px; background: #f8f9fa; border-radius: 4px; }
            .error-message { background: #fff3cd; border: 1px solid #ffc107; padding: 10px; border-radius: 6px; margin-bottom: 12px; font-size: 13px; color: #856404; }
            .steps-table { width: 100%; border-collapse: collapse; font-size: 13px; }
            .steps-table th { background: #f1f3f5; padding: 10px; text-align: left; font-size: 12px; text-transform: uppercase; color: #555; }
            .steps-table td { padding: 10px; border-bottom: 1px solid #f0f0f0; vertical-align: top; }
            .steps-table tr:last-child td { border-bottom: none; }
            .step-row.fail { background: #fff5f5; }
            .step-status { padding: 2px 8px; border-radius: 3px; font-size: 11px; font-weight: bold; }
            .step-status.pass { background: #d4edda; color: #155724; }
            .step-status.fail { background: #f8d7da; color: #721c24; }
            .step-status.info { background: #d1ecf1; color: #0c5460; }
            .step-details { max-width: 400px; word-break: break-word; }
            .screenshot-thumb { width: 120px; height: 70px; object-fit: cover; border-radius: 4px; cursor: pointer; border: 1px solid #ddd; transition: transform 0.2s; }
            .screenshot-thumb:hover { transform: scale(1.05); box-shadow: 0 2px 8px rgba(0,0,0,0.2); }
            .modal { display: none; position: fixed; z-index: 1000; left: 0; top: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.85); cursor: pointer; }
            .modal-content { display: block; max-width: 90%; max-height: 90%; margin: 2% auto; border-radius: 8px; }
            @media (max-width: 768px) { .summary { flex-direction: column; } .test-case-header { flex-wrap: wrap; } }
            """;
    }

    private static String getJavaScript() {
        return """
            function toggleTestCase(header) {
                var body = header.nextElementSibling;
                var icon = header.querySelector('.toggle-icon');
                if (body.style.display === 'none') {
                    body.style.display = 'block';
                    icon.innerHTML = '&#9660;';
                } else {
                    body.style.display = 'none';
                    icon.innerHTML = '&#9654;';
                }
            }
            function toggleCategory(title) {
                var content = title.nextElementSibling;
                var icon = title.querySelector('.toggle-icon');
                if (content.style.display === 'none') {
                    content.style.display = 'block';
                    icon.innerHTML = '&#9660;';
                } else {
                    content.style.display = 'none';
                    icon.innerHTML = '&#9654;';
                }
            }
            function filterTests(status) {
                var testCases = document.querySelectorAll('.test-case');
                testCases.forEach(function(tc) {
                    if (status === 'all' || tc.getAttribute('data-status') === status) {
                        tc.style.display = 'block';
                    } else {
                        tc.style.display = 'none';
                    }
                });
                document.querySelectorAll('.filter-btn').forEach(function(btn) { btn.classList.remove('active'); });
                event.target.classList.add('active');
            }
            function toggleAll(expand) {
                var bodies = document.querySelectorAll('.test-case-body');
                var icons = document.querySelectorAll('.test-case-header .toggle-icon');
                bodies.forEach(function(body) { body.style.display = expand ? 'block' : 'none'; });
                icons.forEach(function(icon) { icon.innerHTML = expand ? '&#9660;' : '&#9654;'; });
            }
            function showModal(src) {
                var modal = document.getElementById('screenshotModal');
                var modalImg = document.getElementById('modalImg');
                modal.style.display = 'block';
                modalImg.src = src;
            }
            function closeModal() {
                document.getElementById('screenshotModal').style.display = 'none';
            }
            """;
    }
}
