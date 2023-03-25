using System.Text.Json.Serialization;

namespace ChatGPTCLI;

public record ChatGPTChoice([property: JsonPropertyName("message")] ChatGPTMessage Message, [property: JsonPropertyName("finish_reason")] string FinishReason, [property: JsonPropertyName("index")] int Index);
