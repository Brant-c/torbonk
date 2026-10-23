using Godot;
using System;
using System.Collections.Generic;

namespace SwtorSurvivors;

/// <summary>Bounded route verification over actual terrain and collision, no runtime navigation baking.</summary>
public static class PlanetRouteProbe
{
    public static bool Reach(ArenaNavigation nav,Vector3 start,Vector3 goal,out int examined)
    {
        const float step=.3f;
        var open=new PriorityQueue<(Vector2I key,Vector3 feet,float cost),float>();
        var visited=new HashSet<Vector2I>();open.Enqueue((Vector2I.Zero,start,0),0);examined=0;
        while(open.TryDequeue(out var next,out _) && examined<700)
        {
            if(!visited.Add(next.key))continue;examined++;
            if(next.feet.DistanceTo(goal)<.5f && nav.CanWalk(next.feet,goal))return true;
            for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
            {
                if(x==0 && z==0)continue;var key=next.key+new Vector2I(x,z);if(visited.Contains(key))continue;
                var seed=new Vector3(start.X+key.X*step,next.feet.Y,start.Z+key.Y*step);
                if(!nav.TryStandingPoint(seed,out var p) || !nav.CanWalk(next.feet,p))continue;
                float cost=next.cost+next.feet.DistanceTo(p);open.Enqueue((key,p,cost),cost+p.DistanceTo(goal)*1.15f);
            }
        }
        return false;
    }
}
