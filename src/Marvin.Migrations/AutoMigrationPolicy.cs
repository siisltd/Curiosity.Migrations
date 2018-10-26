using System;

namespace Marvin.Migrations
{
    /// <summary>
    /// Политики автоматический миграции
    /// </summary>
    [Flags]
    public enum AutoMigrationPolicy
    {
        /// <summary>
        /// Никакие автоматические миграции не разрешены
        /// </summary>
        None = 0x0,
      
        /// <summary>
        /// Допускается миграции при изменении Minor версих.
        /// </summary>
        Minor = 0x1,

        /// <summary>
        /// Допускаются миграции мажорный версий
        /// </summary>
        Major = 0x2
    }
}