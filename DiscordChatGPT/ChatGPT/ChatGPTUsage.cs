using System.Text.Json.Serialization;

namespace ChatGPTCLI;

/// <summary>
/// ChatGPT の使用料金情報を表現します。
/// </summary>
/// <param name="PromptTokens">ユーザーが入力したプロンプトのトークン数。</param>
/// <param name="CompletionTokens">AI が生成したメッセージのトークン数。</param>
/// <param name="TotalTokens">合計のトークン数。これに基づいて課金されます。</param>
public record ChatGPTUsage([property: JsonPropertyName("prompt_tokens")] int PromptTokens, [property: JsonPropertyName("completion_tokens")] int CompletionTokens, [property: JsonPropertyName("total_tokens")] int TotalTokens);
