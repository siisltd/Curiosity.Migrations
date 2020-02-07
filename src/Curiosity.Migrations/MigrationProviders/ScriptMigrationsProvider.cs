using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Provide migrations that uses raw sql scripts from specified directories
    /// </summary>
    public class ScriptMigrationsProvider : IMigrationsProvider
    {
        private readonly Dictionary<string, string> _absoluteDirectoriesPathWithPrefix;
        private readonly Dictionary<Assembly, string> _assembliesWithPrefix;

        public ScriptMigrationsProvider()
        {
            // usually only one item will be added
            _absoluteDirectoriesPathWithPrefix = new Dictionary<string, string>(1);
            // usually only one item will be added
            _assembliesWithPrefix = new Dictionary<Assembly, string>(1);
        }

        /// <summary>
        /// Setup directory to scan for migrations
        /// </summary>
        /// <param name="path">Path to directory where scripts are located. Can be relative and absolute.</param>
        /// <param name="prefix">Specific part of name or path. If no prefix specified provider will process only files without any prefix</param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScriptMigrationsProvider FromDirectory(string path, string prefix = null)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            var innerPath = Path.IsPathRooted(path) 
                ? path 
                : Path.Combine(Directory.GetCurrentDirectory(), path);

            _absoluteDirectoriesPathWithPrefix[innerPath] = prefix;

            return this;
        }

        /// <summary>
        /// Setup assembly where script migrations embedded
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="prefix">Specific part of name or namespace part. If no prefix specified provider will process only files without any prefix</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ScriptMigrationsProvider FromAssembly(Assembly assembly, string prefix = null)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            _assembliesWithPrefix[assembly] = prefix;

            return this;
        }

        /// <inheritdoc />
        public ICollection<IMigration> GetMigrations(
            IDbProvider dbProvider,
            IReadOnlyDictionary<string, string> variables,
            ILogger migrationLogger)
        {
            if (dbProvider == null) throw new ArgumentNullException(nameof(dbProvider));
            if (variables == null) throw new ArgumentNullException(nameof(variables));

            if (_absoluteDirectoriesPathWithPrefix.Count == 0 && _assembliesWithPrefix.Count == 0)
                throw new InvalidOperationException(
                    $"No directories or assemblies specified. First use method {nameof(FromDirectory)} or {nameof(FromAssembly)}");
            
            var migrations = new List<IMigration>();
            foreach (var keyValuePair in _absoluteDirectoriesPathWithPrefix)
            {
                var directoryPath = keyValuePair.Key;
                var prefix = keyValuePair.Value;

                if (String.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));

                if (!Directory.Exists(directoryPath))
                    throw new ArgumentException($"Directory {directoryPath} does not exists");

                var fileNames = Directory.GetFiles(directoryPath);

                var directoryMigrations = GetMigrations(
                    fileNames,
                    File.ReadAllText,
                    dbProvider,
                    prefix,
                    variables);

                if (directoryMigrations == null || directoryMigrations.Count == 0) continue;

                migrations.AddRange(directoryMigrations);
            }

            foreach (var keyValuePair in _assembliesWithPrefix)
            {
                var assembly = keyValuePair.Key;
                var prefix = keyValuePair.Value;

                var resourceFileNames = assembly.GetManifestResourceNames();

                var assemblyMigrations = GetMigrations(
                    resourceFileNames,
                    resourceName =>
                    {
                        using (var stream = assembly.GetManifestResourceStream(resourceName))
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    },
                    dbProvider,
                    prefix,
                    variables);

                if (assemblyMigrations == null || assemblyMigrations.Count == 0) continue;

                migrations.AddRange(assemblyMigrations);
            }

            return migrations.OrderBy(x => x.Version).ToList();
        }

        private ICollection<IMigration> GetMigrations(
            IEnumerable<string> fileNames,
            Func<string, string> sqlScriptReadFunc,
            IDbProvider dbProvider,
            string prefix,
            IReadOnlyDictionary<string, string> variables)
        {
            if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));
            if (sqlScriptReadFunc == null) throw new ArgumentNullException(nameof(sqlScriptReadFunc));
            if (dbProvider == null) throw new ArgumentNullException(nameof(dbProvider));

            var scripts = new Dictionary<DbVersion, ScriptInfo>();

            var regex = String.IsNullOrWhiteSpace(prefix)
                ? new Regex($"[./]{MigrationConstants.MigrationFileNamePattern}", RegexOptions.IgnoreCase)
                : new Regex($"{prefix}[-.]{MigrationConstants.MigrationFileNamePattern}", RegexOptions.IgnoreCase);
            
            foreach (var fileName in fileNames)
            {
                var match = regex.Match(fileName);
                if (!match.Success) continue;

                var majorVersion = match.Groups[1];
                var minorVersion = match.Groups[2];
                var version = new DbVersion(int.Parse(majorVersion.Value), int.Parse(minorVersion.Value));
                if (!scripts.ContainsKey(version))
                {
                    scripts[version] = new ScriptInfo(version);
                }

                var scriptInfo = scripts[version];

                var script = sqlScriptReadFunc.Invoke(fileName);
                if (match.Groups[4].Success)
                {
                    if (!String.IsNullOrWhiteSpace(scriptInfo.DownScript))
                        throw new InvalidOperationException(
                            $"There is more than one downgrade script with version {version}");
                    scriptInfo.DownScript = script;
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(scriptInfo.UpScript))
                        throw new InvalidOperationException(
                            $"There is more than one upgrade script with version {version}");
                    scriptInfo.UpScript = script;
                    var comment = match.Groups[7];
                    scriptInfo.Comment = comment.Success
                        ? comment.Value
                        : null;
                }
            }

            return scripts
                .Select(scriptInfo =>
                    CreateScriptMigration(
                        scriptInfo.Key,
                        scriptInfo.Value,
                        dbProvider,
                        variables))
                .ToList();
        }

        /// <summary>
        /// Creates script migration. Replace variables placeholders with real values
        /// </summary>
        /// <param name="dbVersion"></param>
        /// <param name="scriptInfo"></param>
        /// <param name="dbProvider"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        private IMigration CreateScriptMigration(
            DbVersion dbVersion,
            ScriptInfo scriptInfo,
            IDbProvider dbProvider,
            IReadOnlyDictionary<string, string> variables)
        {
            var upScript = scriptInfo.UpScript;
            var downScript = scriptInfo.DownScript;

            foreach (var keyValuePair in variables)
            {
                upScript = upScript.Replace(keyValuePair.Key, keyValuePair.Value);
                downScript = downScript?.Replace(keyValuePair.Key, keyValuePair.Value);
            }

            return new ScriptMigration(
                dbProvider,
                dbVersion,
                upScript,
                downScript,
                scriptInfo.Comment);
        }

        /// <summary>
        /// Internal class for analysis sql script files
        /// </summary>
        private class ScriptInfo
        {
            public ScriptInfo(DbVersion version)
            {
                Version = version;
            }

            public DbVersion Version { get; }

            public string Comment { get; set; }

            public string UpScript { get; set; }

            public string DownScript { get; set; }
        }
    }
}