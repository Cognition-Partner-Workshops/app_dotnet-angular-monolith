using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using RfpCopilot.Api.Agents;
using RfpCopilot.Api.Data;
using RfpCopilot.Api.Hubs;
using RfpCopilot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=rfpcopilot.db"));

// Services
builder.Services.AddScoped<IDocumentParserService, DocumentParserService>();
builder.Services.AddScoped<ITrackerService, TrackerService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IResponseAssemblerService, ResponseAssemblerService>();

// Semantic Kernel
var kernelBuilder = Kernel.CreateBuilder();

var aiEndpoint = builder.Configuration["AzureOpenAI:Endpoint"] ?? "";
var aiKey = builder.Configuration["AzureOpenAI:ApiKey"] ?? "";
var aiDeployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "";

if (!string.IsNullOrEmpty(aiEndpoint) && !string.IsNullOrEmpty(aiKey) && !string.IsNullOrEmpty(aiDeployment))
{
    kernelBuilder.AddAzureOpenAIChatCompletion(aiDeployment, aiEndpoint, aiKey);
}

var kernel = kernelBuilder.Build();
builder.Services.AddSingleton(kernel);

// Agents
builder.Services.AddScoped<TrackerAgent>();
builder.Services.AddScoped<SolutionApproachAgent>();
builder.Services.AddScoped<EstimationAgent>();
builder.Services.AddScoped<CloudMigrationAgent>();
builder.Services.AddScoped<IntegrationAgent>();
builder.Services.AddScoped<TestingDevOpsAgent>();
builder.Services.AddScoped<StaffingTimelineAgent>();
builder.Services.AddScoped<RisksAssumptionsAgent>();
builder.Services.AddScoped<OrchestratorAgent>();

// Controllers & SignalR
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RFP Copilot API", Version = "v1" });
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials()));

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    SeedData.Initialize(context);
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
// Serve Angular SPA from wwwroot/browser (Angular 17+ output structure)
var spaPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "browser");
if (Directory.Exists(spaPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(spaPath)
    });
}
else
{
    app.UseStaticFiles();
}
app.MapControllers();
app.MapHub<RfpProgressHub>("/hubs/rfp-progress");
app.MapFallback(async context =>
{
    var browserIndexPath = Path.Combine(spaPath, "index.html");
    if (File.Exists(browserIndexPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(browserIndexPath);
    }
    else
    {
        context.Response.StatusCode = 404;
    }
});
app.Run();
