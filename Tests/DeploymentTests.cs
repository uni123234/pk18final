using System.Text.Json;
using System.Xml.Linq;

namespace Tests;

/// <summary>
/// Unit tests verifying Practical Work 17 Deployment and Application Packaging:
/// - Single-file and Self-Contained publish profile specifications
/// - Assembly metadata, configuration integrity, and deployment manifest validation
/// - Installation and launcher script verification
/// </summary>
public class DeploymentTests
{
    private static readonly string ProjectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void ProjectCsproj_DefinesAssemblyMetadataAndReleaseSettings()
    {
        // Arrange
        var csprojPath = Path.Combine(ProjectRoot, "Project.csproj");
        Assert.True(File.Exists(csprojPath), $"Project.csproj not found at {csprojPath}");

        // Act
        var doc = XDocument.Load(csprojPath);
        var root = doc.Root!;

        // Assert
        var assemblyName = root.Descendants("AssemblyName").FirstOrDefault()?.Value;
        var targetFramework = root.Descendants("TargetFramework").FirstOrDefault()?.Value;
        var useWpf = root.Descendants("UseWPF").FirstOrDefault()?.Value;

        Assert.Equal("ModularRpgApp", assemblyName);
        Assert.Equal("net8.0-windows", targetFramework);
        Assert.Equal("true", useWpf);
    }

    [Fact]
    public void StandalonePublishProfile_ConfiguredForSelfContainedWinX64()
    {
        // Arrange
        var profilePath = Path.Combine(ProjectRoot, "Properties", "PublishProfiles", "win-x64-standalone.pubxml");
        Assert.True(File.Exists(profilePath), $"Standalone publish profile missing: {profilePath}");

        // Act
        var doc = XDocument.Load(profilePath);
        var root = doc.Root!;

        // Assert
        var selfContained = root.Descendants("SelfContained").FirstOrDefault()?.Value;
        var singleFile = root.Descendants("PublishSingleFile").FirstOrDefault()?.Value;
        var runtimeId = root.Descendants("RuntimeIdentifier").FirstOrDefault()?.Value;

        Assert.Equal("true", selfContained);
        Assert.Equal("true", singleFile);
        Assert.Equal("win-x64", runtimeId);
    }

    [Fact]
    public void PortablePublishProfile_ConfiguredForFrameworkDependentWinX64()
    {
        // Arrange
        var profilePath = Path.Combine(ProjectRoot, "Properties", "PublishProfiles", "win-x64-portable.pubxml");
        Assert.True(File.Exists(profilePath), $"Portable publish profile missing: {profilePath}");

        // Act
        var doc = XDocument.Load(profilePath);
        var root = doc.Root!;

        // Assert
        var selfContained = root.Descendants("SelfContained").FirstOrDefault()?.Value;
        var singleFile = root.Descendants("PublishSingleFile").FirstOrDefault()?.Value;
        var runtimeId = root.Descendants("RuntimeIdentifier").FirstOrDefault()?.Value;

        Assert.Equal("false", selfContained);
        Assert.Equal("true", singleFile);
        Assert.Equal("win-x64", runtimeId);
    }

    [Fact]
    public void DeploymentManifest_IsValidJsonAndContainsCoreComponents()
    {
        // Arrange
        var manifestPath = Path.Combine(ProjectRoot, "deploy", "manifest.json");
        Assert.True(File.Exists(manifestPath));

        // Act
        var json = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.Equal("ModularRpgPlatform-Distribution", root.GetProperty("package").GetString());
        Assert.Equal("1.0.0", root.GetProperty("version").GetString());
        Assert.Equal("win-x64", root.GetProperty("targetPlatform").GetString());

        var components = root.GetProperty("components");
        Assert.True(components.GetArrayLength() >= 4);
    }

    [Fact]
    public void AppSettings_ContainsValidProductionSettings()
    {
        // Arrange
        var settingsPath = Path.Combine(ProjectRoot, "deploy", "appsettings.json");
        Assert.True(File.Exists(settingsPath));

        // Act
        var json = File.ReadAllText(settingsPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        var appName = root.GetProperty("Application").GetProperty("Name").GetString();
        var storage = root.GetProperty("Storage").GetProperty("AppDataFolder").GetString();

        Assert.Contains("Modular RPG", appName);
        Assert.Equal("ModularRpgPlatform", storage);
    }

    [Fact]
    public void LauncherAndInstallScripts_ExistAndContainRequiredCommands()
    {
        // Arrange
        var buildBat = Path.Combine(ProjectRoot, "build.bat");
        var publishBat = Path.Combine(ProjectRoot, "publish.bat");
        var installBat = Path.Combine(ProjectRoot, "install.bat");
        var runBat = Path.Combine(ProjectRoot, "run.bat");
        var runCliBat = Path.Combine(ProjectRoot, "run_cli.bat");
        var uninstallBat = Path.Combine(ProjectRoot, "uninstall.bat");

        // Assert all scripts exist
        Assert.True(File.Exists(buildBat));
        Assert.True(File.Exists(publishBat));
        Assert.True(File.Exists(installBat));
        Assert.True(File.Exists(runBat));
        Assert.True(File.Exists(runCliBat));
        Assert.True(File.Exists(uninstallBat));

        // Assert content assertions
        Assert.Contains("dotnet build", File.ReadAllText(buildBat));
        Assert.Contains("dotnet publish", File.ReadAllText(publishBat));
        Assert.Contains("LOCALAPPDATA", File.ReadAllText(installBat));
        Assert.Contains("ModularRpgApp.exe", File.ReadAllText(runBat));
        Assert.Contains("--cli", File.ReadAllText(runCliBat));
    }
}
