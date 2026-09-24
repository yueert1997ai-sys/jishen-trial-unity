using UnityEditor;
using UnityEngine;

public class CombatAudioImporter : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer = (AudioImporter)assetImporter;
        bool music = assetPath.Contains("/Music/");
        bool sliceTransient=assetPath.StartsWith("Assets/Resources/Audio/Combat/R9/")||assetPath.StartsWith("Assets/Resources/Audio/Combat/ImpactR2/")||assetPath.StartsWith("Assets/Resources/Audio/Combat/VelocityR1/")||assetPath.StartsWith("Assets/Resources/Audio/Combat/ArsenalR2/");
        var settings = importer.defaultSampleSettings;
        settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = sliceTransient?AudioCompressionFormat.PCM:AudioCompressionFormat.Vorbis;
        if(sliceTransient)settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
        settings.quality = music ? 0.7f : 0.85f;
        settings.preloadAudioData = !music;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = music;
        importer.forceToMono = !music;
    }
}
