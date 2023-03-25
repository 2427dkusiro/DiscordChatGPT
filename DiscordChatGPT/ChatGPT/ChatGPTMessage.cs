using System.Text.Json.Serialization;

namespace ChatGPTCLI;
/// <summary>
/// ChatGPT におけるメッセージを表現します。
/// </summary>
/// <param name="Role"> メッセージの作成者の役割を取得します。 </param>
/// <param name="Content"> メッセージの本文を取得します。 </param>
public record ChatGPTMessage([property: JsonPropertyName("role")] Roles Role, [property: JsonPropertyName("content")] string Content);
