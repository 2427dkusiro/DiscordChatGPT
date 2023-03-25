using DiscordChatGPT;

var instance = await DiscordBOTManager.GetInstance();
var secret = SecretManager.FromFile("./Secret.txt");

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHostedService<Worker>();

		services.AddSingleton<HttpClient>();
		services.AddSingleton(secret);
		services.AddSingleton(instance);
	})
	.Build();

host.Run();
