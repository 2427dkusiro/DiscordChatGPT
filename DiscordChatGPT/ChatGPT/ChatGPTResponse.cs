using System.Text.Json.Serialization;

namespace ChatGPTCLI;

/// <summary>
/// ChatGPTのレスポンスを表現します。
/// </summary>
/// <param name="Id">レスポンスの ID。</param>
/// <param name="ObjectName">レスポンスを作成した環境の名前。</param>
/// <param name="CreatedTimeStamp">レスポンスの作成時刻。</param>
/// <param name="ModelName">使用したモデル。</param>
/// <param name="Usage">使用料金の情報。</param>
/// <param name="Choices">AI が生成した応答の候補。</param>
public record ChatGPTResponse([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("object")] string ObjectName, [property: JsonPropertyName("created")] long CreatedTimeStamp, [property: JsonPropertyName("model")] string ModelName, [property: JsonPropertyName("usage")] ChatGPTUsage Usage, [property: JsonPropertyName("choices")] ChatGPTChoice[] Choices);
