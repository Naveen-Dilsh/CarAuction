using Car_Auction_Backend.DTOs;
using Car_Auction_Backend.Services;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Car_Auction_Backend.Data;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly AuthService _authService;

	public AuthController(AuthService authService)
	{
		_authService = authService;
	}


	//-------------------------------------------------Register-----------------------------------------//

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] AuthDto userDto)
	{
		try
		{
			await _authService.RegisterUser(userDto);
			if (userDto.URole.ToLower() == "user")
			{
				return Ok(new { Message = "Registration successful! Please check your email to verify your account." });
			}
			else if (userDto.URole.ToLower() == "admin")
			{
				return Ok(new { Message = "Admin registration successful! We will inform you once your account is approved." });
			}
			else
			{
				return BadRequest(new { Message = "Invalid role specified." });
			}
		}
		catch (Exception ex)
		{
			return BadRequest(new { Message = ex.Message });
		}
	}

	//-----------------------------------------------VerifyEmail-------------------------------------------------//
	
	[HttpGet("verify-email")]
	public async Task<IActionResult> VerifyEmail([FromQuery] string token)
	{
		var result = await _authService.VerifyEmail(token);
		if (result)
		{
			return Ok(new { Message = "Email verified successfully." });
		}
		return BadRequest(new { Message = "Invalid verification token." });
	}

	//--------------------------------------------Login---------------------------------------------------//
	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
	{
		try
		{
			var token = await _authService.LoginUser(loginDto.Username, loginDto.Password);
			return Ok(new { Token = token });
		}
		catch (Exception ex)
		{
			return BadRequest(new { Message = ex.Message });
		}
	}



	[HttpPost("check-main-admin")]
	public IActionResult CheckMainAdmin([FromBody] TokenDto tokenDto)
	{
		var isMainAdmin = _authService.IsMainAdminToken(tokenDto.Token);
		return Ok(new { isMainAdmin = isMainAdmin });
	}






}
