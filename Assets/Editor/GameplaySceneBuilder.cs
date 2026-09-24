using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ForestJourney;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameplaySceneBuilder
{
    const string ScenePath = "Assets/Scenes/Gameplay.unity";
    static readonly Color Ink = new Color(.035f,.075f,.085f,.94f);
    static readonly Color Gold = new Color(1,.76f,.3f);
    static readonly Color White = new Color(.92f,.96f,.92f);
    static Font font;

    [MenuItem("Tools/Forest Journey/Create gameplay scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before building.");
        if (File.Exists(ScenePath) && Object.FindFirstObjectByType<GameManager>()) throw new InvalidOperationException("Gameplay already exists; edit it directly to preserve your adjustments.");
        if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene first.");
        var original = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorSceneManager.SaveScene(original, ScenePath, true);
        var scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (string path in new[]{"Assets/Materials/Gameplay","Assets/Prefabs/Gameplay","Assets/UI/Gameplay","Assets/Audio/Gameplay","Assets/Models/Gameplay"}) Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var terrain = Object.FindFirstObjectByType<Terrain>();
        if (!terrain) throw new InvalidOperationException("The source map must contain its terrain.");
        AddEnvironmentCollisions(terrain);
        Physics.SyncTransforms();
        var points = FindLocations(terrain);
        var root = new GameObject("Gameplay Systems");
        var coins = root.AddComponent<CoinManager>();
        var game = root.AddComponent<GameManager>();
        var pause = root.AddComponent<PauseMenu>(); pause.game = game;
        Material teal = Material("Sphere - Jade enamel", new Color(.035f,.47f,.48f), .55f, .65f);
        Material gold = Material("Coin - Warm gold", new Color(1,.61f,.08f), .8f, .7f);
        gold.EnableKeyword("_EMISSION"); gold.SetColor("_EmissionColor", new Color(.65f,.24f,.015f));
        Material cream = Material("Sphere - Brass bands", new Color(.98f,.82f,.38f), .65f, .55f);
        var playerObject = new GameObject("Player Sphere"); playerObject.layer = 2; playerObject.transform.position = points[0] + Vector3.up * 1.15f;
        playerObject.AddComponent<SphereCollider>().radius = 1;
        var body = playerObject.AddComponent<Rigidbody>(); body.mass = 2; body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.constraints = RigidbodyConstraints.FreezeRotation;
        var surface = new PhysicsMaterial("Sphere grip") {dynamicFriction = .1f, staticFriction = .1f, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Minimum};
        AssetDatabase.CreateAsset(surface, "Assets/Materials/Gameplay/SphereGrip.physicMaterial");
        playerObject.GetComponent<SphereCollider>().sharedMaterial = surface;
        var player = playerObject.AddComponent<PlayerController>();
        var visual = new GameObject("Rolling Visual"); visual.layer=2; visual.transform.SetParent(playerObject.transform,false);
        Primitive("Jade Ball", PrimitiveType.Sphere, visual.transform, Vector3.zero, Vector3.one*2, teal);
        var band = CreateTorus(); AssetDatabase.CreateAsset(band,"Assets/Models/Gameplay/BallBand.asset");
        for(int i=0;i<3;i++)
        {
            var ring = new GameObject("Brass Meridian " + (i+1),typeof(MeshFilter),typeof(MeshRenderer));
            ring.layer=2; ring.transform.SetParent(visual.transform,false); ring.transform.localRotation=Quaternion.Euler(i==0?0:90,i==2?90:0,0);
            ring.GetComponent<MeshFilter>().sharedMesh=band; ring.GetComponent<MeshRenderer>().sharedMaterial=cream;
        }
        var roll = visual.AddComponent<BallRollingVisual>(); roll.player=player.transform;
        var camera = Camera.main;
        if (!camera) { var c = new GameObject("Gameplay Camera",typeof(Camera),typeof(AudioListener)); c.tag="MainCamera"; camera=c.GetComponent<Camera>(); }
        camera.name="Gameplay Camera"; camera.fieldOfView=62; camera.nearClipPlane=.15f; camera.farClipPlane=700;
        var follow=camera.gameObject.AddComponent<CameraController>(); follow.player=player; player.cameraTransform=camera.transform;
        camera.transform.position=player.transform.position+new Vector3(0,6,-10); camera.transform.LookAt(player.transform.position+Vector3.up);
        PrefabUtility.SaveAsPrefabAsset(playerObject,"Assets/Prefabs/Gameplay/PlayerSphere.prefab");
        var coinPrefab=CreateCoin(gold,cream,teal);
        var coinRoot=new GameObject("Collectibles - Exploration Trail");
        coins.coins=new Coin[points.Count-1];
        for(int i=1;i<points.Count;i++)
        {
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
            instance.name="Coin " + i.ToString("00"); instance.transform.SetParent(coinRoot.transform); instance.transform.position=points[i]+Vector3.up*1.7f;
            var coin=instance.GetComponent<Coin>(); coin.manager=coins; coins.coins[i-1]=coin;
        }
        game.player=player; game.followCamera=follow; game.coins=coins;
        BuildUI(game,pause);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        PlayerSettings.productName="Sendero Dorado";
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Validate(); CapturePreview(camera,"Library/GameplayPreview.png");
    }

    static List<Vector3> FindLocations(Terrain terrain)
    {
        var data=terrain.terrainData; Vector3 origin=terrain.transform.position;
        const float step=4; int nx=Mathf.FloorToInt(data.size.x/step), nz=Mathf.FloorToInt(data.size.z/step);
        var safe=new Dictionary<Vector2Int,Vector3>();
        for(int x=2;x<nx-2;x++) for(int z=2;z<nz-2;z++)
        {
            float px=x*step, pz=z*step;
            if(data.GetSteepness(px/data.size.x,pz/data.size.z)>27) continue;
            var p=origin+new Vector3(px,data.GetInterpolatedHeight(px/data.size.x,pz/data.size.z),pz);
            if(Physics.CheckSphere(p+Vector3.up*1.2f,1.05f,~4,QueryTriggerInteraction.Ignore)) continue;
            safe[new Vector2Int(x,z)]=p;
        }
        if(safe.Count==0) throw new InvalidOperationException("No safe ground found.");
        var start=safe.OrderBy(p=>(p.Value-(origin+new Vector3(100,p.Value.y-origin.y,100))).sqrMagnitude).First().Key;
        var reached=new HashSet<Vector2Int>{start}; var queue=new Queue<Vector2Int>(); queue.Enqueue(start);
        var directions=new[]{Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        while(queue.Count>0)
        {
            var a=queue.Dequeue();
            foreach(var d in directions)
            {
                var b=a+d; if(reached.Contains(b)||!safe.ContainsKey(b)) continue;
                bool valid=true;
                for(int j=1;j<=6;j++)
                {
                    var p=Vector3.Lerp(safe[a],safe[b],j/6f);
                    float u=(p.x-origin.x)/data.size.x,v=(p.z-origin.z)/data.size.z;
                    p.y=origin.y+data.GetInterpolatedHeight(u,v);
                    if(data.GetSteepness(u,v)>31 || Physics.CheckSphere(p+Vector3.up*1.2f,1.05f,~4,QueryTriggerInteraction.Ignore)) {valid=false;break;}
                }
                if(valid) { reached.Add(b); queue.Enqueue(b); }
            }
        }
        if(reached.Count<15) throw new InvalidOperationException("Insufficient connected walkable terrain: " + reached.Count);
        var result=new List<Vector3>{safe[start]};
        var available=reached.Select(k=>safe[k]).Where(p=>Vector3.Distance(p,safe[start])<260).ToList();
        for(int i=0;i<10;i++)
        {
            float angle=i*2.39996f, radius= i==0 ? 15 : 35+i*20;
            var desired=safe[start]+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
            var candidates=available.Where(p=>result.All(r=>Vector3.Distance(r,p)>10)).OrderBy(p=>(p-desired).sqrMagnitude).ToList();
            if(candidates.Count==0) throw new InvalidOperationException("Not enough distinct coin locations.");
            result.Add(candidates[0]);
        }
        File.WriteAllText("Library/GameplayRoutes.txt","Connected terrain nodes: "+reached.Count+"\n"+string.Join("\n",result.Select((p,i)=>i+": "+p)));
        return result;
    }

    static void AddEnvironmentCollisions(Terrain terrain)
    {
        // The imported foliage prefabs contain no colliders. Keep their assets untouched.
        foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            if(filter.sharedMesh && !filter.GetComponent<Collider>()) filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        var group=new GameObject("Environment Collision - Terrain Instances");
        var data=terrain.terrainData;
        foreach(var instance in data.treeInstances)
        {
            var prefab=data.treePrototypes[instance.prototypeIndex].prefab;
            if(prefab.GetComponentsInChildren<Collider>().Length>0 || prefab.name.Contains("Mushroom")) continue;
            var proxy=new GameObject(prefab.name+" Collision");proxy.isStatic=true;proxy.transform.SetParent(group.transform);
            proxy.transform.position=terrain.transform.position+Vector3.Scale(instance.position,data.size);
            proxy.transform.rotation=Quaternion.Euler(0,instance.rotation*Mathf.Rad2Deg,0);
            proxy.transform.localScale=Vector3.Scale(new Vector3(instance.widthScale,instance.heightScale,instance.widthScale),prefab.transform.localScale);
            var renderers=prefab.GetComponentsInChildren<Renderer>();
            Bounds bounds=renderers.Length>0?renderers[0].bounds:new Bounds(Vector3.up*3,new Vector3(1,6,1));
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            if(prefab.name.Contains("Rock"))
            {
                var box=proxy.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
            }
            else
            {
                var capsule=proxy.AddComponent<CapsuleCollider>();capsule.height=Mathf.Max(.5f,bounds.size.y*.8f);
                capsule.radius=prefab.name.Contains("stump")?.65f:.38f;capsule.center=Vector3.up*(bounds.min.y+capsule.height*.5f);
            }
        }
    }

    static Material Material(string name,Color color,float metallic,float smooth)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
        mat.SetColor("_BaseColor",color); mat.SetFloat("_Metallic",metallic); mat.SetFloat("_Smoothness",smooth);
        AssetDatabase.CreateAsset(mat,"Assets/Materials/Gameplay/"+name+".mat"); return mat;
    }
    static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 position,Vector3 scale,Material mat)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.layer=parent.gameObject.layer;
        Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(parent,false);
        go.transform.localPosition=position; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat; return go;
    }
    static Mesh CreateTorus()
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<=64;i++) for(int j=0;j<=8;j++)
        {
            float a=i*Mathf.PI*2/64,b=j*Mathf.PI*2/8;
            vertices.Add(new Vector3((.985f+.025f*Mathf.Cos(b))*Mathf.Cos(a),.025f*Mathf.Sin(b),(.985f+.025f*Mathf.Cos(b))*Mathf.Sin(a)));
            if(i<64&&j<8){int v=i*9+j;triangles.AddRange(new[]{v,v+1,v+9,v+1,v+10,v+9});}
        }
        var mesh=new Mesh{name="Brass sphere meridian"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
    }
    static GameObject CreateCoin(Material gold,Material cream,Material teal)
    {
        var root=new GameObject("Golden Coin"); root.AddComponent<SphereCollider>().isTrigger=true; root.GetComponent<SphereCollider>().radius=1.65f;
        root.AddComponent<Coin>();
        var visual=new GameObject("Floating Coin Visual"); visual.transform.SetParent(root.transform,false);visual.AddComponent<CoinMotion>();
        var disc=Primitive("Gold rim",PrimitiveType.Cylinder,visual.transform,Vector3.zero,new Vector3(1.7f,.14f,1.7f),gold);disc.transform.localRotation=Quaternion.Euler(90,0,0);
        var inset=Primitive("Embossed face",PrimitiveType.Cylinder,visual.transform,Vector3.zero,new Vector3(1.32f,.155f,1.32f),teal);inset.transform.localRotation=Quaternion.Euler(90,0,0);
        var gem=Primitive("Brass diamond",PrimitiveType.Cube,visual.transform,Vector3.zero,new Vector3(.57f,.57f,.34f),cream);gem.transform.localRotation=Quaternion.Euler(0,0,45);
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Gameplay/GoldenCoin.prefab");Object.DestroyImmediate(root);return prefab;
    }

    static RectTransform Box(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
        rt.anchorMin=rt.anchorMax=anchor;rt.pivot=pivot;rt.anchoredPosition=position;rt.sizeDelta=size;return rt;
    }
    static RectTransform Fill(string name,Transform parent,Color? color=null)
    {
        var rt=Box(name,parent,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);rt.anchorMax=Vector2.one;rt.offsetMax=Vector2.zero;
        if(color.HasValue) rt.gameObject.AddComponent<Image>().color=color.Value;return rt;
    }
    static Text Label(string name,Transform parent,string text,int size,Vector2 pos,Vector2 dimensions,Color? color=null,TextAnchor align=TextAnchor.UpperLeft)
    {
        var rt=Box(name,parent,new Vector2(0,1),new Vector2(0,1),pos,dimensions);var label=rt.gameObject.AddComponent<Text>();
        label.font=font;label.fontSize=size;label.text=text;label.color=color??White;label.alignment=align;label.raycastTarget=false;return label;
    }
    static Button Button(string name,Transform parent,string label,Vector2 pos,Vector2 size,UnityAction action,bool primary=false)
    {
        var rt=Box(name,parent,new Vector2(0,1),new Vector2(0,1),pos,size);var image=rt.gameObject.AddComponent<Image>();image.color=primary?Gold:new Color(.13f,.23f,.24f,1);
        var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.highlightedColor=new Color(.8f,1,1);colors.selectedColor=new Color(.8f,1,1);button.colors=colors;
        Label("Label",rt,label,22,Vector2.zero,size,primary?Ink:White,TextAnchor.MiddleCenter);
        UnityEventTools.AddPersistentListener(button.onClick,action);return button;
    }
    static void BuildUI(GameManager game,PauseMenu pause)
    {
        var canvasObject=new GameObject("Gameplay Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var events=new GameObject("Gameplay Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var main=Fill("Main Menu",canvasObject.transform,new Color(.02f,.045f,.06f,.25f));game.mainMenu=main.gameObject;
        var panel=Box("Expedition Panel",main,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(72,0),new Vector2(535,730));panel.gameObject.AddComponent<Image>().color=Ink;
        Label("Eyebrow",panel,"EXPLORACIÓN  /  01",18,new Vector2(44,-44),new Vector2(440,32),Gold);
        Label("Title",panel,"SENDERO\nDORADO",64,new Vector2(40,-115),new Vector2(455,162));
        Label("Description",panel,"Un bosque. Diez monedas.\nEncuentra tu propio camino.",25,new Vector2(44,-310),new Vector2(440,80));
        game.playButton=Button("Play",panel,"Jugar",new Vector2(44,-430),new Vector2(447,64),game.StartGame,true).gameObject;
        Button("Quit",panel,"Salir",new Vector2(44,-512),new Vector2(447,58),game.QuitGame);
        Label("Controls",panel,"WASD / FLECHAS   ·   Moverte\nESC   ·   Pausar la expedición",18,new Vector2(44,-625),new Vector2(445,70),new Color(.64f,.77f,.76f));
        var hud=Fill("Exploration HUD",canvasObject.transform);game.hud=hud.gameObject;
        var counterPanel=Box("Coin Counter",hud,new Vector2(0,1),new Vector2(0,1),new Vector2(32,-30),new Vector2(290,100));counterPanel.gameObject.AddComponent<Image>().color=Ink;
        Label("Collection Label",counterPanel,"TU EXPEDICIÓN",14,new Vector2(22,-15),new Vector2(255,25),Gold);
        var counter=Label("Counter",counterPanel,"Monedas: 0 / 10",27,new Vector2(22,-41),new Vector2(255,40));
        var bar=Box("Progress",counterPanel,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(290,4)).gameObject.AddComponent<Image>();bar.color=Gold;bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;
        var guidance=Box("Direction Guide",hud,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-28),new Vector2(245,135));guidance.gameObject.AddComponent<Image>().color=Ink;
        var arrow=Box("Compass Arrow",guidance,new Vector2(.5f,1),new Vector2(.5f,.5f),new Vector2(0,-48),new Vector2(46,58));arrow.gameObject.AddComponent<CompassArrow>().color=Gold;
        Label("Guide Label",guidance,"RUMBO APROXIMADO",14,new Vector2(0,-97),new Vector2(245,25),White,TextAnchor.MiddleCenter);
        var indicator=hud.gameObject.AddComponent<CoinDirectionIndicator>();indicator.manager=game.coins;indicator.player=game.player.transform;indicator.cameraTransform=game.followCamera.transform;indicator.indicator=guidance.gameObject;indicator.arrow=arrow;
        var help=Box("Controls",hud,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,24),new Vector2(710,46));help.gameObject.AddComponent<Image>().color=Ink;
        Label("Hints",help,"WASD / FLECHAS  ·  Moverte      |      ESC  ·  Pausa",19,Vector2.zero,new Vector2(710,46),White,TextAnchor.MiddleCenter);
        var completed=Box("Expedition Complete",hud,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,160),new Vector2(700,150));completed.gameObject.AddComponent<Image>().color=Ink;
        Label("Success Title",completed,"¡Has recolectado todas las monedas!",30,new Vector2(20,-26),new Vector2(660,48),Gold,TextAnchor.MiddleCenter);
        Label("Success Hint",completed,"Objetivo completado. Sigue explorando o pulsa ESC.",20,new Vector2(20,-88),new Vector2(660,36),White,TextAnchor.MiddleCenter);
        var ui=hud.gameObject.AddComponent<CoinCounterUI>();ui.manager=game.coins;ui.counter=counter;ui.progress=bar;ui.completion=completed.gameObject;
        completed.gameObject.SetActive(false);
        var pauseRoot=Fill("Pause Overlay",canvasObject.transform,new Color(.015f,.025f,.03f,.72f));game.pauseMenu=pauseRoot.gameObject;
        var pausePanel=Box("Pause Panel",pauseRoot,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(520,600));pausePanel.gameObject.AddComponent<Image>().color=Ink;
        Label("Pause Eyebrow",pausePanel,"TOMA UN RESPIRO",17,new Vector2(44,-36),new Vector2(432,30),Gold);
        Label("Pause Title",pausePanel,"En pausa",48,new Vector2(40,-82),new Vector2(440,75));
        game.continueButton=Button("Continue",pausePanel,"Continuar",new Vector2(44,-192),new Vector2(432,64),pause.Continue,true).gameObject;
        Button("Restart",pausePanel,"Reiniciar partida",new Vector2(44,-274),new Vector2(432,60),pause.Restart);
        Button("Main Menu",pausePanel,"Volver al menú principal",new Vector2(44,-350),new Vector2(432,60),pause.MainMenu);
        Button("Quit Game",pausePanel,"Salir del juego",new Vector2(44,-426),new Vector2(432,60),game.QuitGame);
        Label("Resume Hint",pausePanel,"ESC también permite continuar",18,new Vector2(44,-530),new Vector2(432,30),White,TextAnchor.MiddleCenter);
        pauseRoot.gameObject.SetActive(false);hud.gameObject.SetActive(false);
    }

    public static void Validate()
    {
        var game=Object.FindFirstObjectByType<GameManager>();
        if(!game||!game.player||!game.followCamera||!game.coins||game.coins.Total!=10)throw new Exception("Missing gameplay references.");
        if(game.coins.coins.Any(c=>!c||c.manager!=game.coins||!c.GetComponent<SphereCollider>().isTrigger))throw new Exception("Invalid coin trigger/references.");
        if(!game.mainMenu||!game.pauseMenu||!game.hud||!game.playButton||!game.continueButton)throw new Exception("Missing UI references.");
        var missing=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if(missing!=0)throw new Exception("Missing scripts: "+missing);
        File.WriteAllText("Library/GameplayValidation.txt","PASS: player, camera, 10 trigger coins, UI references, no missing scripts. "+DateTime.Now.ToString("O"));
    }
    [MenuItem("Tools/Forest Journey/Apply presentation polish")]
    public static void Polish()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first.");
        var game=Object.FindFirstObjectByType<GameManager>();
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Models/Gameplay/BallBand.asset");
        var updated=CreateTorus();EditorUtility.CopySerialized(updated,existing);Object.DestroyImmediate(updated);EditorUtility.SetDirty(existing);
        const string audioPath="Assets/Audio/Gameplay/CoinChime.wav";
        const int rate=44100,count=17640;
        using(var writer=new BinaryWriter(File.Create(audioPath)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int i=0;i<count;i++){float t=(float)i/rate;float attack=Mathf.Min(1,t/.008f);float sample=(Mathf.Sin(t*880*2*Mathf.PI)+.4f*Mathf.Sin(t*1320*2*Mathf.PI))*Mathf.Exp(-t*12)*attack*.45f;writer.Write((short)(sample*32767));}
        }
        AssetDatabase.ImportAsset(audioPath);
        var spark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gameplay/CollectionSpark.mat");
        if(!spark){spark=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));spark.SetColor("_BaseColor",Gold);AssetDatabase.CreateAsset(spark,"Assets/Materials/Gameplay/CollectionSpark.mat");}
        var feedback=game.GetComponent<CollectionFeedback>();if(!feedback)feedback=game.gameObject.AddComponent<CollectionFeedback>();
        feedback.manager=game.coins;feedback.chime=AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);feedback.sparkMaterial=spark;
        game.GetComponent<AudioSource>().playOnAwake=false;
        var arrow=game.hud.GetComponent<CoinDirectionIndicator>().arrow;
        if(!arrow.GetComponent<CanvasRenderer>())arrow.gameObject.AddComponent<CanvasRenderer>();
        arrow.GetComponent<CompassArrow>().raycastTarget=false;
        Directory.CreateDirectory("Assets/Textures/Gameplay");
        var pattern=new Texture2D(512,256,TextureFormat.RGB24,false);
        for(int y=0;y<256;y++)for(int x=0;x<512;x++)
        {
            float u=(float)x/512,v=(float)y/256;
            bool stripe=Mathf.Abs(Mathf.Sin(u*Mathf.PI*6))<.12f || Mathf.Abs(v-.5f)<.026f;
            pattern.SetPixel(x,y,stripe?new Color(1,.8f,.27f):new Color(.035f,.47f,.48f));
        }
        pattern.Apply();const string patternPath="Assets/Textures/Gameplay/JadeBrassBall.png";File.WriteAllBytes(patternPath,pattern.EncodeToPNG());Object.DestroyImmediate(pattern);AssetDatabase.ImportAsset(patternPath);
        var ballMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Gameplay/Sphere - Jade enamel.mat");
        ballMat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(patternPath));ballMat.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(ballMat);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);AssetDatabase.SaveAssets();
    }
    public static void CapturePreview(Camera camera,string path)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
        File.WriteAllBytes(path,texture.EncodeToPNG());camera.targetTexture=previous;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);
    }
    [MenuItem("Tools/Forest Journey/Build Windows prototype")]
    public static void BuildPlayer()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before building the application.");
        Validate();
        var options=new BuildPlayerOptions {scenes=new[]{ScenePath},locationPathName="Builds/SenderoDorado/SenderoDorado.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None};
        var report=BuildPipeline.BuildPlayer(options);
        File.WriteAllText("Library/GameplayBuild.txt",report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nBytes: "+report.summary.totalSize);
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed.");
    }
}
