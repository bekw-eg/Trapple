using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using Trapple.UI;
using Trapple.Workshop;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trapple.Editor
{
    // All authored scenes/prefabs are serialized by Unity, never assembled as YAML.
    public static class StorybookUIBuilder
    {
        public const string Root="Assets/_Trapple/UI/Storybook/";
        public const string InterfacePath="Assets/_Trapple/Resources/Storybook/GameInterface.prefab";
        private static TMP_FontAsset font;
        private static readonly Color Cream=new Color(1,.93f,.69f);

        [MenuItem("Tools/Trapple/Build Storybook UI")]
        public static void Build()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Directory.CreateDirectory(Root+"Prefabs"); Directory.CreateDirectory("Assets/_Trapple/Resources/Storybook");
                AssetDatabase.Refresh(); ImportSprites(); font=CreateFont();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                BuildTemplates(); BuildGameInterface(); BuildHubInterface(); UpdateWorkshop();
                foreach(string path in new[]{HubSceneFlowController.HubScenePath,TrappleUIManager.ScenePath})
                {
                    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    var facade=Object.FindAnyObjectByType<HubUIController>();
                    if(facade==null) throw new InvalidOperationException("Existing hub facade is missing: "+path);
                    foreach(var old in Object.FindObjectsByType<TrappleUIManager>(FindObjectsInactive.Include))
                        Object.DestroyImmediate(old.gameObject);
                    var view=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(IllustratedUIBuilder.PrefabPath));
                    facade.SetIllustratedUI(view.GetComponent<TrappleUIManager>()); EditorUtility.SetDirty(facade);
                    EnsureEventSystem(); EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                File.WriteAllText("Temp/storybook-build-result.txt","SUCCESS\n"+DateTime.UtcNow.ToString("O"));
                Debug.Log("STORYBOOK UI BUILD COMPLETE");
            }
            finally { if(setup.Length>0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void ImportSprites()
        {
            foreach(string file in Directory.GetFiles(Root,"*.png",SearchOption.AllDirectories))
            {
                string path=file.Replace('\\','/'); var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
                importer.spritePixelsPerUnit=100;
                importer.spriteBorder=path.EndsWith("Panels/Window.png")?new Vector4(170,115,170,115):
                    path.EndsWith("Panels/Card.png")?new Vector4(110,80,110,80):
                    path.EndsWith("Panels/Small.png")?new Vector4(120,50,120,50):Vector4.zero;
                importer.SaveAndReimport();
            }
        }
        private static TMP_FontAsset CreateFont()
        {
            string path=Root+"Fonts/Thaleah SDF.asset";
            var result=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if(result!=null) return result;
            var source=AssetDatabase.LoadAssetAtPath<Font>(Root+"Fonts/ThaleahFat.ttf");
            if(source==null) throw new InvalidOperationException("Original ThaleahFat.ttf is required.");
            result=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            string chars=""; for(int i=32;i<127;i++) chars+=(char)i;
            result.TryAddCharacters(chars,out string missing);
            result.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(result,path); result.material.name="Thaleah Brown Outline";
            result.material.SetColor("_OutlineColor",new Color(.2f,.065f,.012f,1));
            result.material.SetFloat("_OutlineWidth",.25f); result.material.SetFloat("_OutlineSoftness",0);
            result.material.SetFloat("_FaceDilate",.035f); result.material.EnableKeyword("OUTLINE_ON");
            AssetDatabase.AddObjectToAsset(result.material,result);
            foreach(var atlas in result.atlasTextures) { atlas.name="Thaleah SDF Atlas"; atlas.filterMode=FilterMode.Bilinear; AssetDatabase.AddObjectToAsset(atlas,result); }
            EditorUtility.SetDirty(result); return result;
        }
        private static void BuildTemplates()
        {
            string[] names={"PrimaryButton","SecondaryButton","SmallButton","IconButton","WorldButton","LevelButton","UpgradeButton","WardrobeSlot"};
            string[] sprites={"Panels/Banner","Panels/Small","Panels/Small","Icons/Settings","WorldMap/Meadow","WorldMap/Level","Panels/Small","Wardrobe/Slot"};
            Vector2[] sizes={new Vector2(500,140),new Vector2(400,110),new Vector2(280,90),new Vector2(88,88),new Vector2(580,215),new Vector2(120,120),new Vector2(390,108),new Vector2(240,240)};
            for(int i=0;i<names.Length;i++)
            {
                var go=new GameObject(names[i],typeof(RectTransform),typeof(Image),typeof(Button),typeof(UIButtonAnimator));
                ((RectTransform)go.transform).sizeDelta=sizes[i];
                go.GetComponent<Image>().sprite=Sprite(sprites[i]); go.GetComponent<Image>().preserveAspect=true;
                Style(go.GetComponent<Button>());
                if(i==0 || i==1 || i==2 || i==6) Label("Label",go.transform,new Rect(55,12,sizes[i].x-110,sizes[i].y-24),"BUTTON",36);
                PrefabUtility.SaveAsPrefabAsset(go,Root+"Prefabs/"+names[i]+".prefab"); Object.DestroyImmediate(go);
            }
            var window=Rect("UIWindow",null,new Rect(0,0,1200,860));
            var image=window.gameObject.AddComponent<Image>(); image.sprite=Sprite("Panels/Window"); image.type=Image.Type.Sliced;
            PrefabUtility.SaveAsPrefabAsset(window.gameObject,Root+"Prefabs/UIWindow.prefab"); Object.DestroyImmediate(window.gameObject);
            var slider=CreateSlider(null,new Rect(0,0,680,100));
            PrefabUtility.SaveAsPrefabAsset(slider.gameObject,Root+"Prefabs/UISlider.prefab"); Object.DestroyImmediate(slider.gameObject);
        }
        private static void BuildGameInterface()
        {
            var canvas=Canvas("Trapple Game Interface",100); var view=canvas.gameObject.AddComponent<GameInterface>();
            var controls=new Button[17];
            var menu=Page("Main menu",canvas.transform);
            var backdrop=Picture("Orchard",menu,new Rect(0,0,1920,1080),AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Art/Hub/AppleMeadow/SceneryPlate.png"));
            Stretch(backdrop.rectTransform);
            var fit=backdrop.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio=backdrop.sprite.rect.width/backdrop.sprite.rect.height;
            var shade=Picture("Menu shade",menu,new Rect(0,0,1920,1080),null); shade.color=new Color(.07f,.12f,.035f,.6f); Stretch(shade.rectTransform);
            Picture("Trapple logo",menu,new Rect(480,70,960,300),Sprite("Buttons/Logo"));
            controls[0]=Button("Play",menu,new Rect(680,400,560,155),"Buttons/Play");
            controls[1]=Button("Settings",menu,new Rect(680,565,560,155),"Buttons/Settings");
            controls[2]=Button("Exit",menu,new Rect(680,730,560,155),"Buttons/Exit");
            Label("Tagline",menu,new Rect(580,938,760,50),"A LITTLE APPLE. AN UNEXPECTED ADVENTURE.",32);
            var pause=Modal("Pause",canvas.transform);
            Picture("Pause title",pause,new Rect(620,60,680,225),Sprite("Panels/PauseTitle"));
            string[] art={"CONTINUE","SETTINGS","RESTART LEVEL","LOBBY","MAIN MENU"};
            for(int i=0;i<5;i++)
            {
                controls[3+i]=Template("PrimaryButton",pause,new Rect(690,300+i*135,540,125));
                controls[3+i].name=art[i]; controls[3+i].GetComponentInChildren<TMP_Text>().text=art[i];
            }
            var complete=Modal("Level complete",canvas.transform);
            Picture("Completion title",complete,new Rect(570,80,780,385),Sprite("Panels/CompleteTitle"));
            var detail=Label("Completion detail",complete,new Rect(550,470,820,65),"THE APPLE IS SAFE",44);
            controls[8]=Button("Retry",complete,new Rect(360,620,360,155),"Buttons/Retry");
            controls[9]=Button("Next level",complete,new Rect(780,620,360,155),"Buttons/NextLevel");
            controls[10]=Button("Lobby",complete,new Rect(1200,620,360,155),"Buttons/CompleteLobby");
            var settings=Modal("Settings",canvas.transform); BuildSettings(settings,view.CloseSettings,out controls[11]);
            var hud=Page("HUD",canvas.transform);
            var counter=Picture("Seed counter",hud,new Rect(35,30,240,88),Sprite("Panels/Small"));
            Picture("Seed icon",counter.transform,new Rect(20,10,65,65),Sprite("Icons/Seed"));
            var seeds=Label("Seeds",counter.transform,new Rect(90,14,120,60),"0",34);
            var itemSlot=Picture("Held item",hud,new Rect(300,30,88,88),Sprite("Wardrobe/Slot"));
            view.SetHeldItem(Picture("Item icon",itemSlot.transform,new Rect(18,18,52,52),null));
            controls[12]=Button("Pause",hud,new Rect(1797,30,88,88),"Icons/Pause");
            var shortcuts=Rect("Hub shortcuts",hud,new Rect(400,22,1120,98));
            string[] tabs={"WorldMap","Workshop","Wardrobe","HubSettings"};
            for(int i=0;i<4;i++) controls[13+i]=Button(tabs[i],shortcuts,new Rect(i*280,0,265,98),"Buttons/"+tabs[i]);
            var boss=Label("Boss health",hud,new Rect(440,125,1040,56),"",32);
            var hints=Picture("Movement hints",hud,new Rect(480,895,960,135),Sprite("Panels/Card"));
            hints.type=Image.Type.Sliced;
            string[] keys={"A","D","Left","Right","Space","Shift","Esc"};
            for(int i=0;i<keys.Length;i++) Picture(keys[i],hints.transform,new Rect(40+i*126,18,110,60),Sprite("HUD/"+keys[i]));
            var hint=Label("Hint",hints.transform,new Rect(80,80,800,40),"WATCH YOUR STEP",27);
            var prompt=Picture("Interaction prompt",hud,new Rect(660,780,600,110),Sprite("Panels/Small")); prompt.type=Image.Type.Sliced;
            var key=Picture("Interaction key",prompt.transform,new Rect(40,17,75,75),Sprite("HUD/E"));
            var promptText=Label("Prompt",prompt.transform,new Rect(125,20,405,65),"",30);
            view.Configure(new[]{menu.gameObject,pause.gameObject,complete.gameObject,settings.gameObject,hud.gameObject,hints.gameObject,shortcuts.gameObject,prompt.gameObject},
                new TMP_Text[]{seeds,hint,promptText,boss,detail},key,Sprite("HUD/E"),Sprite("HUD/F"),controls);
            // Runtime binding is in Awake; no duplicate persistent and runtime listeners.
            foreach(var button in controls)
                while(button.onClick.GetPersistentEventCount()>0) UnityEventTools.RemovePersistentListener(button.onClick,0);
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject,InterfacePath); Object.DestroyImmediate(canvas.gameObject);
        }
        private static void BuildHubInterface()
        {
            var config=AssetDatabase.LoadAssetAtPath<HubConfiguration>(HubBuilder.ConfigPath);
            var canvas=Canvas("Trapple UI",150); var manager=canvas.gameObject.AddComponent<TrappleUIManager>();
            var surface=Page("Interface",canvas.transform); var shade=Picture("Backdrop",surface,new Rect(0,0,1920,1080),null);
            Stretch(shade.rectTransform); shade.color=new Color(.04f,.08f,.025f,.64f);
            var pages=new List<IllustratedPage>(); WardrobePanelUI wardrobe=null; WorldMapPanelUI map=null;
            foreach(var screen in new[]{HubScreen.Worlds,HubScreen.Workshop,HubScreen.Wardrobe,HubScreen.Settings})
            {
                var page=Page(screen.ToString(),surface);
                Button back;
                if(screen==HubScreen.Settings) BuildSettings(page,manager.Close,out back);
                else
                {
                    Window(page,new Rect(230,160,1460,780));
                    var banner=Picture("Heading",page,new Rect(620,58,680,210),Sprite(screen==HubScreen.Worlds?"Buttons/WorldMap":screen==HubScreen.Workshop?"Buttons/Workshop":"Buttons/Wardrobe"));
                    back=Button("Back",page,new Rect(300,923,300,100),"Buttons/Back",manager.Close);
                    if(screen==HubScreen.Worlds) map=BuildWorldMap(page,config);
                    else if(screen==HubScreen.Wardrobe) wardrobe=BuildWardrobe(page,config);
                    else Label("Workshop entrance",page,new Rect(490,430,940,150),"ENTER THE WORKSHOP FROM THE ORCHARD",44);
                }
                Button("Close",page,new Rect(1740,40,88,88),"Icons/Close",manager.Close);
                pages.Add(new IllustratedPage {screen=screen,root=page.gameObject,firstButton=back});
            }
            var toast=Picture("Message",surface,new Rect(610,935,1000,100),Sprite("Panels/Small")); toast.type=Image.Type.Sliced;
            var text=Label("Message text",toast.transform,new Rect(100,18,800,60),"",30);
            manager.Configure(surface.gameObject,pages.ToArray(),Array.Empty<WorldCardUI>(),Array.Empty<UpgradeCardUI>(),wardrobe,null,text);
            manager.SetWorldMap(map); toast.gameObject.SetActive(false); surface.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(canvas.gameObject,IllustratedUIBuilder.PrefabPath); Object.DestroyImmediate(canvas.gameObject);
        }
        private static WorldMapPanelUI BuildWorldMap(Transform page,HubConfiguration config)
        {
            var worldButtons=new Button[config.worlds.Length]; string[] art={"Meadow","Forest","Factory","Lava"};
            for(int i=0;i<worldButtons.Length;i++)
            {
                worldButtons[i]=Button("World "+config.worlds[i].id,page,
                    new Rect(310+(i%2)*460,280+(i/2)*235,430,200),"WorldMap/"+art[Mathf.Min(i,3)]);
                Picture("Locked",worldButtons[i].transform,new Rect(365,10,60,60),Sprite("Icons/Lock"));
            }
            var detail=Label("World details",page,new Rect(1240,300,280,280),"",32);
            var levels=Rect("Levels",page,new Rect(540,765,840,130));
            var layout=levels.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing=30; layout.childAlignment=TextAnchor.MiddleCenter;
            layout.childControlWidth=layout.childControlHeight=false; layout.childForceExpandWidth=layout.childForceExpandHeight=false;
            var template=Template("LevelButton",levels,new Rect(0,0,120,120)); template.gameObject.SetActive(false);
            var play=Button("Play level",page,new Rect(1320,916,300,100),"Buttons/Play");
            var map=page.gameObject.AddComponent<WorldMapPanelUI>();
            map.Configure(worldButtons,levels,template,play,detail,new[]{Sprite("WorldMap/Level"),Sprite("WorldMap/Level2"),Sprite("WorldMap/Level3")},Sprite("WorldMap/Locked"),Sprite("WorldMap/Complete"));
            return map;
        }
        private static WardrobePanelUI BuildWardrobe(Transform page,HubConfiguration config)
        {
            var rows=new List<SkinItemUI>();
            for(int i=0;i<config.cosmetics.Length;i++)
            {
                var def=config.cosmetics[i]; var button=Template("WardrobeSlot",page,new Rect(770+(i%2)*360,280+(i/2)*310,220,220)); button.name="Outfit "+def.id;
                var selected=Picture("Selection",button.transform,new Rect(0,0,220,220),Sprite("Wardrobe/Selected")); selected.raycastTarget=false;
                var icon=Picture("Outfit",button.transform,new Rect(50,40,120,120),def.previewSprite!=null?def.previewSprite:def.sprite); icon.color=def.tint;
                var title=Label("Name",page,new Rect(725+(i%2)*360,508+(i/2)*310,310,40),"",25);
                var state=Label("State",button.transform,new Rect(20,258,180,32),"",22);
                var description=Label("Description",button.transform,new Rect(0,0,0,0),"",20);
                var row=button.gameObject.AddComponent<SkinItemUI>(); row.Configure(def.id,button,title,description,state,selected); rows.Add(row);
                row.ConfigureAppearance(icon,Sprite("Wardrobe/Slot"),Sprite("Wardrobe/Locked"),Sprite("Wardrobe/Selected"));
            }
            var preview=Picture("Appi preview",page,new Rect(380,335,270,270),config.heroSprite);
            var name=Label("Selected outfit",page,new Rect(300,620,440,70),"",34);
            var descriptionText=Label("Outfit details",page,new Rect(310,700,420,75),"",26);
            var count=Label("Collection count",page,new Rect(1300,225,220,50),"",30);
            var equip=Template("SecondaryButton",page,new Rect(345,805,380,100)); equip.name="Equip outfit";
            var label=equip.GetComponentInChildren<TMP_Text>();
            var panel=page.gameObject.AddComponent<WardrobePanelUI>(); panel.Configure(rows.ToArray(),preview,name,descriptionText,count,equip,label); return panel;
        }
        private static void UpdateWorkshop()
        {
            var root=PrefabUtility.LoadPrefabContents(WorkshopBuilder.PrefabPath);
            try
            {
                var view=root.GetComponentInChildren<WorkshopUIController>(true); var canvas=view.GetComponent<Canvas>(); Scale(canvas.GetComponent<CanvasScaler>());
                var serialized=new SerializedObject(view); var surface=(GameObject)serialized.FindProperty("surface").objectReferenceValue;
                var input=surface.GetComponent<CanvasGroup>(); var preview=root.GetComponentInChildren<UpgradePreviewController>(true);
                preview.transform.SetParent(canvas.transform,false);
                foreach(Transform child in surface.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                Picture("Interior",surface.transform,new Rect(0,0,1920,1080),AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Art/Workshop/Interior.png"));
                var cards=Rect("Upgrade panels",surface.transform,new Rect(0,0,1920,1080)); var group=cards.gameObject.AddComponent<CanvasGroup>();
                Window(cards,new Rect(260,235,1400,730));
                var sign=Picture("Workshop sign",surface.transform,new Rect(580,20,760,220),Sprite("Buttons/Workshop"));
                var animator=surface.GetComponent<WorkshopSignAnimator>(); animator.Configure(sign.rectTransform,group);
                var selections=new Button[3]; var levels=new TMP_Text[3];
                for(int i=0;i<3;i++)
                {
                    selections[i]=Template("PrimaryButton",cards,new Rect(340,320+i*170,520,140)); selections[i].name="Select "+(UpgradeKind)i;
                    selections[i].GetComponentInChildren<TMP_Text>().text=UIWords.Upgrade((UpgradeKind)i);
                    var label=selections[i].GetComponentInChildren<TMP_Text>(); label.rectTransform.offsetMin=new Vector2(145,15);
                    Picture("Stat icon",selections[i].transform,new Rect(65,30,80,80),Sprite("Workshop/"+((UpgradeKind)i)+"Icon"));
                    levels[i]=Label("Level",cards,new Rect(400,443+i*170,400,35),"",28);
                }
                var title=Label("Selected upgrade",cards,new Rect(940,290,560,60),"",42);
                var description=Label("Upgrade description",cards,new Rect(940,350,560,45),"",23);
                preview.transform.SetParent(cards,false); Position((RectTransform)preview.transform,new Rect(940,410,548,256));
                var stats=Label("Current and next",cards,new Rect(950,680,520,90),"",29);
                var cost=Label("Price",cards,new Rect(950,765,520,50),"",29);
                var purchase=Template("UpgradeButton",cards,new Rect(1005,820,410,110)); purchase.name="Purchase upgrade";
                var pips=new Image[3]; for(int i=0;i<3;i++) pips[i]=Picture("Upgrade pip",cards,new Rect(1450+i*35,305,28,28),Sprite("Icons/Seed"));
                var counter=Picture("Seed counter",surface.transform,new Rect(1570,50,270,100),Sprite("Panels/Small"));
                var balance=Label("Seeds",counter.transform,new Rect(30,18,210,65),"",34);
                var feedback=Label("Feedback",cards,new Rect(400,970,1000,65),"",28);
                var exit=Button("Exit workshop",cards,new Rect(1430,970,280,90),"Buttons/Exit");
                view.Configure(surface,input,animator,preview,selections,purchase,exit,levels,pips,title,description,stats,cost,balance,feedback);
                surface.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root,WorkshopBuilder.PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void BuildSettings(Transform page,UnityAction close,out Button back)
        {
            Window(page,new Rect(360,210,1200,650));
            Picture("Settings title",page,new Rect(600,90,720,230),Sprite("Settings/Title"));
            Label("Master volume",page,new Rect(560,370,800,80),"MASTER VOLUME",48);
            var slider=CreateSlider(page,new Rect(600,490,720,100));
            var value=Label("Volume value",page,new Rect(760,615,400,70),"100%",44);
            page.gameObject.AddComponent<UISettingsPanel>().Configure(slider,value);
            back=Button("Back",page,new Rect(760,720,400,110),"Buttons/Back",close);
        }
        private static Slider CreateSlider(Transform parent,Rect box)
        {
            var root=Rect("Master volume slider",parent,box); var slider=root.gameObject.AddComponent<Slider>(); slider.minValue=0; slider.maxValue=1; slider.value=1;
            var track=Picture("Track",root,new Rect(0,18,box.width,64),Sprite("Panels/Small")); track.type=Image.Type.Sliced;
            track.pixelsPerUnitMultiplier=3; track.color=new Color(.42f,.3f,.2f); track.raycastTarget=true;
            var fillArea=Rect("Fill area",root,new Rect(50,34,box.width-100,32));
            var fill=Picture("Fill",fillArea,new Rect(0,0,box.width-100,32),Sprite("Settings/GoldFill")); fill.preserveAspect=false; Stretch(fill.rectTransform);
            var handleArea=Rect("Handle area",root,new Rect(50,0,box.width-100,100));
            var handle=Picture("Handle",handleArea,new Rect(0,0,82,82),Sprite("Icons/Seed"));
            handle.raycastTarget=true; handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);
            handle.rectTransform.pivot=new Vector2(.5f,.5f); handle.rectTransform.anchoredPosition=Vector2.zero;
            slider.fillRect=fill.rectTransform; slider.handleRect=handle.rectTransform; slider.targetGraphic=handle; Style(slider); return slider;
        }
        private static Canvas Canvas(string name,int sorting)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=sorting; Scale(go.GetComponent<CanvasScaler>()); return canvas;
        }
        private static void Scale(CanvasScaler value)
        {
            value.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; value.referenceResolution=new Vector2(1920,1080);
            value.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; value.matchWidthOrHeight=.5f;
        }
        private static RectTransform Page(string name,Transform parent) { var rect=Rect(name,parent,new Rect(0,0,1920,1080)); Stretch(rect); return rect; }
        private static RectTransform Modal(string name,Transform parent)
        {
            var page=Page(name,parent); var shade=Picture("Modal shade",page,new Rect(0,0,1920,1080),null); shade.color=new Color(.035f,.065f,.02f,.8f); Stretch(shade.rectTransform); return page;
        }
        private static Image Window(Transform parent,Rect area)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/UIWindow.prefab"),parent);
            Position((RectTransform)go.transform,area); return go.GetComponent<Image>();
        }
        private static Button Template(string name,Transform parent,Rect area)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+name+".prefab"),parent);
            Position((RectTransform)go.transform,area);
            foreach(var label in go.GetComponentsInChildren<TMP_Text>(true))
            {
                Stretch(label.rectTransform); label.rectTransform.offsetMin=new Vector2(55,15); label.rectTransform.offsetMax=new Vector2(-55,-15);
            }
            return go.GetComponent<Button>();
        }
        private static Button Button(string name,Transform parent,Rect area,string art,UnityAction action=null)
        {
            var button=Template(art.StartsWith("Icons/")?"IconButton":art.StartsWith("WorldMap/")?"WorldButton":
                art=="Buttons/Back"?"SmallButton":"PrimaryButton",parent,area); button.name=name;
            foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)) Object.DestroyImmediate(label.gameObject);
            var image=button.GetComponent<Image>(); image.sprite=Sprite(art); image.type=Image.Type.Simple; image.preserveAspect=true;
            if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action); return button;
        }
        private static void Style(Selectable button)
        {
            button.transition=Selectable.Transition.ColorTint;
            var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1,.97f,.78f);
            colors.selectedColor=new Color(1,.95f,.65f); colors.pressedColor=new Color(.8f,.7f,.5f);
            colors.disabledColor=new Color(.36f,.36f,.32f,.7f); colors.fadeDuration=.11f; button.colors=colors;
        }
        private static TMP_Text Label(string name,Transform parent,Rect area,string value,float size)
        {
            var rect=Rect(name,parent,area); var text=rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font=font;
            text.text=value; text.fontSize=size*1.3f; text.color=Cream; text.alignment=TextAlignmentOptions.Center;
            text.textWrappingMode=TextWrappingModes.Normal; text.raycastTarget=false; text.characterSpacing=1;
            return text;
        }
        private static Image Picture(string name,Transform parent,Rect area,Sprite sprite)
        {
            var image=Rect(name,parent,area).gameObject.AddComponent<Image>(); image.sprite=sprite; image.preserveAspect=sprite!=null; image.raycastTarget=false; return image;
        }
        private static Sprite Sprite(string name)
        {
            var value=AssetDatabase.LoadAssetAtPath<Sprite>(Root+name+".png");
            if(value==null) throw new FileNotFoundException("UI sprite missing: "+name); return value;
        }
        private static RectTransform Rect(string name,Transform parent,Rect area)
        {
            var result=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); if(parent!=null) result.SetParent(parent,false); Position(result,area); return result;
        }
        private static void Position(RectTransform target,Rect area)
        {
            target.anchorMin=target.anchorMax=new Vector2(0,1); target.pivot=new Vector2(0,1);
            target.anchoredPosition=new Vector2(area.x,-area.y); target.sizeDelta=area.size; target.localScale=Vector3.one;
        }
        private static void Stretch(RectTransform target)
        { target.anchorMin=Vector2.zero; target.anchorMax=Vector2.one; target.offsetMin=target.offsetMax=Vector2.zero; }
        private static void EnsureEventSystem()
        { if(Object.FindAnyObjectByType<EventSystem>()==null) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule)); }
    }
}
