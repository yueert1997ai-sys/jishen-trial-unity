using UnityEditor;
using UnityEngine;

public class CombatAudioImporter : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool music = assetPath.Contains("/Music/");
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = music ? 0.7f : 0.85f;
        settings.preloadAudioData = !music;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = music;
        importer.forceToMono = !music;
    }
}
