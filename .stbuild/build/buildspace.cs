using System;
using System.Collections.Generic;
using BuildSystem.Info;
using BuildSystem.Logging;
using BuildSystem.Builder.MsDelphi;
using BuildSystem.SettingsReader;
using BuildSystem.SettingsReader.Object;
using BuildSystem.Cleaner.Common;
using BuildSystem.HashGenerator.Common;
using BuildSystem.PackageManager.Nuget;
using BuildSystem.Restorer.Nuget;
using BuildSystem.VersionManager.Common;
using BuildSystem.Package;
using BuildSystem.Variants;
using BuildSystem.ProjectList.Common;
using BuildSystem.ProjectList.Helpers;
using BuildSystem.ManagerObject.Interfaces;
using BuildSystem.ProjectCache.NuGet;

/// <inheritdoc />
class BuildSpaceSettings : SettingsObject
{
    const StringComparison IGNCASE = StringComparison.CurrentCultureIgnoreCase;

    private string GitBranch => BuildInfo.JenkinsParam(JenkinsInfo.BranchName) + "";

    /// <inheritdoc />
    public BuildSpaceSettings(ILogger logger, string[] configFiles) : base() {
        var readerJson = new ReaderJson(logger);
        readerJson.ReadRules(configFiles);
        ReaderLocalVars = readerJson.LocalVars;
        ReaderDefines = readerJson.Defines;
        ProjectListProps = new ProjectListCommonProps(logger) {
            ProjectRestorerProps = new ProjectRestorerCommonProps() { 
                RestoreInsteadOfBuild = (p) => false 
            },
            DeployerProps = new DeployerCommonProps { 
                SamePackageVersions = true 
            },
            GetNextVersion = GetNextVersion.FromRemotePackages
        };
        Projects = BuildUtils.GetProjectsList(configFiles);
        RegisterBSObjects();
    }

    /// <summary>
    /// Register Build System control objects
    /// </summary>
    private void RegisterBSObjects() {
        Variants = new() {
            new() {
                Name = "Debug_x64",
                Configurations = new() { [Variant.NodeConfig] = "Debug" },
                Platforms =      new() { [Variant.NodePlatform] = "Win64", }
            },
            new() {
                Name = "Release_x64",
                Configurations = new() { [Variant.NodeConfig] = "Release" },
                Platforms =      new() { [Variant.NodePlatform] = "Win64", }
            },
            new() {
                Name = "Debug_x32",
                Configurations = new() { [Variant.NodeConfig] = "Debug" },
                Platforms =      new() { [Variant.NodePlatform] = "Win32", }
            },
            new() {
                Name = "Release_x32",
                Configurations = new() { [Variant.NodeConfig] = "Release" },
                Platforms =      new() { [Variant.NodePlatform] = "Win32", }
            }
        };

        // names in ManagerConstNames
        AddManagerProp("builder_delphi", new() {"Release_x64", "Release_x32"}, builderDelphiRelease);
        AddManagerProp("builder_delphi", new() {"Debug_x64", "Debug_x32"}, builderDelphiDebug);
        AddManagerProp("package_manager", null, packageManagerNuget);
        AddManagerProp("version_manager", null, versionManagerCommon);
        AddManagerProp("hash_generator", null, hashGeneratorCommon);
        AddManagerProp("restorer", null, restorerNuget);
        AddManagerProp("cleaner", null, cleanerCommon);
        AddManagerProp("cleaner_delphi", null, cleanerCommonDelphi);
        AddManagerProp("project_cache", null, projectCacheNuGet);
    }

    BuilderMsDelphiProps builderDelphiCommon => new() {
        Name = "builder_delphi_common",
        BuilderVersion = "23.0",
        MsBuilderPath = "C:/Windows/Microsoft.NET/Framework/v4.0.30319/MSBuild.exe",
        EnvBdsPath = "C:/Program files (x86)/embarcadero/studio/23.0",
        RsVarsPath = "C:/Program files (x86)/embarcadero/studio/23.0/bin/rsvars.bat",
        AutoClean = true,
        BuildParams = new Dictionary<string, string?>
        {
            ["-verbosity"] = "normal",
            ["-consoleloggerparameters"] = "ErrorsOnly",
            ["-nologo"] = "true",
            ["/t:build"] = "true",
            // ["/p:DCC_Warnings"] = "false",
            ["/p:DCC_Hints"] = "false",
            ["/p:DCC_MapFile"] = "3",
            ["/p:DCC_AssertionsAtRuntime"] = "true",
            ["/p:DCC_IOChecking"] = "true",
            ["/p:DCC_WriteableConstants"] = "true"
        }
    };

