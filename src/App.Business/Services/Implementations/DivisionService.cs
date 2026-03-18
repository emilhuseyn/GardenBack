using App.Business.DTOs.Divisions;
using App.Business.Services.Interfaces;
using App.Core.Entities;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using AutoMapper;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles division management operations.
    /// </summary>
    public class DivisionService : IDivisionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DivisionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a new division.
        /// </summary>
        public async Task<DivisionResponse> CreateDivisionAsync(CreateDivisionRequest dto)
        {
            var division = _mapper.Map<Division>(dto);
            await _unitOfWork.Divisions.AddAsync(division);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DivisionResponse>(division);
        }

        /// <summary>
        /// Updates an existing division.
        /// </summary>
        public async Task<DivisionResponse> UpdateDivisionAsync(int id, UpdateDivisionRequest dto)
        {
            var division = await _unitOfWork.Divisions.GetByIdAsync(id)
                ?? throw new EntityNotFoundException($"{id} ID-li bölmə tapılmadı.");

            if (dto.Name != null) division.Name = dto.Name;
            if (dto.Language != null) division.Language = dto.Language;
            if (dto.Description != null) division.Description = dto.Description;

            await _unitOfWork.Divisions.UpdateAsync(division);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<DivisionResponse>(division);
        }

        /// <summary>
        /// Gets all divisions.
        /// </summary>
        public async Task<IEnumerable<DivisionResponse>> GetAllDivisionsAsync()
        {
            var divisions = await _unitOfWork.Divisions.GetAllAsync(
                d => true,
                d => d.Groups);
            return _mapper.Map<IEnumerable<DivisionResponse>>(divisions);
        }

        /// <summary>
        /// Gets a division by ID.
        /// </summary>
        public async Task<DivisionResponse> GetDivisionByIdAsync(int id)
        {
            var division = await _unitOfWork.Divisions.GetByIdAsync(
                d => d.Id == id,
                d => d.Groups)
                ?? throw new EntityNotFoundException($"{id} ID-li bölmə tapılmadı.");

                            return _mapper.Map<DivisionResponse>(division);
        }

        /// <summary>
        /// Soft-deletes a division.
        /// </summary>
        public async Task DeleteDivisionAsync(int id)
        {
            await _unitOfWork.Divisions.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
