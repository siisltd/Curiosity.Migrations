using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;

namespace Marvin.Migrations.Info
{
    /// <summary>
    /// One step of DB migration
    /// </summary>
    //todo add automigration restriction for specific version
    public class DbMigration
    {
        /// <summary>
        /// SQL script for DB update
        /// </summary>
        public string Script { get; }

        /// <summary>
        /// Migration version
        /// </summary>
        public DbVersion Version { get; }

        /// <inheritdoc />
        public DbMigration(string upgradeScript, DbVersion version)
        {
            if (string.IsNullOrEmpty(upgradeScript))
            {
                throw new ArgumentNullException(nameof(upgradeScript));
            }

            Script = upgradeScript;
            Version = version;
        }

        /// <summary>
        /// Read all <see cref="DbMigration"/> from specified directory
        /// </summary>
        /// <param name="path">Full path to directory with scripts</param>
        /// <returns>Returns collection of migration from directory</returns>
        public static ICollection<DbMigration> ReadFromDirectory(string path)
        {
            return Directory.GetFiles(path)
                .Select(x => new DbMigration(File.ReadAllText(x), new DbVersion(Path.GetFileNameWithoutExtension(x))))
                .ToList();
        }
    }
}