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

            // Verificar se o usuário está aprovado (não é "Pending")
            if (user.Role == "Pending")
            {
                _logger.LogWarning("Login falhou: usuário pendente de aprovação para email: {Email}", email);
                return ApiResponse<AuthResultDto>.ErrorResponse("Sua conta está aguardando aprovação. Entre em contato com o administrador.");
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
            _logger.LogInformation("Tentativa de registro de líder para email: {Email}", registerDto.Email);

            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                return ApiResponse<AuthResultDto>.ErrorResponse("Email já está em uso");
            }

            // Criar o usuário primeiro
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                Role = "Pending", // Líderes começam como pendentes
                FullName = registerDto.FullName,
                CPF = registerDto.CPF,
                City = registerDto.City,
                RegionId = registerDto.RegionId
            };

            var createdUser = await _userRepository.AddAsync(user);

            // NOTA: Líderes não devem ser criados como colportores
            // O registro de líder cria apenas o User com role "Pending"
            
            _logger.LogInformation("Líder registrado com sucesso: UserId={UserId}, Email={Email}", 
                createdUser.Id, createdUser.Email);

            // Não gerar token para usuários pendentes - apenas retornar sucesso
            _logger.LogInformation("Registro realizado com sucesso para: {Email}. Aguardando aprovação.", registerDto.Email);
            return ApiResponse<AuthResultDto>.SuccessResponse(new AuthResultDto { User = _mapper.Map<UserDto>(createdUser) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante registro para email: {Email}", registerDto.Email);
            return ApiResponse<AuthResultDto>.ErrorResponse("Erro interno do servidor");
        }
    }

    public async Task<ApiResponse<AuthResultDto>> RegisterColportorAsync(RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation("Tentativa de registro de colportor para email: {Email}", registerDto.Email);

            if (await _userRepository.EmailExistsAsync(registerDto.Email))
            {
                return ApiResponse<AuthResultDto>.ErrorResponse("Email já está em uso");
            }

            // Criar o usuário primeiro
            var user = new User
            {
                Email = registerDto.Email,
                PasswordHash = HashPassword(registerDto.Password),
                Role = "Colportor", // Colportores começam já aprovados
                FullName = registerDto.FullName,
                CPF = registerDto.CPF,
                City = registerDto.City,
                RegionId = registerDto.RegionId
            };

            var createdUser = await _userRepository.AddAsync(user);

            // Criar o registro de colportor
            if (!string.IsNullOrEmpty(registerDto.FullName) && !string.IsNullOrEmpty(registerDto.CPF))
            {
                var colportor = new Models.Colportor
                {
                    FullName = registerDto.FullName,
                    CPF = registerDto.CPF,
                    Gender = registerDto.Gender,
                    BirthDate = registerDto.BirthDate?.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(registerDto.BirthDate.Value, DateTimeKind.Utc) 
                        : registerDto.BirthDate,
                    City = registerDto.City,
                    PhotoUrl = registerDto.PhotoUrl,
                    RegionId = registerDto.RegionId,
                    LeaderId = registerDto.LeaderId,
                    LastVisitDate = registerDto.LastVisitDate?.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(registerDto.LastVisitDate.Value, DateTimeKind.Utc) 
                        : registerDto.LastVisitDate
                };

                // Usar o contexto diretamente para adicionar o colportor
                var context = _userRepository.GetContext();
                context.Colportors.Add(colportor);
                await context.SaveChangesAsync();

                // Associar o usuário ao colportor
                createdUser.ColportorId = colportor.Id;
                await _userRepository.UpdateAsync(createdUser);

                _logger.LogInformation("Colportor criado e associado ao usuário: UserId={UserId}, ColportorId={ColportorId}", 
                    createdUser.Id, colportor.Id);
            }

            _logger.LogInformation("Registro de colportor realizado com sucesso para: {Email}", registerDto.Email);
            return ApiResponse<AuthResultDto>.SuccessResponse(new AuthResultDto { User = _mapper.Map<UserDto>(createdUser) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante registro de colportor para email: {Email}", registerDto.Email);
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

    public async Task<IEnumerable<UserDto>> GetPendingUsersAsync()
    {
        try
        {
            var users = await _userRepository.GetPendingUsersAsync();
            return users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                FullName = u.FullName,
                CPF = u.CPF,
                City = u.City,
                RegionId = u.RegionId,
                RegionName = u.Region?.Name,
                ColportorId = u.ColportorId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar usuários pendentes");
            throw;
        }
    }

    public async Task<ApiResponse<bool>> ApproveUserAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.ErrorResponse("Usuário não encontrado");
            }

            if (user.Role != "Pending")
            {
                return ApiResponse<bool>.ErrorResponse("Usuário já foi processado");
            }

            user.Role = "Leader";
            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Usuário {UserId} aprovado com sucesso", userId);
            return ApiResponse<bool>.SuccessResponse(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar usuário {UserId}", userId);
            return ApiResponse<bool>.ErrorResponse("Erro interno ao aprovar usuário");
        }
    }

    public async Task<IEnumerable<UserDto>> GetLeadersByRegionAsync(int regionId)
    {
        try
        {
            var leaders = await _userRepository.GetByRegionAsync(regionId);
            return leaders
                .Where(u => u.Role == "Leader") // Apenas líderes aprovados
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    Role = u.Role,
                    FullName = u.FullName,
                    CPF = u.CPF,
                    City = u.City,
                    RegionId = u.RegionId,
                    RegionName = u.Region?.Name,
                    ColportorId = u.ColportorId
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar líderes por região: {RegionId}", regionId);
            throw;
        }
    }

    public async Task<IEnumerable<UserDto>> GetAllLeadersAsync()
    {
        try
        {
            var leaders = await _userRepository.GetByRoleAsync("Leader");
            return leaders.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role,
                FullName = u.FullName,
                CPF = u.CPF,
                City = u.City,
                RegionId = u.RegionId,
                RegionName = u.Region?.Name,
                ColportorId = u.ColportorId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar todos os líderes");
            throw;
        }
    }
}
