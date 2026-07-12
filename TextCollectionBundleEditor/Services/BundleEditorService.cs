using AssetsTools.NET;
using AssetsTools.NET.Extra;
using TextCollectionBundleEditor.Models;

namespace TextCollectionBundleEditor.Services;

public sealed class BundleEditorService : IDisposable
{
    private AssetsManager? _manager;
    private BundleFileInstance? _bundleInstance;
    private AssetsFileInstance? _assetsInstance;
    private int _assetsFileIndex;

    public string? OpenedPath { get; private set; }

    public IReadOnlyList<string> Open(string path)
    {
        Close();
        if (!File.Exists(path))
            throw new FileNotFoundException("O arquivo bundle não foi encontrado.", path);

        _manager = new AssetsManager();
        _bundleInstance = _manager.LoadBundleFile(path, true);
        if (_bundleInstance?.file is null)
            throw new InvalidDataException("O arquivo não pôde ser aberto como Unity AssetBundle.");

        var dirs = _bundleInstance.file.BlockAndDirInfo.DirectoryInfos;
        _assetsFileIndex = -1;

        for (int i = 0; i < dirs.Count; i++)
        {
            string name = dirs[i].Name;
            if (name.EndsWith(".resource", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".resS", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                _assetsInstance = _manager.LoadAssetsFileFromBundle(_bundleInstance, i, false);
                if (_assetsInstance?.file is not null)
                {
                    _assetsFileIndex = i;
                    break;
                }
            }
            catch
            {
                _assetsInstance = null;
            }
        }

        if (_assetsInstance?.file is null || _assetsFileIndex < 0)
            throw new InvalidDataException("Nenhum arquivo CAB serializado pôde ser aberto dentro do bundle.");

        OpenedPath = path;
        return FindTextCollections();
    }

    private IReadOnlyList<string> FindTextCollections()
    {
        EnsureOpen();
        var names = new List<string>();

        foreach (AssetFileInfo info in _assetsInstance!.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
        {
            try
            {
                AssetTypeValueField baseField = _manager!.GetBaseField(_assetsInstance, info);
                string name = baseField["m_Name"].AsString;
                AssetTypeValueField array = GetTextListArray(baseField);
                if (!string.IsNullOrWhiteSpace(name) && !array.IsDummy)
                    names.Add(name);
            }
            catch
            {
                // Alguns MonoBehaviours não possuem uma estrutura legível.
            }
        }

        return names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<TranslationEntry> LoadCollection(string collectionName)
    {
        EnsureOpen();
        (AssetFileInfo info, AssetTypeValueField baseField, AssetTypeValueField array) = FindCollection(collectionName);

        var entries = new List<TranslationEntry>(array.Children.Count);
        for (int i = 0; i < array.Children.Count; i++)
        {
            AssetTypeValueField item = array.Children[i];
            AssetTypeValueField idField = item["Id"];
            AssetTypeValueField textField = item["Text"];

            if (idField.IsDummy || textField.IsDummy)
                continue;

            entries.Add(new TranslationEntry
            {
                Index = i,
                Id = idField.AsString,
                OriginalText = textField.AsString,
                TranslatedText = textField.AsString,
                SavedText = textField.AsString,
                TextField = textField
            });
        }

        return entries;
    }

    private (AssetFileInfo Info, AssetTypeValueField BaseField, AssetTypeValueField Array) FindCollection(string collectionName)
    {
        foreach (AssetFileInfo info in _assetsInstance!.file.GetAssetsOfType(AssetClassID.MonoBehaviour))
        {
            AssetTypeValueField baseField;
            try
            {
                baseField = _manager!.GetBaseField(_assetsInstance, info);
            }
            catch
            {
                continue;
            }

            if (!string.Equals(baseField["m_Name"].AsString, collectionName, StringComparison.OrdinalIgnoreCase))
                continue;

            AssetTypeValueField array = GetTextListArray(baseField);
            if (array.IsDummy)
                throw new InvalidDataException($"A coleção '{collectionName}' foi encontrada, mas textList.Array não pôde ser lido.");

            return (info, baseField, array);
        }

        throw new InvalidDataException($"A coleção '{collectionName}' não foi encontrada.");
    }

    private static AssetTypeValueField GetTextListArray(AssetTypeValueField baseField)
    {
        AssetTypeValueField array = baseField["textList.Array"];
        if (!array.IsDummy)
            return array;

        AssetTypeValueField textList = baseField["textList"];
        if (!textList.IsDummy)
        {
            AssetTypeValueField nested = textList["Array"];
            if (!nested.IsDummy)
                return nested;
        }

        return array;
    }

    public void SaveAs(string outputPath, IReadOnlyDictionary<string, List<TranslationEntry>> collections)
    {
        EnsureOpen();
        if (collections.Count == 0)
            throw new InvalidOperationException("Nenhuma coleção foi carregada para salvar.");

        foreach ((string collectionName, List<TranslationEntry> entries) in collections)
        {
            (AssetFileInfo info, AssetTypeValueField baseField, AssetTypeValueField array) = FindCollection(collectionName);
            var byIndex = entries.ToDictionary(e => e.Index);

            for (int i = 0; i < array.Children.Count; i++)
            {
                if (!byIndex.TryGetValue(i, out TranslationEntry? entry))
                    continue;

                AssetTypeValueField textField = array.Children[i]["Text"];
                if (!textField.IsDummy)
                    textField.AsString = entry.TranslatedText ?? string.Empty;
            }

            info.SetNewData(baseField);
        }

        _bundleInstance!.file.BlockAndDirInfo.DirectoryInfos[_assetsFileIndex]
            .SetNewData(_assetsInstance!.file);

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(outputPath))
        {
            string backup = outputPath + ".bak";
            File.Copy(outputPath, backup, true);
        }

        string tempUncompressed = outputPath + ".tmp_uncompressed";
        try
        {
            using (var writer = new AssetsFileWriter(tempUncompressed))
                _bundleInstance.file.Write(writer);

            var uncompressedBundle = new AssetBundleFile();
            try
            {
                using var reader = new AssetsFileReader(File.OpenRead(tempUncompressed));
                uncompressedBundle.Read(reader);
                using var packedWriter = new AssetsFileWriter(outputPath);
                uncompressedBundle.Pack(packedWriter, AssetBundleCompressionType.LZ4);
            }
            finally
            {
                uncompressedBundle.Close();
            }
        }
        finally
        {
            if (File.Exists(tempUncompressed))
                File.Delete(tempUncompressed);
        }
    }

    private void EnsureOpen()
    {
        if (_manager is null || _bundleInstance is null || _assetsInstance is null)
            throw new InvalidOperationException("Nenhum bundle está aberto.");
    }

    public void Close()
    {
        _assetsInstance = null;

        if (_bundleInstance is not null)
        {
            _bundleInstance.file.Close();
            _bundleInstance = null;
        }

        _manager?.UnloadAll();
        _manager = null;
        OpenedPath = null;
    }

    public void Dispose() => Close();
}
