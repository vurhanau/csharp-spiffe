using System.Net;
using System.Net.NetworkInformation;
using CliWrap;
using CliWrap.EventStream;
using Xunit.Abstractions;

namespace Spiffe.Tests.Integration;

internal class TestServer
{
    private const int StartingPort = 5000;

    private const string TestServerStartedLog = "Application started";

    private readonly ITestOutputHelper _output;

    internal TestServer(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetTestServerRoot()
    {
        string solutionName = "csharp-spiffe";
        string dir = Environment.CurrentDirectory;
        int pos = dir.LastIndexOf(solutionName);
        if (pos < 0)
        {
            throw new Exception("Solution name is not found in path");
        }

        string solutionRoot = dir.Substring(0, pos + solutionName.Length);
        string serverRoot = Path.Join(solutionRoot, "tests", "Spiffe.Tests.Server");
        return serverRoot;
    }

    internal static int GetAvailablePort()
    {
        List<int> portArray = [];

        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();

        // Ignore active connections
        TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
        portArray.AddRange(from n in connections
                            where n.LocalEndPoint.Port >= StartingPort
                            select n.LocalEndPoint.Port);

        // Ignore active tcp listners
        IPEndPoint[] endPoints = properties.GetActiveTcpListeners();
        portArray.AddRange(from n in endPoints
                            where n.Port >= StartingPort
                            select n.Port);

        // Ignore active UDP listeners
        endPoints = properties.GetActiveUdpListeners();
        portArray.AddRange(from n in endPoints
                            where n.Port >= StartingPort
                            select n.Port);

        portArray.Sort();

        Random random = new();
        int offset = random.Next(1, 101);
        for (int i = StartingPort + offset; i < ushort.MaxValue; i++)
        {
            if (!portArray.Contains(i))
            {
                return i;
            }
        }

        return 0;
    }

    internal async Task<Task> ListenAsync(string address, CancellationToken cancellationToken)
    {
        bool started = false;
        Task t = Task.Factory.StartNew(async () =>
        {
            string testServerRoot = GetTestServerRoot();
            string framework = GetTargetFramework();
            _output.WriteLine($"Test server root: {testServerRoot}");
            _output.WriteLine($"Test server address: {address}");
            _output.WriteLine($"Test server dotnet framework: {framework}");
            Command cmd = Cli.Wrap("dotnet")
                            .WithArguments(["run", address, "--framework", framework])
                            .WithWorkingDirectory(testServerRoot);
            await foreach (CommandEvent e in cmd.ListenAsync(cancellationToken))
            {
                switch (e)
                {
                    case StartedCommandEvent started:
                        _output.WriteLine($"Process started; ID: {started.ProcessId}");
                        break;
                    case StandardOutputCommandEvent stdOut:
                        _output.WriteLine($"Out> {stdOut.Text}");
                        if (stdOut.Text.Contains(TestServerStartedLog))
                        {
                            started = true;
                        }

                        break;
                    case StandardErrorCommandEvent stdErr:
                        _output.WriteLine($"Err> {stdErr.Text}");
                        break;
                    case ExitedCommandEvent exited:
                        _output.WriteLine($"Process exited; Code: {exited.ExitCode}");
                        break;
                }
            }
        });

        while (!started && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100);
        }

        return t;
    }

    private static string GetTargetFramework()
    {
#if NET9_0_OR_GREATER
        return "net9.0";
#else
        return "net8.0";
#endif
    }
}
