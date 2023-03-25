using System.Diagnostics;

namespace ChatGPTCLI;

class Program
{
	static async Task Main()
	{

		var initMessage = """
=== ChatGPT API テストクライアント ===

`#balance` を入力して使用料金を確認します。
`#clear` を入力して対話コンテキストをリセットします。
`#exit` を入力して終了します。

""";

		var client = new ChatGPTClient();
		var tokens = 0L;
		const decimal pricePerToken = 0.002m / 1000m * 130.71m;

		void WriteLineColor(string msg, ConsoleColor col)
		{
			var c = Console.ForegroundColor;
			Console.ForegroundColor = col;
			Console.WriteLine(msg);
			Console.ForegroundColor = c;
		}

		WriteLineColor(initMessage, ConsoleColor.Green);

		client.AddSystemPrompt("ここではチャットGPTならぬ「キャットGPT」としてふるまい、語尾を「にゃー」か「にゃん」にし、全体的にかわいらしい文体で応答してください。");

		while (true)
		{
			Console.Write("You > ");
			var input = Console.ReadLine();
			if (input is null)
			{
				return;
			}

			if (input.StartsWith('#'))
			{
				var command = input[1..];
				switch (command)
				{
					case "balance":
						WriteLineColor($"* トークン数: {tokens}", ConsoleColor.Green);
						WriteLineColor($"* 推定料金: {tokens * pricePerToken}円", ConsoleColor.Green);
						break;
					case "exit":
						return;
					case "clear":
						client.ClearContext();
						WriteLineColor($"* コンテキストはクリアされました", ConsoleColor.Green);
						break;
					default:
						WriteLineColor("認識されないコマンドです", ConsoleColor.Green);
						break;
				}
			}
			else
			{
				WriteLineColor("* request send", ConsoleColor.DarkGreen);
				var watch = Stopwatch.StartNew();

				var resp = await client.SendMessageAsync(input);

				watch.Stop();
				WriteLineColor($"* receive resnponse in {watch.ElapsedMilliseconds}ms", ConsoleColor.DarkGreen);
				var lines = resp.Choices.First().Message.Content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.TrimEntries);
				var msg = string.Join(Environment.NewLine, lines.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => "AI > " + x));

				tokens += resp.Usage.TotalTokens;
				Console.WriteLine(msg);
			}
		}
	}
}