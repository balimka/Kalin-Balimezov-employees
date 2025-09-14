using EmployeeAnalyzer.Services;
using EmployeeAnalyzer.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<StreamingCsvParsingService>();
builder.Services.AddScoped<OptimizedEmployeeAnalysisService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(origin => 
                  origin.StartsWith("http://localhost") || 
                  origin.StartsWith("https://localhost"))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();

app.MapPost("/api/analyze-employees", async (HttpContext context, 
    StreamingCsvParsingService csvService, 
    OptimizedEmployeeAnalysisService analysisService) =>
{
    var result = await ProcessEmployeeAnalysis(context, csvService, analysisService);
    if (result is EmployeePairResult pair)
        return Results.Ok(pair);
    return (IResult)result;
})
.WithName("AnalyzeEmployees")
.WithDescription("Analyzes a CSV file to find the employee pair who worked together longest");

app.MapPost("/api/analyze-employees/simple", async (HttpContext context, 
    StreamingCsvParsingService csvService, 
    OptimizedEmployeeAnalysisService analysisService) =>
{
    var result = await ProcessEmployeeAnalysis(context, csvService, analysisService);
    if (result is EmployeePairResult pair)
        return Results.Ok($"{pair.Employee1Id}, {pair.Employee2Id}, {pair.TotalDaysWorkedTogether}");
    return (IResult)result;
})
.WithName("SimpleFormat")
.WithDescription("Returns result in simple format: Employee1, Employee2, Days");

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

static async Task<object> ProcessEmployeeAnalysis(
    HttpContext context,
    StreamingCsvParsingService csvService,
    OptimizedEmployeeAnalysisService analysisService)
{
    try
    {
        var file = ValidateAndExtractFile(context);
        if (file is IResult errorResult)
            return errorResult;

        using var stream = ((IFormFile)file).OpenReadStream();
        var records = await csvService.ParseCsvAsync(stream);
        return analysisService.FindLongestCollaboratingPair(records);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest($"CSV parsing error: {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest($"Analysis error: {ex.Message}");
    }
    catch (Exception ex)
    {
        return Results.Problem($"An unexpected error occurred: {ex.Message}");
    }
}

static object ValidateAndExtractFile(HttpContext context)
{
    if (!context.Request.HasFormContentType)
        return Results.BadRequest("Request must contain form data with a file");

    var form = context.Request.ReadFormAsync().Result;
    var file = form.Files.GetFile("file");
    
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded or file is empty");

    if (file.Length > 100 * 1024 * 1024)
        return Results.BadRequest("File size exceeds 100MB limit");

    if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("File must be a CSV file");

    return file;
}