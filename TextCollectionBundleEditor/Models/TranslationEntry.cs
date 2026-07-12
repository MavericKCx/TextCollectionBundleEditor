using AssetsTools.NET;

namespace TextCollectionBundleEditor.Models;

public sealed class TranslationEntry
{
    public int Index { get; set; }
    public string Id { get; set; } = string.Empty;
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string SavedText { get; set; } = string.Empty;
    public AssetTypeValueField? TextField { get; set; }

    public bool IsModified =>
        !string.Equals(TranslatedText, SavedText, StringComparison.Ordinal);
}
