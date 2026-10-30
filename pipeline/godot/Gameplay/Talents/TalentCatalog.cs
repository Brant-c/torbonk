using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SwtorSurvivors.Talents;

public sealed class TalentEffect
{
    public string Kind {get;set;}="";
    public string Ability {get;set;}="";
    public string Key {get;set;}="";
    public float Amount {get;set;}
}
public sealed class TalentRequirement
{
    public string Id {get;set;}="";
    public int Rank {get;set;}=1;
}
public sealed class TalentDefinition
{
    public string Id {get;set;}="";
    public string[] Classes {get;set;}=[];
    public string Tree {get;set;}="";
    public int Tier {get;set;}
    public int Column {get;set;}
    public string Icon {get;set;}="";
    public string Name {get;set;}="";
    public string Description {get;set;}="";
    public int MaxRank {get;set;}=1;
    public int Cost {get;set;}=1;
    public int RequiredPoints {get;set;}
    public bool Available {get;set;}=true;
    public TalentRequirement[] Prerequisites {get;set;}=[];
    public TalentEffect[] Effects {get;set;}=[];
    public string Source {get;set;}="";
}
public sealed class TalentTree
{
    public string Id {get;set;}="";
    public string Name {get;set;}="";
}
public sealed class TalentCatalog
{
    public int Version {get;set;}=1;
    public int Budget {get;set;}=12;
    public TalentTree[] Trees {get;set;}=[];
    public TalentDefinition[] Talents {get;set;}=[];
    private static TalentCatalog? _current;
    public static TalentCatalog Current => _current??=Load();
    private static TalentCatalog Load()
    {
        var catalog=JsonSerializer.Deserialize<TalentCatalog>(FileAccess.GetFileAsString("res://Gameplay/Talents/sorcerer.json"))!;
        var assassin=JsonSerializer.Deserialize<TalentCatalog>(FileAccess.GetFileAsString("res://Gameplay/Talents/assassin.json"))!;
        catalog.Trees=catalog.Trees.Concat(assassin.Trees).ToArray();
        catalog.Talents=catalog.Talents.Concat(assassin.Talents).ToArray();
        var juggernaut=JsonSerializer.Deserialize<TalentCatalog>(FileAccess.GetFileAsString("res://Gameplay/Talents/juggernaut.json"))!;
        catalog.Trees=catalog.Trees.Concat(juggernaut.Trees).ToArray();
        catalog.Talents=catalog.Talents.Concat(juggernaut.Talents).ToArray();
        var marauder=JsonSerializer.Deserialize<TalentCatalog>(FileAccess.GetFileAsString("res://Gameplay/Talents/marauder.json"))!;
        catalog.Trees=catalog.Trees.Concat(marauder.Trees).ToArray();
        catalog.Talents=catalog.Talents.Concat(marauder.Talents).ToArray();
        var agent=JsonSerializer.Deserialize<TalentCatalog>(FileAccess.GetFileAsString("res://Gameplay/Talents/agent.json"))!;
        catalog.Trees=catalog.Trees.Concat(agent.Trees).ToArray();
        catalog.Talents=catalog.Talents.Concat(agent.Talents).ToArray();
        var bountyHunter=JsonSerializer.Deserialize<TalentCatalog>(FileAccess.GetFileAsString("res://Gameplay/Talents/bounty_hunter.json"))!;
        catalog.Trees=catalog.Trees.Concat(bountyHunter.Trees).ToArray();
        catalog.Talents=catalog.Talents.Concat(bountyHunter.Talents).ToArray();
        if(catalog.Budget<0 || catalog.Talents.Select(t=>t.Id).Distinct().Count()!=catalog.Talents.Length ||
            catalog.Talents.Any(t=>t.Cost<1 || t.MaxRank<1 || t.RequiredPoints<0 || t.Tier<0 ||
                !catalog.Trees.Any(tree=>tree.Id==t.Tree) || t.Prerequisites.Any(p=>p.Rank<1 ||
                    !catalog.Talents.Any(a=>a.Id==p.Id && a.Tier<t.Tier && a.Tree==t.Tree && a.MaxRank>=p.Rank))))
            throw new InvalidOperationException("Invalid talent catalog");
        return catalog;
    }
    public TalentDefinition[] ForClass(string classId)=>Talents.Where(t=>t.Classes.Contains(classId)).ToArray();
    public int BudgetFor(string classId)=>classId is "operative" or "sniper"?10:Budget;
    public int Spent(IReadOnlyDictionary<string,int> ranks)=>Talents.Sum(t=>ranks.GetValueOrDefault(t.Id)*t.Cost);
    public int TreeSpent(string tree,IReadOnlyDictionary<string,int> ranks,int belowTier=int.MaxValue)=>
        Talents.Where(t=>t.Tree==tree && t.Tier<belowTier).Sum(t=>ranks.GetValueOrDefault(t.Id)*t.Cost);
    public string? Locked(string classId,TalentDefinition t,IReadOnlyDictionary<string,int> ranks,bool budget=true)
    {
        if(!t.Available || !t.Classes.Contains(classId))return "Unavailable for this combat style";
        if(ranks.GetValueOrDefault(t.Id)>=t.MaxRank)return "Maximum rank";
        if(TreeSpent(t.Tree,ranks,t.Tier)<t.RequiredPoints)return $"Requires {t.RequiredPoints} points in earlier tiers";
        foreach(var p in t.Prerequisites)
            if(ranks.GetValueOrDefault(p.Id)<p.Rank)return $"Requires {Talents.First(a=>a.Id==p.Id).Name} ({p.Rank})";
        return budget && Spent(ranks)+t.Cost>BudgetFor(classId)?"No points available":null;
    }
    // Replay ranks in dependency order. Unknown IDs, negative ranks, invalid prerequisites and excess
    // points are discarded deterministically; later tiers cannot fund their own unlock requirement.
    public Dictionary<string,int> Validate(string classId,IReadOnlyDictionary<string,int>? saved)
    {
        var result=new Dictionary<string,int>();
        if(saved==null)return result;
        // Old Sniper profiles used Operative's shared Lethality IDs. Preserve the
        // chosen positions without importing those effects into the new Sniper tree.
        if(classId=="sniper") {
            var migrated=new Dictionary<string,int>(saved);
            foreach(var pair in saved.Where(p=>p.Key.StartsWith("agent_lethality.",StringComparison.Ordinal))) {
                string id="sniper_lethality."+pair.Key.Split('.',2)[1];
                if(!migrated.ContainsKey(id))migrated[id]=pair.Value;
            }
            saved=migrated;
        }
        foreach(var t in ForClass(classId).OrderBy(t=>t.Tier).ThenBy(t=>t.Id))
            for(int rank=0;rank<Math.Clamp(saved.GetValueOrDefault(t.Id),0,t.MaxRank);rank++)
            {if(Locked(classId,t,result)!=null)break;result[t.Id]=rank+1;}
        return result;
    }
}
