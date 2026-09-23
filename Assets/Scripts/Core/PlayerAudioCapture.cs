using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

// Opt-in replay capture only. AudioRenderer advances the actual Unity mixer by the
// capture clock, so audio stays aligned when screenshot rendering takes longer.
public sealed class PlayerAudioCapture
{
    readonly List<float> samples=new List<float>();
    float[] previous;
    readonly int channels,rate;
    public float Peak {get;private set;}
    public int SampleCount=>samples.Count;
    public double Duration=>samples.Count/(double)(channels*rate);
    public PlayerAudioCapture()
    {
        var config=AudioSettings.GetConfiguration();rate=config.sampleRate;
        switch(config.speakerMode)
        {
            case AudioSpeakerMode.Mono:channels=1;break;
            case AudioSpeakerMode.Quad:channels=4;break;
            case AudioSpeakerMode.Surround:channels=5;break;
            case AudioSpeakerMode.Mode5point1:channels=6;break;
            case AudioSpeakerMode.Mode7point1:channels=8;break;
            default:channels=2;break;
        }
        if(!AudioRenderer.Start())throw new Exception("Unity audio capture could not start");
    }
    public void Advance(bool includeFramePair)
    {
        int count=AudioRenderer.GetSampleCountForCaptureFrame()*channels;
        using(var buffer=new NativeArray<float>(count,Allocator.Temp))
        {
            if(!AudioRenderer.Render(buffer))throw new Exception("Unity mixer capture failed");
            var current=buffer.ToArray();
            if(includeFramePair)
            {
                samples.AddRange(previous??new float[count]);samples.AddRange(current);
                foreach(float f in current)Peak=Mathf.Max(Peak,Mathf.Abs(f));
                if(previous!=null)foreach(float f in previous)Peak=Mathf.Max(Peak,Mathf.Abs(f));
            }
            previous=current;
        }
    }
    public void Save(string path)
    {
        // Stop offline rendering only after muting the listener, so disk IO cannot resume speaker playback.
        AudioListener.volume=0;
        AudioRenderer.Stop();
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            int length=samples.Count*2;
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)channels);
            writer.Write(rate);writer.Write(rate*channels*2);writer.Write((short)(channels*2));writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(length);
            foreach(var value in samples)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));
        }
    }
}