    BuilderMsDelphiProps builderDelphiRelease {
        get {
            var bdr = new BuilderMsDelphiProps(builderDelphiCommon); // inherited
            bdr.Name = "builder_delphi_release";
            bdr.BuildParams.Add("/p:DCC_Optimize", "true");
            bdr.BuildParams.Add("/p:DCC_GenerateStackFrames", "true");
            bdr.BuildParams.Add("/p:DCC_DebugInformation", "0");
            bdr.BuildParams.Add("/p:DCC_DebugDCUs", "false");
            bdr.BuildParams.Add("/p:DCC_LocalDebugSymbols", "false");
            bdr.BuildParams.Add("/p:DCC_SymbolReferenceInfo", "0");
            bdr.BuildParams.Add("/p:DCC_IntegerOverflowCheck", "false");
            bdr.BuildParams.Add("/p:DCC_RangeChecking", "false");
            return bdr;
        }
    }

    BuilderMsDelphiProps builderDelphiDebug {
        get {
            var bdd = new BuilderMsDelphiProps(builderDelphiCommon); // inherited
            bdd.Name = "builder_delphi_debug";
            bdd.BuildParams.Add("/p:DCC_Optimize", "false");
            bdd.BuildParams.Add("/p:DCC_GenerateStackFrames", "true");
            bdd.BuildParams.Add("/p:DCC_DebugInformation", "2");
            bdd.BuildParams.Add("/p:DCC_DebugDCUs", "true");
            bdd.BuildParams.Add("/p:DCC_LocalDebugSymbols", "true");
            bdd.BuildParams.Add("/p:DCC_SymbolReferenceInfo", "2");
            bdd.BuildParams.Add("/p:DCC_IntegerOverflowCheck", "true");
            bdd.BuildParams.Add("/p:DCC_RangeChecking", "true");
            return bdd;
        }
    }

    ProjectCacheNuGetProps projectCacheNuGet => new() {
        Name = "project_cache_nuget",
        VersionManagerProps = versionManagerCommon,
        PackageManagerProps = packageManagerNuget,
        TempDir = "./hash"
    };

    HashGeneratorCommonProps hashGeneratorCommon => new() {
        Name = "hash_generator_main",
        HashAlgorithmType = HashAlgorithmType.Sha256
    };

    CleanerCommonProps cleanerCommon => new() {
        Name = "cleaner_default_main",
        AllBuildResults = true
    };

    CleanerCommonProps cleanerCommonDelphi => new() {
        Name = "cleaner_delphi_main",
        AllBuildResults = true,
        Paths = new Dictionary<string, List<string>>
        {
            ["$project:output_dcu$"] = new() { "*.dcu" }
        }
    };

    VersionManagerCommonProps versionManagerCommon => new() {
        Name = "version_manager_common",
        DepthSearch = 2,
        PullRequestBranchPrefix = "c-",
        DevelopBranchName = GitBranch.EndsWith("develop", IGNCASE) ? GitBranch : "develop",
        MasterBranchName =  GitBranch.EndsWith("master", IGNCASE)  ? GitBranch : "master",
        ReleaseBranchName = GitBranch.EndsWith("release", IGNCASE) ? GitBranch : "release"
    };

    PackageManagerNugetProps packageManagerNuget => new() {
        Name = "package_manager_nuget_master",
        SetStorageInfo = SetStorageInfoFunc
    };

    private StorageInfo SetStorageInfoFunc(PackageAction packageAction, string packageId, VersionProp? packageVersion) {
        var isMaster = GitBranch.EndsWith("master", IGNCASE) 
            && packageAction != PackageAction.Reclaim && packageAction != PackageAction.Delete;
        var si = new StorageInfo() {
            Url = Environment.GetEnvironmentVariable(isMaster ? "NUGET_MASTER_REPO" : "NUGET_DEV_REPO"),
            ApiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY")
        };
        return si;
    }

    RestorerNugetProps restorerNuget => new()
    {
        Name = "restorer_main",
        DepsProp = new()
    };

}