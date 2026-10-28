using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Trapple.Core;
using Trapple.Hub;
using Trapple.Player;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Trapple.Editor
{
    public static class HubBuilder
    {
        public const string ProfilePath = "Assets/_Trapple/Settings/HubPreview.asset";
        public const string ConfigPath = "Assets/_Trapple/Settings/HubConfiguration.asset";
        private const string Root = "Assets/_Trapple/";
        private const string Art = HubArtBuilder.Root;
        private static Sprite[] props, heroes, worldArt;
        private static PlayerAnimationSet playerAnimations;
        private const string PixelWorldSource = Art+"Source/PixelHubReference.png";

        [MenuItem("Trapple/Hub/Rebuild Hub")]
        public static void BuildHub()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Scenes/Hub", "Art/Hub", "Prefabs/Hub", "UI/Hub", "Settings" })
                Directory.CreateDirectory(Root+folder);
            AssetDatabase.Refresh();
            if (!File.Exists(Art+"HeroAtlas.png") || !File.Exists(Art+"HubProps.png") ||
                !File.Exists(PixelWorldSource) || !File.Exists(PixelPlayerAssets.SourcePath))
                throw new InvalidOperationException("Hub source artwork is missing.");
            HubArtBuilder.BuildSupportArt();
            AssetDatabase.Refresh();
            foreach (string file in Directory.GetFiles(Art,"*.png")) HubArtBuilder.Import(file.Replace('\\','/'));
            HubArtBuilder.ImportProps();
            ImportHeroes();
            playerAnimations = PixelPlayerAssets.Import();
            ImportWorlds();
            props=AssetDatabase.LoadAllAssetsAtPath(Art+"HubProps.png").OfType<Sprite>().ToArray();
            heroes=AssetDatabase.LoadAllAssetsAtPath(Art+"HeroAtlas.png").OfType<Sprite>().ToArray();
            worldArt=AssetDatabase.LoadAllAssetsAtPath(PixelWorldSource).OfType<Sprite>().ToArray();
            HubConfiguration config=CreateConfiguration();
            PanelSettings panel=CreatePanel();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";
            cameraObject.transform.position=new Vector3(0f,3.1f,-10f);
            var camera=cameraObject.GetComponent<Camera>();
            camera.orthographic=true; camera.orthographicSize=5.6f;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.61f,.74f,.65f);

            SpriteRenderer background=Visual("Golden hour orchard",Sprite("MeadowBackdrop"),new Vector2(0,3.5f),new Vector2(24,13.5f),-40);
            Motion(background.gameObject,HubAmbience.Motion.Parallax,.92f,.1f,0,cameraObject.transform);
            var hill=Visual("Distant rolling meadow",Sprite("Hill"),new Vector2(0,-.3f),new Vector2(60,6),-30);
            hill.color=new Color(.85f,1f,.86f,.6f);
            Motion(hill.gameObject,HubAmbience.Motion.Parallax,.65f,.1f,0,cameraObject.transform);
            hill=Visual("Near orchard ridge",Sprite("Hill"),new Vector2(-7,-1.5f),new Vector2(55,5),-20);
            hill.color=new Color(.8f,.9f,.72f,.55f);
            Motion(hill.gameObject,HubAmbience.Motion.Parallax,.32f,.1f,0,cameraObject.transform);
            for(int i=0;i<5;i++)
            {
                var cloud=Visual("Drifting cloud "+i,Sprite("Cloud"),new Vector2(-15+i*7,7.7f+(i%2)*.7f),new Vector2(4+i%3,.8f),-35);
                Motion(cloud.gameObject,HubAmbience.Motion.Cloud,1.8f,.35f,i*1.8f);
            }
            var ground=Visual("Safe meadow ground",Sprite("Ground"),new Vector2(0,-2),new Vector2(56,4),1);
            ground.gameObject.layer=6;
            ground.gameObject.AddComponent<BoxCollider2D>();
            Wall(-20); Wall(20);

            var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/Player/Player.prefab");
            if(source==null) throw new InvalidOperationException("Existing player prefab is required.");
            var player=(GameObject)PrefabUtility.InstantiatePrefab(source);
            player.name="Apple Hero (existing PlayerController)";
            player.GetComponent<SpriteRenderer>().sprite=config.heroSprite;
            player.GetComponent<SpriteRenderer>().color=Color.white;
            player.GetComponent<SpriteRenderer>().sortingOrder=9;
            // Normalize the supplied large sheet sprites to the existing capsule, preserving physics.
            player.GetComponent<SpriteRenderer>().drawMode=SpriteDrawMode.Simple;
            player.transform.position=new Vector3(0,.7f,0);
            var playerPrefab=PrefabUtility.SaveAsPrefabAsset(player,Root+"Prefabs/Hub/HubPlayer.prefab");
            UnityEngine.Object.DestroyImmediate(player);
            player=(GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            var controller=player.GetComponent<PlayerController>();
            Set(cameraObject.AddComponent<HubCameraController>(),"target",player.transform);

            var targets=new List<HubInteractable>();
            targets.Add(Station("Workshop",Prop("Workshop"),new Vector2(-8,2.45f),new Vector2(5.7f,4.95f),HubScreen.Workshop,"Открыть мастерскую",-7.5f));
            targets.Add(Station("Wardrobe",Prop("Wardrobe"),new Vector2(-3.8f,1.52f),new Vector2(2.4f,3.2f),HubScreen.Wardrobe,"Открыть гардероб",-3.8f));
            targets.Add(WorldMapBuilder.CreateMap(SceneManager.GetActiveScene()));
            Label("Workshop label","МАСТЕРСКАЯ",new Vector2(-8,5.3f),new Color(.98f,.94f,.73f),.045f);
            Label("Wardrobe label","ГАРДЕРОБ",new Vector2(-3.8f,3.65f),new Color(.98f,.94f,.73f),.035f);
            foreach(var target in targets)
            {
                var shadow=Visual("Soft station shadow",Sprite("Glow"),new Vector2(target.transform.position.x,.035f),new Vector2(3,.35f),2);
                shadow.color=new Color(.1f,.18f,.1f,.8f);
            }

            var random=new System.Random(73);
            for(int i=0;i<100;i++)
            {
                float x=(float)random.NextDouble()*43-21.5f;
                float scale=.35f+(float)random.NextDouble()*.5f;
                var grass=Visual("Meadow grass "+i,Sprite("Grass"),new Vector2(x,-.1f),new Vector2(scale,scale*.65f),12);
                grass.color=Color.Lerp(new Color(.72f,.85f,.56f),Color.white,(float)random.NextDouble());
                if(i%3==0) Motion(grass.gameObject,HubAmbience.Motion.Sway,5f,1.7f,i*.7f);
            }
            for(int i=0;i<16;i++)
            {
                var leaf=Visual("Falling leaf "+i,Sprite("Leaf"),new Vector2((float)random.NextDouble()*38-19,3+(float)random.NextDouble()*5),new Vector2(.13f,.065f),14);
                Motion(leaf.gameObject,HubAmbience.Motion.Leaf,.75f,.4f+(float)random.NextDouble()*.6f,i*.45f);
            }
            var uiObject=new GameObject("Hub UI Toolkit",typeof(UIDocument));
            var document=uiObject.GetComponent<UIDocument>();
            document.panelSettings=panel;
            document.visualTreeAsset=AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Root+"UI/Hub/Hub.uxml");
            var ui=uiObject.AddComponent<HubUIController>();
            var interaction=new GameObject("Hub interactions").AddComponent<HubInteractionController>();
            Set(interaction,"player",controller); Set(interaction,"ui",ui);
            var so=new SerializedObject(interaction);
            var array=so.FindProperty("interactables"); array.arraySize=targets.Count;
            for(int i=0;i<targets.Count;i++) array.GetArrayElementAtIndex(i).objectReferenceValue=targets[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            var bootstrap=new GameObject("Hub bootstrap").AddComponent<HubBootstrap>();
            Set(bootstrap,"configuration",config); Set(bootstrap,"ui",ui); Set(bootstrap,"player",controller);
            Set(bootstrap,"panelSettings",panel);
            Set(bootstrap,"returnTree",AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(Root+"UI/Hub/HubReturn.uxml"));
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),HubSceneFlowController.HubScenePath))
                throw new InvalidOperationException("Could not save Hub scene.");
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("TRAPPLE HUB: scene, configuration, UI panel, profile and reusable prefabs saved by Unity.");
        }

        private static void ImportHeroes()
        {
            string path=Art+"HeroAtlas.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            float w=source.width/2f,h=source.height/2f;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            // Each cell's rendered apple is ~80% of its height. Body stays inside the old capsule.
            importer.spritePixelsPerUnit=h*.78f;
            importer.SaveAndReimport();
#pragma warning disable 618
            importer.spritesheet=new[] {
                new SpriteMetaData { name="Classic",rect=new Rect(0,h,w,h),pivot=new Vector2(.5f,.5f),alignment=0 },
                new SpriteMetaData { name="Forest",rect=new Rect(w,h,w,h),pivot=new Vector2(.5f,.5f),alignment=0 },
                new SpriteMetaData { name="Factory",rect=new Rect(0,0,w,h),pivot=new Vector2(.5f,.5f),alignment=0 },
                new SpriteMetaData { name="Lava",rect=new Rect(w,0,w,h),pivot=new Vector2(.5f,.5f),alignment=0 }
            };
#pragma warning restore 618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static void ImportWorlds()
        {
            string path=PixelWorldSource;
            HubArtBuilder.Import(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            var source=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.SaveAndReimport();
#pragma warning disable 618
            importer.spritesheet=new[] {
                new SpriteMetaData { name="Forest",rect=new Rect(10,source.height-923,560,413),pivot=new Vector2(.5f,.5f),alignment=0 },
                new SpriteMetaData { name="Factory",rect=new Rect(578,source.height-923,517,413),pivot=new Vector2(.5f,.5f),alignment=0 },
                new SpriteMetaData { name="Lava",rect=new Rect(1102,source.height-923,561,413),pivot=new Vector2(.5f,.5f),alignment=0 }
            };
#pragma warning restore 618
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static HubConfiguration CreateConfiguration()
        {
            var config=AssetDatabase.LoadAssetAtPath<HubConfiguration>(ConfigPath);
            if(config==null) { config=ScriptableObject.CreateInstance<HubConfiguration>(); AssetDatabase.CreateAsset(config,ConfigPath); }
            config.heroSprite=playerAnimations.idle[0];
            config.upgrades=new[] {
                new UpgradeDefinition { kind=UpgradeKind.Speed,title="Скорость",description="Чуть быстрее к новым открытиям.",icon="»",percentPerLevel=2 },
                new UpgradeDefinition { kind=UpgradeKind.Agility,title="Ловкость",description="Плавнее разгон. Точнее остановка.",icon="≋",percentPerLevel=3 },
                new UpgradeDefinition { kind=UpgradeKind.Jump,title="Прыжок",description="Ещё немного ближе к облакам.",icon="↑",percentPerLevel=2 }
            };
            config.worlds=new[] {
                new WorldDefinition { id="meadow",title="Яблочная поляна",subtitle="ПЕРВЫЕ ШАГИ",description="Обучение: движение, прыжки и первые неожиданные ловушки",plannedLevels=1,initiallyUnlocked=true,
                    levelScenes=new[] {GameManager.TutorialScene},illustration=Sprite("MeadowBackdrop") },
                new WorldDefinition { id="forest",title="Обманчивый лес",subtitle="НЕ ВЕРЬ ТИШИНЕ",description="Лесные ловушки · подбор и бросок палок",boss="Древний страж",initiallyUnlocked=true,
                    levelScenes=new[]{Trapple.DeceptiveForest.ForestGoal.ScenePath},illustration=worldArt.First(sprite=>sprite.name=="Forest") },
                new WorldDefinition { id="factory",title="Заброшенная фабрика",subtitle="В РИТМЕ МЕХАНИЗМОВ",description="Пилы, конвейеры, прессы · бросок шестерёнок",boss="Механический гигант",illustration=worldArt.First(sprite=>sprite.name=="Factory") },
                new WorldDefinition { id="lava",title="Лавовая крепость",subtitle="НА КРАЮ ОГНЯ",description="Лава, опасные мосты · бросок магматических булыжников",boss="Огненный голем",illustration=worldArt.First(sprite=>sprite.name=="Lava") }
            };
            config.cosmetics=new[] {
                new CosmeticDefinition { id="classic",title="Первое яблоко",description="Яблочная поляна · классический образ",worldId="meadow",initiallyUnlocked=true,sprite=playerAnimations.idle[0] },
                new CosmeticDefinition { id="forest",title="Лесной странник",description="Обманчивый лес · листва и ветви",worldId="forest",sprite=Hero("Forest") },
                new CosmeticDefinition { id="factory",title="Маленький механик",description="Заброшенная фабрика · металл и искры",worldId="factory",sprite=Hero("Factory") },
                new CosmeticDefinition { id="lava",title="Сердце вулкана",description="Лавовая крепость · огненные трещины",worldId="lava",sprite=Hero("Lava") }
            };
            EditorUtility.SetDirty(config);
            return config;
        }

        private static PanelSettings CreatePanel()
        {
            string path=Root+"UI/Hub/HubPanelSettings.asset";
            var panel=AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if(panel==null) { panel=ScriptableObject.CreateInstance<PanelSettings>(); AssetDatabase.CreateAsset(panel,path); }
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution=new Vector2Int(1600,900);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match=.5f;
            panel.sortingOrder=20;
            // Unity's built-in runtime theme, including keyboard focus/navigation defaults.
            string themePath=Root+"UI/Hub/HubTheme.tss";
            if(!File.Exists(themePath)) File.WriteAllText(themePath,"@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(themePath,ImportAssetOptions.ForceSynchronousImport);
            panel.themeStyleSheet=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            EditorUtility.SetDirty(panel);
            return panel;
        }

        private static void RegisterScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if(!scenes.Any(scene=>scene.path==HubSceneFlowController.HubScenePath)) scenes.Insert(Math.Min(1,scenes.Count),new EditorBuildSettingsScene(HubSceneFlowController.HubScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();
            var profile=AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if(profile==null)
            {
                foreach(var platform in BuildProfile.GetInstalledPlatformModules())
                {
                    if(platform.displayName!="Windows") continue;
                    profile=BuildProfile.CreateBuildProfile(platform.platformGuid,"Trapple Hub Preview",null);
                    string error=AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(profile),ProfilePath);
                    if(!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    break;
                }
            }
            if(profile==null) throw new InvalidOperationException("Windows Build Support is required for HubPreview.");
            profile.overrideGlobalScenes=true; profile.scenes=scenes.ToArray(); EditorUtility.SetDirty(profile);
        }

        [MenuItem("Trapple/Hub/Build Windows Preview")]
        public static void BuildWindows()
        {
            var paths=EditorBuildSettings.scenes.Where(scene=>scene.enabled).Select(scene=>scene.path).ToArray();
            var report=BuildPipeline.BuildPlayer(paths,"Builds/Hub/Trapple.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Hub build failed: "+report.summary.result);
        }

        public static void BuildHubAndWindows()
        {
            BuildHub();
            BuildWindows();
        }

        private static HubInteractable Station(string name,Sprite sprite,Vector2 position,Vector2 size,HubScreen screen,string prompt,float interactionX,string worldId=null)
        {
            var obj=new GameObject(name);
            var art=Visual(name+" artwork",sprite,position,size,5);
            art.transform.SetParent(obj.transform,true);
            obj.transform.position=new Vector3(interactionX,.65f,0);
            // Keep art in its authored world position after moving the interaction origin.
            art.transform.position=position;
            var interaction=obj.AddComponent<HubInteractable>();
            var so=new SerializedObject(interaction);
            so.FindProperty("screen").enumValueIndex=(int)screen;
            so.FindProperty("prompt").stringValue=prompt;
            so.FindProperty("worldId").stringValue=worldId ?? "";
            so.FindProperty("radius").floatValue=1.85f;
            so.ApplyModifiedPropertiesWithoutUndo();
            string path=Root+"Prefabs/Hub/"+name.Replace(" ","")+".prefab";
            var prefab=PrefabUtility.SaveAsPrefabAsset(obj,path);
            UnityEngine.Object.DestroyImmediate(obj);
            return ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<HubInteractable>();
        }

        private static void Wall(float x)
        {
            var wall=new GameObject("Meadow boundary"); wall.layer=6;
            wall.transform.position=new Vector2(x,3);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(1,8);
        }
        private static void Label(string name,string text,Vector2 position,Color color,float size)
        {
            var obj=new GameObject(name,typeof(TextMesh)); obj.transform.position=position;
            var label=obj.GetComponent<TextMesh>(); label.text=text; label.fontSize=64; label.characterSize=size;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial=label.font.material;
            label.anchor=TextAnchor.MiddleCenter; label.color=color; label.fontStyle=FontStyle.Bold;
            label.GetComponent<MeshRenderer>().sortingOrder=16;
        }
        private static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static Sprite Prop(string name)=>props.First(sprite=>sprite.name==name);
        private static Sprite Hero(string name)=>heroes.First(sprite=>sprite.name==name);
        private static SpriteRenderer Visual(string name,Sprite sprite,Vector2 position,Vector2 size,int order)
        {
            var obj=new GameObject(name,typeof(SpriteRenderer)); obj.transform.position=position;
            var renderer=obj.GetComponent<SpriteRenderer>(); renderer.sprite=sprite; renderer.sortingOrder=order;
            obj.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
            return renderer;
        }
        private static void Motion(GameObject obj,HubAmbience.Motion motion,float amplitude,float speed,float phase,Transform camera=null)
        {
            var component=obj.AddComponent<HubAmbience>(); var so=new SerializedObject(component);
            so.FindProperty("motion").enumValueIndex=(int)motion; so.FindProperty("amplitude").floatValue=amplitude;
            so.FindProperty("speed").floatValue=speed; so.FindProperty("phase").floatValue=phase;
            so.FindProperty("cameraTransform").objectReferenceValue=camera; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(UnityEngine.Object obj,string field,UnityEngine.Object value)
        {
            var so=new SerializedObject(obj); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
