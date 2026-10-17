namespace AI.Regulatory.API.Contracts;

/// <summary>One section of a CTD template hierarchy (for example 3.2.P.1).</summary>
public sealed record TemplateNode(
    string NodeKey,
    string? ParentKey,
    string Title,
    string? Guidance,
    bool IsMandatory);

/// <summary>The proposed or reviewed hierarchy of a template. Draft until an admin approves it.</summary>
public sealed record TemplateStructure(
    string TemplateId,
    string Status,
    string? Note,
    string UpdatedBy,
    DateTime UpdatedOn,
    IReadOnlyList<TemplateNode> Nodes);

public sealed record SaveTemplateStructureRequest(IReadOnlyList<TemplateNode> Nodes, bool Approve);
