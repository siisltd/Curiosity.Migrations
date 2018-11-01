using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Marvin.Migrations.Migrations;

namespace Marvin.Migrations.MigrationProviders
{
    public class ScriptMigrationsProvider : IMigrationsProvider
    {
        private const string MigrationFileNameRegex = @"^(\d+)\.(\d+)(.(down)|.(up))?(-([\w]*))?\.sql$";

        private string _absolutePath;
        
        public void FromDirectory(string absolutePath)
        {
            if (String.IsNullOrEmpty(_absolutePath)) throw new ArgumentNullException(nameof(_absolutePath));

            _absolutePath = absolutePath;
        }

        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            if (String.IsNullOrEmpty(_absolutePath)) throw new ArgumentNullException(nameof(_absolutePath));
            
            if (!Directory.Exists(_absolutePath)) throw new ArgumentException("Directory does not exists");

            var fileNames = Directory.GetFiles(_absolutePath);

            var regex = new Regex(MigrationFileNameRegex, RegexOptions.IgnoreCase);
            var scripts = new Dictionary<DbVersion, ScriptInfo>();
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

                var script = File.ReadAllText(fileName);
                if (match.Groups[5].Success)
                {
                    scriptInfo.DownScript = script;
                }
                else
                {
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

            return migrations;
        }
        
        private class ScriptInfo
        {
            public DbVersion Version { get; }

            public ScriptInfo(DbVersion version)
            {
                Version = version;
            }

            public string Comment { get; set; }
            
            public string UpScript { get; set; }
            
            public string DownScript { get; set; }
        }
    }
}