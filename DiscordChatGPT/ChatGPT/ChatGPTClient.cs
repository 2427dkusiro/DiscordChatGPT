using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatGPTCLI;

/// <summary>
/// ChatGPT を操作するクライアントを表現します。
/// 使用するには、openAIのAPIトークンを取得し、適切に配置する必要があります。
/// </summary>
public class ChatGPTClient
{
	private static readonly string endPoint = "https://api.openai.com/v1/chat/completions";
	private readonly string credential;
	private string model = "gpt-3.5-turbo";

	private static readonly int apiTimeout = 45;
	private static readonly int maxRetry = 3;

	private static readonly int maxHistory = 10;
	private static readonly int maxHistoryToken = 2048;

	private readonly List<ChatGPTMessageHistory> systemPrompts = new();
	private readonly List<ChatGPTMessageHistory> messageHistories = new();

	private readonly HttpClient _client;

	/*
	public ChatGPTClient()
	{
		_client = new()
		{
			Timeout = TimeSpan.FromSeconds(apiTimeout)
		};

		const string path = "./__credential.secret";
		if (File.Exists(path))
		{
			using StreamReader reader = new(path);
			var token = reader.ReadToEnd();
			if (!string.IsNullOrWhiteSpace(token))
			{
				credential = token;
				return;
			}
		}
		throw new NotSupportedException("APIトークンを `__credential.secret` として配置してください。");
	}
	*/

	public ChatGPTClient(string credential, string model, HttpClient client)
	{
		this.credential = credential;
		this.model = model;
		_client = new()
		{
			Timeout = TimeSpan.FromSeconds(apiTimeout)
		};
	}

	/// <summary>
	/// システムプロンプト(AIに対するメタ指示)を追加します。
	/// </summary>
	/// <param name="content"></param>
	public void AddSystemPrompt(string content)
	{
		var length = content.Length; // トークナイザーとかないので雑計算
		systemPrompts.Add(new ChatGPTMessageHistory(new ChatGPTMessage(Roles.System, content), length));
	}

	/// <summary>
	/// AI にメッセージを送信し、その応答を取得します。
	/// </summary>
	/// <param name="content">送信するメッセージの内容。</param>
	/// <returns></returns>
	public async Task<ChatGPTResponse> SendMessageAsync(string content)
	{
		var message = new ChatGPTMessage(Roles.User, content);
		var sys = systemPrompts.Select(x => x.Message);
		var hist = Array.Empty<ChatGPTMessage>();

		var requiredTokens = systemPrompts.Sum(x => x.TokenLength) + content.Length;
		if (requiredTokens < maxHistoryToken) // どうしようも無いときはもう投げる
		{
			int i;
			for (i = 0; i < Math.Min(maxHistory, messageHistories.Count); i++)
			{
				requiredTokens += messageHistories[^(i + 1)].TokenLength;
				if (requiredTokens > maxHistoryToken)
				{
					break;
				}
			}
			hist = messageHistories.TakeLast(i).Select(x => x.Message).ToArray();
		}

		var req = sys.Concat(hist.Append(message)).ToArray(); // スレッド安全のために固めちゃう
		var response = await PostAsync(new ChatGPTRequest(model, req));

		messageHistories.Add(new ChatGPTMessageHistory(message, response.Usage.PromptTokens));
		messageHistories.Add(new ChatGPTMessageHistory(response.Choices.First().Message, response.Usage.CompletionTokens));

		return response;
	}

	/// <summary>
	/// システムプロンプトを除く、すべての会話履歴を初期化します。
	/// </summary>
	public void ClearContext()
	{
		messageHistories.Clear();
	}

	private async Task<ChatGPTResponse> PostAsync(ChatGPTRequest request)
	{
		var options = new JsonSerializerOptions
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		var str = JsonSerializer.Serialize(request, options);
		Exception? excp = null;

		for (int retryCount = 0; retryCount < maxRetry; retryCount++)
		{
			var content = new StringContent(str, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
			var httpReq = new HttpRequestMessage(HttpMethod.Post, endPoint);
			httpReq.Headers.Add("Authorization", $"Bearer {credential}");
			httpReq.Content = content;

			CancellationTokenSource cancellationTokenSource = new();
			HttpResponseMessage resp;
			try
			{
				resp = await _client.SendAsync(httpReq, cancellationTokenSource.Token);
			}
			catch (Exception ex)
			{
				excp = ex;
				cancellationTokenSource.Cancel();
				await Task.Delay(1000);
				continue;
			}

			var body = await resp.Content.ReadAsStringAsync();
			if (resp.IsSuccessStatusCode)
			{
				var obj = JsonSerializer.Deserialize<ChatGPTResponse>(body, options);
				return obj ?? throw new InvalidOperationException($"Unexpected response: {body}");
			}
			else
			{
				excp = new IOException($"API HTTP error response {(int)resp.StatusCode}: {resp.StatusCode}. {body}");
				continue;
			}
		}
		throw new TimeoutException("API retry limit exceeded.", excp);
	}
}

public record ChatGPTMessageHistory(ChatGPTMessage Message, int TokenLength);
