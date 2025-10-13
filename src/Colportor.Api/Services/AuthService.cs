using Microsoft.Extensions.Configuration;
using Colportor.Api.DTOs;
using Colportor.Api.Models;
using Colportor.Api.Repositories;
using Colportor.Api.Services;
using AutoMapper;

namespace Colportor.Api.Services;

/// <summary>
/// Implementação do serviço de autenticação
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;

    public AuthService(
        IUserRepository userRepository,
        IJwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<AuthResultDto>> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Tentativa de login para email: {Email}", email);

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login falhou: usuário não encontrado para email: {Email}", email);
                return ApiResponse<AuthResultDto>.ErrorResponse("Email ou senha inválidos");
            }

            if (!ValidatePassword(password, user.PasswordHash))
            {
                _logger.LogWarning("Login falhou: senha incorreta para email: {Email}", email);
                return ApiResponse<AuthResultDto>.ErrorResponse("Email ou senha inválidos");
            }

            var token = _jwtService.GenerateToken(user);
            var refreshToken = GenerateRefreshToken();

            var authResult = new AuthResultDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                User = _mapper.Map<UserDto>(user)
            };

            _logger.LogInformation("Login bem-sucedido para usuário: {UserId}", user.Id);
            return ApiResponse<AuthResultDto>.SuccessResponse(authResult, "Login realizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante login para email: {Email}", email);
            return ApiResponse<AuthResultDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse<AuthResultDto>> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation("Tentativa de registro para email: {Email}", registerDto.Email);

            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                return ApiResponse<AuthResultDto>.ErrorResponse("Email já está em uso");
            }

            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                Role = registerDto.Role,
                FullName = registerDto.FullName,
                CPF = registerDto.CPF,
                City = registerDto.City,
                RegionId = registerDto.RegionId
            };

            var createdUser = await _userRepository.AddAsync(user);

            var token = _jwtService.GenerateToken(createdUser);
            var refreshToken = GenerateRefreshToken();

            var authResult = new AuthResultDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                User = _mapper.Map<UserDto>(createdUser)
            };

            _logger.LogInformation("Registro bem-sucedido para usuário: {UserId}", createdUser.Id);
            return ApiResponse<AuthResultDto>.SuccessResponse(authResult, "Registro realizado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante registro para email: {Email}", registerDto.Email);
            return ApiResponse<AuthResultDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse<AuthResultDto>> RefreshTokenAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<AuthResultDto>.ErrorResponse("Usuário não encontrado");
            }

            var token = _jwtService.GenerateToken(user);
            var refreshToken = GenerateRefreshToken();

            var authResult = new AuthResultDto
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                User = _mapper.Map<UserDto>(user)
            };

            _logger.LogInformation("Token renovado para usuário: {UserId}", userId);
            return ApiResponse<AuthResultDto>.SuccessResponse(authResult, "Token renovado com sucesso");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao renovar token para usuário: {UserId}", userId);
            return ApiResponse<AuthResultDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _userRepository.GetByIdWithRelationsAsync(userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    public Task<bool> ValidatePasswordAsync(string password, string hashedPassword)
    {
        return Task.FromResult(ValidatePassword(password, hashedPassword));
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private bool ValidatePassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}
