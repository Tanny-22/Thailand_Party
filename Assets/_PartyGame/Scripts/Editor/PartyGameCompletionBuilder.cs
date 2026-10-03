#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core;
using PartyGame.MiniGames;
using PartyGame.Players;
using PartyGame.UI.Customize;
using PartyGame.UI.Lobby;
using PartyGame.UI.MainMenu;
using PartyGame.UI.Pause;
using PartyGame.UI.Randomizer;
using PartyGame.UI.Results;
using PartyGame.UI.Settings;
using PartyGame.UI.Shared;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PartyGame.EditorTools
{
    public static class PartyGameCompletionBuilder
    {
        const string Root = "Assets/_PartyGame";
        static readonly Color Card = new(.08f, .12f, .2f, .96f), Accent = new(.12f, .72f, .9f, 1f), Muted = new(.18f, .22f, .3f, 1f);

        [MenuItem("Party Game/Complete UI Wiring")]
        public static void BuildAll()
        {
            BuildEntryPrefabs();
            RepairMainMenu(); RepairLobby(); RepairRandomizer(); RepairResults(); RepairMiniGame();
            AssetDatabase.SaveAssets();
            Debug.Log("Party Game completion UI and playable template wiring built.");
        }

        static void BuildEntryPrefabs()
        {
            BuildSelectionPrefab("MiniGameCard"); BuildSelectionPrefab("CharacterCard");
            {
                var root = Row("PublicRoomEntry", 76); var name = Text(root.transform, "RoomName", "Room", 23); var count = Text(root.transform, "PlayerCount", "0/0", 21); var join = MakeButton(root.transform, "JoinButton", "JOIN");
                var view = root.AddComponent<PublicRoomEntryView>(); Ref(view, "roomNameLabel", name); Ref(view, "playerCountLabel", count); Ref(view, "joinButton", join); SavePrefab(root, "PublicRoomEntry");
            }
            {
                var root = Row("LobbyPlayerEntry", 84); var avatar = Image(root.transform, "CharacterThumbnail", new Color(.3f,.35f,.45f,1), 64); var name = Text(root.transform,"DisplayName","Player",23); var badge = Panel(root.transform,"HostBadge",Accent); Text(badge.transform,"Label","HOST",18); var kick=MakeButton(root.transform,"KickButton","KICK"); var ban=MakeButton(root.transform,"BanButton","BAN");
                var view=root.AddComponent<LobbyPlayerEntryView>(); Ref(view,"characterThumbnail",avatar); Ref(view,"displayNameLabel",name); Ref(view,"hostBadge",badge); Ref(view,"kickButton",kick); Ref(view,"banButton",ban); SavePrefab(root,"LobbyPlayerEntry");
            }
            {
                var root=Row("ResultsPlayerEntry",76); var avatar=Image(root.transform,"CharacterThumbnail",Muted,60); var name=Text(root.transform,"DisplayName","Player",23); var wins=Text(root.transform,"Wins","0 WINS",22); var highlight=Panel(root.transform,"WinnerHighlight",new Color(1f,.75f,.1f,.35f));
                var view=root.AddComponent<ResultsPlayerEntryView>(); Ref(view,"characterThumbnail",avatar); Ref(view,"displayNameLabel",name); Ref(view,"winsLabel",wins); Ref(view,"winnerHighlight",highlight); SavePrefab(root,"ResultsPlayerEntry");
            }
        }
        static void BuildSelectionPrefab(string name)
        {
            var root=Row(name,100); var toggle=root.AddComponent<Toggle>(); toggle.targetGraphic=root.GetComponent<Image>(); var thumb=Image(root.transform,"Thumbnail",Muted,80); var title=Text(root.transform,"Title",name,22); var highlight=Image(root.transform,"SelectedHighlight",new Color(.1f,.9f,.65f,.45f),16); toggle.graphic=highlight;
            var view=root.AddComponent<SelectionCardView>(); Ref(view,"thumbnail",thumb); Ref(view,"title",title); Ref(view,"toggle",toggle); Ref(view,"selectedHighlight",highlight); SavePrefab(root,name);
        }

        static void RepairMainMenu()
        {
            Open("Scenes/Menu/01_MainMenu.unity");
            var config=Load<GameConfig>("Data/Config/GameConfig.asset"); var mini=Load<MiniGameRegistry>("Data/MiniGames/MiniGameRegistry.asset"); var chars=Load<CharacterRegistry>("Data/Characters/CharacterRegistry.asset");
            var miniCard=Load<SelectionCardView>("Prefabs/UI/MiniGameCard.prefab"); var charCard=Load<SelectionCardView>("Prefabs/UI/CharacterCard.prefab"); var roomEntry=Load<PublicRoomEntryView>("Prefabs/UI/PublicRoomEntry.prefab");
            var mainController=Find<MainMenuController>("[SCENE]"); EnsureVertical(FindObject("MainPanel"),14,300);
            var create=Find<CreateRoomPanelController>("CreateRoomPanel"); var createGo=create.gameObject; EnsureVertical(createGo,12,120); var miniList=ScrollList(createGo.transform,"MiniGameList",260); var validation=Text(createGo.transform,"ValidationLabel","",20); var createBack=MakeButton(createGo.transform,"BackButton","BACK"); UnityEventTools.AddPersistentListener(createBack.onClick,mainController.ShowMain); Ref(create,"miniGameCardPrefab",miniCard); Ref(create,"miniGameList",miniList); Ref(create,"validationLabel",validation);
            var join=Find<JoinRoomPanelController>("JoinRoomPanel"); EnsureVertical(join.gameObject,12,120); var roomList=ScrollList(join.transform,"PublicRoomList",300); var joinBack=MakeButton(join.transform,"BackButton","BACK"); UnityEventTools.AddPersistentListener(joinBack.onClick,mainController.ShowMain); Ref(join,"roomEntryPrefab",roomEntry); Ref(join,"roomList",roomList); Ref(join,"modal",Find<ModalDialogController>("ModalDialogPanel"));
            var customize=Find<CustomizePanelController>("CustomizePanel"); EnsureVertical(customize.gameObject,12,120); var characterList=ScrollList(customize.transform,"CharacterList",260); var feedback=Text(customize.transform,"FeedbackLabel","",20); var save=Find<Button>("SaveProfileButton"); var customizeBack=MakeButton(customize.transform,"BackButton","BACK"); UnityEventTools.AddPersistentListener(customizeBack.onClick,mainController.ShowMain); Ref(customize,"gameConfig",config); Ref(customize,"characterCardPrefab",charCard); Ref(customize,"characterList",characterList); Ref(customize,"feedbackLabel",feedback); Ref(customize,"saveButton",save);
            var settingsRoot=Find<SettingsMenuController>("SettingsPanel").gameObject; BuildSettings(settingsRoot); var settingsBack=MakeButton(settingsRoot.transform,"BackButton","BACK"); UnityEventTools.AddPersistentListener(settingsBack.onClick,mainController.ShowMain);
            FindObject("THAILANDPARTYLabel").transform.SetAsLastSibling();
            Save();
        }

        static void BuildSettings(GameObject root)
        {
            EnsureVertical(root,12,20); var controller=root.GetComponent<SettingsMenuController>();
            var nav=Row("SettingsNavigation",64); nav.transform.SetParent(root.transform,false); var audioButton=MakeButton(nav.transform,"AudioButton","AUDIO"); var controlsButton=MakeButton(nav.transform,"ControlsButton","CONTROLS"); var graphicsButton=MakeButton(nav.transform,"GraphicsButton","GRAPHICS");
            var audio=Panel(root.transform,"AudioPanel",Card); EnsureVertical(audio,10,18); var audioController=audio.AddComponent<AudioSettingsPanel>(); var master=Slider(audio.transform,"MasterVolume","Master"); var music=Slider(audio.transform,"MusicVolume","Music"); var sfx=Slider(audio.transform,"SFXVolume","SFX"); Ref(audioController,"master",master); Ref(audioController,"music",music); Ref(audioController,"sfx",sfx);
            var controls=Panel(root.transform,"ControlsPanel",Card); EnsureVertical(controls,10,18); var controlsController=controls.AddComponent<ControlsSettingsPanel>(); var actions=Dropdown(controls.transform,"ActionDropdown",new[]{"Loading actions..."}); var binding=Text(controls.transform,"CurrentBinding","Current binding",22); var status=Text(controls.transform,"RebindStatus","",19); var rebind=MakeButton(controls.transform,"RebindButton","REBIND"); var reset=MakeButton(controls.transform,"ResetButton","RESET"); var cancel=MakeButton(controls.transform,"CancelButton","CANCEL"); Ref(controlsController,"actionDropdown",actions); Ref(controlsController,"currentBindingLabel",binding); Ref(controlsController,"statusLabel",status); Ref(controlsController,"rebindButton",rebind); Ref(controlsController,"resetButton",reset); Ref(controlsController,"cancelButton",cancel);
            var graphics=Panel(root.transform,"GraphicsPanel",Card); EnsureVertical(graphics,10,18); var graphicsController=graphics.AddComponent<GraphicsSettingsPanel>(); var resolutions=Dropdown(graphics.transform,"ResolutionDropdown",new[]{"Resolution"}); var fullscreen=MakeToggle(graphics.transform,"FullscreenToggle","Fullscreen"); var vsync=MakeToggle(graphics.transform,"VSyncToggle","VSync"); var apply=MakeButton(graphics.transform,"ApplyGraphicsButton","APPLY"); UnityEventTools.AddPersistentListener(apply.onClick,graphicsController.Apply); Ref(graphicsController,"resolutions",resolutions); Ref(graphicsController,"fullscreen",fullscreen); Ref(graphicsController,"vsync",vsync);
            Ref(controller,"audioPanel",audio); Ref(controller,"controlsPanel",controls); Ref(controller,"graphicsPanel",graphics); Ref(controller,"audioButton",audioButton); Ref(controller,"controlsButton",controlsButton); Ref(controller,"graphicsButton",graphicsButton);
        }

        static void RepairLobby()
        {
            Open("Scenes/Lobby/02_Lobby.unity"); var controller=Object.FindFirstObjectByType<LobbyUIController>(); var root=GameObject.Find("LobbyRoot");
            EnsureVertical(root,10,40); var header=GameObject.Find("Header"); SetLayout(header,100,0,0); var middle=Row("LobbyContent",680); middle.transform.SetParent(root.transform,false); middle.GetComponent<LayoutElement>().flexibleHeight=1;
            var playerPanel=GameObject.Find("PlayerList"); playerPanel.transform.SetParent(middle.transform,false); SetLayout(playerPanel,0,1,1); EnsureVertical(playerPanel,8,16); var legacyRoster=FindObject("Player[HOST]Label"); legacyRoster.SetActive(false); var playerContent=ScrollList(playerPanel.transform,"PlayerListContent",520);
            var settings=GameObject.Find("RoomSettings"); settings.transform.SetParent(middle.transform,false); SetLayout(settings,0,1,1); EnsureVertical(settings,8,16); var hostRoot=Panel(settings.transform,"HostSettingsEditor",Card); EnsureVertical(hostRoot,8,12); var roomInput=Input(hostRoot.transform,"RoomNameInput","Room name"); var privacy=MakeToggle(hostRoot.transform,"PrivateToggle","Private room"); var capacity=Dropdown(hostRoot.transform,"CapacityDropdown",Enumerable.Range(2,9).Select(x=>x.ToString())); var wins=Dropdown(hostRoot.transform,"WinsDropdown",Enumerable.Range(1,10).Select(x=>x.ToString())); var random=MakeToggle(hostRoot.transform,"RandomToggle","Random selection"); var miniList=ScrollList(hostRoot.transform,"MiniGameList",180); var apply=MakeButton(hostRoot.transform,"ApplySettingsButton","APPLY ROOM SETTINGS");
            var hostControls=GameObject.Find("HostControls"); EnsureHorizontal(hostControls,10,120); SetLayout(hostControls,72,0,0); var footer=GameObject.Find("Footer"); EnsureHorizontal(footer,10,120); SetLayout(footer,72,0,0); middle.transform.SetSiblingIndex(1);
            var canvas=Object.FindFirstObjectByType<Canvas>(); var loading=MakeLoading(canvas.transform); var modal=MakeModal(canvas.transform);
            Ref(controller,"playerList",playerContent); Ref(controller,"playerEntryPrefab",Load<LobbyPlayerEntryView>("Prefabs/UI/LobbyPlayerEntry.prefab")); Ref(controller,"characterRegistry",Load<CharacterRegistry>("Data/Characters/CharacterRegistry.asset")); Ref(controller,"hostSettingsRoot",hostRoot); Ref(controller,"roomNameInput",roomInput); Ref(controller,"privateToggle",privacy); Ref(controller,"capacityDropdown",capacity); Ref(controller,"winsDropdown",wins); Ref(controller,"randomToggle",random); Ref(controller,"miniGameList",miniList); Ref(controller,"miniGameCardPrefab",Load<SelectionCardView>("Prefabs/UI/MiniGameCard.prefab")); Ref(controller,"miniGameRegistry",Load<MiniGameRegistry>("Data/MiniGames/MiniGameRegistry.asset")); Ref(controller,"applySettingsButton",apply); Ref(controller,"modal",modal); Ref(controller,"loading",loading); Save();
        }
        static void RepairRandomizer()
        {
            Open("Scenes/Transition/03_MiniGameRandomizer.unity"); var controller=Object.FindFirstObjectByType<MiniGameRandomizerUI>(); var root=GameObject.Find("RandomizerRoot"); var image=Image(root.transform,"SelectedMiniGameThumbnail",Muted,320); Ref(controller,"thumbnail",image); Save();
        }
        static void RepairResults()
        {
            Open("Scenes/Results/04_Results.unity"); var controller=Object.FindFirstObjectByType<ResultsUIController>(); var root=GameObject.Find("ResultsRoot"); EnsureVertical(root,12,80); var panel=GameObject.Find("Scoreboard"); SetLayout(panel,0,0,1); EnsureVertical(panel,8,16); var list=ScrollList(panel.transform,"ResultsList",480); SetLayout(GameObject.Find("RESULTSLabel"),100,0,0); SetLayout(GameObject.Find("ContinueButton"),70,0,0); Ref(controller,"resultList",list); Ref(controller,"resultEntryPrefab",Load<ResultsPlayerEntryView>("Prefabs/UI/ResultsPlayerEntry.prefab")); Ref(controller,"characterRegistry",Load<CharacterRegistry>("Data/Characters/CharacterRegistry.asset")); Save();
        }
        static void RepairMiniGame()
        {
            Open("Scenes/MiniGames/MiniGame_Template.unity"); var canvas=Object.FindFirstObjectByType<Canvas>(); var debug=Panel(canvas.transform,"[DEVELOPMENT TEST UI]",new Color(.2f,.06f,.08f,.95f)); EnsureVertical(debug,10,20); var status=Text(debug.transform,"Status","DEVELOPMENT TEST — HOST ONLY",26); var list=ScrollList(debug.transform,"PlayerWinnerButtons",300); var template=MakeButton(debug.transform,"PlayerButtonTemplate","AWARD WIN"); template.gameObject.SetActive(false); var networkObject=debug.AddComponent<NetworkObject>(); var controller=debug.AddComponent<MiniGameTemplateDebugController>(); Ref(controller,"playerButtonList",list); Ref(controller,"playerButtonPrefab",template); Ref(controller,"statusLabel",status);
            var pause=Panel(canvas.transform,"PauseController",Color.clear); var pauseController=pause.AddComponent<PauseMenuController>(); var overlay=Panel(pause.transform,"PauseRoot",new Color(0,0,0,.9f)); EnsureVertical(overlay,10,24); var main=Panel(overlay.transform,"PauseMain",Card); EnsureVertical(main,10,18); var continueButton=MakeButton(main.transform,"ContinueButton","CONTINUE"); var settingsButton=MakeButton(main.transform,"SettingsButton","SETTINGS"); var customizeButton=MakeButton(main.transform,"CustomizeButton","CUSTOMIZE"); var menuButton=MakeButton(main.transform,"MainMenuButton","MAIN MENU"); var quitButton=MakeButton(main.transform,"QuitButton","QUIT TO WINDOW"); var settingsPanel=Panel(overlay.transform,"PauseSettings",Card); settingsPanel.AddComponent<SettingsMenuController>(); BuildSettings(settingsPanel); var customizePanel=Panel(overlay.transform,"PauseCustomize",Card); var customize=customizePanel.AddComponent<CustomizePanelController>(); EnsureVertical(customizePanel,10,18); var name=Input(customizePanel.transform,"DisplayNameInput","Player name"); var cards=ScrollList(customizePanel.transform,"CharacterList",240); var feedback=Text(customizePanel.transform,"Feedback","",19); var save=MakeButton(customizePanel.transform,"SaveButton","SAVE PROFILE"); Ref(customize,"displayNameInput",name); Ref(customize,"registry",Load<CharacterRegistry>("Data/Characters/CharacterRegistry.asset")); Ref(customize,"gameConfig",Load<GameConfig>("Data/Config/GameConfig.asset")); Ref(customize,"characterCardPrefab",Load<SelectionCardView>("Prefabs/UI/CharacterCard.prefab")); Ref(customize,"characterList",cards); Ref(customize,"feedbackLabel",feedback); Ref(customize,"saveButton",save);
            Ref(pauseController,"root",overlay); Ref(pauseController,"mainPanel",main); Ref(pauseController,"settingsPanel",settingsPanel); Ref(pauseController,"customizePanel",customizePanel); UnityEventTools.AddPersistentListener(continueButton.onClick,pauseController.Continue); UnityEventTools.AddPersistentListener(settingsButton.onClick,pauseController.ShowSettings); UnityEventTools.AddPersistentListener(customizeButton.onClick,pauseController.ShowCustomize); UnityEventTools.AddPersistentListener(menuButton.onClick,pauseController.LeaveToMenu); UnityEventTools.AddPersistentListener(quitButton.onClick,pauseController.QuitToWindow); settingsPanel.SetActive(false); customizePanel.SetActive(false); overlay.SetActive(false); Save();
        }

        static T Load<T>(string relative) where T:Object => AssetDatabase.LoadAssetAtPath<T>($"{Root}/{relative}");
        static T Find<T>(string name) where T:Component => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(x => x.gameObject.scene.IsValid() && x.gameObject.name == name);
        static GameObject FindObject(string name) => Resources.FindObjectsOfTypeAll<Transform>().First(x => x.gameObject.scene.IsValid() && x.gameObject.name == name).gameObject;
        static void Open(string relative) => EditorSceneManager.OpenScene($"{Root}/{relative}",OpenSceneMode.Single);
        static void Save() { EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveOpenScenes(); }
        static void Ref(Object target,string field,Object value) { var so=new SerializedObject(target); var p=so.FindProperty(field); if(p!=null){p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);} }
        static GameObject Row(string name,float height) { var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(HorizontalLayoutGroup),typeof(LayoutElement)); go.GetComponent<Image>().color=Card; go.GetComponent<LayoutElement>().preferredHeight=height; var l=go.GetComponent<HorizontalLayoutGroup>(); l.spacing=12;l.padding=new RectOffset(12,12,8,8);l.childAlignment=TextAnchor.MiddleCenter;l.childControlWidth=true;l.childControlHeight=true;l.childForceExpandWidth=true;l.childForceExpandHeight=true;return go; }
        static GameObject Panel(Transform parent,string name,Color color){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(LayoutElement));go.transform.SetParent(parent,false);Stretch(go.GetComponent<RectTransform>());go.GetComponent<Image>().color=color;go.GetComponent<LayoutElement>().flexibleHeight=1;return go;}
        static void SetLayout(GameObject go,float preferredHeight,float flexibleWidth,float flexibleHeight){var e=go.GetComponent<LayoutElement>()??go.AddComponent<LayoutElement>();e.preferredHeight=preferredHeight;e.flexibleWidth=flexibleWidth;e.flexibleHeight=flexibleHeight;}
        static void EnsureHorizontal(GameObject go,int spacing,int padding){var l=go.GetComponent<HorizontalLayoutGroup>()??go.AddComponent<HorizontalLayoutGroup>();l.spacing=spacing;l.padding=new RectOffset(padding,padding,4,4);l.childAlignment=TextAnchor.MiddleCenter;l.childControlWidth=true;l.childControlHeight=true;l.childForceExpandWidth=true;l.childForceExpandHeight=true;}
        static void EnsureVertical(GameObject go,int spacing,int padding)
        {
            var l=go.GetComponent<VerticalLayoutGroup>()??go.AddComponent<VerticalLayoutGroup>();l.spacing=spacing;l.padding=new RectOffset(padding,padding,padding,padding);l.childAlignment=TextAnchor.UpperCenter;l.childControlWidth=true;l.childControlHeight=true;l.childForceExpandWidth=true;l.childForceExpandHeight=false;
            for(var i=0;i<go.transform.childCount;i++)
            {
                var child=go.transform.GetChild(i);var e=child.GetComponent<LayoutElement>()??child.gameObject.AddComponent<LayoutElement>();
                if(child.GetComponent<Button>())e.preferredHeight=64;else if(child.GetComponent<TMP_InputField>()||child.GetComponent<TMP_Dropdown>())e.preferredHeight=58;else if(child.GetComponent<Toggle>())e.preferredHeight=52;else if(child.GetComponent<TMP_Text>())e.preferredHeight=48;else if(e.preferredHeight<0)e.preferredHeight=64;
            }
        }
        static TMP_Text Text(Transform parent,string name,string value,float size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI),typeof(LayoutElement));go.transform.SetParent(parent,false);var t=go.GetComponent<TMP_Text>();t.text=value;t.fontSize=size;t.color=Color.white;t.alignment=TextAlignmentOptions.Center;go.GetComponent<LayoutElement>().preferredHeight=Mathf.Max(42,size+14);return t;}
        static Image Image(Transform parent,string name,Color color,float size){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(LayoutElement));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;var e=go.GetComponent<LayoutElement>();e.preferredWidth=size;e.preferredHeight=size;return go.GetComponent<Image>();}
        static Button MakeButton(Transform parent,string name,string label){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=Accent;go.GetComponent<LayoutElement>().preferredHeight=58;Text(go.transform,"Label",label,21);return go.GetComponent<Button>();}
        static TMP_InputField Input(Transform parent,string name,string placeholder){var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(TMP_InputField),typeof(LayoutElement));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=Muted;go.GetComponent<LayoutElement>().preferredHeight=58;var text=Text(go.transform,"Text","",21) as TextMeshProUGUI;var hint=Text(go.transform,"Placeholder",placeholder,20) as TextMeshProUGUI;hint.color=new Color(1,1,1,.5f);var input=go.GetComponent<TMP_InputField>();input.textComponent=text;input.placeholder=hint;return input;}
        static Toggle MakeToggle(Transform parent,string name,string label){var row=Row(name,54);row.transform.SetParent(parent,false);var toggle=row.AddComponent<Toggle>();var mark=Image(row.transform,"Checkmark",Accent,34);toggle.targetGraphic=row.GetComponent<Image>();toggle.graphic=mark;Text(row.transform,"Label",label,20);return toggle;}
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
        static TMP_Dropdown Dropdown(Transform parent,string name,IEnumerable<string> options)
        {
            var go = TMP_DefaultControls.CreateDropdown(DropdownResources());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = Muted;
            var layout = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            layout.preferredHeight = 58;
            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
            dropdown.captionText.fontSize = 20;
            dropdown.captionText.color = Color.white;
            dropdown.captionText.alignment = TextAlignmentOptions.Center;
            dropdown.itemText.fontSize = 20;
            dropdown.itemText.color = Color.white;
            return dropdown;
        }
        static Slider Slider(Transform parent,string name,string label){Text(parent,$"{name}Label",label,20);var go=new GameObject(name,typeof(RectTransform),typeof(Slider),typeof(LayoutElement));go.transform.SetParent(parent,false);go.GetComponent<LayoutElement>().preferredHeight=42;var bg=Image(go.transform,"Background",Muted,20);var fill=Image(go.transform,"Fill",Accent,20);var handle=Image(go.transform,"Handle",Color.white,28);var s=go.GetComponent<Slider>();s.minValue=0;s.maxValue=1;s.fillRect=fill.rectTransform;s.handleRect=handle.rectTransform;s.targetGraphic=handle;return s;}
        static Transform ScrollList(Transform parent,string name,float height){var root=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(ScrollRect),typeof(LayoutElement));root.transform.SetParent(parent,false);root.GetComponent<Image>().color=new Color(0,0,0,.2f);root.GetComponent<LayoutElement>().preferredHeight=height;var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));viewport.transform.SetParent(root.transform,false);viewport.GetComponent<Image>().color=new Color(0,0,0,.05f);Stretch(viewport.GetComponent<RectTransform>());var content=new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false);var layout=content.GetComponent<VerticalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;var rt=content.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(.5f,1);var scroll=root.GetComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=rt;scroll.horizontal=false;return content.transform;}
        static ModalDialogController MakeModal(Transform parent){var root=Panel(parent,"ModalDialogPanel",new Color(.12f,.03f,.05f,.98f));EnsureVertical(root,12,30);var label=Text(root.transform,"Message","Message",26);var close=MakeButton(root.transform,"CloseButton","OK");var c=root.AddComponent<ModalDialogController>();Ref(c,"messageLabel",label);Ref(c,"closeButton",close);root.SetActive(false);return c;}
        static LoadingOverlayController MakeLoading(Transform parent){var root=Panel(parent,"LoadingOverlay",new Color(0,0,0,.85f));var label=Text(root.transform,"Label","Loading...",30);var c=root.AddComponent<LoadingOverlayController>();Ref(c,"label",label);root.SetActive(false);return c;}
        static void Stretch(RectTransform rt){rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static void SavePrefab(GameObject root,string name){PrefabUtility.SaveAsPrefabAsset(root,$"{Root}/Prefabs/UI/{name}.prefab");Object.DestroyImmediate(root);}
    }
}
#endif
