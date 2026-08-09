#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CCG.Core;
using CCG.UI;

namespace CCG.Editor
{
    public class SetupGameUI : EditorWindow
    {
        [MenuItem("CCG Tools/Complete Project Setup")]
        public static void PerformSetup()
        {


            // --- AUTOMATIC CLEANUP ---
            string[] objectsToClean = { "CCG_Canvas", "EventSystem", "CCG_Managers", "CCG_3D_Scene" };
            foreach (var name in objectsToClean)
            {
                GameObject go = GameObject.Find(name);
                if (go != null)
                {
                    DestroyImmediate(go);
                }
            }

            // 1. Create or Find Canvas
            GameObject canvasGo = new GameObject("CCG_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Create EventSystem
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            System.Type inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
            {
                eventSystemGo.AddComponent(inputModuleType);
            }
            else
            {
                eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 2. Create UI Panels (toggled by screen manager but laid over 3D scene)
            GameObject loginPanel = CreatePanel(canvas.transform, "LoginPanel", new Color(0.15f, 0.15f, 0.2f, 1f));
            GameObject menuPanel = CreatePanel(canvas.transform, "MenuPanel", Color.clear); // Transparent because we will see the 3D scene!
            GameObject collectionPanel = CreatePanel(canvas.transform, "CollectionPanel", new Color(0.12f, 0.15f, 0.12f, 0.2f)); // semi-transparent overlay
            GameObject shopPanel = CreatePanel(canvas.transform, "ShopPanel", new Color(0.18f, 0.12f, 0.12f, 0.2f)); // semi-transparent
            GameObject matchmakingPanel = CreatePanel(canvas.transform, "MatchmakingPanel", new Color(0.1f, 0.1f, 0.1f, 0.85f));
            GameObject gameplayPanel = CreatePanel(canvas.transform, "GameplayPanel", Color.clear); // Game is also in 3D!

            // Create Play Zone inside Gameplay Panel
            GameObject playZone = CreatePanel(gameplayPanel.transform, "BoardPlayZone", new Color(1f, 1f, 1f, 0.02f));
            playZone.AddComponent<BoardPlayZone>();
            RectTransform playZoneRect = playZone.GetComponent<RectTransform>();
            playZoneRect.anchorMin = new Vector2(0.15f, 0.3f);
            playZoneRect.anchorMax = new Vector2(0.85f, 0.7f);
            playZoneRect.offsetMin = Vector2.zero;
            playZoneRect.offsetMax = Vector2.zero;
            CreateTextMeshPro(playZone.transform, "Label", "PRZECIĄGNIJ KARTĘ TUTAJ ABY ZAGRAĆ", 28, new Color(1f, 1f, 1f, 0.3f), TextAlignmentOptions.Center);

            // Create Hand Container inside Gameplay Panel
            GameObject handContainer = new GameObject("PlayerHand", typeof(RectTransform));
            handContainer.transform.SetParent(gameplayPanel.transform, false);
            RectTransform handRect = handContainer.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0.2f, 0f);
            handRect.anchorMax = new Vector2(0.8f, 0.25f);
            handRect.offsetMin = Vector2.zero;
            handRect.offsetMax = Vector2.zero;

            UIHandManager handManager = handContainer.AddComponent<UIHandManager>();

            // 3. Programmatically Build the UICard Prefab
            GameObject cardPrefab = CreateCardPrefab();
            handManager.GetType().GetField("cardPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(handManager, cardPrefab);
            handManager.GetType().GetField("handContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(handManager, handContainer.transform);

            // 4. Set up 3D environment (Tavern, Bar Counter, Box, Buttons, Anchors)
            SetupThreeDScene(out Transform mainAnchor, out Transform collAnchor, out Transform shopAnch, out Transform gameAnch, 
                out ThreeDButton playBtn, out ThreeDButton collBtn, out ThreeDButton shopBtn, out ThreeDButton backBtn);

            // 5. Create Setup Managers Object
            GameObject managersGo = GameObject.Find("CCG_Managers");
            if (managersGo == null)
            {
                managersGo = new GameObject("CCG_Managers");
            }

            GameManager gameManager = managersGo.GetComponent<GameManager>() ?? managersGo.AddComponent<GameManager>();
            PlayerProfileManager profileManager = managersGo.GetComponent<PlayerProfileManager>() ?? managersGo.AddComponent<PlayerProfileManager>();
            ScreenManager screenManager = managersGo.GetComponent<ScreenManager>() ?? managersGo.AddComponent<ScreenManager>();
            GameDebugger gameDebugger = managersGo.GetComponent<GameDebugger>() ?? managersGo.AddComponent<GameDebugger>();
            ThreeDMenuController menu3DController = managersGo.GetComponent<ThreeDMenuController>() ?? managersGo.AddComponent<ThreeDMenuController>();
            GeminiConnector geminiConnector = managersGo.GetComponent<GeminiConnector>() ?? managersGo.AddComponent<GeminiConnector>();

            // Assign panels to ScreenManager
            screenManager.GetType().GetField("loginPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(screenManager, loginPanel);
            screenManager.GetType().GetField("mainMenuPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(screenManager, menuPanel);
            screenManager.GetType().GetField("collectionPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(screenManager, collectionPanel);
            screenManager.GetType().GetField("shopPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(screenManager, shopPanel);
            screenManager.GetType().GetField("matchmakingPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(screenManager, matchmakingPanel);
            screenManager.GetType().GetField("gameplayPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(screenManager, gameplayPanel);

            // Assign anchors & buttons to ThreeDMenuController
            menu3DController.GetType().GetField("mainCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, Camera.main.transform);
            menu3DController.GetType().GetField("mainMenuAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, mainAnchor);
            menu3DController.GetType().GetField("collectionAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, collAnchor);
            menu3DController.GetType().GetField("shopAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, shopAnch);
            menu3DController.GetType().GetField("gameplayAnchor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, gameAnch);

            menu3DController.GetType().GetField("playButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, playBtn);
            menu3DController.GetType().GetField("collectionButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, collBtn);
            menu3DController.GetType().GetField("shopButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, shopBtn);
            menu3DController.GetType().GetField("backToMenuButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(menu3DController, backBtn);

            EditorUtility.DisplayDialog("CCG 3D Setup Complete", "Środowisko karczmy 3D z klimatycznym oświetleniem i kominkiem zostało wygenerowane!", "Super!");
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            
            Image img = panel.GetComponent<Image>();
            img.color = color;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return panel;
        }

        private static TextMeshProUGUI CreateTextMeshPro(Transform parent, string name, string text, int fontSize, Color color, TextAlignmentOptions alignment)
        {
            GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(parent, false);
            
            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;

            RectTransform rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return tmp;
        }

        private static GameObject CreateCardPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string prefabPath = "Assets/Prefabs/UICard.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            GameObject cardGo = new GameObject("UICard_Prefab", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(UICard));
            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(200, 280);

            Image frameImg = cardGo.GetComponent<Image>();
            frameImg.color = new Color(0.25f, 0.2f, 0.15f, 1f);

            GameObject artGo = new GameObject("CardArt", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(cardGo.transform, false);
            Image artImg = artGo.GetComponent<Image>();
            artImg.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            RectTransform artRect = artGo.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.1f, 0.4f);
            artRect.anchorMax = new Vector2(0.9f, 0.9f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;

            GameObject nameGo = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(cardGo.transform, false);
            TextMeshProUGUI nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.fontSize = 14;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = "Name";
            RectTransform nameRect = nameGo.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.1f, 0.25f);
            nameRect.anchorMax = new Vector2(0.9f, 0.38f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            GameObject descGo = new GameObject("DescText", typeof(RectTransform), typeof(TextMeshProUGUI));
            descGo.transform.SetParent(cardGo.transform, false);
            TextMeshProUGUI descText = descGo.GetComponent<TextMeshProUGUI>();
            descText.fontSize = 11;
            descText.alignment = TextAlignmentOptions.Center;
            descText.text = "Description";
            RectTransform descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.1f, 0.05f);
            descRect.anchorMax = new Vector2(0.9f, 0.23f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            GameObject manaGo = new GameObject("ManaText", typeof(RectTransform), typeof(TextMeshProUGUI));
            manaGo.transform.SetParent(cardGo.transform, false);
            TextMeshProUGUI manaText = manaGo.GetComponent<TextMeshProUGUI>();
            manaText.fontSize = 16;
            manaText.alignment = TextAlignmentOptions.Center;
            manaText.text = "0";
            manaText.color = Color.cyan;
            RectTransform manaRect = manaGo.GetComponent<RectTransform>();
            manaRect.anchorMin = new Vector2(0f, 0.85f);
            manaRect.anchorMax = new Vector2(0.2f, 1f);
            manaRect.offsetMin = Vector2.zero;
            manaRect.offsetMax = Vector2.zero;

            GameObject atkGo = new GameObject("AttackText", typeof(RectTransform), typeof(TextMeshProUGUI));
            atkGo.transform.SetParent(cardGo.transform, false);
            TextMeshProUGUI atkText = atkGo.GetComponent<TextMeshProUGUI>();
            atkText.fontSize = 16;
            atkText.alignment = TextAlignmentOptions.Center;
            atkText.text = "0";
            atkText.color = Color.yellow;
            RectTransform atkRect = atkGo.GetComponent<RectTransform>();
            atkRect.anchorMin = new Vector2(0f, 0f);
            atkRect.anchorMax = new Vector2(0.2f, 0.15f);
            atkRect.offsetMin = Vector2.zero;
            atkRect.offsetMax = Vector2.zero;

            GameObject hpGo = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI));
            hpGo.transform.SetParent(cardGo.transform, false);
            TextMeshProUGUI hpText = hpGo.GetComponent<TextMeshProUGUI>();
            hpText.fontSize = 16;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.text = "0";
            hpText.color = Color.red;
            RectTransform hpRect = hpGo.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0.8f, 0f);
            hpRect.anchorMax = new Vector2(1f, 0.15f);
            hpRect.offsetMin = Vector2.zero;
            hpRect.offsetMax = Vector2.zero;

            UICard uiCard = cardGo.GetComponent<UICard>();
            uiCard.GetType().GetField("cardNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, nameText);
            uiCard.GetType().GetField("descriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, descText);
            uiCard.GetType().GetField("manaCostText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, manaText);
            uiCard.GetType().GetField("attackText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, atkText);
            uiCard.GetType().GetField("healthText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, hpText);
            uiCard.GetType().GetField("cardArtImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, artImg);
            uiCard.GetType().GetField("cardFrameImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiCard, frameImg);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(cardGo, prefabPath);
            DestroyImmediate(cardGo);

            return prefab;
        }

        private static void SetupThreeDScene(out Transform mainAnchor, out Transform collAnchor, out Transform shopAnchor, out Transform gameAnchor,
            out ThreeDButton playBtn, out ThreeDButton collBtn, out ThreeDButton shopBtn, out ThreeDButton backBtn)
        {
            // Create a parent container for 3D elements
            GameObject threeDScene = GameObject.Find("CCG_3D_Scene");
            if (threeDScene != null)
            {
                DestroyImmediate(threeDScene);
            }
            threeDScene = new GameObject("CCG_3D_Scene");

            // Setup Materials (Handling URP/Standard automatically)
            Material woodMat = CreateMaterial(new Color(0.24f, 0.14f, 0.06f)); // Dark brown log wood
            Material counterMat = CreateMaterial(new Color(0.42f, 0.24f, 0.12f)); // Polished counter wood
            Material goldMat = CreateMaterial(new Color(0.9f, 0.72f, 0.15f), 0.7f, 0.6f); // Shiny Gold
            Material stoneMat = CreateMaterial(new Color(0.35f, 0.35f, 0.38f)); // Gray stone brick
            Material blueMat = CreateMaterial(new Color(0.12f, 0.45f, 0.85f), 0.5f, 0.8f); // Glowing magic blue
            Material fireMat = CreateMaterial(new Color(1f, 0.35f, 0.05f)); // Fire orange
            Material bottleMat = CreateMaterial(new Color(0.1f, 0.5f, 0.15f), 0.1f, 0.7f); // Glass green

            // 1. FLOOR (Stylized Wooden Planks)
            // We create 5 long wooden planks side-by-side with tiny seams to look like a real rustic floor
            for (int i = 0; i < 6; i++)
            {
                GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = $"FloorPlank_{i}";
                plank.transform.SetParent(threeDScene.transform);
                plank.transform.position = new Vector3(-5f + (i * 2.1f), -1.2f, 0);
                plank.transform.localScale = new Vector3(2f, 0.8f, 15f);
                plank.GetComponent<Renderer>().material = woodMat;
            }

            // 2. TAVERN WALLS (L-shaped stone room corner)
            GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "StoneBackWall";
            backWall.transform.SetParent(threeDScene.transform);
            backWall.transform.position = new Vector3(0, 2f, 6.5f);
            backWall.transform.localScale = new Vector3(15f, 6f, 1f);
            backWall.GetComponent<Renderer>().material = stoneMat;

            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "StoneLeftWall";
            leftWall.transform.SetParent(threeDScene.transform);
            leftWall.transform.position = new Vector3(-7.5f, 2f, 0);
            leftWall.transform.localScale = new Vector3(1f, 6f, 14f);
            leftWall.GetComponent<Renderer>().material = stoneMat;

            // 3. RUSTIC WOODEN SUPPORT PILLARS
            for (int i = 0; i < 2; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"WoodenPillar_{i}";
                pillar.transform.SetParent(threeDScene.transform);
                pillar.transform.position = new Vector3(-6.8f + (i * 13.6f), 1.8f, 5.5f);
                pillar.transform.localScale = new Vector3(0.6f, 5.2f, 0.6f);
                pillar.GetComponent<Renderer>().material = woodMat;
            }

            // 4. FIREPLACE (Kominek)
            // Left corner fireplace built from stone bricks
            GameObject fireplace = new GameObject("Fireplace");
            fireplace.transform.SetParent(threeDScene.transform);
            fireplace.transform.position = new Vector3(-6.5f, -0.8f, 4f);
            fireplace.transform.rotation = Quaternion.Euler(0, 45, 0);

            // Left Pillar of Fireplace
            GameObject fpLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fpLeft.transform.SetParent(fireplace.transform);
            fpLeft.transform.localPosition = new Vector3(-0.6f, 0.6f, 0);
            fpLeft.transform.localScale = new Vector3(0.4f, 1.2f, 0.6f);
            fpLeft.GetComponent<Renderer>().material = stoneMat;

            // Right Pillar of Fireplace
            GameObject fpRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fpRight.transform.SetParent(fireplace.transform);
            fpRight.transform.localPosition = new Vector3(0.6f, 0.6f, 0);
            fpRight.transform.localScale = new Vector3(0.4f, 1.2f, 0.6f);
            fpRight.GetComponent<Renderer>().material = stoneMat;

            // Mantelpiece (Top beam)
            GameObject fpTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fpTop.transform.SetParent(fireplace.transform);
            fpTop.transform.localPosition = new Vector3(0, 1.3f, 0);
            fpTop.transform.localScale = new Vector3(1.8f, 0.3f, 0.8f);
            fpTop.GetComponent<Renderer>().material = woodMat;

            // Wood logs inside
            GameObject logs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            logs.transform.SetParent(fireplace.transform);
            logs.transform.localPosition = new Vector3(0, 0.1f, 0);
            logs.transform.localScale = new Vector3(0.4f, 0.08f, 0.4f);
            logs.GetComponent<Renderer>().material = woodMat;

            // Fire Glowing Orb
            GameObject fireOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(fireOrb.GetComponent<Collider>());
            fireOrb.transform.SetParent(fireplace.transform);
            fireOrb.transform.localPosition = new Vector3(0, 0.2f, 0);
            fireOrb.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            fireOrb.GetComponent<Renderer>().material = fireMat;

            // Warm Fireplace Light Source (Casting shadows)
            GameObject fireLightGo = new GameObject("Fireplace_Light");
            fireLightGo.transform.SetParent(fireplace.transform);
            fireLightGo.transform.localPosition = new Vector3(0, 0.5f, -0.8f);
            Light fireLight = fireLightGo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.45f, 0.1f); // Warm flame orange
            fireLight.intensity = 8f;
            fireLight.range = 8f;
            fireLight.shadows = LightShadows.Soft;

            // Fire Embers/Sparks Particle system
            GameObject fireParticles = new GameObject("FireParticles");
            fireParticles.transform.SetParent(fireplace.transform);
            fireParticles.transform.localPosition = new Vector3(0, 0.2f, 0);
            fireParticles.transform.localRotation = Quaternion.Euler(-90, 0, 0); // Emit upwards
            ParticleSystem ps = fireParticles.AddComponent<ParticleSystem>();
            var psMain = ps.main;
            psMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.6f, 0f), new Color(1f, 0.2f, 0f));
            psMain.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            psMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            psMain.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f); // Rise gently
            var psEmission = ps.emission;
            psEmission.rateOverTime = new ParticleSystem.MinMaxCurve(12f);
            var psShape = ps.shape;
            psShape.shapeType = ParticleSystemShapeType.Cone;
            psShape.angle = 20;
            psShape.radius = 0.15f;

            // 5. THE BAR COUNTER (Szynkwas)
            GameObject barGroup = new GameObject("Szynkwas_Group");
            barGroup.transform.SetParent(threeDScene.transform);
            barGroup.transform.position = new Vector3(3.8f, -0.8f, 3f);
            barGroup.transform.rotation = Quaternion.Euler(0, -20, 0); // Angle toward player

            // Main Bar Desk
            GameObject barDesk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barDesk.name = "Szynkwas_Counter";
            barDesk.transform.SetParent(barGroup.transform);
            barDesk.transform.localPosition = new Vector3(0, 0.6f, 0);
            barDesk.transform.localScale = new Vector3(3.6f, 1.2f, 1.2f);
            barDesk.GetComponent<Renderer>().material = counterMat;

            // Bar footrest beam (Gold/Bronze tube)
            GameObject footrest = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            footrest.transform.SetParent(barDesk.transform);
            footrest.transform.localPosition = new Vector3(0, -0.4f, -0.55f);
            footrest.transform.localRotation = Quaternion.Euler(0, 0, 90);
            footrest.transform.localScale = new Vector3(0.04f, 0.48f, 0.04f);
            footrest.GetComponent<Renderer>().material = goldMat;

            // Stools in front of the bar (Bar stools)
            for (int i = 0; i < 2; i++)
            {
                GameObject stool = new GameObject($"BarStool_{i}");
                stool.transform.SetParent(barGroup.transform);
                stool.transform.localPosition = new Vector3(-0.9f + (i * 1.8f), 0.2f, -1.1f);
                
                // Stool leg
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.transform.SetParent(stool.transform);
                leg.transform.localPosition = new Vector3(0, 0, 0);
                leg.transform.localScale = new Vector3(0.12f, 0.4f, 0.12f);
                leg.GetComponent<Renderer>().material = woodMat;
                
                // Stool seat (round cushion)
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seat.transform.SetParent(stool.transform);
                seat.transform.localPosition = new Vector3(0, 0.42f, 0);
                seat.transform.localScale = new Vector3(0.35f, 0.06f, 0.35f);
                seat.GetComponent<Renderer>().material = fireMat; // Red cushion seat
            }

            // Decorate Bar: Bottles on top of counter
            for (int i = 0; i < 4; i++)
            {
                GameObject bottle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bottle.name = $"TavernBottle_{i}";
                bottle.transform.SetParent(barDesk.transform);
                bottle.transform.localPosition = new Vector3(-0.4f + (i * 0.25f), 0.7f, 0.3f);
                bottle.transform.localScale = new Vector3(0.06f, 0.15f, 0.06f);
                bottle.GetComponent<Renderer>().material = bottleMat;
            }

            // Interactive component for the Bar Counter to enter the Shop!
            shopBtn = barDesk.AddComponent<ThreeDButton>();
            shopBtn.GetType().GetField("buttonRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shopBtn, barDesk.GetComponent<Renderer>());
            shopBtn.GetType().GetField("hoverColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shopBtn, new Color(0.6f, 0.45f, 0.3f)); // Highlight
            shopBtn.GetType().GetField("hoverOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shopBtn, new Vector3(0, 0.08f, 0));
            shopBtn.GetType().GetField("clickOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(shopBtn, new Vector3(0, -0.04f, 0));

            // Label above the bar
            GameObject barLabel = new GameObject("Szynkwas_Label");
            barLabel.transform.SetParent(threeDScene.transform);
            barLabel.transform.position = new Vector3(3.4f, 0.7f, 2.5f);
            barLabel.transform.rotation = Quaternion.Euler(20, -20, 0);
            barLabel.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            TextMesh tm = barLabel.AddComponent<TextMesh>();
            tm.text = "SKLEP (SZYNKWAS)";
            tm.fontSize = 24;
            tm.color = Color.yellow;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            // 6. HEARTHSTONE BOX & TABLE (Main Menu center)
            GameObject boxTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxTable.name = "CenterTable";
            boxTable.transform.SetParent(threeDScene.transform);
            boxTable.transform.position = new Vector3(-2.2f, -0.8f, -0.6f);
            boxTable.transform.localScale = new Vector3(4f, 0.8f, 3.2f);
            boxTable.GetComponent<Renderer>().material = woodMat;

            GameObject hsBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hsBox.name = "Hearthstone_Box";
            hsBox.transform.SetParent(threeDScene.transform);
            hsBox.transform.position = new Vector3(-2.2f, -0.1f, -0.6f);
            hsBox.transform.localScale = new Vector3(2.5f, 0.6f, 1.8f);
            hsBox.GetComponent<Renderer>().material = stoneMat;

            // Center Medallion
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shield.name = "Center_Shield";
            shield.transform.SetParent(hsBox.transform);
            shield.transform.localPosition = new Vector3(0, 0.55f, 0);
            shield.transform.localScale = new Vector3(0.2f, 0.04f, 0.2f);
            shield.GetComponent<Renderer>().material = goldMat;

            // Pulsing Mystical Blue Light under the Box
            GameObject boxLightGo = new GameObject("Box_MagicLight");
            boxLightGo.transform.SetParent(hsBox.transform);
            boxLightGo.transform.localPosition = new Vector3(0, 0.4f, 0);
            Light boxLight = boxLightGo.AddComponent<Light>();
            boxLight.type = LightType.Point;
            boxLight.color = new Color(0.1f, 0.5f, 1f); // Magic Blue
            boxLight.intensity = 6f;
            boxLight.range = 4f;

            // Play Button on Box
            GameObject playObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            playObj.name = "PlayButton3D";
            playObj.transform.SetParent(hsBox.transform);
            playObj.transform.localPosition = new Vector3(0, 0.55f, -0.7f);
            playObj.transform.localScale = new Vector3(0.5f, 0.1f, 0.25f);
            playObj.GetComponent<Renderer>().material = blueMat;
            playBtn = playObj.AddComponent<ThreeDButton>();
            playBtn.GetType().GetField("buttonRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playBtn, playObj.GetComponent<Renderer>());
            playBtn.GetType().GetField("hoverColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playBtn, Color.cyan);
            playBtn.GetType().GetField("hoverOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playBtn, new Vector3(0, 0.05f, 0));
            playBtn.GetType().GetField("clickOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(playBtn, new Vector3(0, -0.03f, 0));

            // Collection Button on Box
            GameObject collObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            collObj.name = "CollectionButton3D";
            collObj.transform.SetParent(hsBox.transform);
            collObj.transform.localPosition = new Vector3(-0.7f, 0.55f, -0.7f);
            collObj.transform.localScale = new Vector3(0.4f, 0.1f, 0.25f);
            collObj.GetComponent<Renderer>().material = woodMat;
            collBtn = collObj.AddComponent<ThreeDButton>();
            collBtn.GetType().GetField("buttonRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(collBtn, collObj.GetComponent<Renderer>());
            collBtn.GetType().GetField("hoverColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(collBtn, new Color(0.5f, 0.35f, 0.2f));
            collBtn.GetType().GetField("hoverOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(collBtn, new Vector3(0, 0.05f, 0));
            collBtn.GetType().GetField("clickOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(collBtn, new Vector3(0, -0.03f, 0));

            // 7. BACK BUTTON (Tavern Exit Sign)
            GameObject backObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backObj.name = "BackButton3D";
            backObj.transform.SetParent(threeDScene.transform);
            backObj.transform.position = new Vector3(-5.5f, -0.4f, -2.5f);
            backObj.transform.localScale = new Vector3(0.8f, 0.15f, 0.5f);
            backObj.GetComponent<Renderer>().material = goldMat;
            backBtn = backObj.AddComponent<ThreeDButton>();
            backBtn.GetType().GetField("buttonRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(backBtn, backObj.GetComponent<Renderer>());
            backBtn.GetType().GetField("hoverColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(backBtn, Color.white);
            backBtn.GetType().GetField("hoverOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(backBtn, new Vector3(0, 0.08f, 0));
            backBtn.GetType().GetField("clickOffset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(backBtn, new Vector3(0, -0.04f, 0));

            GameObject backLabel = new GameObject("BackLabel");
            backLabel.transform.SetParent(backObj.transform);
            backLabel.transform.localPosition = new Vector3(0, 0.6f, 0);
            backLabel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            backLabel.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            TextMesh backTm = backLabel.AddComponent<TextMesh>();
            backTm.text = "WSTECZ";
            backTm.fontSize = 24;
            backTm.color = Color.black;
            backTm.anchor = TextAnchor.MiddleCenter;
            backTm.alignment = TextAlignment.Center;

            // 8. SCENE AMBIENT LIGHTING
            // Create a dim ambient light for the room so local warm light sources stand out
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.12f, 0.15f);

            // --- CAMERA VIEW ANCHORS ---
            GameObject camAnchors = new GameObject("CameraAnchors");
            camAnchors.transform.SetParent(threeDScene.transform);

            // Main Menu Anchor (Looks at the Box on the table, with the bar visible in background right)
            GameObject mainView = new GameObject("MainMenuAnchor");
            mainView.transform.SetParent(camAnchors.transform);
            mainView.transform.position = new Vector3(-2.2f, 2.5f, -3.2f);
            mainView.transform.rotation = Quaternion.Euler(35, 25, 0);
            mainAnchor = mainView.transform;

            // Collection Anchor (Looks closely at the Box where the collection book resides)
            GameObject collView = new GameObject("CollectionAnchor");
            collView.transform.SetParent(camAnchors.transform);
            collView.transform.position = new Vector3(-2.8f, 1.4f, -2.0f);
            collView.transform.rotation = Quaternion.Euler(38, -15, 0);
            collAnchor = collView.transform;

            // Shop Anchor (Zooms in very close, facing the bar counter / Szynkwas directly!)
            GameObject shopView = new GameObject("ShopAnchor");
            shopView.transform.SetParent(camAnchors.transform);
            shopView.transform.position = new Vector3(3.8f, 1.1f, 0.8f);
            shopView.transform.rotation = Quaternion.Euler(15, 0, 0); // Focus on bar
            shopAnchor = shopView.transform;

            // Gameplay Anchor (Bird-eye view of board)
            GameObject gameView = new GameObject("GameplayAnchor");
            gameView.transform.SetParent(camAnchors.transform);
            gameView.transform.position = new Vector3(-2.2f, 4.2f, -0.8f);
            gameView.transform.rotation = Quaternion.Euler(75, 0, 0);
            gameAnchor = gameView.transform;
            
            // Set main camera starting transform
            Camera.main.transform.position = mainView.transform.position;
            Camera.main.transform.rotation = mainView.transform.rotation;
        }

        private static Material CreateMaterial(Color color, float metallic = 0f, float smoothness = 0f)
        {
            // Dynamic shader matching for URP vs Built-in Render Pipeline
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.color = color;

            if (shader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", smoothness);
            }
            else
            {
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Glossiness", smoothness);
            }

            return mat;
        }
    }
}
#endif
