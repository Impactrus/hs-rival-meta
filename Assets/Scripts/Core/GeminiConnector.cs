using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CCG.Core
{
    public class GeminiConnector : MonoBehaviour
    {
        public static GeminiConnector Instance { get; private set; }

        [Header("API Settings")]
        [SerializeField] private string modelName = "gemini-2.5-flash";

        private string apiKey = "";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadApiKey();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadApiKey()
        {
            // Load key from a local file at the root of the project directory (not committed to git)
            string keyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "gemini_key.txt");
            
            if (File.Exists(keyFilePath))
            {
                apiKey = File.ReadAllText(keyFilePath).Trim();
                Debug.Log("Gemini API key loaded successfully from gemini_key.txt.");
            }
            else
            {
                // Fallback: Create an empty file so the user knows where to put it
                try
                {
                    File.WriteAllText(keyFilePath, "WKLEJ_TUTAJ_SWOJ_KLUCZ_API_GEMINI");
                    Debug.LogWarning($"gemini_key.txt not found. Created template file at: {keyFilePath}. Please paste your Gemini API key there.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to create gemini_key.txt template: {e.Message}");
                }
            }
        }

        public void AskGemini(string prompt, System.Action<string> onResponseCallback)
        {
            if (string.IsNullOrEmpty(apiKey) || apiKey == "WKLEJ_TUTAJ_SWOJ_KLUCZ_API_GEMINI")
            {
                Debug.LogError("Gemini API key is not set! Please write it inside gemini_key.txt at the project root.");
                onResponseCallback?.Invoke("ERROR: Brak klucza API Gemini.");
                return;
            }

            StartCoroutine(SendPromptCoroutine(prompt, onResponseCallback));
        }

        private IEnumerator SendPromptCoroutine(string prompt, System.Action<string> onResponseCallback)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            // Properly escape prompt text for JSON body
            string escapedPrompt = JsonUtility.ToJson(prompt); // Wraps text in quotes and escapes backslashes/quotes
            
            // Build the JSON body structure required by Gemini API
            string jsonPayload = $"{{\"contents\": [{{\"parts\": [{{\"text\": {escapedPrompt}}}]}}]}}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    string resultText = ExtractTextFromGeminiResponse(jsonResponse);
                    onResponseCallback?.Invoke(resultText);
                }
                else
                {
                    string errorMsg = $"Błąd API Gemini: {request.error}\n{request.downloadHandler.text}";
                    Debug.LogError(errorMsg);
                    onResponseCallback?.Invoke($"ERROR: {request.error}");
                }
            }
        }

        private string ExtractTextFromGeminiResponse(string rawJson)
        {
            // Simple parsing to extract the text part from the nested Gemini JSON response
            // Response format structure: { "candidates": [ { "content": { "parts": [ { "text": "RESPONSE_HERE" } ] } } ] }
            try
            {
                int textStartIndex = rawJson.IndexOf("\"text\": \"");
                if (textStartIndex == -1) return rawJson;

                textStartIndex += 9;
                int textEndIndex = rawJson.IndexOf("\"", textStartIndex);
                if (textEndIndex == -1) return rawJson;

                string rawText = rawJson.Substring(textStartIndex, textEndIndex - textStartIndex);
                
                // Decode unicode escapes (like \n, \t) for clean reading
                return System.Text.RegularExpressions.Regex.Unescape(rawText);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to extract text from response: {e.Message}");
                return rawJson;
            }
        }
    }
}
