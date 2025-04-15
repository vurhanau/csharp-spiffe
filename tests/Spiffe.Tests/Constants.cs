namespace Spiffe.Tests;

internal static class Constants
{
#if OS_WINDOWS
    public const int TestTimeoutMillis = 30_000;

    public const int IntegrationTestTimeoutMillis = 60_000;
#else
    public const int TestTimeoutMillis = 10_000;

    public const int IntegrationTestTimeoutMillis = 30_000;
#endif

    public const int IntegrationTestRetriesMax = 5;

    public const string Integration = "Integration";
}
