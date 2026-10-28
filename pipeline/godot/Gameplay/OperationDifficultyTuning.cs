using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SwtorSurvivors;

/// <summary>Multipliers relative to the authored Hard encounter. Applied once to fresh run data.</summary>
public sealed class OperationDifficultyTuning
{
    public float EnemyHealth {get;init;}=1;
    public float EnemyDamage {get;init;}=1;
    public float Population {get;init;}=1;
    public float SpawnInterval {get;init;}=1;
    public float BossAddCapFraction {get;init;}
    public float FinalBossAddCapFraction {get;init;}
    public float BossAddCadence {get;init;}
    public float FinalBossAddCadence {get;init;}
    public float MechanicRadius {get;init;}=1;
    public int MechanicsTier {get;init;}
    public float WarningTime {get;init;}=1;
    public float AttackInterval {get;init;}=1;
    public float RecoveryTime {get;init;}=1;
    public static readonly OperationDifficultyTuning Baseline=new();
    private static Dictionary<string,OperationDifficultyTuning>? _profiles;
    public static bool IsValid(string mode)=>mode is "Normal" or "Hard" or "Nightmare";
    public static OperationDifficultyTuning Resolve(string map,string mode)
    {
        if(map!="eternity_vault")return Baseline;
        if(!IsValid(mode))throw new ArgumentException("Unknown operation difficulty: "+mode);
        _profiles??=JsonSerializer.Deserialize<Dictionary<string,OperationDifficultyTuning>>(
            FileAccess.GetFileAsString("res://Gameplay/Planets/EternityVault/difficulties.json"))!;
        return _profiles[mode];
    }
    public void Apply(PlanetRunDefinition run)
    {
        var d=run.Difficulty;
        d.EnemyHealthMultiplier=EnemyHealth;d.EnemyDamageMultiplier=EnemyDamage;
        d.InitialPopulation=Math.Max(1,(int)Math.Round(d.InitialPopulation*Population));
        d.MaximumPopulation=Math.Max(1,(int)Math.Round(d.MaximumPopulation*Population));
        d.InitialSpawnInterval*=SpawnInterval;d.MinimumSpawnInterval*=SpawnInterval;
        d.RecoverySeconds*=RecoveryTime;d.ReturnRampSeconds*=RecoveryTime;
    }
}
