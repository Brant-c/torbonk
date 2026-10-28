using Godot;
using System.Collections.Generic;
namespace SwtorSurvivors;
public static class AudioPreferences
{
    private static readonly ConfigFile Config = new();
    private static readonly Dictionary<string,float> Values = new();
    public static float Get(string category) => Values.GetValueOrDefault(category,1);
    public static void Load()
    {
        Config.Load("user://audio.cfg");
        foreach(var category in new[]{"Master","Music","World","Combat","UI","Reward","Boss"})
        {
            Values[category]=Mathf.Clamp((float)Config.GetValue("volume",category,1f),0,1);
            string name=category=="Master"?category:"B7"+category;
            if(AudioServer.GetBusIndex(name)<0){AudioServer.AddBus();AudioServer.SetBusName(AudioServer.BusCount-1,name);}
            Apply(category);
        }
    }
    public static float Offset(string category) => Mathf.LinearToDb(Mathf.Max(Get(category),0.0001f));
    private static void Apply(string category)
    {
        int bus=AudioServer.GetBusIndex(category=="Master"?category:"B7"+category);
        if(bus<0)return;
        AudioServer.SetBusMute(bus,Get(category)==0);
        AudioServer.SetBusVolumeDb(bus,Offset(category));
    }
    public static void Set(string category,float value)
    {
        Values[category]=Mathf.Clamp(value,0,1);Apply(category);
        Config.SetValue("volume",category,Values[category]);Config.Save("user://audio.cfg");
    }
}
