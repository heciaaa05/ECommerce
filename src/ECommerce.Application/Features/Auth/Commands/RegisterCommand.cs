using ECommerce.Application.DTOs.Auth;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Application.Features.Auth.Commands;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        var req = request.Request;

        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
        if (existing is not null)
            throw new InvalidOperationException("Email đã được sử dụng.");

        var user = new User
        {
            Email = req.Email,
            PasswordHash = _passwordHasher.Hash(req.Password),
            FullName = req.FullName,
            PhoneNumber = req.PhoneNumber,
            Role = UserRole.Customer
        };

        _context.Users.Add(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshTokenValue = _jwtService.GenerateRefreshToken();

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshTokenValue,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _context.SaveChangesAsync(ct);

        return new AuthResponse(
            user.Id,
            user.Email,
            user.FullName,
            accessToken,
            refreshTokenValue,
            _jwtService.GetAccessTokenExpiry()
        );
    }
}
