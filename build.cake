#tool "nuget:?package=coveralls.io&version=1.4.2"
#addin Cake.Git
// #addin nuget:?package=Nuget.Core
#addin "nuget:?package=Cake.Coveralls&version=0.9.0"


#addin nuget:?package=Cake.Coverlet&version=2.1.2
#tool nuget:?package=ReportGenerator&version=4.0.4

// using NuGet;

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument<string>("target", "Default");
var configuration = Argument<string>("configuration", "Release");

var coverageResultsFileName = "coverage.xml";
// var currentBranch = Argument<string>("currentBranch", GitBranchCurrent("./").FriendlyName);
// var isReleaseBuild = string.Equals(currentBranch, "master", StringComparison.OrdinalIgnoreCase);
var nugetApiKey = Argument<string>("nugetApiKey", null);
var coverallsToken = Argument<string>("coverallsToken", null);
var nugetSource = "https://api.nuget.org/v3/index.json";

var artifactsDir = "./artifacts/";
var solutionPath = "./Marvin.Migrations.sln";
var framework = "netstandard2.0";

/*  Change the output artifacts and their configuration here. */
var parentDirectory = Directory("..");
var coverageDirectory = parentDirectory + Directory("code_coverage");
var cuberturaFileName = "results";
var cuberturaFileExtension = ".cobertura.xml";
var reportTypes = "HtmlInline_AzurePipelines"; // Use "Html" value locally for performance and files' size.
var coverageFilePath = coverageDirectory + File(cuberturaFileName + cuberturaFileExtension);
var jsonFilePath = coverageDirectory + File(cuberturaFileName + ".json");

Task("Clean")
    .Does(() => 
    {            
        DotNetCoreClean(solutionPath);        
        DirectoryPath[] cleanDirectories = new DirectoryPath[] {
            artifactsDir
        };
    
        CleanDirectories(cleanDirectories);
    
        foreach(var path in cleanDirectories) { EnsureDirectoryExists(path); }
    
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does(() => 
    {
        var settings = new DotNetCoreBuildSettings
          {
              Configuration = configuration
          };
          
        DotNetCoreBuild(
            solutionPath,
            settings);
    });

Task("UnitTests")
    .IsDependentOn("Build")
    .Does(() =>
    {        
        Information("UnitTests task...");
        var projects = GetFiles("./tests/UnitTests/**/*csproj");
        foreach(var project in projects)
        {
            Information(project);
            
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = false,
                    ArgumentCustomization = args => args
                        .Append("/p:CollectCoverage=true")
                        .Append("/p:CoverletOutputFormat=opencover")
                });
                
//             MoveFile(project.GetDirectory().FullPath + coverageResultsFileName, artifactsDir + coverageResultsFileName);
        }
    });
     
Task("IntegrationTests")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .Does(() =>
    {        
        Information("IntegrationTests task...");
		
        Information("Running docker...");
        StartProcess("docker-compose", "-f ./tests/IntegrationTests/env-compose.yml up -d");
		Information("Running docker completed");
		
        var projects = GetFiles("./tests/IntegrationTests/**/*csproj");
        foreach(var project in projects)
        {
            Information(project);
            
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = false
                });
        }
    })
    .Finally(() =>
    {  
        Information("Stopping docker...");
        StartProcess("docker-compose", "-f ./tests/IntegrationTests/env-compose.yml down");
        Information("Stopping docker completed");
    });  
     
Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("UnitTests")
    .IsDependentOn("IntegrationTests");
    
RunTarget(target);
