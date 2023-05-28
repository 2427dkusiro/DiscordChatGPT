using ChatGPTWrapper;

using System;

namespace DiscordChatGPT;
public class DiscordBOTManager
{
	private readonly List<ChannelInfo> channels;

	private readonly Dictionary<ChannelInfo, ChatGPTClient> chatGPTs;

	private readonly List<UserInfo> users;

	protected DiscordBOTManager()
	{
		channels = new();
		chatGPTs = new();
		users = new();
	}

	public static async Task<DiscordBOTManager> GetInstance()
	{
		// 将来的にDBアクセスを伴って復元、ということになる公算大
		return new DiscordBOTManager();
	}

	public ValueTask<string> Activate(HttpClient client, SecretManager secretManager, ChannelInfo channel, string model)
	{
		// 二重追加回避
		lock (channels)
		{
			if (channels.Any(c => c.Id == channel.Id))
			{
				// TODO;複数モデルがある場合の切り替え？
				return new ValueTask<string>($"チャンネル '{channel.Name}' ではすでにAIチャットが有効です。");
			}
			else
			{
				channels.Add(channel);
				var gpt = new ChatGPTClient(secretManager.OpenAI_APIKey, model, client);
				gpt.AddSystemPrompt("ここではチャットGPTならぬ「キャットGPT」としてふるまい、語尾を「にゃー」か「にゃん」にし、全体的にかわいらしい文体で応答してください。");
				chatGPTs.Add(channel, gpt);
				return new ValueTask<string>($"チャンネル '{channel.Name}' で '{model}' AIチャットが有効化されました。");
			}
		}
	}

	public ValueTask<string> Clear(ChannelInfo channel)
	{
		// 排他制御？
		if (!channels.Any(c => c.Id == channel.Id))
		{
			return new ValueTask<string>($"チャンネル '{channel.Name}' ではAIチャットが有効ではありません。");
		}
		else
		{
			chatGPTs[channel].ClearContext();
			return new ValueTask<string>($"チャンネル '{channel.Name}' での会話コンテキストが初期化されました。");
		}
	}

	public ValueTask<string> Exit(ChannelInfo channel)
	{
		// 排他制御？
		if (!channels.Any(c => c.Id == channel.Id))
		{
			return new ValueTask<string>($"チャンネル '{channel.Name}' ではAIチャットが有効ではありません。");
		}
		else
		{
			channels.Remove(channel);
			chatGPTs.Remove(channel);
			return new ValueTask<string>($"チャンネル '{channel.Name}' でのAIチャットは終了しました。");
		}
	}

	public ValueTask<string> RegisterUser(UserInfo user)
	{
		if (users.Any(u => u.Id == user.Id))
		{
			return new ValueTask<string>($"{user.Name} さんはすでに登録済のユーザーです。");
		}
		else
		{
			users.Add(user);
			return new ValueTask<string>($"ようこそ。{user.Name} さん。利用規約が適用されます。(課金機能が実装されるまで無効)");
		}
	}

	public async ValueTask<string?> CreateAIResponse(ChannelInfo channel, UserInfo user, string content)
	{
		// 課金機能ができるまで、どんなユーザーにも応答する
		if (channels.Any(x => x.Id == channel.Id) /* && users.Any(x => x.Id == user.Id)*/ )
		{
			var client = chatGPTs[channel];
			// 排他制御？
			var resp = await client.SendMessageAsync(content);
			// ここで課金処理
			return resp.Choices.First().Message.Content;
		}
		return null;
	}
}

public record struct ChannelInfo(string Name, ulong Id);

public record struct UserInfo(string Name, ulong Id);
