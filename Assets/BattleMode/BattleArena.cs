using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborBattle
{
    public enum PowerUp { None, RapidFire, Shotgun, Laser, FastBoat, Shield, Shockwave, FreezeShot, FireShot, HomingMissile, SlowShot, Shotgun360, Star, SmokeScreen, Underwater }
    public enum ItemCategory { Offensive, Defensive }
    public static class ItemCategories
    {
        public static ItemCategory For(PowerUp kind) => kind==PowerUp.FastBoat || kind==PowerUp.Shield || kind==PowerUp.Star || kind==PowerUp.SmokeScreen || kind==PowerUp.Underwater ? ItemCategory.Defensive : ItemCategory.Offensive;
    }

    [System.Serializable]
    public class PickupOption
    {
        public PowerUp kind;
        [Min(0)] public float weight = 1;
        [Min(.1f)] public float duration = 5;
        public Color color = Color.white;
        public Sprite icon;
        public ItemCategory Category => ItemCategories.For(kind);
        public PickupOption(PowerUp kind, float duration, Color color)
        { this.kind=kind; this.duration=duration; this.color=color; }
    }

    // Local input stays here; simulation receives commands instead of reading devices.
    public struct BoatCommand
    {
        public Vector2 Move, Aim;
        public bool Fire, Boost, Activate, ActivateDefense, Mine;
    }

    public class BattleArena : MonoBehaviour
    {
        public static BattleArena Instance { get; private set; }
        public readonly List<BattleBoat> Boats = new List<BattleBoat>();
        public bool Running { get; private set; }
        public const float HalfWidth = 32f, HalfHeight = 18f;
        [Header("Arena and effects")]
        [Header("Fixed fog boundary")]
        public Material fogMaterial;
        [Min(0)] public float fogDamagePerSecond=10;
        [Range(0,1)] public float fogBoatOpacity=.55f;
        [Header("Cargo traffic and dropped loot")]
        public BattleAiBoat cargoBoatPrefab;
        public BattleAiBoat offensiveCargoPrefab,defensiveCargoPrefab;
        [Range(0,1)] public float offensiveCargoChance=.5f;
        [Min(.1f)] public float cargoSpawnInterval=5;
        [Min(0)] public int maxCargoBoats=4;
        [Min(.1f)] public float lootLifetime=20;
        [Header("Sea mines (R1 / F): drops the Animal, tuned on its prefab")]
        public GameObject animalPrefab;
        [Min(0)] public int minesPerPlayer=3;
        [Min(0)] public float cameraPadding=5;
        [Min(.1f)] public float freezeSeconds=1;
        [Min(.1f)] public float slowSeconds=3;
        [Range(.1f,1)] public float slowMultiplier=.5f;
        [Min(.1f)] public float burnSeconds=3;
        [Min(0)] public float burnDamagePerSecond=6;
        [Min(1)] public float shockwaveRadius=6;
        [Header("Category items")]
        [Min(1)] public float laserRange=40;
        [Range(6,48)] public int shotgun360Pellets=16;
        [Min(.1f)] public float shotgun360Cooldown=.8f;
        [Min(0)] public int shotgun360Damage=8;
        [Min(1)] public float starSpeedMultiplier=1.2f;
        [Min(1)] public float fastBoatSpeedMultiplier=2;
        [Min(.1f)] public float smokeRadius=5;
        [Min(.1f)] public float bubbleInterval=1.2f;
        [Min(.05f)] public float bubbleVisibleSeconds=.3f;
        [Min(.1f)] public float pickupPopupSeconds=2;
        [Header("Project artwork")]
        public Sprite boatArt, cannonArt, islandArt, pickupArt, shotArt;
        public GameObject originalPlayerPrefab;
        [Header("Aim guide: line from the cannon with a crosshair at the end")]
        public Sprite crosshairArt;
        [Min(.5f)] public float aimGuideLength=2.5f;
        [Min(.01f)] public float aimGuideWidth=.08f;
        [Min(.1f)] public float crosshairSize=.8f;
        [Header("Swappable VFX mappings")]
        public BattleVfxProfile vfx;
        [Header("Random pickup pool (zero weight disables an entry)")]
        public List<PickupOption> pickupPool = new List<PickupOption>
        {
            new PickupOption(PowerUp.RapidFire,5,Color.green),
            new PickupOption(PowerUp.Shotgun,5,new Color(1,.45f,.25f)),
            new PickupOption(PowerUp.Laser,5,Color.yellow),
            new PickupOption(PowerUp.FastBoat,5,Color.magenta),
            new PickupOption(PowerUp.Shield,5,Color.blue),
            new PickupOption(PowerUp.Shockwave,5,Color.white),
            new PickupOption(PowerUp.FreezeShot,5,Color.cyan),
            new PickupOption(PowerUp.FireShot,5,Color.red),
            new PickupOption(PowerUp.HomingMissile,5,new Color(.8f,.7f,.4f)),
            new PickupOption(PowerUp.SlowShot,5,new Color(.6f,.3f,1)),
            new PickupOption(PowerUp.Shotgun360,5,new Color(1,.6f,.2f)),
            new PickupOption(PowerUp.Star,5,Color.yellow),
            new PickupOption(PowerUp.SmokeScreen,5,Color.gray),
            new PickupOption(PowerUp.Underwater,5,Color.cyan)
        };
        private Camera arenaCamera;
        private readonly List<Gamepad> pads = new List<Gamepad>();
        public readonly List<BattleAiBoat> CargoBoats=new List<BattleAiBoat>();
        private readonly Transform[] fogBands=new Transform[4];
        private float cargoTimer;
        private string result;
        private float countdown;
        private bool countingDown;
        private bool soloTest;
        private readonly Dictionary<Gamepad,InputActionAsset> lobbyInputs=new Dictionary<Gamepad,InputActionAsset>();
        private static Sprite square;
        private static Material artMaterial;

        private void Awake()
        {
            Instance = this;
            var cameraObject = Camera.main != null ? Camera.main.gameObject : new GameObject("Arena Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0,0,-10);
            var cam = cameraObject.GetComponent<Camera>();
            arenaCamera = cam;
            cam.orthographic = true;
            cam.backgroundColor = new Color(.025f,.15f,.22f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            for(int i=0;i<fogBands.Length;i++)
            {
                var band=Visual("Fog Border",Vector2.zero,Vector2.one,new Color(.8f,.85f,.9f,.8f));
                var renderer=band.GetComponentInChildren<SpriteRenderer>();
                if(fogMaterial!=null) renderer.sharedMaterial=fogMaterial;
                renderer.sortingOrder=-2;
                band.transform.SetParent(transform,true);
                fogBands[i]=band.transform;
            }
            if (GameObject.Find("Arena Islands") == null && GameObject.Find("Arena Island") == null)
            {
                Island(new Vector2(0,7.6f));
                Island(new Vector2(0,-7.6f));
            }
            // Players chosen on the start screen skip the in-arena lobby.
            if (BattleLobby.HasPlayers) SpawnLobbyPlayers();
            // Show the actual players immediately for keyboard testing, rather than
            // presenting an arena containing only treasure-chest pickups.
            else if (Gamepad.all.Count==0 && Keyboard.current!=null)
            {
                pads.Add(null); SpawnBoat(null,0);
                pads.Add(null); SpawnBoat(null,1);
            }
        }

        public bool IsInFog(Vector2 position) => FogExposure.Outside(position.x,position.y,HalfWidth,HalfHeight);

        public BattlePickup DropLoot(Vector2 position,ItemCategory category=ItemCategory.Offensive)
        {
            var option=RollPickup(category);
            if(option==null) return null;
            var item=Visual("Cargo Loot",position,Vector2.one,Color.white);
            ApplyArt(item,pickupArt,1,2);
            item.AddComponent<BoxCollider2D>().isTrigger=true;
            var drop=item.AddComponent<BattlePickup>();
            drop.Initialize(option,lootLifetime);
            item.GetComponentInChildren<SpriteRenderer>().color=category==ItemCategory.Offensive ? new Color(1,.65f,.3f) : new Color(.3f,.8f,1);
            return drop;
        }

        public BattleAiBoat SpawnCargoBoat()
        {
            CargoBoats.RemoveAll(boat=>boat==null || !boat.Alive);
            var category=Random.value<offensiveCargoChance ? ItemCategory.Offensive : ItemCategory.Defensive;
            var prefab=category==ItemCategory.Offensive ? offensiveCargoPrefab : defensiveCargoPrefab;
            if(prefab==null) prefab=cargoBoatPrefab;
            if(prefab==null || CargoBoats.Count>=maxCargoBoats) return null;
            Physics2D.SyncTransforms();
            float viewX=arenaCamera.orthographicSize*arenaCamera.aspect;
            var cameraPosition=arenaCamera.transform.position;
            // Both endpoints are beyond the camera AND the fixed safe area.
            float left=Mathf.Min(-HalfWidth-5,cameraPosition.x-viewX-5);
            float right=Mathf.Max(HalfWidth+5,cameraPosition.x+viewX+5);
            float bottom=Mathf.Min(-HalfHeight-5,cameraPosition.y-arenaCamera.orthographicSize-5);
            float top=Mathf.Max(HalfHeight+5,cameraPosition.y+arenaCamera.orthographicSize+5);
            for(int attempt=0;attempt<30;attempt++)
            {
                bool horizontal=Random.value>.5f;
                Vector2 a,b,c,d;
                if(horizontal)
                {
                    a=new Vector2(left,Random.Range(-HalfHeight*.7f,HalfHeight*.7f));
                    d=new Vector2(right,Random.Range(-HalfHeight*.7f,HalfHeight*.7f));
                    b=new Vector2(-HalfWidth*.4f,Random.Range(-HalfHeight*.9f,HalfHeight*.9f));
                    c=new Vector2(HalfWidth*.4f,Random.Range(-HalfHeight*.9f,HalfHeight*.9f));
                }
                else
                {
                    a=new Vector2(Random.Range(-HalfWidth*.75f,HalfWidth*.75f),bottom);
                    d=new Vector2(Random.Range(-HalfWidth*.75f,HalfWidth*.75f),top);
                    b=new Vector2(Random.Range(-HalfWidth*.9f,HalfWidth*.9f),-HalfHeight*.4f);
                    c=new Vector2(Random.Range(-HalfWidth*.9f,HalfWidth*.9f),HalfHeight*.4f);
                }
                if(Random.value>.5f) {var swap=a;a=d;d=swap;swap=b;b=c;c=swap;}
                bool clear=true;
                for(int i=0;i<=40 && clear;i++)
                    foreach(var hit in Physics2D.OverlapCircleAll(BattleAiBoat.Point(a,b,c,d,i/40f),1.4f*Mathf.Max(prefab.transform.localScale.x,prefab.transform.localScale.y)))
                        if(!hit.isTrigger && hit.GetComponentInParent<BattleBoat>()==null && hit.GetComponentInParent<BattleAiBoat>()==null)
                        { clear=false; break; }
                if(!clear) continue;
                var boat=Instantiate(prefab,a,Quaternion.identity);
                boat.category=category;
                boat.SetRoute(a,b,c,d);
                CargoBoats.Add(boat);
                return boat;
            }
            return null;
        }

        private void LateUpdate()
        {
            var bounds=new Bounds(Vector3.zero,Vector3.zero);
            bool found=false;
            bool anyAlive=false;
            foreach(var boat in Boats)
            {
                if(!boat.Alive) continue;
                anyAlive=true;
                if(boat.IsHidden) continue; // The camera must not track/reveal stealth positions.
                if(!found) { bounds=new Bounds(boat.transform.position,Vector3.zero); found=true; }
                else bounds.Encapsulate(boat.transform.position);
            }
            if(!anyAlive) bounds=new Bounds(Vector3.zero,new Vector3(HalfWidth*2,HalfHeight*2,0));
            if(found || !anyAlive)
            {
                float size=Mathf.Max(9,bounds.extents.y+cameraPadding,(bounds.extents.x+cameraPadding)/arenaCamera.aspect);
                // Extra margin leaves space for corner HUD panels.
                size*=1.15f;
                arenaCamera.orthographicSize=size>arenaCamera.orthographicSize ? size : Mathf.Lerp(arenaCamera.orthographicSize,size,Time.deltaTime*3);
                arenaCamera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);
            }
            bounds.center=arenaCamera.transform.position;
            float halfViewX=arenaCamera.orthographicSize*arenaCamera.aspect;
            float minX=Mathf.Min(-HalfWidth-20,bounds.center.x-halfViewX-20), maxX=Mathf.Max(HalfWidth+20,bounds.center.x+halfViewX+20);
            float minY=Mathf.Min(-HalfHeight-20,bounds.center.y-arenaCamera.orthographicSize-20), maxY=Mathf.Max(HalfHeight+20,bounds.center.y+arenaCamera.orthographicSize+20);
            FogBand(0,new Vector2((minX+maxX)/2,(HalfHeight+maxY)/2),new Vector2(maxX-minX,maxY-HalfHeight));
            FogBand(1,new Vector2((minX+maxX)/2,(-HalfHeight+minY)/2),new Vector2(maxX-minX,-HalfHeight-minY));
            FogBand(2,new Vector2((-HalfWidth+minX)/2,0),new Vector2(-HalfWidth-minX,HalfHeight*2));
            FogBand(3,new Vector2((HalfWidth+maxX)/2,0),new Vector2(maxX-HalfWidth,HalfHeight*2));
        }

        private void FogBand(int index,Vector2 position,Vector2 size)
        {
            fogBands[index].position=position;
            fogBands[index].GetChild(0).localScale=new Vector3(size.x,size.y,1);
        }

        public PickupOption RollPickup(ItemCategory? category=null)
        {
            float total=0;
            foreach(var option in pickupPool)
                if(option!=null && option.kind!=PowerUp.None && (!category.HasValue || option.Category==category)) total+=Mathf.Max(0,option.weight);
            if(total<=0) return null;
            float roll=Random.value*total;
            PickupOption fallback=null;
            foreach(var option in pickupPool)
            {
                if(option==null || option.kind==PowerUp.None || option.weight<=0 || (category.HasValue && option.Category!=category)) continue;
                fallback=option;
                roll-=option.weight;
                if(roll<0) return option;
            }
            return fallback;
        }

        public static void ApplyArt(GameObject obj, Sprite sprite, float height, int order)
        {
            if (sprite == null) return;
            var renderer=obj.GetComponentInChildren<SpriteRenderer>();
            renderer.sprite=sprite;
            renderer.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
            renderer.sortingOrder=order;
            if(artMaterial==null)
            {
                var shader=Shader.Find("Sprites/Default");
                if(shader!=null) artMaterial=new Material(shader);
            }
            if(artMaterial!=null) renderer.sharedMaterial=artMaterial;
        }

        private void Island(Vector2 position)
        {
            var obj=Visual("Island",position,Vector2.one*3.5f,Color.white);
            ApplyArt(obj,islandArt,3.5f,0);
            obj.AddComponent<CircleCollider2D>().radius=1.55f;
        }

        public static GameObject Visual(string name, Vector2 position, Vector2 size, Color color)
        {
            if (square == null)
                square = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            var obj = new GameObject(name);
            obj.transform.position = position;
            // Collider dimensions stay in world units; only the renderer child is scaled.
            var art = new GameObject("Visual",typeof(SpriteRenderer));
            art.transform.SetParent(obj.transform,false);
            art.transform.localScale = size;
            var renderer = art.GetComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            return obj;
        }

        private void Update()
        {
            foreach(var pad in Gamepad.all)
                if(!lobbyInputs.ContainsKey(pad))
                {
                    var template=originalPlayerPrefab!=null ? originalPlayerPrefab.GetComponent<PlayerInput>()?.actions : null;
                    lobbyInputs.Add(pad,BattleInput.ForDevice(template,pad));
                }
            if (!Running && !countingDown)
                foreach (var pad in Gamepad.all)
                    if (BattleInput.Pressed(lobbyInputs[pad],"Battle/Reset")) { Restart(); break; }
            var keyboard=Keyboard.current;
            if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) Restart();
            if(keyboard!=null && keyboard.enterKey.wasPressedThisFrame)
            {
                if(result!=null) Restart();
                else if(!Running && !countingDown)
                {
                    // Enter provides a complete two-player keyboard test without controllers.
                    if(pads.Count==0) { pads.Add(null); SpawnBoat(null,0); pads.Add(null); SpawnBoat(null,1); }
                    else if(pads.Count==1) { pads.Add(null); SpawnBoat(null,1); }
                    BeginMatch(false);
                }
            }
            if (!Running && result == null && !countingDown)
            {
                foreach (var pad in Gamepad.all)
                {
                    var actions=lobbyInputs[pad];
                    if(!pads.Contains(pad))
                    {
                        if(BattleInput.Pressed(actions,"Battle/Join") && pads.Count<4)
                        { pads.Add(pad); SpawnBoat(pad,pads.Count-1); }
                    }
                    else if(BattleInput.Pressed(actions,"Battle/StartMatch"))
                    { BeginMatch(pads.Count==1); break; }
                }
            }
            if (countingDown)
            {
                foreach (var pad in pads)
                    if (pad!=null && !pad.added) { countingDown=false; break; }
                if (countingDown && (countdown-=Time.deltaTime)<=0) { countingDown=false; Running=true; }
            }
            if (Running)
            {
                // A disconnected controller pauses the round rather than eliminating its player.
                foreach (var pad in pads) if (pad!=null && !pad.added) return;
                if((cargoTimer-=Time.deltaTime)<=0) {SpawnCargoBoat();cargoTimer=Mathf.Max(.1f,cargoSpawnInterval);}
                int alive=0; BattleBoat survivor=null;
                foreach (var boat in Boats)
                {
                    if (!boat.Alive) continue;
                    alive++; survivor=boat;
                    var pad=pads[boat.Index];
                    boat.Submit(pad==null ? KeyboardCommand(boat) : BattleInput.Read(boat.GetComponent<PlayerInput>()?.actions));
                }
                if ((!soloTest && alive<=1) || alive==0) { Running=false; result=alive==1 ? "Player "+(survivor.Index+1)+" wins!" : "Draw!"; }
            }
            if (result!=null)
                foreach (var pad in Gamepad.all)
                    if (BattleInput.Pressed(lobbyInputs[pad],"Battle/StartMatch")) { Restart(); break; }
        }

        private void SpawnLobbyPlayers()
        {
            foreach(var pad in BattleLobby.Players)
            {
                if(pad!=null && !pad.added) continue;
                pads.Add(pad); SpawnBoat(pad,pads.Count-1);
            }
            BeginMatch(pads.Count==1);
        }

        private void BeginMatch(bool forceSolo)
        {
            if(pads.Count==0 || (pads.Count<2 && !forceSolo)) return;
            soloTest=forceSolo;
            cargoTimer=Mathf.Max(.1f,cargoSpawnInterval);
            countdown=3;
            countingDown=true;
        }

        private void OnDestroy()
        {
            foreach(var actions in lobbyInputs.Values)
                if(actions!=null) { actions.Disable(); Destroy(actions); }
            if(Instance==this) Instance=null;
        }

        public bool CanSimulate
        {
            get { if (!Running) return false; foreach (var pad in pads) if (pad!=null && !pad.added) return false; return true; }
        }

        private BoatCommand KeyboardCommand(BattleBoat boat)
        {
            var k=Keyboard.current;
            if(k==null) return default;
            if(boat.Index==pads.IndexOf(null))
            {
                Vector2 aim=Vector2.zero;
                if(Mouse.current!=null)
                    aim=(Vector2)arenaCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())-(Vector2)boat.transform.position;
                var mapped=BattleInput.Read(boat.KeyboardActions);
                mapped.Aim=aim;
                mapped.Fire|=k.spaceKey.isPressed;
                return mapped;
            }
            var move=new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            return new BoatCommand { Move=move, Aim=move, Fire=k.rightCtrlKey.isPressed, ActivateDefense=k.rightShiftKey.wasPressedThisFrame, Activate=k.enterKey.wasPressedThisFrame, Mine=k.slashKey.wasPressedThisFrame };
        }

        private void SpawnBoat(Gamepad pad,int index)
        {
            var positions=new[] { new Vector2(-24,-12),new Vector2(24,12),new Vector2(-24,12),new Vector2(24,-12) };
            // Configure an inactive clone before any campaign Awake/Start can run.
            var staging=new GameObject("Battle Player Staging");
            staging.SetActive(false);
            GameObject obj;
            if(originalPlayerPrefab!=null) obj=Instantiate(originalPlayerPrefab,staging.transform,false);
            else
            {
                obj=Visual("Boat",positions[index],new Vector2(.8f,1.5f),Color.white);
                obj.transform.SetParent(staging.transform,false);
                ApplyArt(obj,boatArt,1.8f,3);
            }
            obj.name="Player "+(index+1)+" Boat";
            obj.transform.position=positions[index];
            var boat=obj.GetComponent<BattleBoat>() ?? obj.AddComponent<BattleBoat>();
            boat.Index=index;
            boat.Mines=minesPerPlayer;
            var campaignController=obj.GetComponent<PlayerController>();
            if(campaignController!=null) campaignController.enabled=false;
            var campaignHealth=obj.GetComponent<PlayerHealth>();
            if(campaignHealth!=null) campaignHealth.enabled=false;
            foreach(var canvas in obj.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
            var collider=obj.GetComponent<Collider2D>();
            if(collider==null) obj.AddComponent<BoxCollider2D>().size=new Vector2(.8f,1.5f);
            var body=obj.GetComponent<Rigidbody2D>() ?? obj.AddComponent<Rigidbody2D>();
            body.gravityScale=0; body.freezeRotation=true; body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            foreach(var child in obj.GetComponentsInChildren<Transform>(true))
            {
                if(child.name=="Turret") boat.Turret=child;
                var renderer=child.GetComponent<SpriteRenderer>();
                if(renderer!=null) renderer.sortingOrder=child.name=="Turret" ? 4 : 3;
            }
            if(boat.Turret==null)
            {
                var turret=Visual("Turret",positions[index],new Vector2(.22f,1),Color.white);
                ApplyArt(turret,cannonArt,.9f,4);
                turret.transform.SetParent(obj.transform,true);
                boat.Turret=turret.transform;
            }
            // Tint the boat with the player's health-bar color; the cannon gets a lighter shade.
            // BattleBoat records these as its base colors, so fog and Star effects keep them.
            var playerColor=BattleLobby.PlayerColor(index);
            foreach(var renderer in obj.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.color=renderer.transform.IsChildOf(boat.Turret) ? Color.Lerp(Color.white,playerColor,.5f) : playerColor;
            var input=obj.GetComponent<PlayerInput>();
            if(input!=null)
            {
                input.notificationBehavior=PlayerNotifications.InvokeCSharpEvents;
                input.neverAutoSwitchControlSchemes=true;
                // PlayerInput owns a separate action asset for every cloned boat.
                input.actions=Instantiate(input.actions);
                if(pad==null)
                {
                    input.enabled=false;
                    if(index==pads.IndexOf(null)) boat.KeyboardActions=BattleInput.ForKeyboard(input.actions);
                }
            }
            obj.transform.SetParent(null,true);
            Destroy(staging);
            if(input!=null && pad!=null)
            {
                input.SwitchCurrentControlScheme("Controller",pad);
                input.actions.FindActionMap("Player",true).Enable();
                input.actions.FindActionMap("Battle",true).Enable();
            }
            Boats.Add(boat);
        }
        private void Restart()
        {
            foreach(var effect in FindObjectsByType<BattleVfxLifetime>(FindObjectsSortMode.None)) Destroy(effect.gameObject);
            foreach(var smoke in FindObjectsByType<BattleSmoke>(FindObjectsSortMode.None)) {smoke.gameObject.SetActive(false);Destroy(smoke.gameObject);}
            foreach (var boat in Boats) Destroy(boat.gameObject);
            foreach (var shot in FindObjectsByType<BattleShot>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            foreach (var animal in FindObjectsByType<BattleAnimal>(FindObjectsSortMode.None)) Destroy(animal.gameObject);
            foreach(var cargo in CargoBoats) if(cargo!=null) {cargo.gameObject.SetActive(false);Destroy(cargo.gameObject);}
            CargoBoats.Clear();
            foreach(var drop in FindObjectsByType<BattlePickup>(FindObjectsSortMode.None)) {drop.gameObject.SetActive(false);Destroy(drop.gameObject);}
            Boats.Clear(); pads.Clear(); result=null; Running=false; countingDown=false; soloTest=false;
            // Start-screen matches rematch with the same players instead of reopening the lobby.
            if (BattleLobby.HasPlayers) SpawnLobbyPlayers();
        }

        private void OnGUI()
        {
            var style=new GUIStyle(GUI.skin.label) { fontSize=16, alignment=TextAnchor.MiddleCenter, wordWrap=true };
            string heading=result ?? (countingDown ? "Ready: "+Mathf.CeilToInt(countdown) : Running ? (CanSimulate ? (soloTest ? "SOLO TEST" : "LAST BOAT AFLOAT") : "Reconnect controller to resume") : pads.Count+" / 4 joined\nSTART: join; press again to start (one player = solo test)\nENTER: add keyboard opponent / start");
            float panelWidth=Mathf.Min(230,Screen.width*.27f);
            GUI.Label(new Rect(panelWidth+20,8,Screen.width-2*panelWidth-40,65),heading,style);
            if(!Running)
            {
                GUI.Box(new Rect(Screen.width*.2f,Screen.height*.43f,Screen.width*.6f,100),GUIContent.none);
                GUI.Label(new Rect(Screen.width*.22f,Screen.height*.44f,Screen.width*.56f,95),
                    "P1: WASD / mouse / click / E offense / Shift defense / F mine\nP2: arrows / Right Ctrl fire / Enter offense / Right Shift defense / slash mine\nController: sticks / RT fire / X offense / LT defense / RB mine\nEsc: lobby",style);
            }
            for(int i=0;i<Boats.Count;i++)
            {
                float x=i%2==0 ? 12 : Screen.width-panelWidth-12;
                float y=i<2 ? 12 : Screen.height-128;
                GUI.color=Color.white;
                GUI.Box(new Rect(x,y,panelWidth,116),GUIContent.none);
                var boat=Boats[i];
                GUI.Label(new Rect(x+10,y+5,panelWidth-20,24),"P"+(i+1)+"  "+(boat.Alive ? "Hull: "+boat.Health+" / 100  Mines: "+boat.Mines+(IsInFog(boat.transform.position) ? "  FOG!" : "") : "ELIMINATED"));
                GUI.color=new Color(.08f,.1f,.12f);
                GUI.DrawTexture(new Rect(x+10,y+32,panelWidth-20,14),Texture2D.whiteTexture);
                GUI.color=BattleLobby.PlayerColor(i);
                GUI.DrawTexture(new Rect(x+10,y+32,(panelWidth-20)*Mathf.Clamp01(boat.Health/100f),14),Texture2D.whiteTexture);
                GUI.Label(new Rect(x+10,y+52,panelWidth-20,60),boat.PowerLabel);
                if(boat.Alive && !boat.IsHidden)
                {
                    var position=arenaCamera.WorldToScreenPoint(boat.transform.position);
                    GUI.Label(new Rect(position.x-20,Screen.height-position.y-55,55,25),"P"+(i+1));
                }
            }
            GUI.color=Color.white;
        }
    }

    public class BattleBoat : MonoBehaviour
    {
        public InputActionAsset KeyboardActions;
        public int Index;
        public Transform Turret;
        public int Health { get; private set; }=100;
        public bool Alive => Health>0;
        private readonly TimedLoadout<PickupOption> offense=new TimedLoadout<PickupOption>();
        private readonly TimedLoadout<PickupOption> defense=new TimedLoadout<PickupOption>();
        public PowerUp OffensiveItem => (offense.Active ?? offense.Ready)?.kind ?? PowerUp.None;
        public PowerUp DefensiveItem => (defense.Active ?? defense.Ready)?.kind ?? PowerUp.None;
        public PowerUp ReadyItem => offense.Ready?.kind ?? defense.Ready?.kind ?? PowerUp.None;
        public bool IsSubmerged => defense.Active?.kind==PowerUp.Underwater;
        public bool IsHidden => IsSubmerged || BattleSmoke.Contains(transform.position);
        public bool CanBeTargeted => Alive && !IsHidden;
        private PowerUp weapon => offense.Active?.kind ?? PowerUp.None;
        private PowerUp protection => defense.Active?.kind ?? PowerUp.None;
        private bool Invincible => protection==PowerUp.Shield || protection==PowerUp.Star;
        private readonly FogExposure fog=new FogExposure();
        private readonly System.Collections.Generic.Dictionary<SpriteRenderer,Color> originalColors=new System.Collections.Generic.Dictionary<SpriteRenderer,Color>();
        private float cooldown,frozen,slowed,burning,burnTick,waveTimer,bubbleTimer,starHue;
        private Vector2 knockback,aim=Vector2.up;
        private BoatCommand command;
        private bool activateOffense,activateDefense;
        public int Mines { get; set; }
        private bool mineRequested;
        private GameObject shieldVisual,burnVisual,freezeVisual,slowVisual,boostVisual;
        private BattleSmoke smoke;
        private LineRenderer aimLine,aimMarker;
        private LineRenderer aimGuide;
        private SpriteRenderer crosshair;

        public string PowerLabel
        {
            get
            {
                bool controller=GetComponent<PlayerInput>()!=null && GetComponent<PlayerInput>().enabled;
                string attack=controller ? "X" : KeyboardActions!=null ? "E" : "Enter";
                string defend=controller ? "LT" : KeyboardActions!=null ? "Shift" : "R Shift";
                return SlotLabel("OFF "+attack,offense)+"\n"+SlotLabel("DEF "+defend,defense);
            }
        }
        private static string SlotLabel(string button,TimedLoadout<PickupOption> slot)
        {
            var item=slot.Active ?? slot.Ready;
            return button+": "+(item==null ? "Empty" : BattleItemPopup.Name(item.kind)+(slot.Active==null ? " [ready]" : " "+slot.Remaining.ToString("0.0")+"s"));
        }
        public void Submit(BoatCommand input) {command=input;activateOffense|=input.Activate;activateDefense|=input.ActivateDefense;mineRequested|=input.Mine;}
        public void Collect(PickupOption item)
        {
            if(item.Category==ItemCategory.Offensive) {offense.Collect(item);activateOffense=false;cooldown=0;}
            else {defense.Collect(item);EndDefense();activateDefense=false;}
            BattleItemPopup.Show(this,item);
        }
        private void ActivateItems()
        {
            if(activateOffense && offense.TryActivate(item=>item.duration)) {cooldown=0;waveTimer=0;}
            if(activateDefense && defense.TryActivate(item=>item.duration))
            {
                var arena=BattleArena.Instance;
                if(protection==PowerUp.Shield)
                {
                    shieldVisual=BattleVfx.Spawn(arena.vfx?.shield,transform.position,transform,true);
                    if(shieldVisual==null)
                    {
                        shieldVisual=BattleArena.Visual("Shield",transform.position,Vector2.one*2.2f,new Color(.2f,.7f,1,.25f));
                        shieldVisual.GetComponentInChildren<SpriteRenderer>().sprite=BattleItemIcons.Disc;
                        shieldVisual.transform.SetParent(transform,true);
                    }
                }
                if(protection==PowerUp.Star) {frozen=slowed=burning=burnTick=0;}
                if(protection==PowerUp.SmokeScreen) smoke=BattleSmoke.Deploy(transform.position,arena.smokeRadius,defense.Remaining);
                if(IsSubmerged) bubbleTimer=0;
            }
            activateOffense=activateDefense=false;
        }
        private void EndDefense()
        {
            if(shieldVisual!=null) Destroy(shieldVisual);
            if(boostVisual!=null) Destroy(boostVisual);
            if(smoke!=null) {smoke.gameObject.SetActive(false);Destroy(smoke.gameObject);}
        }
        public bool Hit(int amount,PowerUp effect,Vector2 push)
        {
            if(!Alive || Invincible || IsSubmerged || !BattleArena.Instance.CanSimulate) return false;
            Damage(amount);
            if(!Alive) return true;
            var arena=BattleArena.Instance;
            if(effect==PowerUp.FreezeShot) frozen=Mathf.Max(frozen,arena.freezeSeconds);
            if(effect==PowerUp.SlowShot) slowed=Mathf.Max(slowed,arena.slowSeconds);
            if(effect==PowerUp.FireShot) burning=Mathf.Max(burning,arena.burnSeconds);
            knockback+=push;
            return true;
        }
        public void Damage(int amount,bool fogDamage=false)
        {
            if(!Alive || Invincible || (IsSubmerged && !fogDamage) || !BattleArena.Instance.CanSimulate) return;
            Health=Mathf.Max(0,Health-Mathf.Max(0,amount));
            if(!Alive)
            {
                offense.Clear();defense.Clear();EndDefense();
                GetComponent<Collider2D>().enabled=false;
                foreach(var effect in new[] {burnVisual,freezeVisual,slowVisual}) if(effect!=null) Destroy(effect);
                BattleVfx.Spawn(BattleArena.Instance.vfx?.boatExplosion,transform.position);
            }
        }
        private void FixedUpdate()
        {
            var body=GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;
            var arena=BattleArena.Instance;
            if(!Alive || !arena.CanSimulate) return;
            float dt=Time.fixedDeltaTime;
            ActivateItems();offense.Tick(dt);
            if(mineRequested)
            {
                mineRequested=false;
                if(Mines>0) {Mines--;BattleAnimal.Drop(this);}
            }
            if(defense.Tick(dt)) EndDefense();
            int fogDamage=fog.Tick(arena.IsInFog(transform.position),dt,arena.fogDamagePerSecond);
            if(fogDamage>0) {Damage(fogDamage,true);if(!Alive)return;}
            frozen=Mathf.Max(0,frozen-dt);slowed=Mathf.Max(0,slowed-dt);
            if(burning>0)
            {
                burning-=dt;burnTick+=dt*arena.burnDamagePerSecond;
                int damage=Mathf.FloorToInt(burnTick);
                if(damage>0) {Damage(damage);burnTick-=damage;}
                if(!Alive)return;
            }
            else burnTick=0;
            Status(ref burnVisual,burning>0,arena.vfx?.burning);
            Status(ref freezeVisual,frozen>0,arena.vfx?.frozen);
            Status(ref slowVisual,slowed>0,arena.vfx?.slowedPoison);
            Status(ref boostVisual,protection==PowerUp.FastBoat && command.Move.sqrMagnitude>.01f,arena.vfx?.boost);
            float speed=5*(protection==PowerUp.FastBoat ? arena.fastBoatSpeedMultiplier : protection==PowerUp.Star ? arena.starSpeedMultiplier : 1);
            body.linearVelocity=(frozen>0 ? Vector2.zero : Vector2.ClampMagnitude(command.Move,1)*speed*(slowed>0 ? arena.slowMultiplier : 1))+knockback;
            knockback=Vector2.Lerp(knockback,Vector2.zero,dt*4);
            if(command.Move.sqrMagnitude>.04f) body.rotation=Mathf.Atan2(command.Move.y,command.Move.x)*Mathf.Rad2Deg-90;
            if(command.Aim.sqrMagnitude>.04f) aim=command.Aim.normalized;
            Turret.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg-90);
            if(protection==PowerUp.Star) starHue=Mathf.Repeat(starHue+dt*2,1);
            UpdateBoatCollisions();
            if(IsSubmerged && !BattleSmoke.Contains(transform.position) && (bubbleTimer-=dt)<=0)
            {
                Bubbles();bubbleTimer=Random.Range(arena.bubbleInterval*.75f,arena.bubbleInterval*1.25f);
            }
            cooldown-=dt;
            if(IsSubmerged) return; // All offensive firing, including shockwaves, is suppressed.
            if(weapon==PowerUp.Shockwave && (waveTimer-=dt)<=0) {Shockwave();waveTimer=1;}
            if(!command.Fire || cooldown>0)return;
            cooldown=weapon==PowerUp.RapidFire ? .09f : weapon==PowerUp.Shotgun ? .65f : weapon==PowerUp.Shotgun360 ? arena.shotgun360Cooldown : weapon==PowerUp.Laser ? .8f : weapon==PowerUp.HomingMissile ? 1 : .35f;
            if(weapon==PowerUp.Laser) {FireLaser();return;}
            int count=weapon==PowerUp.Shotgun ? 5 : weapon==PowerUp.Shotgun360 ? Mathf.Max(6,arena.shotgun360Pellets) : 1;
            for(int i=0;i<count;i++)
            {
                float angle=weapon==PowerUp.Shotgun360 ? i*360f/count : weapon==PowerUp.Shotgun ? (i-2)*10 : 0;
                Vector2 direction=Quaternion.Euler(0,0,angle)*aim;
                int damage=weapon==PowerUp.Shotgun360 ? arena.shotgun360Damage : weapon==PowerUp.Shotgun ? 8 : weapon==PowerUp.HomingMissile ? 20 : 10;
                BattleShot.Spawn(this,direction,damage,weapon);
            }
        }
        private void UpdateBoatCollisions()
        {
            var collider=GetComponent<Collider2D>();
            foreach(var boat in BattleArena.Instance.Boats)
                if(boat!=this && boat!=null && boat.Alive) Physics2D.IgnoreCollision(collider,boat.GetComponent<Collider2D>(),IsSubmerged || boat.IsSubmerged);
            foreach(var cargo in BattleArena.Instance.CargoBoats)
                if(cargo!=null && cargo.Alive) Physics2D.IgnoreCollision(collider,cargo.GetComponent<Collider2D>(),IsSubmerged);
        }
        private void LateUpdate()
        {
            foreach(var sprite in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(!originalColors.ContainsKey(sprite)) originalColors.Add(sprite,sprite.color);
                Color color=Alive ? (protection==PowerUp.Star ? Color.HSVToRGB(starHue,1,1) : originalColors[sprite]) : Color.gray;
                color.a=BattleArena.Instance.IsInFog(transform.position) ? BattleArena.Instance.fogBoatOpacity : 1;sprite.color=color;
            }
            BattleVisibility.Apply(transform,IsHidden);
            UpdateLaserGuide();
            UpdateAimGuide();
        }
        // A short line in the player's color out from the cannon, with the crosshair at its end.
        // Hidden with the boat so smoke and Underwater still conceal where it's aiming.
        private void UpdateAimGuide()
        {
            var arena=BattleArena.Instance;
            bool visible=Alive && !IsHidden;
            if(!visible){if(aimGuide!=null)aimGuide.enabled=false;if(crosshair!=null)crosshair.enabled=false;return;}
            var color=BattleLobby.PlayerColor(Index);
            if(aimGuide==null){aimGuide=Line("Aim guide",new Color(color.r,color.g,color.b,.55f),arena.aimGuideWidth);aimGuide.positionCount=2;}
            if(crosshair==null)
            {
                var obj=new GameObject("P"+(Index+1)+" Crosshair",typeof(SpriteRenderer));
                crosshair=obj.GetComponent<SpriteRenderer>();
                crosshair.sprite=arena.crosshairArt!=null ? arena.crosshairArt : Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,4,4),Vector2.one*.5f,4);
                crosshair.sharedMaterial=GetComponentInChildren<SpriteRenderer>(true).sharedMaterial;
                crosshair.color=color;crosshair.sortingOrder=9;
                float size=Mathf.Max(crosshair.sprite.bounds.size.x,crosshair.sprite.bounds.size.y);
                obj.transform.localScale=Vector3.one*(arena.crosshairSize/size);
            }
            Vector2 start=(Vector2)(Turret!=null ? Turret.position : transform.position)+aim*.9f;
            Vector2 end=start+aim*arena.aimGuideLength;
            // The Laser item draws its own longer guide, so only the crosshair stays up then.
            aimGuide.enabled=weapon!=PowerUp.Laser;
            aimGuide.SetPosition(0,new Vector3(start.x,start.y,-1));aimGuide.SetPosition(1,new Vector3(end.x,end.y,-1));
            crosshair.enabled=true;
            crosshair.transform.position=new Vector3(end.x,end.y,-1);
        }
        private void Bubbles()
        {
            var arena=BattleArena.Instance;
            var effect=BattleVfx.Spawn(arena.vfx?.bubbles,transform.position);
            if(effect!=null) {effect.GetComponent<BattleVfxLifetime>().Remaining=arena.bubbleVisibleSeconds;return;}
            var ring=Line("Dive bubbles",Color.cyan,.06f);ring.loop=true;ring.positionCount=16;
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;ring.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),-1)*.25f);}
            ring.transform.SetParent(null,true);ring.gameObject.AddComponent<BattleVfxLifetime>().Remaining=arena.bubbleVisibleSeconds;
        }
        private void Status(ref GameObject instance,bool active,VfxVisual visual)
        {
            if(active && instance==null)instance=BattleVfx.Spawn(visual,transform.position,transform,true);
            else if(!active && instance!=null){Destroy(instance);instance=null;}
        }
        private LineRenderer Line(string name,Color color,float width)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);
            var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=true;line.startWidth=line.endWidth=width;
            line.startColor=line.endColor=color;line.sharedMaterial=GetComponentInChildren<SpriteRenderer>(true).sharedMaterial;line.sortingOrder=8;
            return line;
        }
        private Vector2 TraceLaser(out RaycastHit2D result)
        {
            Vector2 start=(Vector2)transform.position+aim*1.6f;result=default;
            foreach(var hit in Physics2D.RaycastAll(start,aim,BattleArena.Instance.laserRange))
            {
                var boat=hit.collider.GetComponentInParent<BattleBoat>();
                if(hit.collider.isTrigger || boat==this || (boat!=null && boat.IsSubmerged))continue;
                var animal=hit.collider.GetComponentInParent<BattleAnimal>();
                if(animal!=null && animal.Owner==this)continue;
                result=hit;return hit.point;
            }
            return start+aim*BattleArena.Instance.laserRange;
        }
        private void UpdateLaserGuide()
        {
            bool visible=Alive && weapon==PowerUp.Laser && !IsHidden;
            if(!visible){if(aimLine!=null)aimLine.enabled=false;if(aimMarker!=null)aimMarker.enabled=false;return;}
            if(aimLine==null){aimLine=Line("Laser aiming ray",new Color(1,.1f,.1f,.6f),.055f);aimLine.positionCount=2;}
            if(aimMarker==null){aimMarker=Line("Laser hit marker",Color.yellow,.06f);aimMarker.loop=true;aimMarker.positionCount=20;}
            Vector2 start=(Vector2)transform.position+aim*1.6f;
            Vector2 end=BattleSmoke.ClipLine(start,TraceLaser(out _));
            aimLine.enabled=true;aimMarker.enabled=true;
            aimLine.SetPosition(0,new Vector3(start.x,start.y,-1));aimLine.SetPosition(1,new Vector3(end.x,end.y,-1));
            for(int i=0;i<20;i++){float a=i*Mathf.PI/10;aimMarker.SetPosition(i,new Vector3(end.x+Mathf.Cos(a)*.22f,end.y+Mathf.Sin(a)*.22f,-1));}
        }
        private void FireLaser()
        {
            Vector2 start=(Vector2)transform.position+aim*1.6f;
            Vector2 end=TraceLaser(out var hit);
            var visual=BattleArena.Instance.vfx?.Shot(PowerUp.Laser);
            BattleVfx.Spawn(visual?.muzzle,start,null,false,aim);
            if(hit.collider!=null)
            {
                hit.collider.GetComponentInParent<BattleBoat>()?.Hit(30,PowerUp.None,Vector2.zero);
                hit.collider.GetComponentInParent<BattleAiBoat>()?.Hit(30,PowerUp.None,Vector2.zero);
                hit.collider.GetComponentInParent<BattleAnimal>()?.Hit(30);
                BattleVfx.Spawn(visual?.impact,end,null,false,aim);
            }
            var beam=BattleVfx.Spawn(visual?.projectile,start,null,false,aim);
            if(beam!=null)beam.transform.localScale=new Vector3(1,1,Vector2.Distance(start,end));
            else {beam=BattleArena.Visual("Laser",(start+end)/2,new Vector2(.12f,Vector2.Distance(start,end)),Color.red);beam.transform.rotation=Turret.rotation;}
            Destroy(beam,.12f);
        }
        private void Shockwave()
        {
            float radius=BattleArena.Instance.shockwaveRadius;
            foreach(var target in BattleArena.Instance.Boats)
            {
                if(target==this || !target.Alive)continue;
                Vector2 offset=target.transform.position-transform.position;
                if(offset.magnitude<=radius)target.Hit(8,PowerUp.None,offset.normalized*12);
            }
            foreach(var cargo in BattleArena.Instance.CargoBoats.ToArray())
            {
                if(cargo==null || !cargo.Alive)continue;
                Vector2 offset=cargo.transform.position-transform.position;
                if(offset.magnitude<=radius)cargo.Hit(8,PowerUp.None,offset.normalized*2);
            }
            if(BattleVfx.Spawn(BattleArena.Instance.vfx?.shockwave,transform.position)!=null)return;
            var ring=Line("Shockwave",Color.cyan,.12f);ring.loop=true;ring.positionCount=48;
            for(int i=0;i<48;i++){float a=i*Mathf.PI/24;ring.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius);}
            Destroy(ring.gameObject,.2f);
        }
        private void OnDestroy()
        {
            if(KeyboardActions!=null){KeyboardActions.Disable();Destroy(KeyboardActions);}
            if(crosshair!=null) Destroy(crosshair.gameObject);
            var input=GetComponent<PlayerInput>();
            if(input!=null && input.actions!=null){input.actions.Disable();Destroy(input.actions);}
        }
    }
    public class BattleShot : MonoBehaviour
    {
        private Transform owner;
        private bool fromAi;
        private Vector2 direction;
        private int damage;
        private PowerUp effect;
        private float life=4;
        private Transform target;
        private ShotVfx visuals;
        private Transform flightVisual;
        public static void Spawn(BattleBoat source,Vector2 direction,int damage,PowerUp effect=PowerUp.None)
        {
            SpawnShot(source.transform,direction,damage,effect,false);
        }
        public static void SpawnFromAi(BattleAiBoat source,Vector2 direction,int damage)
        {
            SpawnShot(source.transform,direction,damage,PowerUp.None,true);
        }
        private static void SpawnShot(Transform source,Vector2 direction,int damage,PowerUp effect,bool fromAi)
        {
            var obj=BattleArena.Visual("Cannon shot",(Vector2)source.position+direction*1.6f,Vector2.one*.18f,Color.white);
            BattleArena.ApplyArt(obj,BattleArena.Instance.shotArt,.25f,5);
            var shot=obj.AddComponent<BattleShot>(); shot.owner=source; shot.fromAi=fromAi; shot.direction=direction; shot.damage=damage; shot.effect=effect;
            shot.visuals=BattleArena.Instance.vfx?.Shot(effect);
            var flight=BattleVfx.Spawn(shot.visuals?.projectile,obj.transform.position,obj.transform,true,direction);
            if(flight!=null) { shot.flightVisual=flight.transform; obj.transform.Find("Visual").gameObject.SetActive(false); }
            BattleVfx.Spawn(shot.visuals?.muzzle,obj.transform.position,null,false,direction);
            var renderer=obj.transform.Find("Visual").GetComponent<SpriteRenderer>();
            renderer.color=effect==PowerUp.FireShot ? Color.red : effect==PowerUp.FreezeShot ? Color.cyan : effect==PowerUp.SlowShot ? Color.magenta : Color.white;
            if(effect==PowerUp.HomingMissile) { renderer.transform.localScale*=2; shot.life=10; }
        }
        private void FixedUpdate()
        {
            if(!BattleArena.Instance.CanSimulate) return;
            if(effect==PowerUp.HomingMissile)
            {
                if(!TargetAlive(target))
                {
                    target=null;
                    float closest=float.PositiveInfinity;
                    foreach(var boat in BattleArena.Instance.Boats)
                        if(boat.transform!=owner && boat.CanBeTargeted && Vector2.Distance(transform.position,boat.transform.position)<closest)
                        { target=boat.transform; closest=Vector2.Distance(transform.position,boat.transform.position); }
                    foreach(var cargo in BattleArena.Instance.CargoBoats)
                        if(cargo!=null && cargo.Alive && !BattleSmoke.Contains(cargo.transform.position) && Vector2.Distance(transform.position,cargo.transform.position)<closest)
                        {target=cargo.transform;closest=Vector2.Distance(transform.position,cargo.transform.position);}
                }
                if(target!=null)
                {
                    Vector2 desired=(target.position-transform.position).normalized;
                    float angle=Vector2.SignedAngle(direction,desired);
                    direction=Quaternion.Euler(0,0,Mathf.Clamp(angle,-75*Time.fixedDeltaTime,75*Time.fixedDeltaTime))*direction;
                }
            }
            float distance=(effect==PowerUp.HomingMissile ? 7:22)*Time.fixedDeltaTime;
            if(flightVisual!=null) flightVisual.rotation=Quaternion.LookRotation(new Vector3(direction.x,direction.y,0),Vector3.back);
            var hits=Physics2D.CircleCastAll(transform.position,.09f,direction,distance);
            foreach(var hit in hits)
            {
                if(hit.collider.isTrigger || (owner!=null && hit.collider.transform.IsChildOf(owner))) continue;
                // Shots pass through the shooter's own Animal and can kill anyone else's.
                var animal=hit.collider.GetComponentInParent<BattleAnimal>();
                if(animal!=null && animal.Owner!=null && animal.Owner.transform==owner) continue;
                animal?.Hit(damage);
                var cargo=hit.collider.GetComponentInParent<BattleAiBoat>();
                if(fromAi && cargo!=null) continue;
                var player=hit.collider.GetComponentInParent<BattleBoat>();
                if(player!=null && player.IsSubmerged) continue;
                Vector2 push=effect==PowerUp.Shotgun360 ? direction*2.5f : Vector2.zero;
                player?.Hit(damage,effect,push);
                if(!fromAi) cargo?.Hit(damage,effect,push*.2f);
                BattleVfx.Spawn(visuals?.impact,hit.point,null,false,direction);
                Destroy(gameObject); return;
            }
            transform.position+=(Vector3)(direction*distance);
            if((life-=Time.fixedDeltaTime)<=0) Destroy(gameObject);
        }
        private static bool TargetAlive(Transform candidate)
        {
            if(candidate==null) return false;
            var player=candidate.GetComponent<BattleBoat>();
            if(player!=null) return player.CanBeTargeted;
            var cargo=candidate.GetComponent<BattleAiBoat>();
            return cargo!=null && cargo.Alive && !BattleSmoke.Contains(cargo.transform.position);
        }
    }

    // Loot has no owner: a physical collection decides who receives the item.
    public class BattlePickup : MonoBehaviour
    {
        private PickupOption option;
        private float remaining;
        private bool claimed;
        public void Initialize(PickupOption item,float lifetime)
        {
            option=item;
            remaining=Mathf.Max(.1f,lifetime);
        }
        private void Update()
        {
            if(!BattleArena.Instance.CanSimulate || claimed) return;
            if((remaining-=Time.deltaTime)<=0) {claimed=true;gameObject.SetActive(false);Destroy(gameObject);}
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            var boat=other.GetComponentInParent<BattleBoat>();
            if(claimed || option==null || boat==null || !boat.Alive || !BattleArena.Instance.CanSimulate) return;
            claimed=true;
            GetComponent<Collider2D>().enabled=false;
            boat.Collect(option);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
        private void OnGUI()
        {
            if(claimed || option==null || Camera.main==null) return;
            var point=Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(point.x-12,Screen.height-point.y-35,30,25),"?");
        }
    }
}
