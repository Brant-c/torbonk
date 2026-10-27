using System;

namespace SwtorSurvivors;

/// <summary>Integer tick indices avoid accumulated interval drift and duplicate boundary ticks.</summary>
public sealed class ChannelClock
{
    public double Duration { get; private set; }
    public double Interval { get; private set; }
    public double Elapsed { get; private set; }
    public int Emitted { get; private set; }
    private double _offset;
    public void SetInterval(double interval)
    {
        if(!double.IsFinite(interval) || interval<=0)throw new ArgumentOutOfRangeException(nameof(interval));
        _offset=Elapsed+(_offset-Elapsed)*interval/Interval;Interval=interval;
    }
    public bool Complete => Elapsed + 1e-9 >= Duration;

    public void Reset(double duration, double interval)
    {
        if (!double.IsFinite(duration) || !double.IsFinite(interval) || duration <= 0 || interval <= 0)
            throw new ArgumentOutOfRangeException(nameof(duration), "Channel duration and interval must be positive and finite.");
        Duration = duration; Interval = interval; Elapsed = 0; Emitted = 0; _offset=0;
    }

    public void Advance(double delta, Func<bool> emit)
    {
        Elapsed = Math.Min(Duration, Elapsed + Math.Max(0, delta));
        // Resource timings originate as floats (e.g. 0.3 * 3 vs 0.9).
        // Allow their sub-microsecond representation error at the final tick.
        while ((Emitted + 1) * Interval + _offset <= Elapsed + 1e-6 * Math.Max(1,Duration))
        {
            Emitted++;
            if (!emit()) break;
        }
    }
}
