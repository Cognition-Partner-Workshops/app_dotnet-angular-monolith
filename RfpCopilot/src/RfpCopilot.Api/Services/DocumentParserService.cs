using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace RfpCopilot.Api.Services;

public interface IDocumentParserService
{
    Task<string> ExtractTextAsync(Stream fileStream, string contentType, string fileName);
}

public class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;

    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => await ExtractFromPdfAsync(fileStream),
            ".docx" => await ExtractFromDocxAsync(fileStream),
            ".txt" => await ExtractFromTextAsync(fileStream),
            _ => throw new NotSupportedException($"File type '{extension}' is not supported. Supported types: .pdf, .docx, .txt")
        };
    }

    private async Task<string> ExtractFromPdfAsync(Stream fileStream)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        try
        {
            using var pdfReader = new PdfReader(memoryStream);
            using var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader);
            var textParts = new List<string>();

            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new SimpleTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    textParts.Add(pageText);
                }
            }

            var result = string.Join("\n\n", textParts);
            _logger.LogInformation("Extracted {Length} characters from PDF", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            // TODO: Integrate Azure AI Document Intelligence for scanned PDFs (OCR)
            throw new InvalidOperationException("Failed to extract text from PDF. Scanned PDFs requiring OCR are not yet supported.", ex);
        }
    }

    private async Task<string> ExtractFromDocxAsync(Stream fileStream)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        try
        {
            using var wordDoc = WordprocessingDocument.Open(memoryStream, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;
            if (body == null)
                return string.Empty;

            var textParts = new List<string>();
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    textParts.Add(text);
                }
            }

            var result = string.Join("\n", textParts);
            _logger.LogInformation("Extracted {Length} characters from DOCX", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from DOCX");
            throw new InvalidOperationException("Failed to extract text from DOCX document.", ex);
        }
    }

    private async Task<string> ExtractFromTextAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        var result = await reader.ReadToEndAsync();
        _logger.LogInformation("Read {Length} characters from text file", result.Length);
        return result;
    }
}
