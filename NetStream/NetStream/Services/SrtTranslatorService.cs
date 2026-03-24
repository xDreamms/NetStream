using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace NetStream.Services
{
    public class SrtTranslatorService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        private const string GOOGLE_TRANSLATE_URL = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}";
        private const int AiMaxBlocksPerBatch = 40;
        private const int AiMaxCharsPerBatch = 6000;
        private static readonly string[] GeminiModelCandidates = { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-2.0-flash-lite" };

        public event EventHandler<string> StatusChanged;
        public event EventHandler<double> ProgressChanged;

        public async Task<string> TranslateSrtFileAsync(string inputSrtPath, string targetLanguageIso, string outputPath, CancellationToken ct = default)
        {
            ReportStatus("Reading subtitle file...");

            string srtContent = await File.ReadAllTextAsync(inputSrtPath, Encoding.UTF8, ct);
            var blocks = ParseSrt(srtContent);

            if (blocks.Count == 0)
            {
                ReportStatus("ERROR: No subtitle blocks found in SRT file.");
                return null;
            }

            ReportStatus($"Found {blocks.Count} subtitle blocks. Starting translation...");

            bool isAI = Method.Contains("OpenAI", StringComparison.OrdinalIgnoreCase) ||
                        Method.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase) ||
                        Method.Contains("Gemini", StringComparison.OrdinalIgnoreCase) ||
                        Method.Contains("DeepL", StringComparison.OrdinalIgnoreCase);

            if (isAI)
            {
                var aiBatches = BuildAiBatches(blocks);
                ReportStatus($"Split into {aiBatches.Count} AI batches. Starting translation...");

                int translatedBlockCount = 0;
                foreach (var batchBlocks in aiBatches)
                {
                    ct.ThrowIfCancellationRequested();

                    string payload = CreateSrtPayload(batchBlocks);
                    string translatedPayload = await TranslateTextAsync(payload, "auto", targetLanguageIso, ct);

                    if (string.IsNullOrEmpty(translatedPayload))
                    {
                        ReportStatus("ERROR: Translation failed or was aborted.");
                        return null;
                    }

                    translatedPayload = NormalizeAiSubtitlePayload(translatedPayload);
                    var translatedBlocks = ParseSrt(translatedPayload);

                    if (translatedBlocks.Count == 0 && batchBlocks.Count == 1)
                    {
                        batchBlocks[0].TranslatedText = translatedPayload.Trim();
                    }
                    else
                    {
                        foreach (var origBlock in batchBlocks)
                        {
                            var translatedBlock = translatedBlocks.FirstOrDefault(x =>
                                x.SequenceNumber == origBlock.SequenceNumber || x.Timestamp == origBlock.Timestamp);

                            if (translatedBlock != null && !string.IsNullOrWhiteSpace(translatedBlock.Text))
                            {
                                origBlock.TranslatedText = translatedBlock.Text;
                            }
                            else
                            {
                                origBlock.TranslatedText = origBlock.Text;
                            }
                        }
                    }

                    translatedBlockCount += batchBlocks.Count;
                    double progress = (double)translatedBlockCount / blocks.Count * 100;
                    ReportProgress(progress);
                    ReportStatus($"Translated {translatedBlockCount}/{blocks.Count} blocks...");

                    if (translatedBlockCount < blocks.Count)
                    {
                        await Task.Delay(Method.Contains("Gemini", StringComparison.OrdinalIgnoreCase) ? 900 : 450, ct);
                    }
                }
            }
            else
            {
                // Group consecutive blocks that form a single sentence for Google Translate
                var sentenceGroups = GroupBlocksIntoSentences(blocks);
                ReportStatus($"Grouped into {sentenceGroups.Count} sentences. Starting translation...");

                // Translate each sentence group
                for (int g = 0; g < sentenceGroups.Count; g++)
                {
                    ct.ThrowIfCancellationRequested();

                    var group = sentenceGroups[g];

                    // Merge text from all blocks in this group into one sentence
                    string mergedText = string.Join(" ", group.Select(idx => blocks[idx].Text.Replace("\r", " ").Replace("\n", " ")));

                    // Translate the full sentence
                    string translated = await TranslateTextAsync(mergedText, "auto", targetLanguageIso, ct);

                    if (!string.IsNullOrEmpty(translated))
                    {
                        // Split translated text back across the original blocks proportionally
                        SplitTranslationAcrossBlocks(blocks, group, translated);
                    }

                    // Report progress based on blocks completed
                    int totalBlocksDone = group[group.Count - 1] + 1;
                    double progress = (double)totalBlocksDone / blocks.Count * 100;
                    ReportProgress(progress);

                    if ((g + 1) % 10 == 0 || g == sentenceGroups.Count - 1)
                    {
                        ReportStatus($"Translated {g + 1}/{sentenceGroups.Count} sentences ({totalBlocksDone}/{blocks.Count} blocks)...");
                    }

                    // Small delay every 5 sentences to avoid rate limiting
                    if ((g + 1) % 5 == 0 && g + 1 < sentenceGroups.Count)
                    {
                        await Task.Delay(100, ct);
                    }
                }
            }

            // Write output SRT
            ReportStatus("Writing translated subtitle file...");
            var sb = new StringBuilder();
            for (int i = 0; i < blocks.Count; i++)
            {
                sb.AppendLine(blocks[i].SequenceNumber.ToString());
                sb.AppendLine(blocks[i].Timestamp);
                // In case it's missed, fallback to original txt
                string finalTxt = string.IsNullOrWhiteSpace(blocks[i].TranslatedText) ? blocks[i].Text : blocks[i].TranslatedText;
                sb.AppendLine(finalTxt);
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(outputPath, sb.ToString(), new UTF8Encoding(false), ct);
            ReportStatus($"Translation complete! Saved to: {Path.GetFileName(outputPath)}");

            return outputPath;
        }

        /// <summary>
        /// Groups consecutive SRT blocks that form a single sentence.
        /// A block ends a sentence if its text ends with . ! or ?
        /// Otherwise it continues into the next block.
        /// </summary>
        private List<List<int>> GroupBlocksIntoSentences(List<SrtBlock> blocks)
        {
            var groups = new List<List<int>>();
            var currentGroup = new List<int>();

            for (int i = 0; i < blocks.Count; i++)
            {
                currentGroup.Add(i);

                string text = blocks[i].Text.TrimEnd();

                // Check if this block ends a sentence
                if (text.Length == 0 || EndsWithSentenceTerminator(text) || i == blocks.Count - 1)
                {
                    groups.Add(currentGroup);
                    currentGroup = new List<int>();
                }
            }

            // Handle any remaining blocks
            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
            }

            return groups;
        }

        private bool EndsWithSentenceTerminator(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;

            char last = text[text.Length - 1];

            // Common sentence-ending punctuation
            // Also treat " ' ) ] as terminators if preceded by . ! ?
            if (last == '.' || last == '!' || last == '?')
                return true;

            // Check for closing quotes/brackets after a sentence terminator: e.g. `something."` or `something?"`
            if ((last == '"' || last == '\'' || last == ')' || last == ']' || last == '\u201D') && text.Length >= 2)
            {
                char secondLast = text[text.Length - 2];
                if (secondLast == '.' || secondLast == '!' || secondLast == '?')
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Splits translated text back across multiple blocks proportionally based on original text lengths.
        /// </summary>
        private void SplitTranslationAcrossBlocks(List<SrtBlock> blocks, List<int> groupIndices, string translatedText)
        {
            // Single block - no splitting needed
            if (groupIndices.Count == 1)
            {
                blocks[groupIndices[0]].TranslatedText = translatedText;
                return;
            }

            // Calculate proportional split based on original text character lengths
            int[] originalLengths = groupIndices.Select(idx => blocks[idx].Text.Length).ToArray();
            int totalOriginalLength = originalLengths.Sum();

            if (totalOriginalLength == 0)
            {
                blocks[groupIndices[0]].TranslatedText = translatedText;
                for (int i = 1; i < groupIndices.Count; i++)
                    blocks[groupIndices[i]].TranslatedText = "";
                return;
            }

            string[] words = translatedText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int totalWords = words.Length;

            // If fewer words than blocks, give each block at least something
            if (totalWords <= groupIndices.Count)
            {
                int wIdx = 0;
                for (int i = 0; i < groupIndices.Count; i++)
                {
                    if (wIdx < totalWords)
                    {
                        blocks[groupIndices[i]].TranslatedText = words[wIdx];
                        wIdx++;
                    }
                    else
                    {
                        blocks[groupIndices[i]].TranslatedText = "";
                    }
                }
                return;
            }

            int wordIndex = 0;
            for (int i = 0; i < groupIndices.Count; i++)
            {
                int blockIdx = groupIndices[i];

                if (i == groupIndices.Count - 1)
                {
                    // Last block gets all remaining words
                    blocks[blockIdx].TranslatedText = string.Join(" ", words.Skip(wordIndex));
                }
                else
                {
                    // Calculate how many words this block should get based on proportion
                    double ratio = (double)originalLengths[i] / totalOriginalLength;
                    int wordsForThisBlock = Math.Max(1, (int)Math.Round(totalWords * ratio));

                    // Make sure we leave at least 1 word for each remaining block
                    int remainingBlocks = groupIndices.Count - i - 1;
                    int maxWords = totalWords - wordIndex - remainingBlocks;
                    wordsForThisBlock = Math.Min(wordsForThisBlock, maxWords);
                    wordsForThisBlock = Math.Max(1, wordsForThisBlock);

                    blocks[blockIdx].TranslatedText = string.Join(" ", words.Skip(wordIndex).Take(wordsForThisBlock));
                    wordIndex += wordsForThisBlock;
                }
            }
        }

        private List<List<SrtBlock>> BuildAiBatches(List<SrtBlock> blocks)
        {
            var result = new List<List<SrtBlock>>();
            var currentBatch = new List<SrtBlock>();
            int currentCharCount = 0;

            foreach (var block in blocks)
            {
                int estimatedChars = EstimateBatchSize(block);
                bool wouldOverflow = currentBatch.Count >= AiMaxBlocksPerBatch ||
                                     (currentBatch.Count > 0 && currentCharCount + estimatedChars > AiMaxCharsPerBatch);

                if (wouldOverflow)
                {
                    result.Add(currentBatch);
                    currentBatch = new List<SrtBlock>();
                    currentCharCount = 0;
                }

                currentBatch.Add(block);
                currentCharCount += estimatedChars;
            }

            if (currentBatch.Count > 0)
            {
                result.Add(currentBatch);
            }

            return result;
        }

        private static int EstimateBatchSize(SrtBlock block)
        {
            return (block.Timestamp?.Length ?? 0) + (block.Text?.Length ?? 0) + 32;
        }

        private static string CreateSrtPayload(IEnumerable<SrtBlock> blocks)
        {
            var payload = new StringBuilder();
            foreach (var block in blocks)
            {
                payload.AppendLine(block.SequenceNumber.ToString());
                payload.AppendLine(block.Timestamp);
                payload.AppendLine(block.Text);
                payload.AppendLine();
            }

            return payload.ToString();
        }

        private static string NormalizeAiSubtitlePayload(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return responseText;
            }

            string normalized = responseText.Trim();

            if (normalized.StartsWith("```", StringComparison.Ordinal))
            {
                normalized = StripMarkdownFence(normalized);
            }

            var lines = normalized
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .ToList();

            int firstSrtLineIndex = lines.FindIndex(line =>
                int.TryParse(line.Trim(), out _) || line.Contains("-->", StringComparison.Ordinal));

            if (firstSrtLineIndex > 0)
            {
                normalized = string.Join("\n", lines.Skip(firstSrtLineIndex)).Trim();
            }

            return normalized;
        }

        private static string StripMarkdownFence(string responseText)
        {
            var lines = responseText
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .ToList();

            if (lines.Count == 0)
            {
                return responseText;
            }

            if (lines[0].StartsWith("```", StringComparison.Ordinal))
            {
                lines.RemoveAt(0);
            }

            int lastFenceIndex = lines.FindLastIndex(line => line.StartsWith("```", StringComparison.Ordinal));
            if (lastFenceIndex >= 0)
            {
                lines.RemoveAt(lastFenceIndex);
            }

            return string.Join("\n", lines).Trim();
        }

        public string Method { get; set; } = "Google Translate (Free)";
        public string ApiKey { get; set; } = "";

        private async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            try
            {
                if (Method.Contains("OpenAI", StringComparison.OrdinalIgnoreCase) ||
                    Method.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase))
                {
                    return await TranslateTextOpenAiAsync(text, targetLang, ct);
                }
                else if (Method.Contains("Gemini", StringComparison.OrdinalIgnoreCase))
                {
                    return await TranslateTextGeminiAsync(text, targetLang, ct);
                }
                else if (Method.Contains("DeepL", StringComparison.OrdinalIgnoreCase))
                {
                    return await TranslateTextDeepLAsync(text, targetLang, ct);
                }
                else
                {
                    return await TranslateTextGoogleFreeAsync(text, sourceLang, targetLang, ct);
                }
            }
            catch (Exception ex)
            {
                ReportStatus($"API Error: {ex.Message}");
                Log.Error($"Translation error with {Method}: {ex.Message}");
                return null;
            }
        }

        private async Task<string> TranslateTextOpenAiAsync(string text, string targetLang, CancellationToken ct)
        {
            string url = "https://api.openai.com/v1/chat/completions";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {ApiKey}");
            
            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = $"You are a professional subtitle translator. Translate the given text to the language matching ISO code/name '{targetLang}'. Preserve all SRT formatting and sequence numbers exactly as they are. ONLY output the translated text without markdown or quotes." },
                    new { role = "user", content = text }
                }
            };

            string jsonContent = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(ct);
                string message = BuildOpenAiErrorMessage(response.StatusCode, error);
                Console.WriteLine($"OpenAI API Error: {response.StatusCode} - {error}");
                throw new Exception(message);
            }

            string jsonResponse = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim() ?? null;
        }

        private async Task<string> TranslateTextGeminiAsync(string text, string targetLang, CancellationToken ct)
        {
            var payload = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = $"Translate the following text to language code/name: '{targetLang}'. Preserve all SRT formatting and sequence numbers perfectly. Output ONLY the translated text without quotes or markdown. Text:\n{text}" } } }
                }
            };

            string jsonContent = JsonSerializer.Serialize(payload);

            string lastError = null;
            HttpStatusCode lastStatusCode = HttpStatusCode.BadRequest;

            foreach (var modelName in GeminiModelCandidates)
            {
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={ApiKey}";
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync(ct);
                    using var doc = JsonDocument.Parse(jsonResponse);

                    string translatedText = TryExtractGeminiText(doc);
                    if (!string.IsNullOrWhiteSpace(translatedText))
                    {
                        return translatedText.Trim();
                    }

                    lastStatusCode = response.StatusCode;
                    lastError = "Gemini returned an empty response.";
                    continue;
                }

                lastStatusCode = response.StatusCode;
                lastError = await response.Content.ReadAsStringAsync(ct);
            }

            string availableModels = await TryGetAvailableGeminiModelsAsync(ct);
            string message = BuildGeminiErrorMessage(lastStatusCode, lastError, availableModels);
            Console.WriteLine($"Gemini API Error: {(int)lastStatusCode} - {lastError}");
            throw new Exception(message);
        }

        private async Task<string> TranslateTextDeepLAsync(string text, string targetLang, CancellationToken ct)
        {
            bool isFree = ApiKey.EndsWith(":fx", StringComparison.OrdinalIgnoreCase);
            string endpoint = isFree ? "api-free.deepl.com" : "api.deepl.com";
            string url = $"https://{endpoint}/v2/translate";

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"DeepL-Auth-Key {ApiKey}");
            
            var payload = new
            {
                text = new[] { text },
                target_lang = targetLang.ToUpper()
            };

            string jsonContent = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"DeepL API Error: {response.StatusCode} - {error}");
                throw new Exception($"DeepL API Error: {response.StatusCode} - {error}");
            }

            string jsonResponse = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(jsonResponse);
            return doc.RootElement.GetProperty("translations")[0].GetProperty("text").GetString()?.Trim() ?? null;
        }

        private async Task<string> TranslateTextGoogleFreeAsync(string text, string sourceLang, string targetLang, CancellationToken ct)
        {
            try
            {
                string encoded = Uri.EscapeDataString(text);
                string url = string.Format(GOOGLE_TRANSLATE_URL, sourceLang, targetLang, encoded);

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                var response = await _httpClient.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Warning($"Google Translate returned {response.StatusCode}");
                    return null;
                }

                string json = await response.Content.ReadAsStringAsync(ct);
                return ParseGoogleTranslateResponse(json);
            }
            catch (Exception ex)
            {
                Log.Error($"Translation error: {ex.Message}");
                return null;
            }
        }

        private static string BuildOpenAiErrorMessage(HttpStatusCode statusCode, string errorBody)
        {
            string apiMessage = ExtractApiErrorMessage(errorBody);

            if (statusCode == HttpStatusCode.Unauthorized)
            {
                return $"OpenAI API key gecersiz veya yetkisiz. Detay: {apiMessage}";
            }

            if (statusCode == HttpStatusCode.TooManyRequests ||
                apiMessage.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase) ||
                apiMessage.Contains("quota", StringComparison.OrdinalIgnoreCase))
            {
                return $"OpenAI API kotasi yetersiz. Resmi OpenAI API tarafinda free tier desteklenmiyor; hesaba kredi tanimli olmasi gerekiyor. Detay: {apiMessage}";
            }

            return $"OpenAI API Error ({(int)statusCode}): {apiMessage}";
        }

        private static string BuildGeminiErrorMessage(HttpStatusCode statusCode, string errorBody, string availableModels)
        {
            string apiMessage = ExtractApiErrorMessage(errorBody);

            if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
            {
                apiMessage = $"Gemini API key gecersiz veya projede Gemini API yetkisi acik degil. Detay: {apiMessage}";
            }
            else if (statusCode == HttpStatusCode.TooManyRequests)
            {
                apiMessage = $"Gemini free tier/rate limit asildi. Daha sonra tekrar deneyin veya daha kucuk bir altyazi secin. Detay: {apiMessage}";
            }
            else if (statusCode == HttpStatusCode.BadRequest &&
                     apiMessage.Contains("model", StringComparison.OrdinalIgnoreCase))
            {
                apiMessage = $"Bu Gemini API key secili modellere erisemiyor. Detay: {apiMessage}";
            }

            if (!string.IsNullOrWhiteSpace(availableModels))
            {
                apiMessage += $"\nKullanilabilir modeller: {availableModels}";
            }

            return $"Gemini API Error ({(int)statusCode}): {apiMessage}";
        }

        private static string ExtractApiErrorMessage(string errorBody)
        {
            if (string.IsNullOrWhiteSpace(errorBody))
            {
                return "Bilinmeyen hata";
            }

            try
            {
                using var doc = JsonDocument.Parse(errorBody);

                if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    if (errorElement.ValueKind == JsonValueKind.Object &&
                        errorElement.TryGetProperty("message", out var messageElement))
                    {
                        return messageElement.GetString() ?? errorBody;
                    }

                    if (errorElement.ValueKind == JsonValueKind.String)
                    {
                        return errorElement.GetString() ?? errorBody;
                    }
                }

                if (doc.RootElement.TryGetProperty("message", out var directMessage))
                {
                    return directMessage.GetString() ?? errorBody;
                }
            }
            catch
            {
            }

            return errorBody;
        }

        private static string TryExtractGeminiText(JsonDocument doc)
        {
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) ||
                candidates.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var candidate in candidates.EnumerateArray())
            {
                if (!candidate.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("parts", out var parts) ||
                    parts.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var builder = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textPart) &&
                        textPart.ValueKind == JsonValueKind.String)
                    {
                        builder.Append(textPart.GetString());
                    }
                }

                if (builder.Length > 0)
                {
                    return builder.ToString();
                }
            }

            return null;
        }

        private async Task<string> TryGetAvailableGeminiModelsAsync(CancellationToken ct)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"https://generativelanguage.googleapis.com/v1beta/models?key={ApiKey}");
                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                string payload = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(payload);
                if (!doc.RootElement.TryGetProperty("models", out var models) || models.ValueKind != JsonValueKind.Array)
                {
                    return null;
                }

                return string.Join(", ",
                    models.EnumerateArray()
                        .Select(model => model.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null)
                        .Where(name => !string.IsNullOrWhiteSpace(name)));
            }
            catch
            {
                return null;
            }
        }

        private string ParseGoogleTranslateResponse(string json)
        {
            try
            {
                // Google Translate returns: [[["çevrilmiş metin","original text",...],...],...]]
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                    return null;

                var translationsArray = root[0];
                if (translationsArray.ValueKind != JsonValueKind.Array)
                    return null;

                var sb = new StringBuilder();
                foreach (var segment in translationsArray.EnumerateArray())
                {
                    if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                    {
                        var translatedPart = segment[0];
                        if (translatedPart.ValueKind == JsonValueKind.String)
                        {
                            sb.Append(translatedPart.GetString());
                        }
                    }
                }

                string result = sb.ToString().Trim();
                return string.IsNullOrEmpty(result) ? null : result;
            }
            catch (Exception ex)
            {
                Log.Error($"Error parsing Google Translate response: {ex.Message}");
                return null;
            }
        }

        private List<SrtBlock> ParseSrt(string srtContent)
        {
            var blocks = new List<SrtBlock>();
            var lines = srtContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int i = 0;
            while (i < lines.Length)
            {
                // Skip empty lines
                while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
                if (i >= lines.Length) break;

                // Line 1: sequence number
                if (!int.TryParse(lines[i].Trim(), out int seqNum))
                {
                    i++;
                    continue;
                }
                i++;

                // Line 2: timestamp
                if (i >= lines.Length) break;
                string timestamp = lines[i].Trim();
                if (!timestamp.Contains("-->"))
                {
                    i++;
                    continue;
                }
                i++;

                // Lines 3+: text until empty line
                var textLines = new List<string>();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    textLines.Add(lines[i]);
                    i++;
                }

                if (textLines.Count > 0)
                {
                    string blockText = string.Join("\n", textLines);
                    blocks.Add(new SrtBlock
                    {
                        SequenceNumber = seqNum,
                        Timestamp = timestamp,
                        Text = blockText,
                        TranslatedText = blockText // default to original
                    });
                }
            }

            return blocks;
        }

        private void ReportStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }

        private void ReportProgress(double progress)
        {
            ProgressChanged?.Invoke(this, progress);
        }

        private class SrtBlock
        {
            public int SequenceNumber { get; set; }
            public string Timestamp { get; set; }
            public string Text { get; set; }
            public string TranslatedText { get; set; }
        }
    }
}
