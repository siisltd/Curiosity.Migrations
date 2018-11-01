var target = Argument("target", "Default");
var configuration = Argument("Configuration", "Release");

Task("Prepare")
    .Does(() => 
    {
        StartProcess("docker-compose", "-f env-compose.yml up -d");
    });

Task("Test")
    .IsDependentOn("Prepare")
    .Does(() =>
    {        
        var projects = GetFiles("./**/*csproj");
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
    });
    
Task("CleanUp")
    .Does(() => 
        {
            StartProcess("docker-compose", "-f env-compose.yml down");
        });
    
Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("CleanUp");

RunTarget(target);