using System;
using System.Text.RegularExpressions;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Database version
    /// </summary>
    public readonly struct DbVersion : IComparable, IEquatable<DbVersion>
    {
        private static readonly Regex Regex = new Regex(MigrationConstants.VersionPattern, RegexOptions.IgnoreCase);

        /// <summary>
        /// Major version
        /// </summary>
        public long Major { get; }

        /// <summary>
        /// Minor version
        /// </summary>
        /// <remarks>
        /// Useful when combining migrations to a single logical group (eg. 1 step of data manipulation, 2 step and etc.) 
        /// </remarks>
        public short Minor { get; }
        
        public DbVersion(long major, short minor = 0) : this()
        {
            AssertMajor(major);
            AssertMinor(minor);
            
            Major = major;
            Minor = minor;
        }
        
        public DbVersion(string version): this()
        {
            if (!TryParse(version, out var validVersion))
                throw new ArgumentException($"Incorrect version. Version must be parsed by this regexp: {MigrationConstants.VersionPattern}");

            Major = validVersion.Major;
            Minor = validVersion.Minor;
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void AssertMajor(long major)
        {
            if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
        }
        
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void AssertMinor(short minor)
        {
            if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is DbVersion version && Equals(version);
        }

        /// <inheritdoc />
        public bool Equals(DbVersion other)
        {
            return Major == other.Major && Minor == other.Minor;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Major:D4}.{Minor:D2}";
        }

        /// <inheritdoc />
        public int CompareTo(object obj)
        {
            var version = (DbVersion) obj;

            var result = Major.CompareTo(version.Major);
            if (result != 0)
            {
                return result;
            }

            return Minor.CompareTo(version.Minor);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Major.GetHashCode();
                hashCode = (hashCode * 397) ^ Minor;
                return hashCode;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static bool operator ==(DbVersion version1, DbVersion version2)
        {
            return version1.CompareTo(version2) == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static bool operator !=(DbVersion version1, DbVersion version2)
        {
            return version1.CompareTo(version2) != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static bool operator <(DbVersion version1, DbVersion version2)
        {
            return version1.CompareTo(version2) < 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static bool operator >(DbVersion version1, DbVersion version2)
        {
            return version1.CompareTo(version2) > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static bool operator <=(DbVersion version1, DbVersion version2)
        {
            return version1.CompareTo(version2) <= 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version1"></param>
        /// <param name="version2"></param>
        /// <returns></returns>
        public static bool operator >=(DbVersion version1, DbVersion version2)
        {
            return version1.CompareTo(version2) >= 0;
        }

        /// <summary>
        /// Try parse <see cref="DbVersion"/> from <see cref="String"/>
        /// </summary>
        /// <param name="source">Text presentation of <see cref="DbVersion"/></param>
        /// <param name="version">Database version</param>
        /// <returns>Result of parsing</returns>
        public static bool TryParse(string source, out DbVersion version)
        {
            version = default;

            if (String.IsNullOrWhiteSpace(source))
                return false;
            
            var match = Regex.Match(source);
            if (!match.Success)
                return false;

            var result = true;

            // major version may contains '-'
            result &= long.TryParse(match.Groups[1].Value.Replace("-", ""), out var major);
            if (!result)
                return false;
            
            try
            {
                AssertMajor(major);
            }
            catch
            {
                return false;
            }
            
            // it's optional
            short minor = 0;
            if (!String.IsNullOrEmpty(match.Groups[3].Value))
            {
                result &= short.TryParse(match.Groups[3].Value, out minor);
                if (!result)
                    return false;

                try
                {
                    AssertMinor(minor);
                }
                catch
                {
                    return false;
                }
            }

            version = new DbVersion(major, minor);

            return result;
        }
    }
}