namespace Curiosity.Migrations
{
    /// <summary>
    /// Batch of single migration
    /// </summary>
    /// <remarks>
    /// Script migration is splitted into batches by text "--BATCH:"
    /// </remarks>
    public class ScriptMigrationBatch
    {
        /// <summary>
        /// Batch order in a migration
        /// </summary>
        public int OrderIndex { get; set; }
            
        /// <summary>
        /// Batch name (shown at logs, name is a text after --BATCH:)
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// SQL
        /// </summary>
        public string Script { get; set; }
    }
}