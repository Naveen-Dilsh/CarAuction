using Car_Auction_Backend.DTOs;
using Car_Auction_Backend.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly AuthService _authService;

	public AuthController(AuthService authService)
	{
		_authService = authService;
	}

	[HttpPost("login-user")]
	public IActionResult LoginUser([FromBody] UserDto userDto)
	{
		var user = _authService.FindUser(userDto.UName, userDto.UPassword);

		if (user == null)
			return Unauthorized(new { Message = "Invalid credentials" });

		var token = _authService.GenerateJwtTokenForUser(user);
		return Ok(new { Token = token });
	}

	[HttpPost("login-admin")]
	public IActionResult LoginAdmin([FromBody] AdminDto adminDto)
	{
		var admin = _authService.FindAdmin(adminDto.AName, adminDto.APassword);

		if (admin == null)
			return Unauthorized(new { Message = "Invalid credentials" });

		var token = _authService.GenerateJwtTokenForAdmin(admin);
		return Ok(new { Token = token });
	}
}
