using System.Net.Http;
using System.Text;
using System.Web;
using GTranslate.Translators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreTranPlus;

public class TranslationService : ITranslationService
{
    private readonly YandexTranslator _yandexTranslator;
    private readonly BingTranslator _bingTranslator;
    private readonly SettingsModel _settings;

    public TranslationService(ISettingsService settingsService)
    {
        _yandexTranslator = new YandexTranslator();
        _bingTranslator = new BingTranslator();
        _settings = settingsService.Settings;
    }

    public string Translate(string input, Enumerations.Translator translator, Enumerations.TargetLanguage targetLanguage)
    {
        return Task.Run(async () => await TranslateAsync(input, translator, targetLanguage)).Result;
    }

    public string TranslateVision(List<byte[]> images, Enumerations.TargetLanguage targetLanguage)
    {
        return Task.Run(async () => await TranslateVisionAsync(images, targetLanguage)).Result;
    }

    private string GetLangCode(Enumerations.TargetLanguage lang)
    {
        return lang switch
        {
            Enumerations.TargetLanguage.Russian => "ru",
            Enumerations.TargetLanguage.Ukrainian => "uk",
            Enumerations.TargetLanguage.English => "en",
            Enumerations.TargetLanguage.Spanish => "es",
            Enumerations.TargetLanguage.German => "de",
            Enumerations.TargetLanguage.French => "fr",
            Enumerations.TargetLanguage.Japanese => "ja",
            _ => "ru"
        };
    }

    private async Task<string> TranslateAsync(string input, Enumerations.Translator translator, Enumerations.TargetLanguage targetLanguage)
    {
        var langCode = GetLangCode(targetLanguage);

        if (translator == Enumerations.Translator.Google)
            return await TranslateGoogleAsync(input, langCode);
        if (translator == Enumerations.Translator.Yandex)
            return (await _yandexTranslator.TranslateAsync(input, langCode)).Translation;
        if (translator == Enumerations.Translator.Bing)
            return (await _bingTranslator.TranslateAsync(input, langCode)).Translation;
        if (translator == Enumerations.Translator.Gemini)
            return await TranslateGeminiAsync(input, targetLanguage.ToString());

        return input;
    }

    public async Task<string> TranslateGoogleAsync(string input, string langCode)
    {
        var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={langCode}&dt=t&q={HttpUtility.UrlEncode(input)}";

        using var client = new HttpClient();
        var response = await client.GetStringAsync(url).ConfigureAwait(false);
        return string.Join(string.Empty, JArray.Parse(response)[0].Select(x => x[0]));
    }

    private async Task<string> TranslateGeminiAsync(string input, string targetLanguage)
    {
        var apiKey = _settings.GeminiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Error: Gemini API Key is missing!";
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key={apiKey}";

        // Промпт требует JSON ответа
        var prompt = $"Translate the following text into natural, fluent {targetLanguage}. " +
                     "Return your response strictly as a JSON object matching this schema: " +
                     "{ \"hasText\": true, \"translation\": \"string\" }. " +
                     "If the input is empty or invalid, set hasText to false and translation to empty string. " +
                     $"Do not write any markdown code block wrap tags. Just raw JSON:\n\n{input}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        try
        {
            var json = JsonConvert.SerializeObject(requestBody);
            using var client = new HttpClient();
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return $"Error: API returned status {response.StatusCode}";
            }

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jsonDoc = JObject.Parse(responseJson);
            var rawText = jsonDoc["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

            var cleanJson = CleanMarkdownBlocks(rawText);
            var result = JObject.Parse(cleanJson);

            if (result["hasText"]?.Value<bool>() == true)
            {
                return result["translation"]?.ToString() ?? string.Empty;
            }
            return "PRESERVE_LAST"; // Сигнальная строка, чтобы не затирать старый перевод
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> TranslateVisionAsync(List<byte[]> images, Enumerations.TargetLanguage targetLanguage)
    {
        var apiKey = _settings.GeminiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Error: Gemini API Key is missing!";
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent?key={apiKey}";

        var parts = new List<object>();

        foreach (var imgBytes in images)
        {
            parts.Add(new
            {
                inlineData = new
                {
                    mimeType = "image/png",
                    data = Convert.ToBase64String(imgBytes)
                }
            });
        }

        // Инструкция на строгое JSON-форматирование
        var prompt = $"Analyze these cropped screenshots from a video game. Read all dialogue and narrative text in order. " +
                     $"Translate them into natural, high-quality {targetLanguage}. " +
                     "Return your response strictly as a JSON object matching this schema: " +
                     "{ \"hasText\": bool, \"translation\": \"string\" }. " +
                     "If there is no readable dialogue or text in the images, set hasText to false and translation to empty string. " +
                     "Return ONLY the JSON object, do not wrap it in markdown block tags (like ```json ... ```).";

        parts.Add(new { text = prompt });

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = parts.ToArray() }
            }
        };

        try
        {
            var json = JsonConvert.SerializeObject(requestBody);
            using var client = new HttpClient();
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return $"Error: API returned status {response.StatusCode}";
            }

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var jsonDoc = JObject.Parse(responseJson);
            var rawText = jsonDoc["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

            var cleanJson = CleanMarkdownBlocks(rawText);
            var result = JObject.Parse(cleanJson);

            if (result["hasText"]?.Value<bool>() == true)
            {
                return result["translation"]?.ToString() ?? string.Empty;
            }
            return "PRESERVE_LAST"; // Сигнал сохранить прошлый перевод на экране
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Очищает ответ Gemini от возможных маркдаун-тегов ```json ... ```
    /// </summary>
    private string CleanMarkdownBlocks(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "{}";

        input = input.Trim();
        if (input.StartsWith("```"))
        {
            var lines = input.Split('\n');
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("```"))
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString().Trim();
        }
        return input;
    }
}