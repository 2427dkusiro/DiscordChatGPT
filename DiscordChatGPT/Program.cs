using DiscordChatGPT;

using Microsoft.EntityFrameworkCore;

var instance = await DiscordBOTManager.GetInstance();
var secret = SecretManager.FromFile("./Secret.txt");

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHostedService<Worker>();

		services.AddSingleton<HttpClient>();
		services.AddDbContext<DiscordChatGPT.DB.DiscordDBContext>(options =>
		{
			options.UseInMemoryDatabase("TestDB");
		});
		services.AddSingleton(secret);
		services.AddSingleton(instance);
	})
	.Build();

host.Run();
