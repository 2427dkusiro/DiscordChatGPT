using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace DiscordChatGPT;

public class Worker : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<Worker> _logger;
	private readonly SecretManager _secretManager;
	private readonly DiscordBOTManager _botManager;

	private DiscordSocketClient _client;

	public Worker(IServiceProvider provider, ILogger<Worker> logger, SecretManager secretManager, DiscordBOTManager botManager)
	{
		_serviceProvider = provider;
		_logger = logger;
		_secretManager = secretManager;
		_botManager = botManager;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_client = new DiscordSocketClient(new DiscordSocketConfig()
		{
			GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
		});

		_client.Log += msg => WriteLog(_logger, msg);

		await _client.LoginAsync(TokenType.Bot, _secretManager.Discord_BotKey);
		await _client.StartAsync();

		var interactionService = new InteractionService(_client);
		await interactionService.AddModuleAsync<CommandGroupModule>(_serviceProvider);
		_client.InteractionCreated += async (x) =>
		{
			var ctx = new SocketInteractionContext(_client, x);
			await interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
		};

#if DEBUG
		while (!(_client.Guilds.Any(x => x?.Name == "�^�z��") && _client.Guilds.Any(x => x?.Name == "HUIT")))
		{
			await Task.Delay(100);
		}
		var id_np = _client.Guilds.First(guild => guild.Name == "�^�z��").Id;
		var id_huit = _client.Guilds.First(guild => guild.Name == "HUIT").Id;
		_logger.LogInformation($"find debug guild id:{id_np}");
		await interactionService.RegisterCommandsToGuildAsync(id_np);
		await interactionService.RegisterCommandsToGuildAsync(id_huit);
#else
		await interactionService.RegisterCommandsGloballyAsync();
#endif

		_client.MessageReceived += HandleMessage;

		_logger.LogInformation("start eventloop...");
		await Task.Delay(-1, stoppingToken);
	}

	private async Task HandleMessage(SocketMessage message)
	{
		var user = message.Author;
		if (user.IsBot)
		{
			return;
		}
		var channel = message.Channel;
		if (message.Channel is IDMChannel)
		{
			await channel.SendMessageAsync("DM�ɂ͋ߓ��Ή��\��I");
			return;
		}

		_ = Task.Run(async () =>
		{
			var resp = await _botManager.CreateAIResponse(new ChannelInfo(channel.Name, channel.Id), new UserInfo(user.Username, user.Id), message.Content);
			if (resp is not null)
			{
				await channel.SendMessageAsync(resp);
			}
		});
		return;
	}

	private static Task WriteLog(ILogger<Worker> logger, LogMessage msg)
	{
		var level = msg.Severity switch
		{
			LogSeverity.Critical => LogLevel.Critical,
			LogSeverity.Error => LogLevel.Error,
			LogSeverity.Warning => LogLevel.Warning,
			LogSeverity.Info => LogLevel.Information,
			LogSeverity.Debug => LogLevel.Debug,
			_ => LogLevel.None,
		};
		logger.Log(level, msg.Message);
		return Task.CompletedTask;
	}
}

[Group("gpt", "chatGPT�ɂ��`���b�g�{�b�g��񋟂��܂��B")]
public class CommandGroupModule : InteractionModuleBase<SocketInteractionContext>
{
	private readonly HttpClient _client;
	// private readonly ILogger<CommandGroupModule> _logger;
	private readonly SecretManager _secretManager;
	private readonly DiscordBOTManager _botManager;

	public CommandGroupModule(HttpClient httpClient, SecretManager secretManager, DiscordBOTManager manager)
	{
		_client = httpClient;
		_secretManager = secretManager;
		_botManager = manager;
	}

	[SlashCommand("activate", "AI��L�������܂�")]
	public async Task Activate([Choice("gpt3.5", "gpt-3.5-turbo"), Choice("gpt4(not available)", "gpt4(NA)")] string model)
	{
		if (model == "gpt4(NA)")
		{
			await RespondAsync("�Ή����Ă��Ȃ����f���ł�(���p�\����)");
		}

		var channel = Context.Channel;
		var resp = await _botManager.Activate(_client, _secretManager, new ChannelInfo(channel.Name, channel.Id), model);
		await RespondAsync(resp);
	}

	[SlashCommand("administrator-keygen", "�Ǘ��҂Ƃ��ēo�^���܂�")]
	public async Task AdministratorKeyGen()
	{
		await RespondAsync($"�����I�ɂ�Digest���̔F�؋@�\����������錩���݂ł��B");
	}

	[SlashCommand("administrator-login", "�Ǘ��҂Ƃ��ēo�^���܂�")]
	public async Task AdministratorLogin(string key)
	{
		var user = Context.User.Id;
		await RespondAsync($"[������]���[�U�[ '{user}' ���Ǘ��҂Ƃ��ēo�^���܂����B");
	}

	[SlashCommand("balance", "�c�����Ɖ�܂�")]
	public async Task Balance()
	{
		var user = Context.User.Id;
		await RespondAsync($"[������]���[�U�[ '{user}' �̎c����...���̓A���t�@�łȂ̂Ŗ����ł��I", ephemeral: true);
	}

	[SlashCommand("charge", "�Ǘ��҂��c����t�^���܂�")]
	public async Task Charge(IUser target, decimal amount)
	{
		var sender = Context.User.Id;
		await RespondAsync($"[������]���[�U�[ '{target.Username}'(ID:{target.Id}) �� '{amount}' �̎c����t�^���܂����B");
	}

	[SlashCommand("clear", "��b�R���e�L�X�g���N���A���܂�")]
	public async Task Clear()
	{
		var channel = Context.Channel;
		var resp = await _botManager.Clear(new ChannelInfo(channel.Name, channel.Id));
		await RespondAsync(resp);
	}

	[SlashCommand("exit", "AI�Ƃ̑Θb���I�����܂�")]
	public async Task Exit()
	{
		var channel = Context.Channel;
		var resp = await _botManager.Exit(new ChannelInfo(channel.Name, channel.Id));
		await RespondAsync(resp);
	}

	[SlashCommand("register", "���[�U�[�o�^�����܂�")]
	public async Task Register()
	{
		var user = Context.User;
		var resp = await _botManager.RegisterUser(new UserInfo(user.Username, user.Id));
		await RespondAsync(resp);
	}
}