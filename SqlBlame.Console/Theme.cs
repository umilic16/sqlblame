using Spectre.Console;

namespace SqlBlame.Console;

internal static class Theme
{
    // palette, used only to build the purpose-named styles below
    private static Color Primary => Color.MediumTurquoise;
    private static Color Success => Color.PaleGreen1;
    private static Color Error => Color.LightSalmon1;
    private static Color Warning => Color.Gold1;

    internal static Color Logo => Primary;

    internal static Style Accent => new(Primary, decoration: Decoration.Bold);
    internal static Style Muted => new(decoration: Decoration.Dim);
    internal static Style DefaultValue => Muted;
    internal static Style Choices => new(Primary);
    internal static Style Selected => new(Primary);
    internal static Style Divider => new(Primary);

    internal static Style SuccessText => new(Success);
    internal static Style ErrorText => new(Error);
    internal static Style WarningText => new(Warning);

    internal static Style DiffAdded => new(Success);
    internal static Style DiffRemoved => new(Error);
    internal static Style DiffContext => Muted;

    internal static string AsAccent(string text) => Format(Accent, text);
    internal static string AsMuted(string text) => Format(Muted, text);
    internal static string AsSuccess(string text) => Format(SuccessText, text);
    internal static string AsError(string text) => Format(ErrorText, text);
    internal static string AsWarning(string text) => Format(WarningText, text);

    private static string Format(Style style, string text) => $"[{style.ToMarkup()}]{Markup.Escape(text)}[/]";
}
