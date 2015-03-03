using System.Data.Entity;

namespace SolrDbFiller.Db
{
	public class SolrDbContext : DbContext
	{
		public DbSet<Company> Companies { get; set; }
	}
}