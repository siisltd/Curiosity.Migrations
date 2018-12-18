using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marvin.Migrations
{
    internal static class ScriptMigrationsProvider
    {
        private static readonly Regex regex;
        
        static ScriptMigrationsProvider()
        {
            regex = new Regex(MigrationConstants.MigrationFileNamePattern, RegexOptions.IgnoreCase);
        }
        
        public static ICollection<IMigration> GetMigrations(
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