using System.ComponentModel.DataAnnotations;

namespace Car_Auction_Backend.DTOs
{
	public class UserDto
	{
		[Required]
		public string UName {  get; set; }

		[Required]
		public string UPassword { get; set; }
	}
}
