using System.Text.Json.Serialization;

namespace ChatGPTCLI;

/// <summary>
/// ChatGPT に対するリクエストを表現します。
/// </summary>
public class ChatGPTRequest
{
	public ChatGPTRequest(string model, IEnumerable<ChatGPTMessage> messages)
	{
		Model = model;
		Messages = messages;
	}

	/// <summary>
	/// 使用するモデルの ID を取得します。
	/// Chat API で動作するモデルの詳細については、モデル エンドポイントの互換性テーブルを参照してください。
	/// </summary>
	[JsonPropertyName("model")]
	public string Model { get; }

	/// <summary>
	/// チャットの補完を生成するメッセージを取得します。
	/// </summary>
	[JsonPropertyName("messages")]
	public IEnumerable<ChatGPTMessage> Messages { get; }

	/// <summary>
	/// 使用するサンプリング温度を取得します。
	/// デフォルトは 1 で、有効な範囲は 0 から 2 です。0.8 のような高い値は出力をよりランダムにしますが、0.2 のような低い値はより集中的で確定的なものにします。
	/// </summary>
	[JsonPropertyName("temprature")]
	public double? Temperature { get; set; } = null;

	/// <summary>
	/// 核サンプリングを取得します。
	/// 温度によるサンプリングの代替手段であり、モデルは top_p 確率質量を持つトークンの結果を考慮します。したがって、0.1 は、上位 10% の確率質量を構成するトークンのみが考慮されることを意味します。
	/// <see cref="Temperature"/> 両方を変更することはお勧めしません。
	/// </summary>
	[JsonPropertyName("top_p")]
	public double? TopP { get; set; } = null;

	/// <summary>
	/// 入力メッセージごとに生成するチャット完了の選択肢の数を取得します。
	/// デフォルトは 1 です。
	/// </summary>
	[JsonPropertyName("n")]
	public int? N { get; set; } = null;

	/// <summary>
	/// 出力をストリームするかを表す値を取得します。
	/// 設定すると、ChatGPT のように部分的なメッセージ デルタが送信されます。トークンは、利用可能になるとデータのみのサーバー送信イベントとして送信され、ストリームはdata: [DONE]メッセージで終了します。
	/// デフォルトは <c>false</c> です。
	/// </summary>
	[JsonPropertyName("stream")]
	public bool? DoStreamimg { get; set; } = null;

	/// <summary>
	/// API がそれ以上のトークンの生成を停止する最大 4 つのシーケンスを取得します。
	/// デフォルトは <c>null</c> です。
	/// </summary>
	[JsonPropertyName("stop")]
	public string[]? Stop { get; set; } = null;

	/// <summary>
	/// チャット完了で生成するトークンの最大数を取得します。
	/// 入力トークンと生成されたトークンの合計の長さは、モデルのコンテキストの長さによって制限されます。
	/// デフォルトは +INF です。
	/// </summary>
	[JsonPropertyName("max_tokens")]
	public int? MaxTokenCount { get; set; } = null;

	/// <summary>
	/// プレゼンス_ペナルティ の値を取得します。
	/// -2.0 から 2.0 までの数値。正の値は、それまでのテキストに出現するかどうかに基づいて新しいトークンにペナルティを課し、モデルが新しいトピックについて話す可能性を高めます。
	/// </summary>
	[JsonPropertyName("presence_penalty")]
	public double? PresencePenalty { get; set; } = null;

	/// <summary>
	/// 頻度_ペナルティ の値を取得します。
	/// -2.0 から 2.0 までの数値。正の値は、これまでのテキスト内の既存の頻度に基づいて新しいトークンにペナルティを課し、モデルが同じ行を逐語的に繰り返す可能性を減らします。
	/// </summary>
	[JsonPropertyName("frequency_penalty")]
	public double? FrequencyPenalty { get; set; } = null;

	/// <summary>
	/// 指定したトークンが補完に表示される可能性を変更する値を取得します。
	/// トークン(トークナイザーのトークン ID で指定) を -100 から 100 の関連するバイアス値にマップする json オブジェクトを受け入れます。数学的には、サンプリングの前にモデルによって生成されたロジットにバイアスが追加されます。正確な効果はモデルごとに異なりますが、-1 から 1 の間の値では、選択の可能性が減少または増加します。-100 や 100 などの値を指定すると、関連するトークンが禁止または排他的に選択されます。
	/// </summary>
	[JsonPropertyName("logit_bias")]
	public Dictionary<string, double>? LogitBias { get; set; } = null;

	/// <summary>
	/// エンドユーザーを表す一意の識別子を取得します。
	/// これは、OpenAI が不正行為を監視および検出するのに役立ちます。
	/// </summary>
	[JsonPropertyName("user")]
	public string? User { get; set; } = null;
}