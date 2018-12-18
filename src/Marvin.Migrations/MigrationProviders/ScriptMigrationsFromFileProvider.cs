using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marvin.Migrations
{
    /// <summary>
    /// Provide migrations that uses raw sql scripts from specified directories
    /// </summary>
    public class ScriptMigrationsFromFileProvider : IMigrationsProvider
    {

        private readonly List<string> _absoluteDirectoriesPath;

        /// <inheritdoc />
        public ScriptMigrationsFromFileProvider()
        {
            _absoluteDirectoriesPath = new List<string>();
        }
        
        /// <summary>
        /// Setup directory to scan for migrations
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ScriptMigrationsFromFileProvider FromDirectory(string absolutePath)
        {
            if (String.IsNullOrEmpty(absolutePath)) throw new ArgumentNullException(nameof(_absoluteDirectoriesPath));

            _absoluteDirectoriesPath.Add(absolutePath);

            return this;
        }

        /// <inheritdoc />
        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            if (_absoluteDirectoriesPath.Count == 0) throw new InvalidOperationException($"No directories specified. First use method {nameof(FromDirectory)}");
            
            var migrations = new List<IMigration>();
            foreach (var directoryPath in _absoluteDirectoriesPath)
            {
                if (String.IsNullOrEmpty(directoryPath)) throw new ArgumentNullException(nameof(directoryPath));
            
                if (!Directory.Exists(directoryPath)) throw new ArgumentException($"Directory {directoryPath} does not exists");

                var fileNames = Directory.GetFiles(directoryPath).Select(Path.GetFileName);

                var directoryMigrations = ScriptMigrationsProvider.GetMigrations(
                    fileNames, 
                    fileName => File.ReadAllText(Path.Combine(directoryPath, fileName)), 
                    dbProvider);
                
                if (directoryMigrations == null) continue;
                
                migrations.AddRange(directoryMigrations);
            }

            return migrations;
        }
        
        
    }
}