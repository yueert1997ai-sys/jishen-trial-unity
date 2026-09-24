using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// Compact, sampled source data. FK and coordinate conversion happen offline;
// no PSA skeleton, model or thousands of unused part bones are instantiated.
public sealed class ImportedBodyMotion
{
    public struct Joint
    {
        public Vector3 p; public Quaternion q;
        public static Joint Blend(Joint a,Joint b,float t)=>new Joint{p=Vector3.Lerp(a.p,b.p,t),q=Quaternion.Slerp(a.q,b.q,t)};
    }
    public sealed class Clip
    {
        public int frames;public float rate;public Joint[] data;
        public float Duration=>(frames-1)/rate;
        public void Sample(float seconds,Joint[] target)
        {
            float f=Mathf.Clamp(seconds*rate,0,frames-1);int a=(int)f,b=Mathf.Min(a+1,frames-1);
            for(int j=0;j<target.Length;j++)target[j]=Joint.Blend(data[a*target.Length+j],data[b*target.Length+j],f-a);
        }
    }
    public readonly string[] names;public readonly Joint[] bind;
    public readonly Dictionary<string,Clip> clips=new Dictionary<string,Clip>();
    public int Count=>names.Length;
    static ImportedBodyMotion shared;static bool attempted;
    static readonly Dictionary<string,ImportedBodyMotion> banks=new Dictionary<string,ImportedBodyMotion>();
    public static ImportedBodyMotion LoadBank(string name)
    {
        if(banks.TryGetValue(name,out var bank))return bank;
        var source=Resources.Load<TextAsset>("GB4Motion/"+name);if(source==null)return null;
        bank=new ImportedBodyMotion(source.bytes);banks.Add(name,bank);return bank;
    }
    public static ImportedBodyMotion Load()
    {
        if(attempted)return shared;attempted=true;
        var source=Resources.Load<TextAsset>("GB4Motion/body");
        if(source==null)return null;
        shared=new ImportedBodyMotion(source.bytes);return shared;
    }
    static string ReadName(BinaryReader r)=>Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
    static Joint ReadJoint(BinaryReader r)=>new Joint{p=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle()),q=new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle())};
    ImportedBodyMotion(byte[] bytes)
    {
        using(var r=new BinaryReader(new MemoryStream(bytes)))
        {
            if(Encoding.ASCII.GetString(r.ReadBytes(4))!="GBM1"||r.ReadInt32()!=1)throw new InvalidDataException("Invalid imported body motion");
            int count=r.ReadInt32(),clipCount=r.ReadInt32();
            if(count<1||count>400||clipCount<1||clipCount>100)throw new InvalidDataException("Incomplete imported body motion");
            names=new string[count];bind=new Joint[count];
            for(int i=0;i<count;i++)names[i]=ReadName(r);
            for(int i=0;i<count;i++)bind[i]=ReadJoint(r);
            for(int i=0;i<clipCount;i++)
            {
                string name=ReadName(r);var c=new Clip{frames=r.ReadInt32(),rate=r.ReadSingle()};
                if(c.frames<2||c.frames>10000||c.rate<=0)throw new InvalidDataException("Invalid motion clip "+name);
                c.data=new Joint[c.frames*count];for(int j=0;j<c.data.Length;j++)c.data[j]=ReadJoint(r);clips.Add(name,c);
            }
            if(r.BaseStream.Position!=bytes.Length)throw new InvalidDataException("Motion bank has trailing data");
        }
    }
}
