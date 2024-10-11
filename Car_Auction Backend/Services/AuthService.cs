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

		public async Task<string> LoginUser(string username, string password)
		{
			var user = await _context.users.FirstOrDefaultAsync(u => u.UName == username);
			if (user != null && BCrypt.Net.BCrypt.Verify(password, user.UPassword))
			{
				if (!user.IsEmailVerified)
				{
					throw new Exception("Please verify your email before logging in.");
				}
				return GenerateJwtToken(new { Id = user.UId, Name = user.UName, Role = "User" });
			}

			var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AName == username);
			if (admin != null && BCrypt.Net.BCrypt.Verify(password, admin.APassword))
			{
				if (admin.AStatus != "Approved")
				{
					throw new Exception("Your admin account is pending approval.");
				}
				return GenerateJwtToken(admin);
			}

			throw new Exception("Invalid username or password.");
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

		public bool IsMainAdminToken(string token)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

				tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false,
					ClockSkew = TimeSpan.Zero
				}, out SecurityToken validatedToken);

				var jwtToken = (JwtSecurityToken)validatedToken;

				// Logging claims for debugging
				foreach (var claim in jwtToken.Claims)
				{
					Console.WriteLine($"Claim: {claim.Type} - Value: {claim.Value}");
				}

				var roleClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "role");
				var isMainAdminClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "IsMainAdmin");

				if (roleClaim == null || isMainAdminClaim == null)
				{
					Console.WriteLine("Role or IsMainAdmin claim is missing.");
					return false;
				}

				var role = roleClaim.Value;
				var isMainAdmin = isMainAdminClaim.Value;
				Console.WriteLine("All good");
				return role == "Admin" && string.Equals(isMainAdmin, "true", StringComparison.OrdinalIgnoreCase);
				


			}
			catch
			{
				return false;
			}
		}

	}
}
