using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Car_Auction_Backend.Models
{
	public class Bid
	{

		public int BidId { get; set; }

		//Foreign Key
		public int AdminId { get; set; }

		public decimal OpeningBid { get; set; }

		public DateTime StartTime { get; set; }

		public DateTime EndTime { get; set; }

		public virtual Admin Admin { get; set; }
	}

}
