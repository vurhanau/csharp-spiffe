using FluentAssertions;
using Spiffe.WorkloadApi;

namespace Spiffe.Tests.WorkloadApi;

public class TestWatcher
{
    [Fact]
    public void TestConstructorFails()
    {
        Action f = () => new Watcher<int>(null, e => { });
        f.Should().Throw<ArgumentNullException>().WithParameterName("onUpdate");

        f = () => new Watcher<int>(_ => { }, null);
        f.Should().Throw<ArgumentNullException>().WithParameterName("onError");
    }
}
