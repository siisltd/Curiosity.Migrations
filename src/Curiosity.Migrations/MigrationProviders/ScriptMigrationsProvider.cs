using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations;

/// <summary>
/// Provide migrations that uses raw sql scripts from specified directories
/// </summary>
public class ScriptMigrationsProvider : IMigrationsProvider
{
    private const ScriptIncorrectNamingAction DefaultScriptIncorrectNamingAction = ScriptIncorrectNamingAction.LogToWarn;

    private readonly Dictionary<string, ScriptParsingOptions> _absoluteDirectoriesPathParsingOptions;
    private readonly Dictionary<Assembly, ScriptParsingOptions> _assembliesParsingOptions;

    /// <inheritdoc cref="ScriptMigrationsProvider"/>
    public ScriptMigrationsProvider()
    {
        // usually only one item will be added
        _absoluteDirectoriesPathParsingOptions = new Dictionary<string, ScriptParsingOptions>(1);
        // usually only one item will be added
        _assembliesParsingOptions = new Dictionary<Assembly, ScriptParsingOptions>(1);
    }

    /// <summary>
    /// Setup directory to scan for migrations
    /// </summary>
    /// <param name="path">Path to directory where scripts are located. Can be relative and absolute.</param>
    /// <param name="prefix">Specific part of name or path. If no prefix specified provider will process only files without any prefix</param>
    /// <param name="scriptIncorrectNamingAction">What should we do if found script file with incorrect naming?</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ScriptMigrationsProvider FromDirectory(
        string path,
        string? prefix = null,
        ScriptIncorrectNamingAction scriptIncorrectNamingAction = DefaultScriptIncorrectNamingAction)
    {
        Guard.AssertNotEmpty(path, nameof(path));

        var innerPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(Directory.GetCurrentDirectory(), path);

        _absoluteDirectoriesPathParsingOptions[innerPath] = new ScriptParsingOptions(prefix, scriptIncorrectNamingAction);

        return this;
    }

    /// <summary>
    /// Setup assembly where script migrations embedded
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="prefix">Specific part of name or namespace part. If no prefix specified provider will process only files without any prefix</param>
    /// <param name="scriptIncorrectNamingAction">What should we do if found script file with incorrect naming?</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public ScriptMigrationsProvider FromAssembly(
        Assembly assembly,
        string? prefix = null,
        ScriptIncorrectNamingAction scriptIncorrectNamingAction = DefaultScriptIncorrectNamingAction)
    {
        Guard.AssertNotNull(assembly, nameof(assembly));

        _assembliesParsingOptions[assembly] = new ScriptParsingOptions(prefix, scriptIncorrectNamingAction);

        return this;
    }

    /// <inheritdoc />
    public ICollection<IMigration> GetMigrations(
        IMigrationConnection migrationConnection,
        IReadOnlyDictionary<string, string> variables,
        ILogger? migrationLogger)
    {
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotNull(variables, nameof(variables));

        if (_absoluteDirectoriesPathParsingOptions.Count == 0 && _assembliesParsingOptions.Count == 0)
            throw new InvalidOperationException(
                $"No directories or assemblies specified. First use method {nameof(FromDirectory)} or {nameof(FromAssembly)}");

        var migrations = new List<IMigration>();
        foreach (var keyValuePair in _absoluteDirectoriesPathParsingOptions)
        {
            var directoryPath = keyValuePair.Key;
            var parsingOptions = keyValuePair.Value;

            if (String.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new ArgumentException($"Directory \"{directoryPath}\" does not exists");

            var fileNames = Directory.GetFiles(directoryPath);

            var directoryMigrations = GetMigrations(
                fileNames,
                File.ReadAllText,
                migrationConnection,
                parsingOptions,
                variables,
                migrationLogger);

            if (directoryMigrations.Count == 0) continue;

            migrations.AddRange(directoryMigrations);
        }

        foreach (var keyValuePair in _assembliesParsingOptions)
        {
            var assembly = keyValuePair.Key;
            var scriptParsingOptions = keyValuePair.Value;

            var resourceFileNames = assembly.GetManifestResourceNames();

            var assemblyMigrations = GetMigrations(
                resourceFileNames,
                resourceName =>
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null) throw new InvalidOperationException($"Can't open a stream for resource \"{resourceName}\"");

                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                },
                migrationConnection,
                scriptParsingOptions,
                variables,
                migrationLogger);

            if (assemblyMigrations.Count == 0) continue;

            migrations.AddRange(assemblyMigrations);
        }

        return migrations.OrderBy(x => x.Version).ToArray();
    }

    private ICollection<IMigration> GetMigrations(
        IReadOnlyList<string> fileNames,
        Func<string, string> sqlScriptReadFunc,
        IMigrationConnection migrationConnection,
        ScriptParsingOptions scriptParsingOptions,
        IReadOnlyDictionary<string, string> variables,
        ILogger? migrationLogger)
    {
        Guard.AssertNotNull(fileNames, nameof(fileNames));
        Guard.AssertNotNull(sqlScriptReadFunc, nameof(sqlScriptReadFunc));
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotNull(variables, nameof(variables));

        var scripts = new Dictionary<MigrationVersion, MigrationScriptInfo>();

        var regex = new Regex(MigrationConstants.MigrationFileNamePattern, RegexOptions.IgnoreCase);

        for (var i = 0; i < fileNames.Count; i++)
        {
            var fileName = fileNames[i];

            if (fileName.ToLower().EndsWith("sql"))
            {
                if (!String.IsNullOrWhiteSpace(scriptParsingOptions.Prefix) && !fileName.StartsWith(scriptParsingOptions.Prefix))
                {
                    migrationLogger?.LogTrace($"\"{fileName}\" skipped because of incorrect prefix. Prefix \"{scriptParsingOptions.Prefix}\" is expected");
                    continue;
                }

                var match = regex.Match(fileName);
                if (!match.Success)
                {
                    var message = $"\"{fileName}\" has incorrect name for script migration. File must matches this regex pattern - \"{MigrationConstants.MigrationFileNamePattern}\"";
                    switch (scriptParsingOptions.ScriptIncorrectNamingAction)
                    {
                        case ScriptIncorrectNamingAction.Ignore:
                            migrationLogger?.LogTrace(message);
                            break;
                        case ScriptIncorrectNamingAction.LogToWarn:
                            migrationLogger?.LogWarning(message);
                            break;
                        case ScriptIncorrectNamingAction.LogToError:
                            migrationLogger?.LogError(message);
                            break;
                        case ScriptIncorrectNamingAction.ThrowException:
                            throw new MigrationException(MigrationErrorCode.IncorrectMigrationFileName, message);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    continue;
                }

                if (!MigrationVersion.TryParse(match.Groups[1].Value, out var version))
                {
                    migrationLogger?.LogWarning($"\"{fileName}\" has incorrect version migration.");
                    continue;
                }

                if (!scripts.ContainsKey(version))
                {
                    scripts[version] = new MigrationScriptInfo();
                }

                var scriptInfo = scripts[version];

                var script = sqlScriptReadFunc.Invoke(fileName);

                // extract options for current migration
                scriptInfo.Options = ExtractMigrationOptions(script);

                // split into batches
                var batches = new List<ScriptMigrationBatch>();
                var batchNameRegex = new Regex(@"--\s*BATCH:\s*(.*)\s*\n(.*)", RegexOptions.IgnoreCase);
                var batchIndex = 0;

                // Use positive lookahead to split script into batches.
                foreach (var batch in Regex.Split(script, @"(?=--\s*BATCH:)"))
                {
                    if (String.IsNullOrWhiteSpace(batch)) continue;

                    var batchNameMatch = batchNameRegex.Match(batch);
                    batches.Add(new ScriptMigrationBatch(
                        batchIndex++,
                        batchNameMatch.Success ? batchNameMatch.Groups[1].Value : null,
                        batch));
                }

                if (match.Groups[6].Success)
                {
                    if (scriptInfo.DownScript.Count > 0)
                        throw new InvalidOperationException(
                            $"There is more than one downgrade script with version {version}");

                    scriptInfo.DownScript.AddRange(batches);
                }
                else
                {
                    if (scriptInfo.UpScript.Count > 0)
                        throw new InvalidOperationException(
                            $"There is more than one upgrade script with version {version}");

                    scriptInfo.UpScript.AddRange(batches);
                }

                var comment = match.Groups[9];
                scriptInfo.Comment = comment.Success
                    ? comment.Value
                    : null;
            }
            else
            {
                migrationLogger?.LogTrace($"\"{fileName}\" has incorrect extension. \".sql\" is expected");
            }
        }

        return scripts
            .Select(scriptInfo =>
                CreateScriptMigration(
                    scriptInfo.Key,
                    scriptInfo.Value,
                    migrationConnection,
                    variables,
                    migrationLogger))
            .ToArray();
    }

    private MigrationOptions ExtractMigrationOptions(string sourceScript)
    {
        Guard.AssertNotEmpty(sourceScript, nameof(sourceScript));

        var options = new MigrationOptions();

        var optionsRegex = new Regex(@"--\s*CURIOSITY:\s*(.*)\s*=\s*(.*)\s*\n", RegexOptions.IgnoreCase);
        foreach (var line in Regex.Split(sourceScript, @"(?=--\s*CURIOSITY:)"))
        {
            if (String.IsNullOrWhiteSpace(line)) continue;

            var optionsMatch = optionsRegex.Match(line);
            if (!optionsMatch.Success) continue;

            switch (optionsMatch.Groups[1].Value.ToUpper())
            {
                case "TRANSACTION":
                    switch (optionsMatch.Groups[2].Value.ToUpper().Trim().TrimEnd(';'))
                    {
                        case "ON":
                            options.IsTransactionRequired = true;
                            break;
                        case "OFF":
                            options.IsTransactionRequired = false;
                            break;
                        default:
                            throw new InvalidOperationException($"Value \"{optionsMatch.Groups[2].Value}\" is not assignable to the option \"{optionsMatch.Groups[1].Value}\"");
                    }

                    break;
                case "LONG-RUNNING":
                    switch (optionsMatch.Groups[2].Value.ToUpper().Trim().TrimEnd(';'))
                    {
                        case "TRUE":
                            options.IsLongRunning = true;
                            break;
                        case "FALSE":
                            options.IsLongRunning = false;
                            break;
                        default:
                            throw new InvalidOperationException($"Value \"{optionsMatch.Groups[2].Value}\" is not assignable to the option \"{optionsMatch.Groups[1].Value}\"");
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Option \"{optionsMatch.Groups[1].Value}\" is unknown");
            }
        }

        return options;
    }

    /// <summary>
    /// Creates script migration. Replace variables placeholders with real values
    /// </summary>
    /// <param name="migrationVersion"></param>
    /// <param name="migrationScriptInfo"></param>
    /// <param name="migrationConnection"></param>
    /// <param name="variables"></param>
    /// <param name="migrationLogger"></param>
    /// <returns></returns>
    private IMigration CreateScriptMigration(
        MigrationVersion migrationVersion,
        MigrationScriptInfo migrationScriptInfo,
        IMigrationConnection migrationConnection,
        IReadOnlyDictionary<string, string> variables,
        ILogger? migrationLogger)
    {
        Guard.AssertNotNull(migrationScriptInfo, nameof(migrationScriptInfo));
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotNull(variables, nameof(variables));

        var upScript = migrationScriptInfo.UpScript;
        var downScript = migrationScriptInfo.DownScript;

        foreach (var keyValuePair in variables)
        {
            foreach (var batch in upScript)
            {
                batch.Script = batch.Script.Replace(keyValuePair.Key, keyValuePair.Value);
            }

            foreach (var batch in downScript)
            {
                batch.Script = batch.Script.Replace(keyValuePair.Key, keyValuePair.Value);
            }
        }

        return downScript.Count > 0
            ? new DowngradeScriptMigration(
                migrationLogger,
                migrationConnection,
                migrationVersion,
                upScript,
                downScript,
                migrationScriptInfo.Comment,
                migrationScriptInfo.Options.IsTransactionRequired,
                migrationScriptInfo.Options.IsLongRunning)
            : new ScriptMigration(
                migrationLogger,
                migrationConnection,
                migrationVersion,
                upScript,
                migrationScriptInfo.Comment,
                migrationScriptInfo.Options.IsTransactionRequired,
                migrationScriptInfo.Options.IsLongRunning);
    }

    private struct ScriptParsingOptions
    {
        public string? Prefix { get; }

        public ScriptIncorrectNamingAction ScriptIncorrectNamingAction { get; }

        public ScriptParsingOptions(
            string? prefix,
            ScriptIncorrectNamingAction scriptIncorrectNamingAction)
        {
            Prefix = prefix;
            ScriptIncorrectNamingAction = scriptIncorrectNamingAction;
        }
    }

    /// <summary>
    /// Internal class for analysis sql script files
    /// </summary>
    private class MigrationScriptInfo
    {
        public string? Comment { get; set; }

        public List<ScriptMigrationBatch> UpScript { get; } = new();

        public List<ScriptMigrationBatch> DownScript { get; } = new();

        public MigrationOptions Options { get; set; } = null!;
    }

    private class MigrationOptions
    {
        public bool IsTransactionRequired { get; set; } = true;

        public bool IsLongRunning { get; set; }
    }
}
