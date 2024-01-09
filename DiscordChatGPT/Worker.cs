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

        TaskCompletionSource taskCompletionSource = new();
        _client.Ready += () =>
        {
            taskCompletionSource.TrySetResult();
            return Task.CompletedTask;
        };
        await taskCompletionSource.Task;

        var interactionService = new InteractionService(_client);
        await interactionService.AddModuleAsync<CommandGroupModule>(_serviceProvider);
        _client.InteractionCreated += async (x) =>
        {
            var ctx = new SocketInteractionContext(_client, x);
            await interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        };

#if DEBUG
        while (!(_client.Guilds.Any(x => x?.Name == "真ホ場") && _client.Guilds.Any(x => x?.Name == "HUIT")))
        {
            await Task.Delay(100);
        }
        var id_np = _client.Guilds.First(guild => guild.Name == "真ホ場").Id;
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
            await channel.SendMessageAsync("DMには近日対応予定！");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var resp = await _botManager.CreateAIResponse(new ChannelInfo(channel.Name, channel.Id), new UserInfo(user.Username, user.Id), message.Content);
                if (resp is not null)
                {
                    _logger.LogDebug(resp);
                    await channel.SendMessageAsync(resp);
                }
            }
            catch (Exception ex)
            {
                var error = WriteErrorResponse(_secretManager, exception: ex);
                _logger.LogError(error);
                await channel.SendMessageAsync(error);
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

    public static string WriteErrorResponse(SecretManager secretManager, string message = "アプリケーションで内部的な不具合が発生しました。", Exception? exception = null)
    {
        string result;
        if (exception is null)
        {
            result = message;
        }
        else
        {
            if (exception.InnerException is null)
            {
                var msg = $"""
				{message}
				以下は問題の特定と修正に役立つ可能性のある情報です。
				```
				{exception.GetType().Name}: {exception.Message}
				{exception.StackTrace}
				```
				""";
                result = msg;
            }
            else
            {
                var msg = $"""
				{message}
				以下は問題の特定と修正に役立つ可能性のある情報です。
				```
				{exception.GetType().Name}: {exception.Message}
				   ---> 内部例外: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}
				{exception.StackTrace}
				```
				""";
                result = msg;
            }
        }

        if (result.Contains(secretManager.OpenAI_APIKey))
        {
            result = result.Replace(secretManager.OpenAI_APIKey, "***");
        }
        if (result.Contains(secretManager.Discord_BotKey))
        {
            result = result.Replace(secretManager.Discord_BotKey, "***");
        }
        return result;
    }
}

[Group("gpt", "chatGPTによるチャットボットを提供します。")]
public class CommandGroupModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly HttpClient _client;
    private readonly ILogger<CommandGroupModule> _logger;
    private readonly SecretManager _secretManager;
    private readonly DiscordBOTManager _botManager;

    public CommandGroupModule(HttpClient httpClient, ILogger<CommandGroupModule> logger, SecretManager secretManager, DiscordBOTManager manager)
    {
        _client = httpClient;
        _logger = logger;
        _secretManager = secretManager;
        _botManager = manager;
    }

    [SlashCommand("activate", "AIを有効化します")]
    public async Task Activate([Choice("gpt3.5", "gpt-3.5-turbo-1106"), Choice("gpt4", "gpt-4-1106-preview"), Choice("gpt4-old", "gpt-4-0613")] string model)
    {
        var channel = Context.Channel;
        _logger.Log(LogLevel.Information, $"チャット開始 場所:{channel.Name}, モデル:{model} 実行者:{Context.User.Username}");
        var resp = await _botManager.Activate(_client, _secretManager, new ChannelInfo(channel.Name, channel.Id), model);
        await RespondAsync(resp);
    }

    /*
	[SlashCommand("administrator-keygen", "管理者として登録します")]
	public async Task AdministratorKeyGen()
	{
		await RespondAsync($"将来的にはDigest風の認証機構が実装される見込みです。");
	}

	[SlashCommand("administrator-login", "管理者として登録します")]
	public async Task AdministratorLogin(string key)
	{
		var user = Context.User.Id;
		await RespondAsync($"[未実装]ユーザー '{user}' を管理者として登録しました。");
	}

	[SlashCommand("balance", "残高を照会します")]
	public async Task Balance()
	{
		var user = Context.User.Id;
		await RespondAsync($"[未実装]ユーザー '{user}' の残高は...今はアルファ版なので無料です！", ephemeral: true);
	}

	[SlashCommand("charge", "管理者が残高を付与します")]
	public async Task Charge(IUser target, decimal amount)
	{
		var sender = Context.User.Id;
		await RespondAsync($"[未実装]ユーザー '{target.Username}'(ID:{target.Id}) に '{amount}' の残高を付与しました。");
	}
	*/

    [SlashCommand("clear", "会話コンテキストをクリアします")]
    public async Task Clear()
    {
        var channel = Context.Channel;
        var resp = await _botManager.Clear(new ChannelInfo(channel.Name, channel.Id));
        await RespondAsync(resp);
    }

    [SlashCommand("exit", "AIとの対話を終了します")]
    public async Task Exit()
    {
        var channel = Context.Channel;
        var resp = await _botManager.Exit(new ChannelInfo(channel.Name, channel.Id));
        await RespondAsync(resp);
    }

    [SlashCommand("register", "ユーザー登録をします")]
    public async Task Register()
    {
        var user = Context.User;
        var resp = await _botManager.RegisterUser(new UserInfo(user.Username, user.Id));
        await RespondAsync(resp);
    }
}