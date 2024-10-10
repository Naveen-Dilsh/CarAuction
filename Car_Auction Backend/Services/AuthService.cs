using Car_Auction_Backend.Data;
using Car_Auction_Backend.Data.ExtraConfigs;
using Car_Auction_Backend.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;  // Import this
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Car_Auction_Backend.Services
{
	public class AuthService
	{
		private readonly ApplicationDbContext _context;
		private readonly JWTSettings _jwtSettings;

		// Modify the constructor to accept IOptions<JWTSettings>
		public AuthService(ApplicationDbContext context, IOptions<JWTSettings> jwtSettings)
		{
			_context = context;
			_jwtSettings = jwtSettings.Value;  // Access the actual JWTSettings from IOptions
		}

		// Method to find user in the database
		public User FindUser(string username, string password)
		{
			return _context.users.FirstOrDefault(u => u.UName == username && u.UPassword == password);
		}

		// Method to find admin in the database
		public Admin FindAdmin(string username, string password)
		{
			return _context.Admins.FirstOrDefault(a => a.AName == username && a.APassword == password);
		}

		// Generate JWT Token for User
		public string GenerateJwtTokenForUser(User user)
		{
			return GenerateToken(user.UName, user.URole);
		}

		// Generate JWT Token for Admin
		public string GenerateJwtTokenForAdmin(Admin admin)
		{
			return GenerateToken(admin.AName, admin.ARole);
		}

		// Helper method to generate a JWT token
		private string GenerateToken(string username, string role)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
					new Claim(ClaimTypes.Name, username),
					new Claim(ClaimTypes.Role, role)
				}),
				Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};
			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}
	}
}
