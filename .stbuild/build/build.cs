using System;
using System.IO;
using Nuke.Common;
using BuildSystem.BuildSpace;
using BuildSystem.BuildSpace.Common;
using BuildSystem.Info;
using BuildSystem.Loggers;
using BuildSystem.Logging;
using BuildSystem.SettingsReader;
using LoggingLevel = BuildSystem.Logging.LogLevel;

/// <inheritdoc />
public class Build : NukeBuild
{
    /// <summary>
    /// Calling target by default
    /// </summary>
    public static int Main() => Execute<Build>(x => x.Compile);
    
    /// <summary>
    /// Configuration to build - 'Debug' (default) or 'Release'
    /// </summary>
    [Parameter("Settings provided for running build space")]
    public readonly string Variant = "Debug_x64";

    /// <summary> Logging level </summary>
    [Parameter("Logging level")]
    public readonly string LogLevel = "info";

    /// <summary>
    /// Force build of projects
    /// </summary>
    [Parameter("Force build of projects")]
    public readonly string ForceBuild = "false";

    /// <summary> Logging </summary>
    public static ILogger Logger = new LoggerConsole();

    private IBuildSpace? _buildSpace;
    private IBuildSpace BSpace => _buildSpace ??= InitBuildSpace();

    private IBuildSpace InitBuildSpace() {
        var localJsonFile = Path.Combine(RootDirectory, $"buildspace.local.json");
        var bsJsonFile = Path.Combine(RootDirectory, "buildspace.json");
        var config = new BuildSpaceSettings(Logger, new[] { bsJsonFile, localJsonFile });
        return new BuildSpaceCommon(Logger, RootDirectory + "//temp", SettingsReaderType.Object, config);
    }

    private LoggingLevel _logLevel => LogLevel.ToLower() switch {
        "debug"   => LoggingLevel.debug,
        "verbose" => LoggingLevel.verbose,
        "head"    => LoggingLevel.head,
        _         => LoggingLevel.info
    };

    /// <summary> 
    /// Set build constants 
    /// </summary>
    private Target SetBuildInfo => _ => _
        .Executes(() => {
            Logger.setMinLevel(_logLevel);
            Logger.debug("CommandLine: " + Environment.CommandLine);
            BuildInfo.RunParams[RunInfo.Variant] = Variant;
            BuildInfo.RunParams[RunInfo.ForceBuild] = ForceBuild;
            BuildInfo.JenkinsParams[JenkinsInfo.BranchName] = "master"; // current branch name
        });

    /// <summary>
    /// Restore nuget dependecies
    /// </summary>
    private Target Restore => _ => _
        .DependsOn(SetBuildInfo)
        .Executes(() =>
        {
            BSpace.Projects.Restore(Variant);
        });

    /// <summary>
    /// Parameterized compile
    /// </summary>
    private Target Compile => _ => _
        .DependsOn(SetBuildInfo)
        .Executes(() =>
        {
            BSpace.Projects.Compile(Variant, true);
        });

    /// <summary>
    /// Compile all projects in Release
    /// </summary>
    private Target CompileAllRelease => _ => _
        .DependsOn(SetBuildInfo)
        .Executes(() =>
        {
            BSpace.Projects.Compile("Release_x32", true);
            BSpace.Projects.Compile("Release_x64", true);
        });

    /// <summary>
    /// Publishing packages
    /// </summary>
    private Target Deploy => _ => _
        .DependsOn(SetBuildInfo, CompileAllRelease)
        .Executes(() =>
        {
            BSpace.Projects.Deploy(Variant, true); // true - only create packages
        });

    /// <summary>
    /// Parameterized clean
    /// </summary>
    private Target Clean => _ => _
        .DependsOn(SetBuildInfo)
        .Executes(() =>
        {
            BSpace.Projects.Clean(Variant);
        });

}