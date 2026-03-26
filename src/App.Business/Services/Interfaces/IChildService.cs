using App.Business.DTOs.Children;
using App.Core.Common;

namespace App.Business.Services.Interfaces
{
    /// <summary>
    /// Service for child management operations.
    /// </summary>
    public interface IChildService
    {
        /// <summary>
        /// Creates a new child record.
        /// </summary>
        Task<ChildResponse> CreateChildAsync(CreateChildRequest dto);

        /// <summary>
        /// Updates an existing child record.
        /// </summary>
        Task<ChildResponse> UpdateChildAsync(int id, UpdateChildRequest dto);

        /// <summary>
        /// Gets a child's full details including attendance and payment summaries.
        /// </summary>
        Task<ChildDetailResponse> GetChildByIdAsync(int id);

        /// <summary>
        /// Gets all children with filtering and pagination.
        /// </summary>
        Task<PagedResponse<ChildResponse>> GetAllChildrenAsync(ChildFilterRequest filter);

        /// <summary>
        /// Activates a child.
        /// </summary>
        Task ActivateChildAsync(int id);

        /// <summary>
        /// Deactivates a child.
        /// </summary>
        Task DeactivateChildAsync(int id);

        /// <summary>
        /// Soft-deletes a child.
        /// </summary>
        Task DeleteChildAsync(int id);

        /// <summary>
        /// Searches children by name, parent name, or phone.
        /// </summary>
        Task<IEnumerable<ChildResponse>> SearchChildrenAsync(string term);

        /// <summary>
        /// Activates multiple children at once.
        /// </summary>
        Task ActivateChildrenAsync(List<int> ids);

        /// <summary>
        /// Deactivates multiple children at once.
        /// </summary>
        Task DeactivateChildrenAsync(List<int> ids);

        /// <summary>
        /// Soft-deletes multiple children at once.
        /// </summary>
        Task DeleteChildrenAsync(List<int> ids);
    }
}
