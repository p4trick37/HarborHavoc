using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborBattle
{
    public enum PowerUp { None, RapidFire, Shotgun, Laser, FastBoat }

    // Local input stays here; simulation receives commands instead of reading devices.
    public struct BoatCommand
    {
        public Vector2 Move, Aim;
        public bool Fire, Boost;
    }

    public class BattleArena : MonoBehaviour
    {
        public static BattleArena Instance { get; private set; }
        public readonly List<BattleBoat> Boats = new List<BattleBoat>();
        public bool Running { get; private set; }
        public const float HalfWidth = 16f, HalfHeight = 9f;
        [Header("Project artwork")]
        public Sprite boatArt, cannonArt, islandArt, pickupArt, shotArt;
        public GameObject originalPlayerPrefab;
        private Camera arenaCamera;
        private readonly List<Gamepad> pads = new List<Gamepad>();
        private readonly List<BattlePickup> pickups = new List<BattlePickup>();
        private string result;
        private float countdown;
        private bool countingDown;
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
            Wall(new Vector2(0,9.5f),new Vector2(34,1));
            Wall(new Vector2(0,-9.5f),new Vector2(34,1));
            Wall(new Vector2(16.5f,0),new Vector2(1,18));
            Wall(new Vector2(-16.5f,0),new Vector2(1,18));
            if (GameObject.Find("Arena Islands") == null && GameObject.Find("Arena Island") == null)
            {
                Island(new Vector2(0,3.8f));
                Island(new Vector2(0,-3.8f));
            }
            for (int i=0;i<4;i++)
            {
                var item = Visual("Pickup",new Vector2((i%2==0 ? -1:1)*5,(i<2 ? -1:1)*3),Vector2.one*.85f,colors[i]);
                ApplyArt(item,pickupArt,1f,2);
                item.AddComponent<BoxCollider2D>().isTrigger = true;
                var pickup = item.AddComponent<BattlePickup>();
                pickup.Kind = (PowerUp)(i+1);
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
            if (!Running && !countingDown)
                foreach (var pad in Gamepad.all)
                    if (pad.buttonEast.wasPressedThisFrame) { Restart(); break; }
            arenaCamera.orthographicSize = Mathf.Max(11.5f,18f/arenaCamera.aspect);
            var keyboard=Keyboard.current;
            if(keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) Restart();
            if(keyboard!=null && keyboard.enterKey.wasPressedThisFrame)
            {
                if(result!=null) Restart();
                else if(!Running && !countingDown)
                {
                    // Enter provides a complete two-player keyboard test without controllers.
                    if(pads.Count==0) { pads.Add(null); SpawnBoat(null,0); pads.Add(null); SpawnBoat(null,1); }
                    if(pads.Count>=2) { countdown=3; countingDown=true; }
                }
            }
            if (!Running && result == null && !countingDown)
            {
                foreach (var pad in Gamepad.all)
                    if (pad.startButton.wasPressedThisFrame && !pads.Contains(pad) && pads.Count<4)
                    { pads.Add(pad); SpawnBoat(pad,pads.Count-1); }
                if (pads.Count>=2)
                    foreach (var pad in pads)
                        if (pad!=null && pad.buttonSouth.wasPressedThisFrame) { countdown=3; countingDown=true; break; }
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
                    boat.Submit(pad==null ? KeyboardCommand(boat) : new BoatCommand { Move=pad.leftStick.ReadValue(), Aim=pad.rightStick.ReadValue(),
                        Fire=pad.rightTrigger.isPressed, Boost=pad.leftTrigger.isPressed });
                }
                if (alive<=1) { Running=false; result=alive==1 ? "Player "+(survivor.Index+1)+" wins!" : "Draw!"; }
            }
            if (result!=null)
                foreach (var pad in Gamepad.all)
                    if (pad.startButton.wasPressedThisFrame) { Restart(); break; }
        }

        public bool CanSimulate
        {
            get { if (!Running) return false; foreach (var pad in pads) if (pad!=null && !pad.added) return false; return true; }
        }

        private BoatCommand KeyboardCommand(BattleBoat boat)
        {
            var k=Keyboard.current;
            if(k==null) return default;
            if(boat.Index==0)
            {
                Vector2 aim=Vector2.zero;
                if(Mouse.current!=null)
                    aim=(Vector2)arenaCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue())-(Vector2)boat.transform.position;
                return new BoatCommand { Move=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0)),
                    Aim=aim, Fire=(Mouse.current!=null && Mouse.current.leftButton.isPressed)||k.spaceKey.isPressed, Boost=k.leftShiftKey.isPressed };
            }
            var move=new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.upArrowKey.isPressed?1:0)-(k.downArrowKey.isPressed?1:0));
            return new BoatCommand { Move=move, Aim=move, Fire=k.rightCtrlKey.isPressed, Boost=k.rightShiftKey.isPressed };
        }

        private void SpawnBoat(Gamepad pad,int index)
        {
            var positions=new[] { new Vector2(-12,-6),new Vector2(12,6),new Vector2(-12,6),new Vector2(12,-6) };
            var obj=Visual("Player "+(index+1)+" Boat",positions[index],new Vector2(.8f,1.5f),Color.white);
            ApplyArt(obj,boatArt,1.8f,3);
            // Reuse the campaign player's Boat renderer, not the pickup artwork.
            // Clone only its visual child so campaign input/health scripts never run.
            if(originalPlayerPrefab!=null)
            {
                var source=originalPlayerPrefab.transform.Find("Boat");
                if(source!=null)
                {
                    obj.transform.Find("Visual").gameObject.SetActive(false);
                    var hull=Instantiate(source.gameObject,obj.transform,false);
                    hull.name="Boat Hull";
                    hull.transform.localPosition=Vector3.zero;
                    hull.transform.localRotation=Quaternion.identity;
                    var renderer=hull.GetComponent<SpriteRenderer>();
                    renderer.color=Color.white;
                    renderer.sortingOrder=3;
                    hull.transform.localScale=Vector3.one*(1.8f/renderer.sprite.bounds.size.y);
                }
            }
            obj.AddComponent<BoxCollider2D>().size=new Vector2(.8f,1.5f);
            var body=obj.AddComponent<Rigidbody2D>();
            body.gravityScale=0; body.freezeRotation=true; body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var boat=obj.AddComponent<BattleBoat>(); boat.Index=index;
            var turret=Visual("Cannon",positions[index],new Vector2(.22f,1),Color.white);
            ApplyArt(turret,cannonArt,.9f,4);
            turret.transform.SetParent(obj.transform,true);
            boat.Turret=turret.transform;
            Boats.Add(boat);
        }

        private void Restart()
        {
            foreach (var boat in Boats) Destroy(boat.gameObject);
            foreach (var shot in FindObjectsByType<BattleShot>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            Boats.Clear(); pads.Clear(); result=null; Running=false; countingDown=false;
            foreach (var pickup in pickups) pickup.ResetPickup();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(8,8,Screen.width-16,100),GUIContent.none);
            var style=new GUIStyle(GUI.skin.label) { fontSize=20, alignment=TextAnchor.MiddleCenter };
            string heading=result ?? (countingDown ? "Ready: "+Mathf.CeilToInt(countdown) : Running ? (CanSimulate ? "LAST BOAT AFLOAT" : "Reconnect controller to resume") : "ENTER: keyboard match   |   Controllers: START to join, A to begin");
            GUI.Label(new Rect(0,8,Screen.width,40),heading,style);
            GUI.Label(new Rect(0,45,Screen.width,30),"Left stick: sail   |   Right stick: aim   |   RT: fire   |   LT: boost   "+(result!=null ? "| START: new match" : "| B: reset lobby"));
            GUI.Label(new Rect(15,75,Screen.width,25),"P1: WASD / mouse aim / click or Space / Left Shift boost     P2: arrows move + aim / Right Ctrl fire / Right Shift boost     Esc: lobby");
            for(int i=0;i<Boats.Count;i++)
            {
                GUI.Box(new Rect(10+i*Screen.width/4f,Screen.height-70,Screen.width/4f-20,65),GUIContent.none);
                var boat=Boats[i]; GUI.color=colors[i];
                GUI.Label(new Rect(15+i*Screen.width/4f,Screen.height-65,Screen.width/4f,60),
                    "P"+(i+1)+"  "+(boat.Alive ? "Hull: "+boat.Health : "ELIMINATED")+"\n"+boat.PowerLabel);
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
        public int Index;
        public Transform Turret;
        public int Health { get; private set; } = 100;
        public bool Alive => Health>0;
        private PowerUp power;
        private float remaining, cooldown;
        private BoatCommand command;
        private Vector2 aim=Vector2.up;
        public string PowerLabel => power==PowerUp.None ? "Basic cannon" : power+": "+Mathf.CeilToInt(remaining)+(power==PowerUp.FastBoat ? " fuel" : " shots");
        public void Submit(BoatCommand input) { command=input; }
        public void Collect(PowerUp kind) { power=kind; remaining=kind==PowerUp.RapidFire ? 40 : kind==PowerUp.Shotgun ? 8 : kind==PowerUp.Laser ? 5 : 6; }
        public void Damage(int amount)
        {
            if (!Alive || !BattleArena.Instance.CanSimulate) return;
            Health=Mathf.Max(0,Health-amount);
            if (!Alive) { GetComponent<Collider2D>().enabled=false; GetComponentInChildren<SpriteRenderer>().color=Color.gray; }
        }
        private void FixedUpdate()
        {
            var body=GetComponent<Rigidbody2D>();
            body.linearVelocity=Vector2.zero;
            if (!Alive || !BattleArena.Instance.CanSimulate) return;
            bool boost=power==PowerUp.FastBoat && command.Boost && command.Move.sqrMagnitude>.01f;
            body.linearVelocity=Vector2.ClampMagnitude(command.Move,1)*(boost ? 10 : 5);
            if(command.Move.sqrMagnitude>.04f) body.rotation=Mathf.Atan2(command.Move.y,command.Move.x)*Mathf.Rad2Deg-90;
            if(command.Aim.sqrMagnitude>.04f) aim=command.Aim.normalized;
            Turret.rotation=Quaternion.Euler(0,0,Mathf.Atan2(aim.y,aim.x)*Mathf.Rad2Deg-90);
            if(boost) { remaining-=Time.fixedDeltaTime; Expire(); }
            cooldown-=Time.fixedDeltaTime;
            if(command.Fire && cooldown<=0)
            {
                var weapon=power;
                cooldown=weapon==PowerUp.RapidFire ? .09f : weapon==PowerUp.Shotgun ? .65f : weapon==PowerUp.Laser ? .8f : .35f;
                if(weapon==PowerUp.Laser) FireLaser();
                else for(int i=0;i<(weapon==PowerUp.Shotgun ? 5:1);i++)
                {
                    Vector2 direction=Quaternion.Euler(0,0,weapon==PowerUp.Shotgun ? (i-2)*10:0)*aim;
                    BattleShot.Spawn(this,direction,weapon==PowerUp.Shotgun ? 8:10);
                }
                if(weapon!=PowerUp.None && weapon!=PowerUp.FastBoat) { remaining--; Expire(); }
            }
        }
        private void Expire() { if(remaining<=0) { power=PowerUp.None; remaining=0; } }
        private void FireLaser()
        {
            Vector2 start=(Vector2)transform.position+aim*1.1f;
            Vector2 end=start+aim*40;
            foreach(var hit in Physics2D.RaycastAll(start,aim,40))
            {
                if(hit.collider.isTrigger || hit.collider.GetComponent<BattleBoat>()==this) continue;
                end=hit.point;
                hit.collider.GetComponent<BattleBoat>()?.Damage(30);
                break;
            }
            var beam=BattleArena.Visual("Laser",(start+end)/2,new Vector2(.12f,Vector2.Distance(start,end)),Color.red);
            beam.transform.rotation=Turret.rotation;
            Destroy(beam,.12f);
        }
    }

    public class BattleShot : MonoBehaviour
    {
        private BattleBoat owner;
        private Vector2 direction;
        private int damage;
        private float life=2;
        public static void Spawn(BattleBoat source,Vector2 direction,int damage)
        {
            var obj=BattleArena.Visual("Cannon shot",(Vector2)source.transform.position+direction*1.1f,Vector2.one*.18f,Color.white);
            BattleArena.ApplyArt(obj,BattleArena.Instance.shotArt,.25f,5);
            var shot=obj.AddComponent<BattleShot>(); shot.owner=source; shot.direction=direction; shot.damage=damage;
        }
        private void FixedUpdate()
        {
            if(!BattleArena.Instance.CanSimulate) return;
            float distance=22*Time.fixedDeltaTime;
            var hits=Physics2D.CircleCastAll(transform.position,.09f,direction,distance);
            foreach(var hit in hits)
            {
                if(hit.collider.isTrigger || hit.collider.GetComponent<BattleBoat>()==owner) continue;
                hit.collider.GetComponent<BattleBoat>()?.Damage(damage);
                Destroy(gameObject); return;
            }
            transform.position+=(Vector3)(direction*distance);
            if((life-=Time.fixedDeltaTime)<=0) Destroy(gameObject);
        }
    }

    public class BattlePickup : MonoBehaviour
    {
        public PowerUp Kind;
        private float respawn;
        public void ResetPickup() { respawn=0; GetComponent<Collider2D>().enabled=true; GetComponentInChildren<SpriteRenderer>().enabled=true; }
        private void Update()
        {
            if(!BattleArena.Instance.CanSimulate || respawn<=0) return;
            if((respawn-=Time.deltaTime)<=0) ResetPickup();
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            var boat=other.GetComponent<BattleBoat>();
            if(respawn>0 || boat==null || !boat.Alive || !BattleArena.Instance.CanSimulate) return;
            boat.Collect(Kind); respawn=10;
            GetComponent<Collider2D>().enabled=false;
            GetComponentInChildren<SpriteRenderer>().enabled=false;
        }
        private void OnGUI()
        {
            if(respawn>0 || Camera.main==null) return;
            var point=Camera.main.WorldToScreenPoint(transform.position);
            GUI.Label(new Rect(point.x-45,Screen.height-point.y-35,100,25),Kind.ToString());
        }
    }
}
