namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for creating automated database backups.
    /// </summary>
    public interface IBackupService
    {
        /// <summary>
        /// Creates a timestamped backup of the database.
        /// </summary>
        Task CreateBackupAsync();
    }
}
