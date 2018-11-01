using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Marvin.Migrations.Migrations;

namespace Marvin.Migrations.MigrationProviders
{
    /// <summary>
    /// Class for providing migration that uses raq sql scripts
    /// </summary>
    public class ScriptMigrationsProvider : IMigrationsProvider
    {
        /// <summary>
        /// Regex patterns for scanning files
        /// </summary>
        private const string MigrationFileNamePattern = @"^(\d+)\.(\d+)(.(down)|.(up))?(-([\w]*))?\.sql$";

        private string _absolutePath;
        
        /// <summary>
        /// Setup directory to scan for migrations
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void FromDirectory(string absolutePath)
        {
            if (String.IsNullOrEmpty(absolutePath)) throw new ArgumentNullException(nameof(_absolutePath));

            _absolutePath = absolutePath;
        }

        /// <inheritdoc />
        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            if (String.IsNullOrEmpty(_absolutePath)) throw new ArgumentNullException(nameof(_absolutePath));
            
            if (!Directory.Exists(_absolutePath)) throw new ArgumentException("Directory does not exists");

            var fileNames = Directory.GetFiles(_absolutePath);

            var regex = new Regex(MigrationFileNamePattern, RegexOptions.IgnoreCase);
            var scripts = new Dictionary<DbVersion, ScriptInfo>();
            foreach (var fileName in fileNames)
            {
                var match = regex.Match(Path.GetFileName(fileName));
                if (!match.Success) continue;

                var majorVersion = match.Groups[1];
                var minorVersion = match.Groups[2];
                var version = new DbVersion(int.Parse(majorVersion.Value), int.Parse(minorVersion.Value));
                if (!scripts.ContainsKey(version))
                {
                    scripts[version] = new ScriptInfo(version);
                }
                var scriptInfo = scripts[version];

                var script = File.ReadAllText(fileName);
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

            var migrations = new List<IMigration>(scripts.Count);
            foreach (var scriptInfo in scripts)
            {
                migrations.Add(
                    new ScriptMigration(
                        dbProvider,
                        scriptInfo.Key,
                        scriptInfo.Value.UpScript,
                        scriptInfo.Value.DownScript,
                        scriptInfo.Value.Comment));
            }

            return migrations.OrderBy(x => x.Version).ToList();
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