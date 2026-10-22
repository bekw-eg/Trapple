using System;
using System.IO;
using System.Linq;
using TMPro;
using Trapple.Hub;
using Trapple.Hub.Illustrated;
using Trapple.Player;
using Trapple.Workshop;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Trapple.Editor
{
    public static class WorkshopBuilder
    {
        public const string PrefabPath="Assets/_Trapple/Prefabs/Hub/WorkshopSystem.prefab";
        private const string Art="Assets/_Trapple/Art/Workshop/";
        private static TMP_FontAsset font;
        private static Sprite pip;
        private static Sprite[] doorFrames;
        private static readonly Color Paper=new Color(1,.88f,.65f);
        private static readonly Color Ink=new Color(.25f,.11f,.035f);

        [MenuItem("Tools/Trapple/Build 2D Workshop")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                ImportArt();
                doorFrames=WorkshopDoorAssets.Import();
                font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(IllustratedUIBuilder.Root+"Fonts/Trapple Cyrillic.asset");
                pip=AssetDatabase.LoadAssetAtPath<Sprite>(IllustratedUIBuilder.Root+"Generated/pip.png");
                if(font==null || pip==null) throw new InvalidOperationException("Build the existing illustrated UI first.");
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=new GameObject("Workshop 2D System");
                BuildPrefab(root);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                foreach(string path in new[]{HubSceneFlowController.HubScenePath,TrappleUIManager.ScenePath})
                {
                    scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                    Install(); EditorSceneManager.SaveScene(scene);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("TRAPPLE WORKSHOP BUILD COMPLETE: prefab and both playable hub scenes installed.");
            }
            finally { if(!Application.isBatchMode && setup.Length>0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
        private static void ImportArt()
        {
            foreach(string path in Directory.GetFiles(Art,"*.png"))
            {
                string normalized=path.Replace('\\','/');
                AssetDatabase.ImportAsset(normalized);
                var importer=(TextureImporter)AssetImporter.GetAtPath(normalized);
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.maxTextureSize=4096;
                importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            }
        }
        private static void BuildPrefab(GameObject root)
        {
            var transition=root.AddComponent<WorkshopTransitionController>();
            var path=new GameObject("Entrance path"); path.transform.SetParent(root.transform,false);
            Vector2[] positions={new Vector2(-8.65f,.6f),new Vector2(-8.9f,.72f),new Vector2(-9.12f,.87f),new Vector2(-9.29f,1.02f)};
            for(int i=0;i<positions.Length;i++)
            {
                var point=new GameObject("Step "+i); point.transform.SetParent(path.transform,false); point.transform.position=positions[i];
            }
            var focus=new GameObject("Door focus"); focus.transform.SetParent(root.transform,false); focus.transform.position=new Vector2(-9.29f,1.15f);
            var door=new GameObject("Door presentation").AddComponent<WorkshopDoorAnimator>(); door.transform.SetParent(root.transform,false);
            var opening=new GameObject("Animated workshop",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            opening.transform.SetParent(door.transform,false); opening.transform.position=new Vector2(-8,2.45f);
            opening.sprite=doorFrames[0]; opening.sortingOrder=5; opening.sharedMaterial=WorkshopDoorAssets.Material();
            opening.transform.localScale=new Vector3(5.7f/opening.sprite.bounds.size.x,4.95f/opening.sprite.bounds.size.y,1);
            var light=new GameObject("Warm door light",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>(); light.transform.SetParent(door.transform,false);
            light.transform.position=new Vector2(-9.29f,.52f); light.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Art/Hub/Glow.png");
            light.sortingOrder=8; light.transform.localScale=new Vector3(1.5f,.7f,1);
            door.Configure(opening,light,doorFrames); door.Set(0);
            var canvas=UI("Workshop Canvas",root.transform).gameObject.AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=200;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var view=canvas.gameObject.AddComponent<WorkshopUIController>();
            var surface=UI("Workshop Interior",canvas.transform); Stretch(surface);
            var input=surface.gameObject.AddComponent<CanvasGroup>();
            var backing=Picture("Warm background",surface,new Rect(0,0,1920,1080),null,new Color(.11f,.055f,.02f)); Stretch(backing.rectTransform);
            var interior=Picture("Interior artwork",surface,new Rect(0,0,1920,1080),Sprite("Interior")); interior.preserveAspect=true;
            var cards=UI("Upgrade panels",surface); Stretch(cards); var cardsGroup=cards.gameObject.AddComponent<CanvasGroup>();
            Picture("Wood and parchment frame",cards,new Rect(310,130,1300,900),Sprite("UpgradeFrame"));
            Picture("List parchment",cards,new Rect(377,288,566,558),null,Paper);
            Picture("Preview parchment",cards,new Rect(972,288,582,558),null,Paper);
            var buttons=new Button[3]; var rowLevels=new TMP_Text[3];
            var animations=AssetDatabase.LoadAssetAtPath<PlayerAnimationSet>(PixelPlayerAssets.SetPath);
            if(animations==null || !animations.IsValid) throw new InvalidOperationException("Existing APPi frames are required.");
            string[] titles={"Скорость","Ловкость","Прыжок"};
            string[] hints={"Беги быстрее к открытиям.","Точное движение и уклонение.","Ещё немного выше к облакам."};
            for(int i=0;i<3;i++)
            {
                int y=294+i*112;
                buttons[i]=Button("Select "+(UpgradeKind)i,cards,new Rect(386,y,550,106),null);
                var iconParent=buttons[i].transform;
                if(i==1)
                {
                    for(int j=2;j>0;j--) Picture("Dodge afterimage",iconParent,new Rect(14-j*7,14,72,72),animations.walkRight[3],new Color(1,1,1,.17f/j));
                    Picture("Running apple agility icon",iconParent,new Rect(14,14,72,72),animations.walkRight[3]);
                    for(int j=0;j<3;j++) Picture("Speed line",iconParent,new Rect(3,30+j*9,17+j*4,3),null,new Color(.23f,.48f,.14f,.6f));
                }
                else Picture("Upgrade icon",iconParent,new Rect(14,14,72,72),AssetDatabase.LoadAssetAtPath<Sprite>(IllustratedUIBuilder.Root+"Art/workshop/icons/"+(i==0?"speed":"jump")+"_icon.png"));
                Text("Upgrade title",iconParent,new Rect(100,8,290,40),titles[i],30,true);
                Text("Upgrade hint",iconParent,new Rect(100,49,420,28),hints[i],21);
                rowLevels[i]=Text("Row level",iconParent,new Rect(366,9,175,30),"",18,true);
            }
            for(int i=0;i<2;i++)
            {
                int y=636+i*105;
                Picture("Locked upgrade",cards,new Rect(386,y,550,97),null,new Color(.76f,.63f,.44f));
                Text("Locked title",cards,new Rect(407,y+12,300,36),i==0?"Здоровье":"Сбор семечек",29,true);
                Text("Locked hint",cards,new Rect(407,y+51,500,30),"Откроется позже",21);
            }
            var title=Text("Selected upgrade",cards,new Rect(997,302,495,44),"Скорость",34,true);
            var description=Text("Upgrade description",cards,new Rect(998,352,525,57),"",22);
            var previewRect=UI("Live preview",cards,new Rect(990,414,548,256));
            previewRect.gameObject.AddComponent<RectMask2D>();
            Picture("Preview sky",previewRect,new Rect(0,0,548,256),null,new Color(.94f,.82f,.56f));
            for(int i=0;i<4;i++)
            {
                var mark=Picture("Jump height marker "+i,previewRect,new Rect(8,208-i*40,532,2),null,new Color(.48f,.56f,.28f,.23f));
                mark.raycastTarget=false;
            }
            Picture("Meadow ground",previewRect,new Rect(0,236,548,20),AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Trapple/Art/Hub/Ground.png"));
            Picture("Meadow grass",previewRect,new Rect(0,233,548,8),null,new Color(.28f,.52f,.09f));
            var rock=Picture("Dodge obstacle",previewRect,new Rect(0,0,33,30),pip,new Color(.5f,.34f,.17f)); Bottom(rock.rectTransform);
            var dust=new Image[8]; for(int i=0;i<dust.Length;i++) { dust[i]=Picture("Dust "+i,previewRect,new Rect(0,0,10,8),pip); Bottom(dust[i].rectTransform); }
            var trails=new Image[4]; for(int i=0;i<trails.Length;i++) { trails[i]=Picture("Motion trail "+i,previewRect,new Rect(0,0,88,88),animations.walkRight[0]); Bottom(trails[i].rectTransform); trails[i].preserveAspect=true; }
            var actor=Picture("Animated APPi",previewRect,new Rect(0,0,88,88),animations.walkRight[0]); actor.preserveAspect=true; Bottom(actor.rectTransform);
            var preview=previewRect.gameObject.AddComponent<UpgradePreviewController>(); preview.Configure(previewRect,actor,rock,trails,dust,animations);
            var stats=Text("Current and next values",cards,new Rect(998,677,535,64),"",22,true);
            var cost=Text("Purchase cost",cards,new Rect(998,745,535,35),"",23,true);
            var purchase=Button("Purchase upgrade",cards,new Rect(1003,788,525,50),"УЛУЧШИТЬ ↑",new Color(.025f,.34f,.17f));
            var pips=new Image[3]; for(int i=0;i<3;i++) pips[i]=Picture("Level pip "+i,cards,new Rect(1440+i*30,311,23,23),pip);
            Picture("Hint parchment",cards,new Rect(450,878,750,78),null,Paper);
            var feedback=Text("Workshop feedback",cards,new Rect(466,895,710,52),"",22);
            feedback.alignment=TextAlignmentOptions.MidlineLeft;
            Picture("Exit parchment",cards,new Rect(1259,888,277,68),null,Paper);
            var exit=Button("Exit workshop",cards,new Rect(1259,888,277,68),"ВЫЙТИ",Paper);
            exit.GetComponentInChildren<TMP_Text>().color=Ink;
            var sign=Picture("Workshop hanging sign",surface,new Rect(570,-15,780,260),Sprite("WorkshopSign"));
            var signAnimator=surface.gameObject.AddComponent<WorkshopSignAnimator>(); signAnimator.Configure(sign.rectTransform,cardsGroup);
            var counter=Picture("Seed counter art",surface,new Rect(1450,34,390,130),Sprite("SeedsSign"));
            // Cover the baked example amount and label with the live saved balance.
            Picture("Counter parchment",counter.transform,new Rect(116,43,195,57),null,Paper);
            var balance=Text("Seed balance",counter.transform,new Rect(116,41,75,62),"0",35,true);
            Text("Seed currency",counter.transform,new Rect(194,48,123,49),"СЕМЕЧЕК",20,true);
            view.Configure(surface.gameObject,input,signAnimator,preview,buttons,purchase,exit,rowLevels,pips,title,description,stats,cost,balance,feedback);
            surface.gameObject.SetActive(false);
            var irisCanvas=UI("Iris Canvas",root.transform).gameObject.AddComponent<Canvas>();
            irisCanvas.renderMode=RenderMode.ScreenSpaceOverlay; irisCanvas.sortingOrder=1000;
            irisCanvas.gameObject.AddComponent<GraphicRaycaster>();
            var aperture=UI("Iris aperture",irisCanvas.transform); Stretch(aperture);
            aperture.gameObject.AddComponent<Image>().color=Color.black;
            var iris=aperture.gameObject.AddComponent<IrisTransitionController>();
            const string materialPath=Art+"WorkshopIris.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null) { material=new Material(Shader.Find("Trapple/UI/Workshop Iris")); AssetDatabase.CreateAsset(material,materialPath); }
            iris.Configure(material); iris.Clear();
            transition.Configure(null,null,null,null,path.GetComponentsInChildren<Transform>().Skip(1).ToArray(),focus.transform,door,iris,view);
        }
        private static void Install()
        {
            foreach(var old in UnityEngine.Object.FindObjectsByType<WorkshopTransitionController>(FindObjectsInactive.Include)) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var hub=UnityEngine.Object.FindAnyObjectByType<HubUIController>();
            var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            var camera=UnityEngine.Object.FindAnyObjectByType<HubCameraController>().GetComponent<Camera>();
            var transition=root.GetComponent<WorkshopTransitionController>();
            var station=UnityEngine.Object.FindObjectsByType<HubInteractable>().Single(value=>value.Screen==HubScreen.Workshop);
            var original=station.GetComponentInChildren<SpriteRenderer>(true);
            var animated=root.GetComponentInChildren<WorkshopDoorAnimator>().Presentation;
            var bounds=original.bounds;
            animated.transform.position=original.transform.position;
            animated.transform.localScale=new Vector3(bounds.size.x/animated.sprite.bounds.size.x,bounds.size.y/animated.sprite.bounds.size.y,1);
            animated.sortingLayerID=original.sortingLayerID; animated.sortingOrder=original.sortingOrder;
            original.enabled=false; EditorUtility.SetDirty(original);
            PrefabUtility.RecordPrefabInstancePropertyModifications(original);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animated.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animated);
            transition.Configure(player,hub,camera,camera.GetComponent<HubCameraController>(),root.transform.Find("Entrance path").Cast<Transform>().ToArray(),root.transform.Find("Door focus"),
                root.GetComponentInChildren<WorkshopDoorAnimator>(true),root.GetComponentInChildren<IrisTransitionController>(true),root.GetComponentInChildren<WorkshopUIController>(true));
            hub.SetWorkshop(transition); EditorUtility.SetDirty(hub);
            PrefabUtility.RecordPrefabInstancePropertyModifications(transition);
        }
        public static void VerifyRebuildAndBuildWindows()
        {
            Build(); Build();
            foreach(string path in new[]{HubSceneFlowController.HubScenePath,TrappleUIManager.ScenePath})
            {
                EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                if(UnityEngine.Object.FindObjectsByType<WorkshopTransitionController>().Length!=1 ||
                    UnityEngine.Object.FindObjectsByType<WorkshopUIController>().Length!=1 ||
                    UnityEngine.Object.FindObjectsByType<IrisTransitionController>().Length!=1 ||
                    UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length!=1)
                    throw new BuildFailedException("Workshop rebuild duplicated or lost an object in "+path);
                var hub=UnityEngine.Object.FindAnyObjectByType<HubUIController>();
                var serialized=new SerializedObject(hub.Workshop);
                foreach(string field in new[]{"player","hub","worldCamera","cameraFollow","doorFocus","door","iris","view"})
                    if(serialized.FindProperty(field).objectReferenceValue==null) throw new BuildFailedException("Missing workshop reference: "+field);
            }
            Debug.Log("WORKSHOP REBUILD VALIDATION PASS: one system, view, iris and EventSystem per hub; all scene references assigned.");
            BuildWindows();
        }
        public static void BuildWindows()
        {
            var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            var report=BuildPipeline.BuildPlayer(scenes,"Builds/Workshop/Trapple.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException("Workshop player build failed.");
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        private static RectTransform UI(string name,Transform parent,Rect rect=default)
        {
            var tr=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); tr.SetParent(parent,false);
            tr.anchorMin=tr.anchorMax=new Vector2(0,1); tr.pivot=new Vector2(0,1);
            tr.anchoredPosition=new Vector2(rect.x,-rect.y); tr.sizeDelta=rect.size; return tr;
        }
        private static void Stretch(RectTransform rect)
        { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.pivot=new Vector2(.5f,.5f); rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private static void Bottom(RectTransform rect) { rect.anchorMin=rect.anchorMax=Vector2.zero; rect.pivot=new Vector2(.5f,0); }
        private static Image Picture(string name,Transform parent,Rect rect,Sprite sprite,Color? color=null)
        {
            var image=UI(name,parent,rect).gameObject.AddComponent<Image>(); image.sprite=sprite; image.color=color??Color.white; image.raycastTarget=false; return image;
        }
        private static TMP_Text Text(string name,Transform parent,Rect rect,string value,float size,bool bold=false)
        {
            var text=UI(name,parent,rect).gameObject.AddComponent<TextMeshProUGUI>(); text.font=font; text.text=value; text.fontSize=size;
            text.color=Ink; text.fontStyle=bold?FontStyles.Bold:FontStyles.Normal; text.raycastTarget=false;
            text.textWrappingMode=TextWrappingModes.Normal; text.overflowMode=TextOverflowModes.Ellipsis; return text;
        }
        private static Button Button(string name,Transform parent,Rect rect,string label,Color? color=null)
        {
            var image=Picture(name,parent,rect,null,color??Paper); image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            if(label!=null)
            {
                var text=Text("Label",button.transform,new Rect(5,3,rect.width-10,rect.height-6),label,27,true);
                text.alignment=TextAlignmentOptions.Center; text.color=Color.white;
            }
            return button;
        }
    }
}
