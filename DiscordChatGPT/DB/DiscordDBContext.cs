using Microsoft.EntityFrameworkCore;

namespace DiscordChatGPT.DB;

public class DiscordDBContext : DbContext
{
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
	}
}
