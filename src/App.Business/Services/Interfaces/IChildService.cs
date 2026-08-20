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
        /// Activates a child. <paramref name="returnDate"/> is the first day the child attends again
        /// (inclusive); null means "as of today". The return month is re-billed for that day onward.
        /// </summary>
        Task<ReactivationResult> ActivateChildAsync(int id, DateTime? returnDate = null);

        /// <summary>
        /// Deactivates a child. <paramref name="effectiveDate"/> is the last day the child actually
        /// attended (inclusive); null means "as of now".
        /// </summary>
        Task<DeactivationRecalcResult> DeactivateChildAsync(int id, DateTime? effectiveDate = null);

        /// <summary>
        /// Soft-deletes a child.
        /// </summary>
        Task DeleteChildAsync(int id);

        /// <summary>
        /// Searches children by name, parent name, or phone.
        /// </summary>
        Task<IEnumerable<ChildResponse>> SearchChildrenAsync(string term);

        /// <summary>
        /// Activates multiple children at once. <paramref name="returnDate"/> applies to every child.
        /// </summary>
        Task<List<ReactivationResult>> ActivateChildrenAsync(List<int> ids, DateTime? returnDate = null);

        /// <summary>
        /// Deactivates multiple children at once. <paramref name="effectiveDate"/> is applied to every
        /// child in the list; null means "as of now".
        /// </summary>
        Task<List<DeactivationRecalcResult>> DeactivateChildrenAsync(List<int> ids, DateTime? effectiveDate = null);

        /// <summary>
        /// Soft-deletes multiple children at once.
        /// </summary>
        Task DeleteChildrenAsync(List<int> ids);
    }
}
