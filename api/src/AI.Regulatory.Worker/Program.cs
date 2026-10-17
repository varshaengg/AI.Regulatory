using AI.Regulatory.API.Data;
using AI.Regulatory.Worker;

// Background worker host: consumes dossier-run commands from Service Bus and executes the stages.
// It exposes /health/live only so App Service health checks pass.
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DataOptions>(builder.Configuration.GetSection(DataOptions.SectionName));
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddSingleton<ProjectsRepository>();
builder.Services.AddSingleton<TemplatesRepository>();
builder.Services.AddSingleton<GlobalSourcesRepository>();
builder.Services.AddSingleton<ProjectSourcesRepository>();
builder.Services.AddSingleton<PreparationRunsRepository>();
builder.Services.AddSingleton<GuidelinesRepository>();
builder.Services.AddGuidelineAi(builder.Configuration);
builder.Services.AddSingleton<ISourceDocumentLister, BlobSourceDocumentLister>();
builder.Services.AddSingleton<PreparationRunService>();
builder.Services.AddHostedService<PreparationWorker>();

var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.Run();
