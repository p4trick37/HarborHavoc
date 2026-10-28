using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborBattle
{
    public enum PowerUp { None, RapidFire, Shotgun, Laser, FastBoat, Shield, Shockwave, FreezeShot, FireShot, HomingMissile, SlowShot }

    [System.Serializable]
    public class PickupOption
    {
        public PowerUp kind;
        [Min(0)] public float weight = 1;
        [Min(.1f)] public float duration = 5;
        public Color color = Color.white;
        public Sprite icon;
        public PickupOption(PowerUp kind, float duration, Color color)
        { this.kind=kind; this.duration=duration; this.color=color; }
    }

    // Local input stays here; simulation receives commands instead of reading devices.
    public struct BoatCommand
    {
        public Vector2 Move, Aim;
        public bool Fire, Boost, Activate;
    }

    public class BattleArena : MonoBehaviour
    {
        public static BattleArena Instance { get; private set; }
        public readonly List<BattleBoat> Boats = new List<BattleBoat>();
        public bool Running { get; private set; }
        public const float HalfWidth = 32f, HalfHeight = 18f;
        [Header("Arena and effects")]
        [Min(1)] public int pickupCount=10;
        [Min(0)] public float cameraPadding=5;
        [Min(.1f)] public float freezeSeconds=1;
        [Min(.1f)] public float slowSeconds=3;
        [Range(.1f,1)] public float slowMultiplier=.5f;
        [Min(.1f)] public float burnSeconds=3;
        [Min(0)] public float burnDamagePerSecond=6;
        [Min(1)] public float shockwaveRadius=6;
        [Header("Project artwork")]
        public Sprite boatArt, cannonArt, islandArt, pickupArt, shotArt;
        public GameObject originalPlayerPrefab;
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
            new PickupOption(PowerUp.SlowShot,5,new Color(.6f,.3f,1))
        };
        [Min(0.1f)] public float pickupRespawnSeconds=10;
        private Camera arenaCamera;
        private readonly List<Gamepad> pads = new List<Gamepad>();
        private readonly List<BattlePickup> pickups = new List<BattlePickup>();
        private string result;
        private float countdown;
        private bool countingDown;
        private bool soloTest;
        private readonly Dictionary<Gamepad,InputActionAsset> lobbyInputs=new Dictionary<Gamepad,InputActionAsset>();
        private static Sprite square;
        private static Material artMaterial;
        private readonly Color[] colors = { Color.cyan, new Color(1,.45f,.25f), Color.yellow, Color.magenta };

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
            Wall(new Vector2(0,HalfHeight+.5f),new Vector2(HalfWidth*2+2,1));
            Wall(new Vector2(0,-HalfHeight-.5f),new Vector2(HalfWidth*2+2,1));
            Wall(new Vector2(HalfWidth+.5f,0),new Vector2(1,HalfHeight*2));
            Wall(new Vector2(-HalfWidth-.5f,0),new Vector2(1,HalfHeight*2));
            if (GameObject.Find("Arena Islands") == null && GameObject.Find("Arena Island") == null)
            {
                Island(new Vector2(0,7.6f));
                Island(new Vector2(0,-7.6f));
            }
            for (int i=0;i<pickupCount;i++)
            {
                var item = Visual("Mystery Pickup",Vector2.zero,Vector2.one*.85f,Color.white);
                ApplyArt(item,pickupArt,1f,2);
                item.AddComponent<BoxCollider2D>().isTrigger = true;
                var pickup = item.AddComponent<BattlePickup>();
                pickup.ResetPickup();
                pickups.Add(pickup);
            }
            // Show the actual players immediately for keyboard testing, rather than
            // presenting an arena containing only treasure-chest pickups.
            if (Gamepad.all.Count==0 && Keyboard.current!=null)
            {
                pads.Add(null); SpawnBoat(null,0);
                pads.Add(null); SpawnBoat(null,1);
            }
        }

        public Vector2 PickupPosition(BattlePickup self)
        {
            Physics2D.SyncTransforms();
            for(int attempt=0;attempt<100;attempt++)
            {
                var point=new Vector2(Random.Range(-HalfWidth+3,HalfWidth-3),Random.Range(-HalfHeight+3,HalfHeight-3));
                if(Physics2D.OverlapCircle(point,1.5f)!=null) continue;
                bool clear=true;
                foreach(var pickup in pickups)
                    if(pickup!=self && Vector2.Distance(point,pickup.transform.position)<4) { clear=false; break; }
                if(clear) return point;
            }
            return self.transform.position;
        }

        private void LateUpdate()
        {
            var bounds=new Bounds(Vector3.zero,Vector3.zero);
            bool found=false;
            foreach(var boat in Boats)
            {
                if(!boat.Alive) continue;
                if(!found) { bounds=new Bounds(boat.transform.position,Vector3.zero); found=true; }
                else bounds.Encapsulate(boat.transform.position);
            }
            if(!found) bounds=new Bounds(Vector3.zero,new Vector3(HalfWidth*2,HalfHeight*2,0));
            float size=Mathf.Max(9,bounds.extents.y+cameraPadding,(bounds.extents.x+cameraPadding)/arenaCamera.aspect);
            // Extra margin leaves space for corner HUD panels. Zoom out immediately
            // to avoid losing boats; ease back in when they converge.
            size*=1.15f;
            arenaCamera.orthographicSize=size>arenaCamera.orthographicSize ? size : Mathf.Lerp(arenaCamera.orthographicSize,size,Time.deltaTime*3);
            arenaCamera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-10);
        }

        public PickupOption RollPickup()
        {
            float total=0;
            foreach(var option in pickupPool)
                if(option!=null && option.kind!=PowerUp.None) total+=Mathf.Max(0,option.weight);
            if(total<=0) return null;
            float roll=Random.value*total;
            PickupOption fallback=null;
            foreach(var option in pickupPool)
            {
                if(option==null || option.kind==PowerUp.None || option.weight<=0) continue;
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

        private void Wall(Vector2 position, Vector2 size)
        {
            var obj = Visual("Breakwater",position,size,new Color(.35f,.4f,.32f));
            obj.AddComponent<BoxCollider2D>().size = size;
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

        private void BeginMatch(bool forceSolo)
        {
            if(pads.Count==0 || (pads.Count<2 && !forceSolo)) return;
            soloTest=forceSolo;
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
            return new BoatCommand { Move=move, Aim=move, Fire=k.rightCtrlKey.isPressed, Boost=k.rightShiftKey.isPressed, Activate=k.enterKey.wasPressedThisFrame };
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
            foreach (var boat in Boats) Destroy(boat.gameObject);
            foreach (var shot in FindObjectsByType<BattleShot>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            Boats.Clear(); pads.Clear(); result=null; Running=false; countingDown=false; soloTest=false;
            foreach (var pickup in pickups) pickup.ResetPickup();
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
                    "P1: WASD / mouse / click / Shift boost / E item\nP2: arrows / Right Ctrl fire / Right Shift boost / Enter item\nController: sticks / RT fire / LT boost / X item\nEsc: lobby",style);
            }
            for(int i=0;i<Boats.Count;i++)
            {
                float x=i%2==0 ? 12 : Screen.width-panelWidth-12;
                float y=i<2 ? 12 : Screen.height-128;
                GUI.color=Color.white;
                GUI.Box(new Rect(x,y,panelWidth,116),GUIContent.none);
                var boat=Boats[i];
                GUI.Label(new Rect(x+10,y+5,panelWidth-20,24),"P"+(i+1)+"  "+(boat.Alive ? "Hull: "+boat.Health+" / 100" : "ELIMINATED"));
                GUI.color=new Color(.08f,.1f,.12f);
                GUI.DrawTexture(new Rect(x+10,y+32,panelWidth-20,14),Texture2D.whiteTexture);
                GUI.color=colors[i];
                GUI.DrawTexture(new Rect(x+10,y+32,(panelWidth-20)*Mathf.Clamp01(boat.Health/100f),14),Texture2D.whiteTexture);
                GUI.Label(new Rect(x+10,y+52,panelWidth-20,60),boat.PowerLabel);
                if(boat.Alive)
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
        private void OnDestroy()
        {
            if(KeyboardActions!=null) { KeyboardActions.Disable(); Destroy(KeyboardActions); }
            var input=GetComponent<PlayerInput>();
            if(input!=null && input.actions!=null) { input.actions.Disable(); Destroy(input.actions); }
        }
        public int Index;
        public Transform Turret;
        public int Health { get; private set; } = 100;
        public bool Alive => Health>0;
        private readonly TimedLoadout<PickupOption> loadout=new TimedLoadout<PickupOption>();
        private PowerUp power => loadout.Active==null ? PowerUp.None : loadout.Active.kind;
        private float remaining => loadout.Remaining;
        private PickupOption primary => loadout.Ready;
        private PickupOption reserve => loadout.Reserve;
        private float cooldown, frozen, slowed, burning, burnTick, waveTimer;
        private Vector2 knockback;
        private GameObject shieldVisual;
        private GameObject burnVisual, freezeVisual, slowVisual, boostVisual;
        private BoatCommand command;
        private Vector2 aim=Vector2.up;
        private bool activateRequested;
        public string PowerLabel => (power!=PowerUp.None ? "ACTIVE: "+power+" "+remaining.ToString("0.0")+"s" : "READY: "+(primary==null ? "Empty" : primary.kind.ToString()))
            +"\nRESERVE: "+(reserve==null ? "Empty" : reserve.kind.ToString())+"\nActivate: "+(Index==0 ? "E / X" : "Enter / X");
        public void Submit(BoatCommand input) { command=input; activateRequested|=input.Activate; }
        public void Collect(PickupOption option)
        {
            loadout.Collect(option);
        }
        private void Activate()
        {
            if(!loadout.TryActivate(option=>option.duration)) return;
            waveTimer=0; cooldown=0;
            if(power==PowerUp.Shield)
            {
                shieldVisual=BattleVfx.Spawn(BattleArena.Instance.vfx?.shield,transform.position,transform,true);
                if(shieldVisual==null) shieldVisual=BattleArena.Visual("Shield",transform.position,new Vector2(2.2f,2.2f),new Color(.2f,.7f,1,.25f));
                shieldVisual.transform.SetParent(transform,true);
                var sprite=shieldVisual.GetComponentInChildren<SpriteRenderer>();
                if(sprite!=null) sprite.sortingOrder=6;
            }
        }
        public bool Hit(int amount, PowerUp effect, Vector2 push)
        {
            if(!Alive || power==PowerUp.Shield || !BattleArena.Instance.CanSimulate) return false;
            Damage(amount);
            if(!Alive) return true;
            var settings=BattleArena.Instance;
            if(effect==PowerUp.FreezeShot) frozen=Mathf.Max(frozen,settings.freezeSeconds);
            if(effect==PowerUp.SlowShot) slowed=Mathf.Max(slowed,settings.slowSeconds);
            if(effect==PowerUp.FireShot) burning=Mathf.Max(burning,settings.burnSeconds);
            knockback+=push;
            return true;
        }
        public void Damage(int amount)
        {
            if (!Alive || power==PowerUp.Shield || !BattleArena.Instance.CanSimulate) return;
            Health=Mathf.Max(0,Health-amount);
            if (!Alive)
            {
                GetComponent<Collider2D>().enabled=false; GetComponentInChildren<SpriteRenderer>().color=Color.gray;
                foreach(var effect in new[] {shieldVisual,burnVisual,freezeVisual,slowVisual,boostVisual}) if(effect!=null) Destroy(effect);
                BattleVfx.Spawn(BattleArena.Instance.vfx?.boatExplosion,transform.position);
            }
        }
        private void FixedUpdate()
        {
            var body=GetComponent<Rigidbody2D>();
            body.linearVelocity=Vector2.zero;
            if (!Alive || !BattleArena.Instance.CanSimulate) return;
            float dt=Time.fixedDeltaTime;
            if(activateRequested) { Activate(); activateRequested=false; }
            if(loadout.Tick(dt))
            {
                if(shieldVisual!=null) Destroy(shieldVisual);
            }
            frozen=Mathf.Max(0,frozen-dt); slowed=Mathf.Max(0,slowed-dt);
            if(burning>0)
            {
                burning-=dt; burnTick+=dt*BattleArena.Instance.burnDamagePerSecond;
                int damage=Mathf.FloorToInt(burnTick);
                if(damage>0) { Damage(damage); burnTick-=damage; }
                if(!Alive) return;
            }
            else burnTick=0;
            bool boost=power==PowerUp.FastBoat && command.Boost && command.Move.sqrMagnitude>.01f;
            var profile=BattleArena.Instance.vfx;
            StatusVfx(ref burnVisual,burning>0,profile?.burning);
            StatusVfx(ref freezeVisual,frozen>0,profile?.frozen);
            StatusVfx(ref slowVisual,slowed>0,profile?.slowedPoison);
            StatusVfx(ref boostVisual,boost,profile?.boost);
            body.linearVelocity=(frozen>0 ? Vector2.zero : Vector2.ClampMagnitude(command.Move,1)*(boost ? 10 : 5)*(slowed>0 ? BattleArena.Instance.slowMultiplier : 1))+knockback;
            knockback=Vector2.Lerp(knockback,Vector2.zero,dt*4);
            if(command.Move.sqrMagnitude>.04f) body.rotation=Mathf.Atan2(command.Move.y,command.Move.x)*Mathf.Rad2Deg-90;
            if(command.Aim.sqrMagnitude>.04f) aim=command.Aim.normalized;
            Turret.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg-90);
            if(power==PowerUp.Shockwave && (waveTimer-=dt)<=0) { Shockwave(); waveTimer=1; }
            cooldown-=Time.fixedDeltaTime;
            if(command.Fire && cooldown<=0)
            {
                var weapon=power;
                cooldown=weapon==PowerUp.RapidFire ? .09f : weapon==PowerUp.Shotgun ? .65f : weapon==PowerUp.Laser ? .8f : .35f;
                if(weapon==PowerUp.Laser) FireLaser();
                else for(int i=0;i<(weapon==PowerUp.Shotgun ? 5:1);i++)
                {
                    Vector2 direction=Quaternion.Euler(0,0,weapon==PowerUp.Shotgun ? (i-2)*10:0)*aim;
                    BattleShot.Spawn(this,direction,weapon==PowerUp.Shotgun ? 8:weapon==PowerUp.HomingMissile ? 20:10,weapon);
                }
                if(weapon==PowerUp.HomingMissile) cooldown=1;
            }
        }
        private void Shockwave()
        {
            float radius=BattleArena.Instance.shockwaveRadius;
            foreach(var target in BattleArena.Instance.Boats)
            {
                if(target==this || !target.Alive) continue;
                Vector2 offset=target.transform.position-transform.position;
                if(offset.magnitude<=radius) target.Hit(8,PowerUp.None,offset.normalized*12);
            }
            if(BattleVfx.Spawn(BattleArena.Instance.vfx?.shockwave,transform.position)!=null) return;
            var ring=new GameObject("Shockwave pulse").AddComponent<LineRenderer>();
            ring.useWorldSpace=true; ring.loop=true; ring.positionCount=48;
            ring.startWidth=ring.endWidth=.12f;
            ring.sharedMaterial=GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            ring.startColor=ring.endColor=Color.cyan;
            for(int i=0;i<48;i++) { float a=i*Mathf.PI*2/48; ring.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*radius); }
            ring.sortingOrder=7; Destroy(ring.gameObject,.2f);
        }
        private void StatusVfx(ref GameObject instance,bool active,VfxVisual visual)
        {
            if(active && instance==null) instance=BattleVfx.Spawn(visual,transform.position,transform,true);
            else if(!active && instance!=null) { Destroy(instance); instance=null; }
        }
        private void FireLaser()
        {
            Vector2 start=(Vector2)transform.position+aim*1.1f;
            var visuals=BattleArena.Instance.vfx?.Shot(PowerUp.Laser);
            BattleVfx.Spawn(visuals?.muzzle,start,null,false,aim);
            Vector2 end=start+aim*40;
            foreach(var hit in Physics2D.RaycastAll(start,aim,40))
            {
                if(hit.collider.isTrigger || hit.collider.GetComponent<BattleBoat>()==this) continue;
                end=hit.point;
                hit.collider.GetComponent<BattleBoat>()?.Damage(30);
                BattleVfx.Spawn(visuals?.impact,end,null,false,aim);
                break;
            }
            var beam=BattleVfx.Spawn(visuals?.projectile,start,null,false,aim);
            if(beam!=null) beam.transform.localScale=new Vector3(1,1,Vector2.Distance(start,end));
            else
            {
                beam=BattleArena.Visual("Laser",(start+end)/2,new Vector2(.12f,Vector2.Distance(start,end)),Color.red);
                beam.transform.rotation=Turret.rotation;
            }
            Destroy(beam,.12f);
        }
    }

    public class BattleShot : MonoBehaviour
    {
        private BattleBoat owner;
        private Vector2 direction;
        private int damage;
        private PowerUp effect;
        private float life=4;
        private BattleBoat target;
        private ShotVfx visuals;
        private Transform flightVisual;
        public static void Spawn(BattleBoat source,Vector2 direction,int damage,PowerUp effect=PowerUp.None)
        {
            var obj=BattleArena.Visual("Cannon shot",(Vector2)source.transform.position+direction*1.1f,Vector2.one*.18f,Color.white);
            BattleArena.ApplyArt(obj,BattleArena.Instance.shotArt,.25f,5);
            var shot=obj.AddComponent<BattleShot>(); shot.owner=source; shot.direction=direction; shot.damage=damage; shot.effect=effect;
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
                if(target==null || !target.Alive)
                {
                    float closest=float.PositiveInfinity;
                    foreach(var boat in BattleArena.Instance.Boats)
                        if(boat!=owner && boat.Alive && Vector2.Distance(transform.position,boat.transform.position)<closest)
                        { target=boat; closest=Vector2.Distance(transform.position,boat.transform.position); }
                }
                if(target!=null)
                {
                    Vector2 desired=(target.transform.position-transform.position).normalized;
                    float angle=Vector2.SignedAngle(direction,desired);
                    direction=Quaternion.Euler(0,0,Mathf.Clamp(angle,-75*Time.fixedDeltaTime,75*Time.fixedDeltaTime))*direction;
                }
            }
            float distance=(effect==PowerUp.HomingMissile ? 7:22)*Time.fixedDeltaTime;
            if(flightVisual!=null) flightVisual.rotation=Quaternion.LookRotation(new Vector3(direction.x,direction.y,0),Vector3.back);
            var hits=Physics2D.CircleCastAll(transform.position,.09f,direction,distance);
            foreach(var hit in hits)
            {
                if(hit.collider.isTrigger || hit.collider.GetComponent<BattleBoat>()==owner) continue;
                hit.collider.GetComponent<BattleBoat>()?.Hit(damage,effect,Vector2.zero);
                BattleVfx.Spawn(visuals?.impact,hit.point,null,false,direction);
                Destroy(gameObject); return;
            }
            transform.position+=(Vector3)(direction*distance);
            if((life-=Time.fixedDeltaTime)<=0) Destroy(gameObject);
        }
    }

    public class BattlePickup : MonoBehaviour
    {
        private PickupOption option;
        private float respawn;
        public void ResetPickup()
        {
            option=BattleArena.Instance.RollPickup();
            transform.position=BattleArena.Instance.PickupPosition(this);
            respawn=0;
            var renderer=GetComponentInChildren<SpriteRenderer>();
            GetComponent<Collider2D>().enabled=option!=null;
            renderer.enabled=option!=null;
            if(option==null) return;
            BattleArena.ApplyArt(gameObject,BattleArena.Instance.pickupArt,1,2);
            renderer.color=Color.white;
        }
        private void Update()
        {
            if(!BattleArena.Instance.CanSimulate || respawn<=0) return;
            if((respawn-=Time.deltaTime)<=0) ResetPickup();
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            var boat=other.GetComponent<BattleBoat>();
            if(respawn>0 || boat==null || !boat.Alive || !BattleArena.Instance.CanSimulate) return;
            if(option==null) return;
            var revealed=BattleArena.Instance.RollPickup();
            if(revealed==null) return;
            boat.Collect(revealed); respawn=BattleArena.Instance.pickupRespawnSeconds;
            GetComponent<Collider2D>().enabled=false;
            GetComponentInChildren<SpriteRenderer>().enabled=false;
        }
        private void OnGUI()
        {
            if(option==null || respawn>0 || Camera.main==null) return;
            var point=Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(point.x-12,Screen.height-point.y-35,30,25),"?");
        }
    }
}
