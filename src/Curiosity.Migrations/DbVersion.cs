using System;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Database version
    /// </summary>
    public struct DbVersion : IComparable, IEquatable<DbVersion>
    {
        private const char VersionNumberSeparator = '.';

        private const int VersionNumbersCount = 2;

        /// <summary>
        /// Major version
        /// </summary>
        /// <remarks>
        /// Change it if you change DB schema (create or drop table, column, relation)
        /// </remarks>
        public int Major { get; }

        /// <summary>
        /// Minor version
        /// </summary>
        /// <remarks>
        /// Change it if your migration do not change DB schema (cleaning data, inserting new values for existed tables, creating index, etc)
        /// </remarks>
        public int Minor { get; }

        /// <inheritdoc />
        public DbVersion(int major, int minor) : this()
        {
            Major = GetCorrectNumber(major);
            Minor = GetCorrectNumber(minor);
        }

        private int GetCorrectNumber(int number)
        {
            return number < 0
                ? 0
                : number;
        }

        /// <inheritdoc />
        public DbVersion(string version) : this()
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentNullException($"{nameof(version)}");
            }

            var mainValues = version.Split(VersionNumberSeparator);
            if (mainValues.Length < VersionNumbersCount)
            {
                throw new ArgumentException("Incorrect version format. Required is \"Major.Minor\"");
            }

            Major = GetCorrectNumber(int.Parse(mainValues[0]));
            Minor = GetCorrectNumber(int.Parse(mainValues[1]));
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
            return $"{Major}.{Minor}";
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
                var hashCode = Major;
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
        /// Returns difference between two <see cref="DbVersion"/>
        /// </summary>
        /// <param name="version1">First database version</param>
        /// <param name="version2">Second database version</param>
        /// <returns>returns the difference in version number(major or minor)</returns>
        public static DbVersionDifference GetDifference(DbVersion version1, DbVersion version2)
        {
            if (version1.Major != version2.Major)
            {
                return DbVersionDifference.Major;
            }

            if (version1.Minor != version2.Minor)
            {
                return DbVersionDifference.Minor;
            }

            return DbVersionDifference.NoDifference;
        }

        /// <summary>
        /// Try parse <see cref="DbVersion"/> from <see cref="String"/>
        /// </summary>
        /// <param name="source">Text presentation of <see cref="DbVersion"/></param>
        /// <param name="version">Database version</param>
        /// <returns>Result of parsing</returns>
        public static bool TryParse(string source, out DbVersion version)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                version = default(DbVersion);
                return false;
            }

            var mainValues = source.Split(VersionNumberSeparator);
            if (mainValues.Length < VersionNumbersCount)
            {
                version = default(DbVersion);
                return false;
            }

            var result = true;

            result &= int.TryParse(mainValues[0], out var major);
            result &= int.TryParse(mainValues[1], out var minor);

            version = new DbVersion(major, minor);

            return result;
        }
    }
}