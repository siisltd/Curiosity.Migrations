using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Marvin.Migrations
{
    /// <summary>
    /// Provide migrations that uses raw sql scripts from specified directories
    /// </summary>
    public class ScriptMigrationsProvider : IMigrationsProvider
    {

        private readonly List<string> _absoluteDirectoriesPath;
        private readonly List<Assembly> _assemblies;
        
        private readonly Regex _regex;

        /// <inheritdoc />
        public ScriptMigrationsProvider()
        {
            // usually only one item will be added
            _absoluteDirectoriesPath = new List<string>(1);
            // usually only one item will be added
            _assemblies = new List<Assembly>(1);
            
            _regex = new Regex(MigrationConstants.MigrationFileNamePattern, RegexOptions.IgnoreCase);
        }
        
        /// <summary>
        /// Setup directory to scan for migrations
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScriptMigrationsProvider FromDirectory(string absolutePath)
        {
            if (String.IsNullOrEmpty(absolutePath)) throw new ArgumentNullException(nameof(_absoluteDirectoriesPath));

            _absoluteDirectoriesPath.Add(absolutePath);
            
            return this;
        }
        
        /// <summary>
        /// Setup assembly where script migrations embedded
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public ScriptMigrationsProvider FromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            _assemblies.Add(assembly);

            return this;
        }

        /// <inheritdoc />
        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            if (_absoluteDirectoriesPath.Count == 0 && _assemblies.Count == 0) throw new InvalidOperationException($"No directories or assemblies specified. First use method {nameof(FromDirectory)} or {nameof(FromAssembly)}");
            
            var migrations = new List<IMigration>();
            foreach (var directoryPath in _absoluteDirectoriesPath)
            {
                if (String.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            
                if (!Directory.Exists(directoryPath)) throw new ArgumentException($"Directory {directoryPath} does not exists");

                var fileNames = Directory.GetFiles(directoryPath);

                var directoryMigrations = GetMigrations(
                    fileNames, 
                    File.ReadAllText, 
                    dbProvider);
                
                if (directoryMigrations == null || directoryMigrations.Count == 0) continue;
                
                migrations.AddRange(directoryMigrations);
            }
            
            foreach (var assembly in _assemblies)
            {
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
                    dbProvider);

                
                if (assemblyMigrations == null || assemblyMigrations.Count == 0) continue;
                
                migrations.AddRange(assemblyMigrations);
            }

            return migrations.OrderBy(x => x.Version).ToList();
        }
        
          private ICollection<IMigration> GetMigrations(
            IEnumerable<string> fileNames,
            Func<string, string> sqlScriptReadFunc,
            IDbProvider dbProvider)
        {
            if (fileNames == null) throw new ArgumentNullException(nameof(fileNames));
            if (sqlScriptReadFunc == null) throw new ArgumentNullException(nameof(sqlScriptReadFunc));
            if (dbProvider == null) throw new ArgumentNullException(nameof(dbProvider));
            
            var scripts = new Dictionary<DbVersion, ScriptInfo>();

            foreach (var fileName in fileNames)
            {
                var match = _regex.Match(fileName);
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
                    if (!String.IsNullOrWhiteSpace(scriptInfo.DownScript)) throw new InvalidOperationException($"There is more than one downgrade script with version {version}");
                    scriptInfo.DownScript = script;
                }
                else
                {
                    if (!String.IsNullOrWhiteSpace(scriptInfo.UpScript)) throw new InvalidOperationException($"There is more than one upgrade script with version {version}");
                    scriptInfo.UpScript = script;
                    var comment = match.Groups[7];
                    scriptInfo.Comment = comment.Success 
                        ? comment.Value
                        : null;
                }
            }
            
            return scripts.Select(scriptInfo => 
                    new ScriptMigration(
                        dbProvider,
                        scriptInfo.Key,
                        scriptInfo.Value.UpScript,
                        scriptInfo.Value.DownScript,
                        scriptInfo.Value.Comment) 
                    as IMigration).ToList();
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