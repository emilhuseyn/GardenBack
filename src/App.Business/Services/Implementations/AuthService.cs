using App.Business.DTOs.Auth;
using App.Business.Helpers;
using App.Business.Services.Interfaces;
using App.Core.Entities.Identity;
using App.Core.Enums;
using App.Core.Exceptions;
using App.Core.Exceptions.Commons;
using App.DAL.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace App.Business.Services.Implementations
{
    /// <summary>
    /// Handles authentication and user management operations.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Registers a new user and returns a JWT token.
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(RegisterRequest dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new ConflictException($"'{dto.Email}' e-poçt ünvanı ilə istifadəçi artıq mövcuddur.");

            if (!Enum.TryParse<EUserRole>(dto.Role, true, out var role))
                throw new Core.Exceptions.ValidationException("Yanlış rol göstərilib.");

            var user = new User
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = role,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new Core.Exceptions.ValidationException(TranslateIdentityErrors(result));

            await _userManager.AddToRoleAsync(user, role.ToString());

            var token = JwtGenerator.GenerateToken(user, _configuration);
            return new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = user.Role.ToString(),
                Expiration = DateTime.UtcNow.AddHours(4 + 8) // UTC+4, 8 hours session
            };
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        public async Task<AuthResponse> LoginAsync(LoginRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email)
                ?? throw new EntityNotFoundException("E-poçt və ya şifrə yanlışdır.");

            if (!user.IsActive)
                throw new ForbiddenException("Bu hesab deaktiv edilib.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
                throw new UnauthorizedException("E-poçt və ya şifrə yanlışdır.");

            var token = JwtGenerator.GenerateToken(user, _configuration);
            return new AuthResponse
            {
                Token = token,
                Email = user.Email!,
                FullName = $"{user.FirstName} {user.LastName}",
                Role = user.Role.ToString(),
                Expiration = DateTime.UtcNow.AddHours(4 + 8) // UTC+4, 8 hours session
            };
        }

        /// <summary>
        /// Gets the current authenticated user's profile.
        /// </summary>
        public async Task<UserResponse> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("İstifadəçi tapılmadı.");

            return MapToResponse(user);
        }

        /// <summary>
        /// Updates the current user's own profile. Role and IsActive cannot be changed.
        /// </summary>
        public async Task<UserResponse> UpdateProfileAsync(string userId, UpdateProfileRequest dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("Istifadəçi tapılmadı.");

            if (dto.Email != null && dto.Email != user.Email)
            {
                var emailOwner = await _userManager.FindByEmailAsync(dto.Email);
                if (emailOwner != null)
                    throw new ConflictException($"'{dto.Email}' e-poçt ünvanı artıq istifadə olunur.");

                user.Email = dto.Email;
                user.UserName = dto.Email;
            }

            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

            user.UpdatedAt = DateTime.UtcNow.AddHours(4);

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Core.Exceptions.ValidationException(TranslateIdentityErrors(result));

            return MapToResponse(user);
        }

        /// <summary>
        /// Updates a user's profile information.
        /// </summary>
        public async Task UpdateUserAsync(string userId, UpdateUserRequest dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("Istifadəçi tapılmadı.");

            if (dto.FirstName != null) user.FirstName = dto.FirstName;
            if (dto.LastName != null) user.LastName = dto.LastName;
            if (dto.Email != null)
            {
                user.Email = dto.Email;
                user.UserName = dto.Email;
            }
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
            if (dto.Role != null && Enum.TryParse<EUserRole>(dto.Role, true, out var role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, role.ToString());
                user.Role = role;
            }

            user.UpdatedAt = DateTime.UtcNow.AddHours(4);

            await _userManager.UpdateAsync(user);
        }

        /// <summary>
        /// Changes the user's password.
        /// </summary>
        public async Task ChangePasswordAsync(string userId, ChangePasswordRequest dto)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("İstifadəçi tapılmadı.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                throw new Core.Exceptions.ValidationException(TranslateIdentityErrors(result));
        }

        private static IEnumerable<string> TranslateIdentityErrors(IdentityResult result)
        {
            return result.Errors.Select(e => e.Code switch
            {
                "PasswordMismatch"              => "Cari şifrə yanlışdır.",
                "PasswordTooShort"              => "Şifrə ən azı 8 simvol olmalıdır.",
                "PasswordRequiresDigit"         => "Şifrə ən azı bir rəqəm (0-9) ehtiva etməlidir.",
                "PasswordRequiresLower"         => "Şifrə ən azı bir kiçik hərf (a-z) ehtiva etməlidir.",
                "PasswordRequiresUpper"         => "Şifrə ən azı bir böyük hərf (A-Z) ehtiva etməlidir.",
                "PasswordRequiresNonAlphanumeric" => "Şifrə ən azı bir xüsusi simvol (!@#$...) ehtiva etməlidir.",
                "PasswordRequiresUniqueChars"   => "Şifrə daha çox fərqli simvol ehtiva etməlidir.",
                "DuplicateUserName"             => "Bu istifadəçi adı artıq mövcuddur.",
                "DuplicateEmail"                => "Bu e-poçt artıq qeydiyyatdan keçib.",
                "InvalidEmail"                  => "E-poçt formatı yanlışdır.",
                "InvalidUserName"               => "İstifadəçi adında yolverilməz simvollar var.",
                "UserNotFound"                  => "İstifadəçi tapılmadı.",
                "UserAlreadyHasPassword"        => "İstifadəçinin artıq şifrəsi mövcuddur.",
                "UserLockedOut"                 => "Hesab müvəqqəti bloklanıb. Bir az sonra yenidən cəhd edin.",
                _                               => e.Description
            });
        }

        /// <summary>
        /// Gets all users in the system.
        /// </summary>
        public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            return users.Select(MapToResponse);
        }

        /// <summary>
        /// Gets users filtered by role (e.g., Teacher list for AdmissionStaff).
        /// </summary>
        public async Task<IEnumerable<UserResponse>> GetUsersByRoleAsync(string role)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            return users.Where(u => u.IsActive).Select(MapToResponse);
        }

        /// <summary>
        /// Gets a specific user by ID.
        /// </summary>
        public async Task<UserResponse> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("İstifadəçi tapılmadı.");
            return MapToResponse(user);
        }

        /// <summary>
        /// Deletes a user.
        /// </summary>
        public async Task DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("İstifadəçi tapılmadı.");

            user.IsActive = false;
            await _userManager.UpdateAsync(user);
        }

        /// <summary>
        /// Permanently removes a user from the database.
        /// </summary>
        public async Task RemoveUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new EntityNotFoundException("İstifadəçi tapılmadı.");

            // Prevent removal if user is Teacher and has groups
            if (user.Role == EUserRole.Teacher)
            {
                var groups = await _unitOfWork.Groups.GetGroupsByTeacherIdAsync(userId);
                if (groups.Any())
                    throw new Core.Exceptions.ValidationException("Bu müəllimə aid qruplar var, ona görə silmək mümkün deyil.");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new Core.Exceptions.ValidationException(TranslateIdentityErrors(result));
        }

        private static UserResponse MapToResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt.AddHours(4),
                UpdatedAt = user.UpdatedAt.AddHours(4)
            };
        }
    }
}
