using System.Text.Json.Serialization;
using AI.Regulatory.API.Auth;
using AI.Regulatory.API.Data;
using AI.Regulatory.API.Errors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── MVC controllers ──────────────────────────────────────────────────────────
builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// ─── Data layer ───────────────────────────────────────────────────────────────
// Every repository extends BaseRepository<T> and respects the single
// Data:IsMocked flag defined below. Register concrete repositories as singletons
// so the in-memory seed persists for the lifetime of the process.
builder.Services.Configure<DataOptions>(builder.Configuration.GetSection(DataOptions.SectionName));
builder.Services.AddSingleton<ProjectsRepository>();
builder.Services.AddSingleton<TemplatesRepository>();
builder.Services.AddSingleton<ProjectSourcesRepository>();
builder.Services.AddSingleton<SubModulesRepository>();
builder.Services.AddSingleton<AssignmentsRepository>();
builder.Services.AddSingleton<CommentsRepository>();
builder.Services.AddSingleton<RunsRepository>();
builder.Services.AddSingleton<NotificationsRepository>();
builder.Services.AddSingleton<AdminRepository>();
builder.Services.AddSingleton<DocTreeRepository>();
builder.Services.AddSingleton<PersonasRepository>();
builder.Services.AddSingleton<FeaturesRepository>();
builder.Services.AddSingleton<PermissionsRepository>();
builder.Services.AddSingleton<PermissionMatrixRepository>();
builder.Services.AddSingleton<AppUsersRepository>();
builder.Services.AddSingleton<AadPeopleRepository>();

// Forwarded headers (App Service / reverse proxy sets X-Forwarded-Proto/For)
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

// ─── Authentication & authorization ───────────────────────────────────────────
// Interim variant (API-Design §3): validate MSAL SPA access tokens as JWT Bearer.
// Target variant (ADR-020): BFF cookie — introduced later without changing controllers.
// If EntraId:TenantId is not configured (e.g. local dev / smoke deploy), fall back
// to a no-op auth scheme so [AllowAnonymous] endpoints (like /health/live) work.
var entraSection = builder.Configuration.GetSection("EntraId");
var entraConfigured = !string.IsNullOrWhiteSpace(entraSection["TenantId"])
                   && !string.IsNullOrWhiteSpace(entraSection["ClientId"]);

if (entraConfigured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(entraSection);
}
else
{
    builder.Services.AddAuthentication("Noop").AddScheme<
        Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        AI.Regulatory.API.Auth.NoopAuthHandler>("Noop", _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.AdminOnly,     p => p.RequireRole("admin"));
    options.AddPolicy(AuthPolicies.RaLeadOrAdmin, p => p.RequireRole("raLead", "admin"));
    options.AddPolicy(AuthPolicies.AuthorScope,   p => p.RequireRole("raAuthor", "raLead", "admin"));
    options.AddPolicy(AuthPolicies.ReviewerScope, p => p.RequireRole("raReviewer", "raLead", "admin"));
});

// ─── CORS — origins driven entirely by Cors:AllowedOrigins config.
// appsettings.Development.json seeds http://localhost:5173; each deployed env
// supplies its own SPA origin via the Cors__AllowedOrigins__0 app-setting.
const string SpaCorsPolicy = "spa";
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy(SpaCorsPolicy, p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ─── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI.Regulatory.API",
        Version = "v1",
        Description = "Backend API for the Regulatory AI Assistant. See docs/API-Design.md."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entra ID access token from the SPA."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

// ─── Problem Details (RFC 9457) ───────────────────────────────────────────────
builder.Services.AddProblemDetails(o =>
{
    o.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => c.RouteTemplate = "api/v1/openapi/{documentName}.json");
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "api/v1/openapi";
        c.SwaggerEndpoint("/api/v1/openapi/v1.json", "AI.Regulatory.API v1");
    });
}

app.UseCors(SpaCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Controllers self-register their routes via [Route] attributes.
// New controllers automatically show up in OpenAPI — no manual registration.
app.MapControllers();

// Root redirect — friendly UX when someone hits the API host directly (e.g. bookmark).
// Set app-setting WebApp__PublicUrl to the SPA URL; if unset, root returns 404 as before.
var webAppPublicUrl = builder.Configuration["WebApp:PublicUrl"];
if (!string.IsNullOrWhiteSpace(webAppPublicUrl))
{
    app.MapGet("/", () => Results.Redirect(webAppPublicUrl, permanent: false))
       .AllowAnonymous()
       .ExcludeFromDescription();
}

app.Run();

public partial class Program;

