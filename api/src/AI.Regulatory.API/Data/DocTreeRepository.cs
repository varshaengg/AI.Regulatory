using AI.Regulatory.API.Contracts;
using Microsoft.Extensions.Options;

namespace AI.Regulatory.API.Data;

/// <summary>Compiled-dossier document tree — R1 left rail.</summary>
public sealed class DocTreeRepository : BaseRepository<DocTreeNode>
{
    public DocTreeRepository(IOptions<DataOptions> options) : base(options) { }

    protected override bool MatchesId(DocTreeNode item, string id)
        => string.Equals(item.Code, id, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<DocTreeNode> SeedData() => new[]
    {
        Mod("M1", "Administrative", "#0F6CBD", comments: 0),
        Mod("M2", "Summaries",      "#5C2E91", comments: 1),
        new DocTreeNode("M3", "Quality", "M3", "#107C10", Comments: 2, Active: false, Children: new []
        {
            Group("3.2.S", "Drug Substance", new[]
            {
                Section("3.2.S.1", "General information"),
                Section("3.2.S.2", "Manufacture"),
                Section("3.2.S.3", "Characterisation"),
                new DocTreeNode("3.2.S.4", "Control of Drug Substance", null, null, Comments: 2, Active: false, Children: new []
                {
                    Section("3.2.S.4.1", "Specification"),
                    new DocTreeNode("3.2.S.4.2", "Analytical Procedures", null, null, Comments: 2, Active: true,
                                    Children: Array.Empty<DocTreeNode>()),
                    Section("3.2.S.4.3", "Validation of Analytical Procedures"),
                }),
                Section("3.2.S.5", "Reference standards"),
            }),
            Group("3.2.P", "Drug Product", new[]
            {
                Section("3.2.P.1", "Description and composition"),
                Section("3.2.P.2", "Pharmaceutical development"),
                Section("3.2.P.3", "Manufacture"),
            }),
        }),
        Mod("M4", "Nonclinical",    "#B58500", comments: 1),
        Mod("M5", "Clinical",       "#D13438", comments: 0),
    };

    private static DocTreeNode Mod(string id, string label, string color, int comments)
        => new(id, label, id, color, Comments: comments, Active: false, Children: Array.Empty<DocTreeNode>());

    private static DocTreeNode Group(string code, string label, DocTreeNode[] sections)
        => new(code, label, null, null, Comments: 0, Active: false, Children: sections);

    private static DocTreeNode Section(string code, string label)
        => new(code, label, null, null, Comments: 0, Active: false, Children: Array.Empty<DocTreeNode>());
}
