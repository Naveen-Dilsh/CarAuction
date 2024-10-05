using Microsoft.EntityFrameworkCore;
using Car_Auction_Backend.Models;
using Car_Auction_Backend.Data.Configs;

namespace Car_Auction_Backend.Data
{
	public class ApplicationDbContext:DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		public DbSet<Admin> Admins { get; set; }
		public DbSet<Car> Cars { get; set; }
		public DbSet<Bid> Bids { get; set; }


		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.ApplyConfiguration(new AdminConfig());
			modelBuilder.ApplyConfiguration(new CarConfig());
			modelBuilder.ApplyConfiguration(new BidConfig());
		}
	}
}
