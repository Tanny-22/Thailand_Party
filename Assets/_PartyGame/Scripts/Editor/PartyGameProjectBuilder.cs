#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PartyGame.Bootstrap;
using PartyGame.Core;
using PartyGame.Development;
using PartyGame.Input;
using PartyGame.Match;
using PartyGame.MiniGames;
using PartyGame.Networking;
using PartyGame.Persistence;
using PartyGame.Players;
using PartyGame.Settings;
using PartyGame.UI.Customize;
using PartyGame.UI.Lobby;
using PartyGame.UI.MainMenu;
using PartyGame.UI.Randomizer;
using PartyGame.UI.Results;
using PartyGame.UI.Settings;
using PartyGame.UI.Shared;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PartyGame.EditorTools
{
    public static class PartyGameProjectBuilder
    {
        const string Root = "Assets/_PartyGame";
        static readonly Color Navy = new(0.035f, 0.055f, 0.11f, 1f);
        static readonly Color Card = new(0.09f, 0.13f, 0.22f, .96f);
        static readonly Color Accent = new(0.15f, 0.78f, 0.92f, 1f);

        [MenuItem("Party Game/Build or Repair Framework")]
        public static void BuildAll()
        {
            EnsureFolders();
            var config = LoadOrCreate<GameConfig>($"{Root}/Data/Config/GameConfig.asset");
            var character = LoadOrCreate<CharacterDefinition>($"{Root}/Data/Characters/Character_Default.asset");
            Set(character, "id", "default"); Set(character, "displayName", "Green"); SetObject(character, "characterPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Characters/Character_Green_Visual.prefab"));
            var redCharacter = LoadOrCreate<CharacterDefinition>($"{Root}/Data/Characters/Character_Red.asset");
            Set(redCharacter, "id", "red"); Set(redCharacter, "displayName", "Red"); SetObject(redCharacter, "characterPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Characters/Character_Red_Visual.prefab"));
            var yellowCharacter = LoadOrCreate<CharacterDefinition>($"{Root}/Data/Characters/Character_Yellow.asset");
            Set(yellowCharacter, "id", "yellow"); Set(yellowCharacter, "displayName", "Yellow"); SetObject(yellowCharacter, "characterPrefab", AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Characters/Character_Yellow_Visual.prefab"));
            var characters = LoadOrCreate<CharacterRegistry>($"{Root}/Data/Characters/CharacterRegistry.asset"); SetObjectList(characters, "entries", character, redCharacter, yellowCharacter);
            var miniGame = LoadOrCreate<MiniGameDefinition>($"{Root}/Data/MiniGames/MiniGame_Template.asset"); Set(miniGame, "id", "template"); Set(miniGame, "displayName", "Template MiniGame"); Set(miniGame, "sceneName", "MiniGame_Template");
            var miniGames = LoadOrCreate<MiniGameRegistry>($"{Root}/Data/MiniGames/MiniGameRegistry.asset"); SetObjectList(miniGames, "entries", miniGame);
            var input = BuildInput();
            var playerPrefab = BuildNetworkPlayer(characters);
            var managers = BuildManagers(config, miniGames, input, playerPrefab);
            BuildReusableUiPrefabs();
            BuildBootstrap(managers, miniGames);
            BuildMainMenu();
            BuildLobby();
            BuildRandomizer();
            BuildResults();
            BuildMiniGameTemplate();
            PartyGameCompletionBuilder.BuildAll();
            EditorBuildSettings.scenes = ScenePaths().Select(x => new EditorBuildSettingsScene(x, true)).ToArray();
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("Party Game framework built and validated.");
        }

        [MenuItem("Party Game/Validate Registries and Scenes")]
        public static void ValidateProject()
        {
            var missing = ScenePaths().Where(x => !File.Exists(x)).ToList();
            var mini = AssetDatabase.LoadAssetAtPath<MiniGameRegistry>($"{Root}/Data/MiniGames/MiniGameRegistry.asset");
            var chars = AssetDatabase.LoadAssetAtPath<CharacterRegistry>($"{Root}/Data/Characters/CharacterRegistry.asset");
            if (!mini || !mini.Entries.Any()) Debug.LogError("MiniGameRegistry is missing or empty.");
            if (!chars || !chars.Entries.Any()) Debug.LogError("CharacterRegistry is missing or empty.");
            foreach (var path in missing) Debug.LogError($"Missing required scene: {path}");
            if (missing.Count == 0) Debug.Log("Party Game validation complete: required scenes and registries exist.");
        }

        static string[] ScenePaths() => new[]
        {
            $"{Root}/Scenes/Core/00_Bootstrap.unity", $"{Root}/Scenes/Menu/01_MainMenu.unity",
            $"{Root}/Scenes/Lobby/02_Lobby.unity", $"{Root}/Scenes/Transition/03_MiniGameRandomizer.unity",
            $"{Root}/Scenes/Results/04_Results.unity", $"{Root}/Scenes/MiniGames/MiniGame_Template.unity"
        };
        static void EnsureFolders() { foreach (var path in new[]{"Data/Config","Data/Characters","Data/MiniGames","Input","Prefabs/Managers","Prefabs/Network","Prefabs/Characters","Prefabs/UI","Scenes/Core","Scenes/Menu","Scenes/Lobby","Scenes/Transition","Scenes/Results","Scenes/MiniGames"}) Directory.CreateDirectory($"{Root}/{path}"); }
        static T LoadOrCreate<T>(string path) where T : ScriptableObject { var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset; }
        static void Set(UnityEngine.Object target, string property, string value) { var so = new SerializedObject(target); so.FindProperty(property).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value) { var so = new SerializedObject(target); so.FindProperty(property).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        static void SetObjectList(UnityEngine.Object target, string property, params UnityEngine.Object[] values) { var so = new SerializedObject(target); var list = so.FindProperty(property); list.arraySize = values.Length; for (var i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        static void Ref(Component target, string property, UnityEngine.Object value) { var so = new SerializedObject(target); var field = so.FindProperty(property); if (field != null) { field.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); } }

        static InputActionAsset BuildInput()
        {
            var path = $"{Root}/Input/PartyGameInput.asset"; var old = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path); if (old) AssetDatabase.DeleteAsset(path);
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var ui = asset.AddActionMap("UI"); ui.AddAction("Pause", InputActionType.Button, "<Keyboard>/escape"); ui.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter"); ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            var gameplay = asset.AddActionMap("Gameplay"); gameplay.AddAction("Move", InputActionType.Value).AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d"); gameplay.AddAction("Action", InputActionType.Button, "<Keyboard>/space");
            AssetDatabase.CreateAsset(asset, path); return asset;
        }

        static GameObject BuildNetworkPlayer(CharacterRegistry characters)
        {
            var go = new GameObject("NetworkPlayer"); go.AddComponent<NetworkObject>(); var player = go.AddComponent<NetworkPlayer>();
            var visualRoot = new GameObject("CharacterVisualRoot"); visualRoot.transform.SetParent(go.transform, false);
            var visual = go.AddComponent<NetworkPlayerCharacterVisual>(); Ref(visual, "networkPlayer", player); Ref(visual, "registry", characters); Ref(visual, "visualRoot", visualRoot.transform);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/Network/NetworkPlayer.prefab"); UnityEngine.Object.DestroyImmediate(go); return prefab;
        }

        static GameObject BuildManagers(GameConfig config, MiniGameRegistry registry, InputActionAsset input, GameObject playerPrefab)
        {
            var root = new GameObject("[GAME SYSTEMS]"); var bootstrap = root.AddComponent<PartyGameBootstrap>(); Ref(bootstrap, "gameConfig", config);
            root.AddComponent<PartyGameMppmSmokeDriver>();
            Add<SaveManager>(root, "SaveManager"); var settings = Add<SettingsManager>(root, "SettingsManager");
            var inputManager = Add<InputManager>(root, "InputManager"); Ref(inputManager, "actions", input);
            Add<SceneFlowManager>(root, "SceneFlowManager");
            var network = root.AddComponent<NetworkManager>(); var transport = root.AddComponent<UnityTransport>(); network.NetworkConfig.NetworkTransport = transport; network.NetworkConfig.PlayerPrefab = playerPrefab;
            var session = Add<SessionManager>(root, "SessionManager"); Ref(session, "gameConfig", config);
            foreach (var name in new[]{"GameManager","LobbyManager","PlayerManager","AudioManager","UIManager"}) Child(root, name);
            var path = $"{Root}/Prefabs/Managers/ManagersRoot.prefab"; AssetDatabase.DeleteAsset(path);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root); return prefab;
        }

        static void BuildReusableUiPrefabs()
        {
            foreach (var name in new[]{"LobbyPlayerEntry","MiniGameCard","CharacterCard","PublicRoomEntry","ModalDialog","LoadingOverlay","PauseMenu","SettingsMenu","ResultsPlayerEntry"})
            {
                var root = new GameObject(name, typeof(RectTransform), typeof(Image)); root.GetComponent<Image>().color = Card;
                Label(root.transform, name, 24, TextAlignmentOptions.Center);
                PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/UI/{name}.prefab"); UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static void BuildBootstrap(GameObject managers, MiniGameRegistry registry)
        {
            NewScene(); PrefabUtility.InstantiatePrefab(managers);
            var state=Child(null,"[NETWORK STATE]"); state.AddComponent<PersistentNetworkState>(); state.AddComponent<NetworkObject>(); state.AddComponent<MatchManager>(); var mini=state.AddComponent<MiniGameManager>(); Ref(mini,"registry",registry);
            Child(null, "[GLOBAL UI]"); Child(null, "[DEBUG]"); Save($"{Root}/Scenes/Core/00_Bootstrap.unity");
        }
        static void BuildMainMenu()
        {
            NewScene(); var sceneRoot=Child(null, "[SCENE]"); var controller=sceneRoot.AddComponent<MainMenuController>(); var uiRoot = Child(null, "[UI]"); var canvas = Canvas(uiRoot.transform, "Canvas_MainMenu"); var safe = Panel(canvas.transform, "SafeArea", Navy);
            Label(safe.transform, "THAILAND PARTY", 64, TextAlignmentOptions.Top);
            var main = Panel(safe.transform, "MainPanel", Color.clear); var buttons=new List<Button>(); foreach (var label in new[]{"START","JOIN","SETTINGS","CUSTOMIZE","QUIT"}) buttons.Add(Button(main.transform, label + "Button", label));
            var create = Panel(safe.transform, "CreateRoomPanel", Card); var roomName=InputField(create.transform,"RoomNameInput","Party Room"); var privacy=Toggle(create.transform,"PrivateToggle","Private Room"); var players=Dropdown(create.transform,"PlayerCountDropdown",Enumerable.Range(2,9).Select(x=>x.ToString())); var wins=Dropdown(create.transform,"WinsDropdown",Enumerable.Range(1,10).Select(x=>x.ToString())); var random=Toggle(create.transform,"RandomToggle","Random MiniGame"); random.isOn=true; var createButton=Button(create.transform,"CreateButton","CREATE ROOM"); var createController=create.AddComponent<CreateRoomPanelController>(); Ref(createController,"roomNameInput",roomName); Ref(createController,"privateToggle",privacy); Ref(createController,"playerCountDropdown",players); Ref(createController,"winsDropdown",wins); Ref(createController,"randomToggle",random); Ref(createController,"registry",AssetDatabase.LoadAssetAtPath<MiniGameRegistry>($"{Root}/Data/MiniGames/MiniGameRegistry.asset")); Ref(createController,"createButton",createButton);
            var join=Panel(safe.transform,"JoinRoomPanel",Card); var code=InputField(join.transform,"JoinCodeInput","JOIN CODE"); var search=InputField(join.transform,"SearchInput","Search rooms"); var includeFull=Toggle(join.transform,"IncludeFullToggle","Show Full Rooms"); var joinButton=Button(join.transform,"JoinCodeButton","JOIN"); var refresh=Button(join.transform,"RefreshButton","REFRESH"); var results=Label(join.transform,"Room Browser",22,TextAlignmentOptions.Center); var joinController=join.AddComponent<JoinRoomPanelController>(); Ref(joinController,"codeInput",code); Ref(joinController,"searchInput",search); Ref(joinController,"includeFullToggle",includeFull); Ref(joinController,"joinCodeButton",joinButton); Ref(joinController,"refreshButton",refresh); Ref(joinController,"browserResults",results);
            var settings=Panel(safe.transform,"SettingsPanel",Card); settings.AddComponent<SettingsMenuController>(); Label(settings.transform,"AUDIO  •  CONTROLS  •  GRAPHICS",30,TextAlignmentOptions.Center);
            var customize=Panel(safe.transform,"CustomizePanel",Card); var displayName=InputField(customize.transform,"DisplayNameInput","Player Name"); var customizeController=customize.AddComponent<CustomizePanelController>(); Ref(customizeController,"displayNameInput",displayName); Ref(customizeController,"registry",AssetDatabase.LoadAssetAtPath<CharacterRegistry>($"{Root}/Data/Characters/CharacterRegistry.asset")); Button(customize.transform,"SaveProfileButton","SAVE PROFILE");
            var loading=Panel(safe.transform,"LoadingOverlay",new Color(0,0,0,.8f)); var loadingLabel=Label(loading.transform,"Loading...",36,TextAlignmentOptions.Center); var loadingController=loading.AddComponent<LoadingOverlayController>(); Ref(loadingController,"label",loadingLabel);
            var modal=Panel(safe.transform,"ModalDialogPanel",new Color(.12f,.05f,.08f,.98f)); var modalLabel=Label(modal.transform,"Message",28,TextAlignmentOptions.Center); var close=Button(modal.transform,"CloseButton","OK"); var modalController=modal.AddComponent<ModalDialogController>(); Ref(modalController,"messageLabel",modalLabel); Ref(modalController,"closeButton",close);
            Ref(controller,"mainPanel",main); Ref(controller,"createRoomPanel",create); Ref(controller,"joinRoomPanel",join); Ref(controller,"settingsPanel",settings); Ref(controller,"customizePanel",customize); Ref(controller,"startButton",buttons[0]); Ref(controller,"joinButton",buttons[1]); Ref(controller,"settingsButton",buttons[2]); Ref(controller,"customizeButton",buttons[3]); Ref(controller,"quitButton",buttons[4]); Ref(controller,"modal",modalController);
            Ref(createController,"loading",loadingController); Ref(createController,"modal",modalController); Ref(joinController,"loading",loadingController);
            foreach(var p in new[]{create,join,settings,customize,loading,modal}) p.SetActive(false);
            EventSystem(); Save($"{Root}/Scenes/Menu/01_MainMenu.unity");
        }
        static void BuildLobby()
        {
            NewScene(); var scene=Child(null, "[SCENE]"); var controller=scene.AddComponent<LobbyUIController>(); Child(null, "[SPAWN POINTS]"); var ui = Child(null, "[UI]"); var canvas = Canvas(ui.transform, "Canvas_Lobby"); var root = Panel(canvas.transform, "LobbyRoot", Navy);
            var header=Panel(root.transform,"Header",Card); var room=Label(header.transform,"Room Name",38,TextAlignmentOptions.Center); var code=Label(header.transform,"Join Code",24,TextAlignmentOptions.Bottom); var privacy=Label(header.transform,"PUBLIC",20,TextAlignmentOptions.TopRight);
            var playerList=Panel(root.transform,"PlayerList",Card); var players=Label(playerList.transform,"Players 1/4",28,TextAlignmentOptions.Top); var roster=Label(playerList.transform,"Player [HOST]",24,TextAlignmentOptions.Center);
            var roomSettings=Panel(root.transform,"RoomSettings",Card); var summary=Label(roomSettings.transform,"First to 3 wins",26,TextAlignmentOptions.Center);
            var hostControls=Panel(root.transform,"HostControls",Card); var start=Button(hostControls.transform,"StartButton","START GAME"); var footer=Panel(root.transform,"Footer",Card); var leave=Button(footer.transform,"LeaveButton","LEAVE");
            Ref(controller,"roomName",room); Ref(controller,"joinCode",code); Ref(controller,"visibility",privacy); Ref(controller,"players",players); Ref(controller,"settingsSummary",summary); Ref(controller,"roster",roster); Ref(controller,"startButton",start); Ref(controller,"leaveButton",leave);
            EventSystem(); Save($"{Root}/Scenes/Lobby/02_Lobby.unity");
        }
        static void BuildRandomizer() { NewScene(); var scene=Child(null, "[SCENE]"); var controller=scene.AddComponent<MiniGameRandomizerUI>(); var canvas = Canvas(null, "Canvas_Randomizer"); var root = Panel(canvas.transform, "RandomizerRoot", Navy); var title=Label(root.transform, "CHOOSING THE NEXT MINIGAME", 48, TextAlignmentOptions.Center); var count=Label(root.transform,"3",80,TextAlignmentOptions.Bottom); Ref(controller,"title",title); Ref(controller,"countdown",count); EventSystem(); Save($"{Root}/Scenes/Transition/03_MiniGameRandomizer.unity"); }
        static void BuildResults() { NewScene(); var scene=Child(null, "[SCENE]"); var controller=scene.AddComponent<ResultsUIController>(); var canvas = Canvas(null, "Canvas_Results"); var root = Panel(canvas.transform, "ResultsRoot", Navy); var title=Label(root.transform, "RESULTS", 56, TextAlignmentOptions.Top); var scorePanel=Panel(root.transform, "Scoreboard", Card); var score=Label(scorePanel.transform,"Waiting for results...",28,TextAlignmentOptions.Center); var button=Button(root.transform, "ContinueButton", "CONTINUE"); Ref(controller,"title",title); Ref(controller,"scoreboard",score); Ref(controller,"continueButton",button); EventSystem(); Save($"{Root}/Scenes/Results/04_Results.unity"); }
        static void BuildMiniGameTemplate() { NewScene(); Child(null, "[MINIGAME]"); var spawns = Child(null, "[SPAWN POINTS]"); for (var i=0;i<10;i++) { var p=Child(spawns, $"PlayerSpawn_{i+1:00}"); p.transform.position = new Vector3((i%5)*2-4, 0, (i/5)*3-1.5f); } var canvas=Canvas(null,"Canvas_MiniGame"); Label(canvas.transform,"MINIGAME TEMPLATE",42,TextAlignmentOptions.Top); EventSystem(); Save($"{Root}/Scenes/MiniGames/MiniGame_Template.unity"); }

        static T Add<T>(GameObject parent, string name) where T : Component { var go = Child(parent, name); return go.AddComponent<T>(); }
        static GameObject Child(GameObject parent, string name) { var go = new GameObject(name); if (parent) go.transform.SetParent(parent.transform); return go; }
        static void NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        static void Save(string path) => EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        static GameObject Canvas(Transform parent, string name) { var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); if (parent) go.transform.SetParent(parent); go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; var scaler=go.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f; return go; }
        static GameObject Panel(Transform parent, string name, Color color) { var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false); Stretch(go.GetComponent<RectTransform>()); go.GetComponent<Image>().color=color; return go; }
        static TMP_Text Label(Transform parent, string text, float size, TextAlignmentOptions alignment) { var go=new GameObject(text.Replace(" ","")+"Label",typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); var rt=go.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(.1f,.1f); rt.anchorMax=new Vector2(.9f,.9f); rt.offsetMin=rt.offsetMax=Vector2.zero; var label=go.GetComponent<TextMeshProUGUI>(); label.text=text; label.fontSize=size; label.alignment=alignment; label.color=Color.white; return label; }
        static Button Button(Transform parent,string name,string text) { var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(parent,false); var image=go.GetComponent<Image>(); image.color=Accent; var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=new Vector2(360,64); Label(go.transform,text,26,TextAlignmentOptions.Center); return go.GetComponent<Button>(); }
        static TMP_InputField InputField(Transform parent,string name,string placeholder) { var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(TMP_InputField)); go.transform.SetParent(parent,false); go.GetComponent<Image>().color=new Color(.04f,.06f,.1f,1); var text=Label(go.transform,string.Empty,24,TextAlignmentOptions.Left) as TextMeshProUGUI; var hint=Label(go.transform,placeholder,22,TextAlignmentOptions.Left) as TextMeshProUGUI; hint.color=new Color(1,1,1,.45f); var field=go.GetComponent<TMP_InputField>(); field.textComponent=text; field.placeholder=hint; return field; }
        static Toggle Toggle(Transform parent,string name,string labelText) { var go=new GameObject(name,typeof(RectTransform),typeof(Toggle)); go.transform.SetParent(parent,false); var bg=new GameObject("Background",typeof(RectTransform),typeof(Image)); bg.transform.SetParent(go.transform,false); var bgRt=bg.GetComponent<RectTransform>(); bgRt.anchorMin=bgRt.anchorMax=new Vector2(0,.5f); bgRt.pivot=new Vector2(0,.5f); bgRt.anchoredPosition=new Vector2(12,0); bgRt.sizeDelta=new Vector2(36,36); bg.GetComponent<Image>().color=Color.gray; var mark=new GameObject("Checkmark",typeof(RectTransform),typeof(Image)); mark.transform.SetParent(bg.transform,false); var markRt=mark.GetComponent<RectTransform>(); markRt.anchorMin=Vector2.zero; markRt.anchorMax=Vector2.one; markRt.offsetMin=new Vector2(6,6); markRt.offsetMax=new Vector2(-6,-6); mark.GetComponent<Image>().color=Accent; var toggle=go.GetComponent<Toggle>(); toggle.targetGraphic=bg.GetComponent<Image>(); toggle.graphic=mark.GetComponent<Image>(); var label=Label(go.transform,labelText,22,TextAlignmentOptions.Left); label.rectTransform.anchorMin=Vector2.zero; label.rectTransform.anchorMax=Vector2.one; label.rectTransform.offsetMin=new Vector2(60,0); label.rectTransform.offsetMax=Vector2.zero; return toggle; }
        static TMP_DefaultControls.Resources DropdownResources() => new()
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
        };
        static TMP_Dropdown Dropdown(Transform parent,string name,IEnumerable<string> values)
        {
            var go = TMP_DefaultControls.CreateDropdown(DropdownResources());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(.04f, .06f, .1f, 1);
            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(values.ToList());
            dropdown.captionText.fontSize = 22;
            dropdown.captionText.color = Color.white;
            dropdown.captionText.alignment = TextAlignmentOptions.Center;
            dropdown.itemText.fontSize = 20;
            dropdown.itemText.color = Color.white;
            return dropdown;
        }
        static void EventSystem() { var root=Child(null,"[EVENT SYSTEM]"); var go=Child(root,"EventSystem"); go.AddComponent<UnityEngine.EventSystems.EventSystem>(); go.AddComponent<InputSystemUIInputModule>(); }
        static void Stretch(RectTransform rt) { rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero; }
    }
}
#endif
