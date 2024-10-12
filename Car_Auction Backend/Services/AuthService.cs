using Car_Auction_Backend.Data;
using Car_Auction_Backend.Data.ExtraConfigs;
using Car_Auction_Backend.DTOs;
using Car_Auction_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Car_Auction_Backend.Services
{
	public class AuthService
	{
		private readonly ApplicationDbContext _context;
		private readonly JWTSettings _jwtSettings;
		private readonly IEmailService _emailService;

		public AuthService(ApplicationDbContext context, IOptions<JWTSettings> jwtSettings, IEmailService emailService)
		{
			_context = context;
			_jwtSettings = jwtSettings.Value;
			_emailService = emailService;
		}

		//-------------------------------------------Register-------------------------------------------------------------------------------------------------------------------------------------------------//

		
		public async Task RegisterUser(AuthDto userDto)
		{
			if (_context.users.Any(u => u.UName == userDto.UName) || _context.Admins.Any(a => a.AName == userDto.UName))
			{
				throw new Exception("Username already exists.");
			}

			var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.UPassword);

			if (userDto.URole.ToLower() == "user")
			{
				var user = new User
				{
					UName = userDto.UName,
					UPassword = hashedPassword,
					UEmail = userDto.UEmail,
					URole = "User",
					IsEmailVerified = false,
					EmailVerificationToken = GenerateEmailVerificationToken()
				};
				_context.users.Add(user);
				await _context.SaveChangesAsync();

				await _emailService.SendVerificationEmail(user, user.EmailVerificationToken);
			}

			else if (userDto.URole.ToLower() == "admin")
			{
				var isFirstAdmin = !await _context.Admins.AnyAsync();
				var admin = new Admin
				{
					AName = userDto.UName,
					APassword = hashedPassword,
					AEmail = userDto.UEmail,
					ARole = "Admin",
					IsMainAdmin = isFirstAdmin,
					AStatus = isFirstAdmin ? "Approved" : "Pending"
				};
				_context.Admins.Add(admin);
				await _context.SaveChangesAsync();

				if (!isFirstAdmin)
				{
					await _emailService.SendAdminRegistrationNotification(admin);
				}
			}
			else
			{
				throw new Exception("Invalid role specified.");
			}
		}



		private string GenerateEmailVerificationToken(int userId)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[] { new Claim("id", userId.ToString()) }),
				Expires = DateTime.UtcNow.AddDays(1),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}


		private string GenerateEmailVerificationToken()
		{
			return Guid.NewGuid().ToString();
		}


		public async Task<bool> VerifyEmail(string token)
		{
			var user = await _context.users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
			if (user == null)
			{
				return false;
			}

			user.IsEmailVerified = true;
			user.EmailVerificationToken = null;
			await _context.SaveChangesAsync();
			return true;
		}


		//------------------------------------------login---------------------------------------------------------------------------------------------------------------------------------------------------//

		public async Task<(string accessToken, string refreshToken)> LoginUser(string username, string password)
		{
			var user = await _context.users.FirstOrDefaultAsync(u => u.UName == username);
			if (user != null && BCrypt.Net.BCrypt.Verify(password, user.UPassword))
			{
				if (!user.IsEmailVerified)
				{
					throw new Exception("Please verify your email before logging in.");
				}
				return GenerateTokens(new { Id = user.UId, Name = user.UName, Role = "User" });
			}

			var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AName == username);
			if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.APassword))
			{
				if (admin.AStatus != "Approved")
				{
					throw new Exception("Your admin account is pending approval.");
				}
				return GenerateTokens(admin);
			}

			throw new Exception("Invalid username or password.");
		}
	

	// New method to generate both access and refresh tokens
	private (string accessToken, string refreshToken) GenerateTokens(object user)
	{
		var accessToken = GenerateAccessToken(user);
		var refreshToken = GenerateRefreshToken(user);
		return (accessToken, refreshToken);
	}

		// Modified to generate access token
		private string GenerateAccessToken(object user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var claims = new List<Claim>();

			if (user is Admin admin)
			{
				claims.Add(new Claim(ClaimTypes.NameIdentifier, admin.AId.ToString()));
				claims.Add(new Claim(ClaimTypes.Name, admin.AName));
				claims.Add(new Claim(ClaimTypes.Role, "Admin"));
				claims.Add(new Claim("IsMainAdmin", admin.IsMainAdmin.ToString().ToLower()));
			}
			else if (user is User regularUser)
			{
				claims.Add(new Claim(ClaimTypes.NameIdentifier, regularUser.UId.ToString()));
				claims.Add(new Claim(ClaimTypes.Name, regularUser.UName));
				claims.Add(new Claim(ClaimTypes.Role, "User"));
			}

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		// New method to generate refresh token
		private string GenerateRefreshToken(object user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var claims = new List<Claim>();

			// Safely get properties
			var idProperty = user.GetType().GetProperty("Id");
			var nameProperty = user.GetType().GetProperty("Name");
			var roleProperty = user.GetType().GetProperty("Role");
			var isMainAdminProperty = user.GetType().GetProperty("IsMainAdmin");

			if (idProperty != null)
				claims.Add(new Claim(ClaimTypes.NameIdentifier, idProperty.GetValue(user)?.ToString() ?? ""));

			if (nameProperty != null)
				claims.Add(new Claim(ClaimTypes.Name, nameProperty.GetValue(user)?.ToString() ?? ""));

			if (roleProperty != null)
				claims.Add(new Claim(ClaimTypes.Role, roleProperty.GetValue(user)?.ToString() ?? ""));

			// Add IsMainAdmin claim if the property exists
			if (isMainAdminProperty != null)
				claims.Add(new Claim("IsMainAdmin", isMainAdminProperty.GetValue(user)?.ToString()?.ToLower() ?? "false"));

			// Add the tokenType claim for refresh token
			claims.Add(new Claim("tokenType", "refresh"));

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}


		// New method to refresh tokens
		public (string accessToken, string refreshToken) RefreshTokens(string refreshToken)
	{
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

		try
		{
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = false,
				ValidateAudience = false,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};

			// Validate the refresh token
			ClaimsPrincipal principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out SecurityToken validatedToken);

			// Check if it's actually a refresh token
			if (principal.FindFirst("tokenType")?.Value != "refresh")
			{
				throw new SecurityTokenException("Invalid token type");
			}

			// Extract claims
			var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var username = principal.FindFirst(ClaimTypes.Name)?.Value;
			var role = principal.FindFirst(ClaimTypes.Role)?.Value;

			// Generate new tokens
			return GenerateTokens(new { Id = userId, Name = username, Role = role });
		}
		catch (Exception)
		{
			throw new SecurityTokenException("Invalid refresh token");
		}
	}




		public bool IsMainAdminToken(string token)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

				var validationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
					ValidateLifetime = true,
					ClockSkew = TimeSpan.Zero
				};

				var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

				// Check the role claim
				var roleClaim = principal.FindFirst(ClaimTypes.Role);
				if (roleClaim == null || roleClaim.Value != "Admin")
				{
					return false;
				}

				// Check the IsMainAdmin claim
				var isMainAdminClaim = principal.FindFirst("IsMainAdmin");
				return isMainAdminClaim != null && bool.TryParse(isMainAdminClaim.Value, out bool isMainAdmin) && isMainAdmin;
			}
			catch (Exception ex)
			{
				// Log the exception
				Console.WriteLine($"Error in IsMainAdminToken: {ex.Message}");
				return false;
			}
		}







		private string GenerateJwtToken(Admin admin)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.NameIdentifier, admin.AId.ToString()),
					new Claim(ClaimTypes.Name, admin.AName),
					new Claim(ClaimTypes.Role, "Admin"),
					new Claim("IsMainAdmin", admin.IsMainAdmin.ToString().ToLower())
				}),
				Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		private string GenerateJwtToken(object user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.NameIdentifier, user.GetType().GetProperty("Id").GetValue(user).ToString()),
					new Claim(ClaimTypes.Name, user.GetType().GetProperty("Name").GetValue(user).ToString()),
					new Claim(ClaimTypes.Role, user.GetType().GetProperty("Role").GetValue(user).ToString())
				}),
				Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		

	}
}
