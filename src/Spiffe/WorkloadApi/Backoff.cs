﻿namespace Spiffe.WorkloadApi;

internal class Backoff
{
    private volatile int _n;

    public TimeSpan InitialDelay { get; init; }

    public TimeSpan MaxDelay { get; init; }

    public static Backoff Create()
    {
        return new Backoff
        {
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            _n = 0,
        };
    }

    public TimeSpan Duration()
    {
        int backoff = _n + 1;
        int d = (int)Math.Min(InitialDelay.TotalSeconds * backoff, MaxDelay.TotalSeconds);
        Interlocked.Increment(ref _n);
        return TimeSpan.FromSeconds(d);
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _n, 0);
    }
}
