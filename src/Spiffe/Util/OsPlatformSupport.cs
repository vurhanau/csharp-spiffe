using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Spiffe.Grpc;

internal static class OSPlatformSupport
{
    [ExcludeFromCodeCoverage(Justification = "OS platform detection varies by runtime environment and cannot be reliably tested in all scenarios")]
    public static OSPlatform CurrentPlatform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system platform.");
            }
        }
    }
}
