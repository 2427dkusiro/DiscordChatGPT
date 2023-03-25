namespace DiscordChatGPT;

public class SecretManager
{
	public string OpenAI_APIKey { get; }

	public string Discord_BotKey { get; }

	public SecretManager(string openAI_APIKey, string discord_BotKey)
	{
		OpenAI_APIKey = openAI_APIKey;
		Discord_BotKey = discord_BotKey;
	}

	public static SecretManager FromFile(string path)
	{
		using StreamReader reader = new(path);
		var line1 = reader.ReadLine();
		var line2 = reader.ReadLine();

		if (string.IsNullOrWhiteSpace(line1) || string.IsNullOrWhiteSpace(line2))
		{
			throw new IOException("cannnot read secret");
		}

		return new SecretManager(line1, line2);
	}
}
