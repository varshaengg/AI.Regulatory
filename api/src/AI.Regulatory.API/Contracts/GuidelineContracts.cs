namespace AI.Regulatory.API.Contracts;

/// <summary>A checklist rule the AI applies when validating documents of a module (advisory only).</summary>
public sealed record GuidelineRule(
    string RuleKey,
    string Text,
    string Severity,
    string? NodeRef);

/// <summary>Admin-managed guideline document for one CTD module, with its reviewed checklist rules.</summary>
public sealed record ModuleGuideline(
    string Id,
    string ModuleId,
    string Country,
    string Version,
    string FileName,
    string UploadedBy,
    DateTime UploadedOn,
    bool IsPublished,
    string? ExtractionNote,
    IReadOnlyList<GuidelineRule> Rules);

public sealed record SaveGuidelineRulesRequest(IReadOnlyList<GuidelineRule> Rules);
