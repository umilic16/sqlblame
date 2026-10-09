using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SqlBlame.Core;

public static class DiffBuilder
{
    public static DiffPaneModel GetDiff(string before, string after)
    {
        var diff = InlineDiffBuilder.Diff(before, after);
        return diff;
    }
}
