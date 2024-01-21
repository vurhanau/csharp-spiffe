using FluentAssertions;
using Spiffe.WorkloadApi;

namespace Spiffe.Test.WorkloadApi;

public class TestBackoff
{
    private const int MinBackoff = 1;

    private const int MaxBackoff = 30;

    [Fact]
    public void TestUntilMax()
    {
        Backoff backoff = GetBackoff();

        IterateUntilMax(backoff);

        TimeSpan max = TimeSpan.FromSeconds(MaxBackoff);
        backoff.Duration().Should().Be(max);
        backoff.Duration().Should().Be(max);
        backoff.Duration().Should().Be(max);
    }

    [Fact]
    public async Task TestUntilMaxParallel()
    {
        Backoff backoff = GetBackoff();
        List<Task> tasks = [];
        for (int i = 0; i < MaxBackoff / 2; i++)
        {
            tasks.Add(Task.Run(backoff.Duration));
            tasks.Add(Task.Run(backoff.Duration));
        }

        await Task.WhenAll(tasks);

        backoff.Duration().Should().Be(TimeSpan.FromSeconds(MaxBackoff));
    }

    [Fact]
    public void TestReset()
    {
        Backoff backoff = GetBackoff();

        IterateUntilMax(backoff);

        backoff.Reset();

        IterateUntilMax(backoff);
    }

    private static Backoff GetBackoff()
    {
        return new()
        {
            InitialDelay = TimeSpan.FromSeconds(MinBackoff),
            MaxDelay = TimeSpan.FromSeconds(MaxBackoff),
        };
    }

    private static void IterateUntilMax(Backoff backoff)
    {
        for (int i = MinBackoff; i < MaxBackoff; i++)
        {
            backoff.Duration().Should().Be(TimeSpan.FromSeconds(i));
        }
    }
}
