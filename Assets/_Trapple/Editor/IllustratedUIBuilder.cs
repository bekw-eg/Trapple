using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Trapple.Editor
{
    public static class IllustratedUIBuilder
    {
        public const string Root = "Assets/_Trapple/UI/Illustrated/";
        public const string PrefabPath = Root + "Prefabs/TrappleUI.prefab";
        public const string ScenePath = TrappleUIManager.ScenePath;
        private static TMP_FontAsset font;
        private static Transform page;
        private static TrappleUIManager manager;
        private static string hotspots;
        private static readonly Color Ink = new Color(.22f,.09f,.025f);
        private static readonly Color Green = new Color(.01f,.3f,.17f);

        [MenuItem("Tools/Trapple/Build UI")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            foreach (string folder in new[] { "Generated", "Prefabs", "Fonts" }) Directory.CreateDirectory(Root + folder);
            AssetDatabase.Refresh();
            ImportArt();
            hotspots = File.ReadAllText(Root + "Art/unity_hotspots.json");
            font = CreateFont();
            var config = AssetDatabase.LoadAssetAtPath<HubConfiguration>(HubBuilder.ConfigPath);
            if (config == null) throw new InvalidOperationException("Existing HubConfiguration is required.");
            ConfigureData(config);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvasObject = new GameObject("Trapple UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            manager = canvasObject.AddComponent<TrappleUIManager>();
            var surface = new GameObject("Interface", typeof(RectTransform)); surface.transform.SetParent(canvasObject.transform,false); Stretch((RectTransform)surface.transform);
            var backdrop = new GameObject("Backdrop",typeof(RectTransform),typeof(Image)); backdrop.transform.SetParent(surface.transform,false);
            Stretch((RectTransform)backdrop.transform); backdrop.GetComponent<Image>().color = new Color(.11f,.1f,.09f,.98f);
            var pages = new List<IllustratedPage>(); var worlds = new List<WorldCardUI>(); var upgrades = new List<UpgradeCardUI>();
            WardrobePanelUI wardrobe = null; TMP_Text currency = null;
            foreach (string name in new[] { "worlds", "workshop", "wardrobe" })
            {
                var texture = ReadTexture(Root + "Art/full_screens/" + name + "_full.png");
                CleanBackground(name,texture);
                string cleanPath = Root + "Generated/" + name + "_background.png";
                File.WriteAllBytes(cleanPath,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(cleanPath); ImportSprite(cleanPath,false);
                Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(cleanPath);
                var obj = new GameObject(name,typeof(RectTransform),typeof(AspectRatioFitter)); obj.transform.SetParent(surface.transform,false);
                var rect = (RectTransform)obj.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
                var fit = obj.GetComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = background.rect.width / background.rect.height;
                // A native-pixel child scales uniformly with its fitted container.
                var pixels = new GameObject("Reference pixels",typeof(RectTransform),typeof(ReferencePixelScale)); pixels.transform.SetParent(obj.transform,false);
                var pixelsRect = (RectTransform)pixels.transform; pixelsRect.anchorMin = pixelsRect.anchorMax = new Vector2(.5f,.5f);
                pixelsRect.sizeDelta = background.rect.size; pixels.GetComponent<ReferencePixelScale>().SetReferenceSize(background.rect.size);
                page = pixels.transform;
                Picture("Original artwork",new Rect(0,0,background.rect.width,background.rect.height),background);
                Button workshopTab = HotButton(name,"workshop","tabs",manager.OpenWorkshop);
                Button worldsTab = HotButton(name,"worlds","tabs",manager.OpenWorldMap);
                Button wardrobeTab = HotButton(name,"wardrobe","tabs",manager.OpenWardrobe);
                HotButton(name,"close",null,manager.Close);
                var screen = name == "worlds" ? HubScreen.Worlds : name == "workshop" ? HubScreen.Workshop : HubScreen.Wardrobe;
                pages.Add(new IllustratedPage {screen=screen,root=obj,firstButton=name=="worlds"?worldsTab:name=="workshop"?workshopTab:wardrobeTab});
                if (name == "worlds") BuildWorlds(worlds,config);
                else if (name == "workshop")
                {
                    BuildUpgrades(upgrades); currency = Text("Seeds",new Rect(1352,66,57,43),"0",30,Ink,true);
                    currency.enableAutoSizing=true; currency.fontSizeMin=12; currency.fontSizeMax=30; currency.textWrappingMode=TextWrappingModes.NoWrap;
                    HotButton(name,"menu",null,manager.ReturnToMenu);
                }
                else wardrobe = BuildWardrobe(config);
            }
            page = surface.transform;
            var bg=ColorImage("Message background",new Rect(380,970,1160,70),new Color(.035f,.22f,.12f,.98f));
            page=bg.transform;
            var toast=Text("Message",new Rect(22,0,1116,70),"",26,Color.white,true);
            manager.Configure(surface,pages.ToArray(),worlds.ToArray(),upgrades.ToArray(),wardrobe,currency,toast);
            bg.gameObject.SetActive(false);
            surface.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(canvasObject,PrefabPath);
            EnsureEventSystem();
            var bootstrap = new GameObject("UI bootstrap").AddComponent<IllustratedUIBootstrap>();
            bootstrap.Configure(config,manager,AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>("Assets/_Trapple/UI/Hub/HubPanelSettings.asset"),
                AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Assets/_Trapple/UI/Hub/HubReturn.uxml"));
            var camera = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)); camera.tag="MainCamera";
            camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor=new Color(.11f,.1f,.09f);
            EditorSceneManager.SaveScene(scene,ScenePath);
            // Integrate into the real hub using Unity's scene and prefab APIs; preserve its gameplay objects.
            scene = EditorSceneManager.OpenScene(HubSceneFlowController.HubScenePath,OpenSceneMode.Single);
            var facade = UnityEngine.Object.FindAnyObjectByType<HubUIController>();
            if (facade == null) throw new InvalidOperationException("Existing hub UI facade is missing.");
            foreach (var old in UnityEngine.Object.FindObjectsByType<TrappleUIManager>()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            facade.SetIllustratedUI(instance.GetComponent<TrappleUIManager>()); EditorUtility.SetDirty(facade);
            EnsureEventSystem(); EditorSceneManager.SaveScene(scene);
            var scenes = EditorBuildSettings.globalScenes.ToList(); if (!scenes.Any(value=>value.path==ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.globalScenes = scenes.ToArray();
            var preview=AssetDatabase.LoadAssetAtPath<BuildProfile>(HubBuilder.ProfilePath);
            if(preview!=null && preview.overrideGlobalScenes)
            {
                var profileScenes=preview.scenes.ToList();
                if(!profileScenes.Any(value=>value.path==ScenePath)) profileScenes.Add(new EditorBuildSettingsScene(ScenePath,true));
                preview.scenes=profileScenes.ToArray(); EditorUtility.SetDirty(preview);
            }
            var activeScenes=EditorBuildSettings.scenes.ToList();
            if(!activeScenes.Any(value=>value.path==ScenePath)) { activeScenes.Add(new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=activeScenes.ToArray(); }
            AssetDatabase.SaveAssets();
            if (!Application.isBatchMode && setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            Debug.Log("TRAPPLE UI BUILD COMPLETE: reusable prefab, standalone scene and existing Hub integration saved.");
        }

        public static void BuildAndWindows() { Build(); HubBuilder.BuildWindows(); }

        private static void BuildWorlds(List<WorldCardUI> cards, HubConfiguration config)
        {
            string[] ids={"meadow","forest","factory","lava"}; string[] keys={"apple_meadow","deceptive_forest","abandoned_factory","lava_fortress"};
            int[] xs={195,543,892,1239};
            for(int i=0;i<ids.Length;i++)
            {
                int x=xs[i]; var world=config.World(ids[i]);
                Text("Title "+ids[i],new Rect(x+27,496,304,34),world.title,25,Ink,true);
                var progress=Text("Progress "+ids[i],new Rect(x+73,536,234,32),"",19,Ink,true);
                var state=Text("Select label "+ids[i],new Rect(x+65,633,225,32),"",22,Color.white,true);
                var select=HotButton("worlds",keys[i],"world_buttons",null);
                var shade=ColorImage("Locked shade "+ids[i],new Rect(x+25,331,294,162),new Color(0,0,0,.22f));
                shade.raycastTarget=false;
                var seal=Picture("Availability seal "+ids[i],new Rect(x+258,342,60,60),AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Generated/pip.png")); seal.color=new Color(.28f,.16f,.07f);
                var lockText=Text("Lock badge "+ids[i],new Rect(x+260,345,56,54),"",14,new Color(.98f,.85f,.6f),true); lockText.alignment=TextAlignmentOptions.Center;
                state.alignment=TextAlignmentOptions.Center;
                var component=select.gameObject.AddComponent<WorldCardUI>(); component.Configure(ids[i],select,progress,state,shade,lockText); cards.Add(component);
            }
            HotButton("worlds","secret_worlds",null,manager.SecretWorlds);
        }
        private static void BuildUpgrades(List<UpgradeCardUI> cards)
        {
            int[] xs={0,398,786}; string[] ids={"speed","agility","jump"};
            for(int i=0;i<3;i++)
            {
                int x=xs[i];
                var level=Text("Level "+ids[i],new Rect(485+x,375,130,24),"",16,Ink,true);
                var bonus=Text("Bonus "+ids[i],new Rect(304+x,585,303,31),"",27,Green,true);
                var price=Text("Price "+ids[i],new Rect(305+x,616,303,23),"",16,Ink);
                var label=Text("Upgrade label "+ids[i],new Rect(340+x,654,245,36),"",22,Color.white,true);
                label.alignment=TextAlignmentOptions.Center;
                var button=HotButton("workshop",ids[i],"upgrade_buttons",null);
                var pips=new Image[3]; for(int j=0;j<3;j++) pips[j]=Picture("Level pip",new Rect((i==1?492:500)+x+j*35,407,25,25),AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Generated/pip.png"));
                var component=button.gameObject.AddComponent<UpgradeCardUI>(); component.Configure((UpgradeKind)i,button,level,bonus,price,label,pips); cards.Add(component);
            }
        }
        private static WardrobePanelUI BuildWardrobe(HubConfiguration config)
        {
            var rows=new List<SkinItemUI>(); string[] ids={"classic","forest","factory","lava"}; string[] keys={"first_apple","forest_traveler","little_miner","volcano_heart"};
            int[] ys={334,430,518,614};
            for(int i=0;i<4;i++)
            {
                int y=ys[i]; var button=HotButton("wardrobe",keys[i],"skin_rows",null);
                var title=Text("Skin title "+ids[i],new Rect(996,y+22,409,30),"",26,Ink,true);
                var description=Text("Skin description "+ids[i],new Rect(996,y+54,401,26),"",18,Ink);
                var state=Text("Skin status "+ids[i],new Rect(1402,y+25,151,45),"",17,Green,true);
                state.alignment=TextAlignmentOptions.Center;
                var highlight=ColorImage("Selected "+ids[i],new Rect(858,y+10,700,71),Color.clear); highlight.raycastTarget=false;
                var row=button.gameObject.AddComponent<SkinItemUI>(); row.Configure(ids[i],button,title,description,state,highlight); rows.Add(row);
            }
            var preview=Picture("Character preview",new Rect(415,426,198,192),null); preview.preserveAspect=true;
            var previewTitle=Text("Preview title",new Rect(276,641,487,45),"",38,Ink,true);
            var previewDescription=Text("Preview description",new Rect(276,691,487,38),"",21,Ink,true);
            previewTitle.alignment=previewDescription.alignment=TextAlignmentOptions.Center;
            var count=Text("Unlocked count",new Rect(1470,302,71,24),"",20,Ink,true);
            var equip=ButtonAt("Equip selected skin",new Rect(366,730,309,34),null);
            var equipText=Text("Equip label",new Rect(366,730,309,34),"",20,Green,true);
            equipText.alignment=TextAlignmentOptions.Center;
            var component=page.gameObject.AddComponent<WardrobePanelUI>(); component.Configure(rows.ToArray(),preview,previewTitle,previewDescription,count,equip,equipText); return component;
        }

        private static void ConfigureData(HubConfiguration config)
        {
            if(config.illustratedDefaultsVersion==0)
            {
                config.previewStartingSeeds=0; config.tutorialReward=0;
                foreach(var upgrade in config.upgrades) upgrade.costs=new[]{-1,-1,-1};
                config.World("forest").requiredCompletedWorldId="meadow";
                config.World("factory").requiredCompletedWorldId="forest";
                config.World("lava").requiredCompletedWorldId="factory";
                foreach(var world in config.worlds) world.completionRequiresBoss=world.id!="meadow";
                foreach(var skin in config.cosmetics) if(!skin.initiallyUnlocked) skin.unlockRule=CosmeticUnlockRule.Manual;
                config.Cosmetic("factory").title="Маленький шахтёр";
                config.Cosmetic("classic").description="Классика. С этого всё и началось.";
                config.Cosmetic("forest").description="Объездил леса, поля и холмы.";
                config.Cosmetic("factory").description="Глубже копает — дальше идёт.";
                config.Cosmetic("lava").description="Закалён огнём. Но остаётся яблоком.";
                config.illustratedDefaultsVersion=1;
            }
            // Existing gameplay sprites remain available for cosmetic application.
            EditorUtility.SetDirty(config);
        }
        private static TMP_FontAsset CreateFont()
        {
            string path=Root+"Fonts/Trapple Cyrillic.asset";
            var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path); if(asset!=null) return asset;
            // Bake Cyrillic into a static atlas from TMP's OFL-licensed font.
            string source="Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
            if(!File.Exists(source)) throw new FileNotFoundException("Import TMP Essential Resources before building UI.",source);
            asset=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source),64,8,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
            if(asset==null) throw new InvalidOperationException("Could not bake the Cyrillic font atlas.");
            string chars=""; for(int i=32;i<127;i++) chars+=(char)i; for(int i=0x400;i<=0x45f;i++) chars+=(char)i; chars+="→↑·…●";
            asset.TryAddCharacters(chars,out string missing);
            asset.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset,path);
            asset.material.name="Trapple Cyrillic Material"; AssetDatabase.AddObjectToAsset(asset.material,asset);
            foreach(var atlas in asset.atlasTextures) { atlas.name="Trapple Cyrillic Atlas"; AssetDatabase.AddObjectToAsset(atlas,asset); }
            EditorUtility.SetDirty(asset); return asset;
        }
        private static void ImportArt()
        {
            foreach(string file in Directory.GetFiles(Root+"Art","*.png",SearchOption.AllDirectories)) ImportSprite(file.Replace('\\','/'),file.Contains("character") || file.Contains("skin_icons"));
            var pip=new Texture2D(32,32,TextureFormat.RGBA32,false); for(int y=0;y<32;y++) for(int x=0;x<32;x++) pip.SetPixel(x,y,Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))<=15 ? Color.white : Color.clear);
            pip.Apply(); string path=Root+"Generated/pip.png"; File.WriteAllBytes(path,pip.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(pip);
            AssetDatabase.ImportAsset(path); ImportSprite(path,false);
        }
        private static void ImportSprite(string path,bool pixel)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter; if(importer==null) throw new FileNotFoundException(path);
            importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single; importer.maxTextureSize=2048;
            importer.filterMode=pixel?FilterMode.Point:FilterMode.Bilinear; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        private static Texture2D ReadTexture(string path) { var texture=new Texture2D(2,2,TextureFormat.RGBA32,false); if(!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException(path); return texture; }
        private static void CleanBackground(string name,Texture2D texture)
        {
            if(name=="workshop")
            {
                Erase(texture,new RectInt(1357,72,45,33),new Vector2Int(1403,75));
                foreach(int x in new[]{0,398,786})
                {
                    Erase(texture,new RectInt(483+x,378,126,19),new Vector2Int(609+x,378));
                    Erase(texture,new RectInt(301+x,584,230,31),new Vector2Int(612+x,612));
                    EraseVertical(texture,new RectInt(301+x,616,309,20),623+x);
                    EraseVertical(texture,new RectInt(340+x,657,229,29),571+x);
                }
            }
            else if(name=="worlds")
            {
                foreach(int x in new[]{195,543,892,1239})
                {
                    EraseVertical(texture,new RectInt(x+26,498,305,32),x+318);
                    EraseVertical(texture,new RectInt(x+73,535,250,38),x+327);
                    EraseVertical(texture,new RectInt(x+65,638,223,26),x+115);
                }
            }
            else
            {
                EraseInterpolated(texture,new RectInt(410,426,205,192));
                EraseVertical(texture,new RectInt(340,642,400,82),335);
                Erase(texture,new RectInt(1477,303,60,23),new Vector2Int(1410,303));
                foreach(int y in new[]{334,430,518,614})
                {
                    EraseVertical(texture,new RectInt(990,y+20,570,63),980);
                }
            }
            texture.Apply();
        }
        private static void Erase(Texture2D texture,RectInt rect,Vector2Int sample)
        {
            Color color=texture.GetPixel(Mathf.Clamp(sample.x,0,texture.width-1),Mathf.Clamp(texture.height-1-sample.y,0,texture.height-1));
            for(int y=rect.yMin;y<rect.yMax;y++) for(int x=rect.xMin;x<rect.xMax;x++)
                texture.SetPixel(x,texture.height-1-y,color);
        }
        private static void EraseVertical(Texture2D texture,RectInt rect,int sampleX)
        {
            for(int y=rect.yMin;y<rect.yMax;y++)
            {
                Color color=texture.GetPixel(sampleX,texture.height-1-y);
                for(int x=rect.xMin;x<rect.xMax;x++) texture.SetPixel(x,texture.height-1-y,color);
            }
        }
        private static void EraseInterpolated(Texture2D texture,RectInt rect)
        {
            for(int y=rect.yMin;y<rect.yMax;y++)
            {
                Color left=texture.GetPixel(rect.xMin-1,texture.height-1-y),right=texture.GetPixel(rect.xMax+1,texture.height-1-y);
                for(int x=rect.xMin;x<rect.xMax;x++) texture.SetPixel(x,texture.height-1-y,Color.Lerp(left,right,(x-rect.xMin)/(float)rect.width));
            }
        }
        private static Rect Hotspot(string screen,string key,string group)
        {
            // Read the provided integer-pixel layout rather than recomputing coordinates by eye.
            var screens=Regex.Matches(hotspots,"^  \"(worlds|workshop|wardrobe)\"\\s*:",RegexOptions.Multiline);
            var screenMatch=screens.Cast<Match>().First(value=>value.Groups[1].Value==screen);
            int start=screenMatch.Index;
            int end=screens.Cast<Match>().Where(value=>value.Index>start).Select(value=>value.Index).DefaultIfEmpty(hotspots.Length).First();
            string section=hotspots.Substring(start,end-start);
            if(group!=null) section=section.Substring(section.IndexOf("\""+group+"\"",StringComparison.Ordinal));
            var match=Regex.Match(section,"\\\""+key+"\\\"\\s*:\\s*\\{\\s*\\\"px\\\"\\s*:\\s*\\[\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)\\s*,\\s*(\\d+)");
            if(!match.Success) throw new InvalidOperationException("Missing hotspot: "+screen+"/"+key);
            return new Rect(int.Parse(match.Groups[1].Value),int.Parse(match.Groups[2].Value),int.Parse(match.Groups[3].Value),int.Parse(match.Groups[4].Value));
        }
        private static Button HotButton(string screen,string key,string group,UnityAction action)=>ButtonAt(screen+"-"+(group==null?"":group+"-")+key,Hotspot(screen,key,group),action);
        private static Button ButtonAt(string name,Rect rect,UnityAction action)
        {
            var image=ColorImage(name,rect,new Color(1,1,1,0)); image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.normalColor=Color.clear; colors.highlightedColor=new Color(1,.9f,.5f,.15f); colors.selectedColor=new Color(1,.9f,.5f,.14f); colors.pressedColor=new Color(.02f,.25f,.1f,.25f); colors.disabledColor=new Color(0,0,0,.2f); button.colors=colors;
            // Image.color must be white for ColorTint to display its alpha.
            image.color=Color.white;
            if(action!=null) UnityEventTools.AddPersistentListener(button.onClick,action); return button;
        }
        private static Image Picture(string name,Rect rect,Sprite sprite)
        { var image=Node(name,rect).AddComponent<Image>(); image.sprite=sprite; image.raycastTarget=false; return image; }
        private static Image ColorImage(string name,Rect rect,Color color) { var image=Picture(name,rect,null); image.color=color; return image; }
        private static TMP_Text Text(string name,Rect rect,string text,float size,Color color,bool bold=false)
        {
            var label=Node(name,rect).AddComponent<TextMeshProUGUI>(); label.font=font; label.text=text; label.fontSize=size;
            label.color=color; label.fontStyle=bold?FontStyles.Bold:FontStyles.Normal; label.alignment=TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode=TextWrappingModes.Normal; label.overflowMode=TextOverflowModes.Ellipsis; label.raycastTarget=false;
            return label;
        }
        private static GameObject Node(string name,Rect rect)
        {
            var obj=new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(page,false); var rt=(RectTransform)obj.transform;
            rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(0,1); rt.anchoredPosition=new Vector2(rect.x,-rect.y); rt.sizeDelta=rect.size; return obj;
        }
        private static void Stretch(RectTransform rt) { rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero; }
        private static void EnsureEventSystem()
        {
            var systems=UnityEngine.Object.FindObjectsByType<EventSystem>();
            if(systems.Length==0) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            else if(systems[0].GetComponent<BaseInputModule>()==null) systems[0].gameObject.AddComponent<StandaloneInputModule>();
        }
    }
}
