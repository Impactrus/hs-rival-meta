#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace CCG.Editor
{
    public class MeshyAssetGenerator : EditorWindow
    {
        private string apiKey = "";
        private string prompt = "stylized fantasy wooden tavern chair, hand-painted texture, cozy";
        private string artStyle = "stylized"; // Options: stylized, realistic
        private string negativePrompt = "low quality, bad anatomy, deformed, noisy";
        
        // Progress tracking
        private string statusMessage = "Gotowy.";
        private float progressPercentage = 0f;
        private bool isGenerating = false;
        private string activeTaskId = "";
        private double nextPollTime = 0;
        private const double PollIntervalSeconds = 4.0;

        [MenuItem("CCG Tools/AI 3D Asset Generator (Meshy)")]
        public static void ShowWindow()
        {
            GetWindow<MeshyAssetGenerator>("Meshy 3D Generator");
        }

        private void OnEnable()
        {
            // Load saved API Key from EditorPrefs
            apiKey = EditorPrefs.GetString("Meshy_API_Key", "");
            EditorApplication.update += MonitorTask;
        }

        private void OnDisable()
        {
            EditorApplication.update -= MonitorTask;
        }

        private void OnGUI()
        {
            GUILayout.Label("<b>GENERATOR MODELI 3D (AI MESHY)</b>", new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true });
            GUILayout.Space(10);

            // API Key field (saved locally)
            EditorGUI.BeginChangeCheck();
            apiKey = EditorGUILayout.PasswordField("Klucz API Meshy:", apiKey);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("Meshy_API_Key", apiKey);
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Skąd wziąć klucz API? (Darmowe konto)", GUILayout.Height(20)))
            {
                Application.OpenURL("https://web.meshy.ai/settings/api-tokens");
            }

            GUILayout.Space(15);
            GUILayout.Label("<b>Ustawienia Generatora:</b>", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextField("Opis modelu (Prompt):", prompt);
            
            string[] styles = { "stylized", "realistic" };
            int selectedStyleIndex = Array.IndexOf(styles, artStyle);
            if (selectedStyleIndex == -1) selectedStyleIndex = 0;
            selectedStyleIndex = EditorGUILayout.Popup("Styl graficzny:", selectedStyleIndex, styles);
            artStyle = styles[selectedStyleIndex];

            negativePrompt = EditorGUILayout.TextField("Negative Prompt:", negativePrompt);

            GUILayout.Space(20);

            GUI.enabled = !isGenerating && !string.IsNullOrEmpty(apiKey);
            if (GUILayout.Button("GENERUJ MODEL 3D", GUILayout.Height(40)))
            {
                StartGenerationTask();
            }
            GUI.enabled = true;

            GUILayout.Space(15);
            GUILayout.Label("<b>Status zadania:</b>", EditorStyles.boldLabel);
            
            if (isGenerating)
            {
                Rect r = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(r, progressPercentage / 100f, $"{statusMessage} ({progressPercentage:F0}%)");
            }
            else
            {
                GUILayout.Label(statusMessage, EditorStyles.wordWrappedLabel);
            }
        }

        private void StartGenerationTask()
        {
            isGenerating = true;
            progressPercentage = 0f;
            statusMessage = "Wysyłanie zapytania do Meshy...";
            Repaint();

            // Request body JSON
            string jsonBody = $"{{\"prompt\":\"{prompt}\",\"art_style\":\"{artStyle}\",\"negative_prompt\":\"{negativePrompt}\"}}";
            
            UnityWebRequest request = new UnityWebRequest("https://api.meshy.ai/v2/text-to-3d", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = request.SendWebRequest();
            operation.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;
                    // Extract task ID using simple parsing
                    int idIndex = responseText.IndexOf("\"result\":\"");
                    if (idIndex != -1)
                    {
                        idIndex += 10;
                        int endIdx = responseText.IndexOf("\"", idIndex);
                        activeTaskId = responseText.Substring(idIndex, endIdx - idIndex);
                        statusMessage = "Zadanie przyjęte. Trwa generowanie modelu przez AI...";
                        nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;
                        Debug.Log($"Meshy task started successfully. Task ID: {activeTaskId}");
                    }
                    else
                    {
                        isGenerating = false;
                        statusMessage = "Błąd: Nie udało się odczytać ID zadania z odpowiedzi Meshy.";
                        Debug.LogError(responseText);
                    }
                }
                else
                {
                    isGenerating = false;
                    statusMessage = $"Błąd podczas wysyłania: {request.error}";
                    Debug.LogError(request.downloadHandler.text);
                }
                Repaint();
                request.Dispose();
            };
        }

        private void MonitorTask()
        {
            if (!isGenerating || string.IsNullOrEmpty(activeTaskId)) return;

            // Wait for poll interval
            if (EditorApplication.timeSinceStartup < nextPollTime) return;
            nextPollTime = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            UnityWebRequest request = UnityWebRequest.Get($"https://api.meshy.ai/v2/text-to-3d/{activeTaskId}");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = request.SendWebRequest();
            operation.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    
                    // Extract progress percentage
                    int progIndex = json.IndexOf("\"progress\":");
                    if (progIndex != -1)
                    {
                        progIndex += 11;
                        int endProgIdx = json.IndexOf(",", progIndex);
                        if (endProgIdx == -1) endProgIdx = json.IndexOf("}", progIndex);
                        float.TryParse(json.Substring(progIndex, endProgIdx - progIndex), out progressPercentage);
                    }

                    // Extract status
                    int statusIndex = json.IndexOf("\"status\":\"");
                    if (statusIndex != -1)
                    {
                        statusIndex += 10;
                        int endStatusIdx = json.IndexOf("\"", statusIndex);
                        string status = json.Substring(statusIndex, endStatusIdx - statusIndex);

                        if (status == "SUCCEEDED")
                        {
                            isGenerating = false;
                            statusMessage = "Sukces! Pobieranie wygenerowanego pliku model 3D...";
                            ExtractModelUrlAndDownload(json);
                        }
                        else if (status == "FAILED")
                        {
                            isGenerating = false;
                            statusMessage = "Generowanie zakończyło się błędem po stronie Meshy.";
                            Debug.LogError($"Meshy task failed: {json}");
                        }
                        else
                        {
                            statusMessage = $"Trwa generowanie przez AI: {status}...";
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Błąd sprawdzania statusu zadania: {request.error}");
                }
                Repaint();
                request.Dispose();
            };
        }

        private void ExtractModelUrlAndDownload(string jsonResponse)
        {
            // Simple parsing to extract the FBX or GLTF url
            // Meshy outputs URLs under model_urls block
            string urlKey = "\"fbx\":\"";
            int urlStartIndex = jsonResponse.IndexOf(urlKey);
            string fileExt = ".fbx";

            if (urlStartIndex == -1)
            {
                // Fallback to gltf
                urlKey = "\"gltf\":\"";
                urlStartIndex = jsonResponse.IndexOf(urlKey);
                fileExt = ".gltf";
            }

            if (urlStartIndex == -1)
            {
                statusMessage = "Nie znaleziono linku do pobrania pliku FBX lub GLTF.";
                return;
            }

            urlStartIndex += urlKey.Length;
            int urlEndIndex = jsonResponse.IndexOf("\"", urlStartIndex);
            string fileUrl = jsonResponse.Substring(urlStartIndex, urlEndIndex - urlStartIndex);
            
            // Clean url backslashes if present
            fileUrl = fileUrl.Replace("\\/", "/");

            DownloadModel(fileUrl, fileExt);
        }

        private void DownloadModel(string url, string extension)
        {
            statusMessage = "Pobieranie pliku modelu 3D...";
            Repaint();

            // Create target folder in Assets
            string folderPath = Path.Combine(Application.dataPath, "AI_Generated");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = $"Model_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            string savePath = Path.Combine(folderPath, fileName);

            UnityWebRequest request = UnityWebRequest.Get(url);
            var operation = request.SendWebRequest();
            operation.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        File.WriteAllBytes(savePath, request.downloadHandler.data);
                        AssetDatabase.Refresh(); // Force Unity to import the downloaded FBX/GLTF
                        
                        string localPath = $"Assets/AI_Generated/{fileName}";
                        statusMessage = $"Sukces! Pobrano model do: {localPath}";
                        Debug.Log($"Successfully imported AI generated 3D Model: {localPath}");
                        
                        // Select the imported model in the editor automatically
                        UnityEngine.Object importedObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(localPath);
                        if (importedObj != null)
                        {
                            Selection.activeObject = importedObj;
                        }
                    }
                    catch (Exception e)
                    {
                        statusMessage = $"Błąd zapisu pliku: {e.Message}";
                        Debug.LogError(e.Message);
                    }
                }
                else
                {
                    statusMessage = $"Błąd pobierania pliku: {request.error}";
                    Debug.LogError(request.error);
                }
                Repaint();
                request.Dispose();
            };
        }
    }
}
#endif
