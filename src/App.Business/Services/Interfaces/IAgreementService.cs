namespace App.Business.Services.Interfaces
{
    public interface IAgreementService
    {
        /// <summary>
        /// Generates a filled Razilaşma DOCX for the given child.
        /// Returns the file bytes and a suggested filename.
        /// </summary>
        Task<(byte[] FileBytes, string FileName)> GenerateAgreementAsync(int childId);
    }
}
