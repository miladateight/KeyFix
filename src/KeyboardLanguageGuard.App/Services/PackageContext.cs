using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// Tells the app whether it is running from an MSIX package (the Microsoft Store
/// build) or as an ordinary installed executable.
///
/// The distinction matters in two places where the packaged build must behave
/// differently, not merely look different: startup registration, which cannot go
/// through the Run registry key inside a package, and update checking, which the
/// Store owns.
/// </summary>
public static class PackageContext
{
    // GetCurrentPackageFullName returns this when the process has no package
    // identity, which is the normal case for the installer build.
    private const int AppModelErrorNoPackage = 15700;
    private const int ErrorInsufficientBuffer = 122;

    private static readonly Lazy<bool> Packaged = new(DetectPackage);

    /// <summary>True when the process is running with MSIX package identity.</summary>
    public static bool IsPackaged => Packaged.Value;

    private static bool DetectPackage()
    {
        try
        {
            int length = 0;
            int result = GetCurrentPackageFullName(ref length, null);

            // A package identity always needs a buffer; no identity is reported
            // outright. Anything else is treated as "not packaged" rather than
            // guessed at.
            return result == ErrorInsufficientBuffer ||
                   (result != AppModelErrorNoPackage && length > 0);
        }
        catch (EntryPointNotFoundException)
        {
            // Pre-Windows-8 API surface. Not a platform this build supports, but
            // an absent entry point is not a reason to fail to start.
            return false;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder? packageFullName);
}
