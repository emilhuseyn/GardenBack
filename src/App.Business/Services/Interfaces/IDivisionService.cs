using App.Business.DTOs.Divisions;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for division management operations.
    /// </summary>
    public interface IDivisionService
    {
        /// <summary>Creates a new division.</summary>
        Task<DivisionResponse> CreateDivisionAsync(CreateDivisionRequest dto);

        /// <summary>Updates an existing division.</summary>
        Task<DivisionResponse> UpdateDivisionAsync(int id, UpdateDivisionRequest dto);

        /// <summary>Gets all divisions.</summary>
        Task<IEnumerable<DivisionResponse>> GetAllDivisionsAsync();

        /// <summary>Gets a division by ID.</summary>
        Task<DivisionResponse> GetDivisionByIdAsync(int id);

        /// <summary>Soft-deletes a division.</summary>
        Task DeleteDivisionAsync(int id);
    }
}
