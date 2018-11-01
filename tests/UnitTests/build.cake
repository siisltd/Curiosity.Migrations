var target = Argument("target", "Test");
var configuration = Argument("Configuration", "Release");

Task("Test")
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

RunTarget(target);
