using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using SkinwalkerMod;
using LethalNetworkAPI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Linq;
using System.Threading.Tasks;

namespace LethalIntelligence.Patches
{
    public class BushSystem : MonoBehaviour
    {
        public bool bushWithItem;
    }
    public class GlobalItemList : MonoBehaviour
    {
        public List<GrabbableObject> allitems = new List<GrabbableObject>();

        private List<GrabbableObject> previtems = new List<GrabbableObject>();

        public List<WalkieTalkie> allWalkieTalkies = new List<WalkieTalkie>();

        public bool isShotgun;

        public bool isShovel;

        public bool isWalkie;

        public static GlobalItemList Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (allitems != previtems)
            {
                CheckItem(1);
                CheckItem(2);
                CheckItem(3);
                previtems = allitems;
            }
        }

        private void CheckItem(int id)
        {
            foreach (GrabbableObject allitem in allitems)
            {
                if (id == 1)
                {
                    if (allitem is ShotgunItem)
                    {
                        isShotgun = true;
                        break;
                    }
                    isShotgun = false;
                }
                if (id == 2)
                {
                    if (allitem is Shovel)
                    {
                        isShovel = true;
                        break;
                    }
                    isShovel = false;
                }
                if (id == 3)
                {
                    if (allitem is WalkieTalkie)
                    {
                        isWalkie = true;
                        break;
                    }
                    isWalkie = false;
                }
            }
        }
    }

    public class SyncConfiguration : NetworkBehaviour
    {
    }
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        private static void Awake_Postfix()
        {
            if (Plugin.enableMaskedFeatures)
            {
                ((Component)StartOfRound.Instance).gameObject.AddComponent<SyncConfiguration>();
                ((Component)StartOfRound.Instance).gameObject.AddComponent<GlobalItemList>();
            }
        }
    }
    public class CheckItemCollision : MonoBehaviour
    {
        public bool hidedByMasked;
    }
    [HarmonyPatch(typeof(GrabbableObject))]
    internal class GrabbableObjectPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(GrabbableObject __instance)
        {
            if (Plugin.enableMaskedFeatures)
            {
                ((Component)__instance).gameObject.AddComponent<CheckItemCollision>();
                GlobalItemList.Instance.allitems.Add(__instance);
                if (__instance is WalkieTalkie)
                {
                    GlobalItemList.Instance.allWalkieTalkies.Add(((Component)__instance).GetComponent<WalkieTalkie>());
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("DestroyObjectInHand")]
        private static void DestroyObjectInHand_Postfix(GrabbableObject __instance)
        {
            if (Plugin.enableMaskedFeatures)
            {
                GlobalItemList.Instance.allitems.Remove(__instance);
                if (__instance is WalkieTalkie)
                {
                    GlobalItemList.Instance.allWalkieTalkies.Remove(((Component)__instance).GetComponent<WalkieTalkie>());
                }
            }
        }
    }
    public class MaskedAIRevamp : NetworkBehaviour
    {
        public enum Personality
        {
            None,
            Aggressive,
            Stealthy,
            Cunning,
            Deceiving,
            Insane
        }

        public enum Focus
        {
            None,
            Player,
            Items,
            Terminal,
            BreakerBox,
            Hiding,
            Mimicking
        }

        public enum Activity
        {
            None,
            MainEntrance,
            FireExit,
            ItemLocker,
            BreakerBox,
            Apparatus,
            RandomItem,
            RandomPlayer
        }

        private void TestConfig()
        {
            maskedPersonality = Personality.Deceiving; //for testing a specific personality
            SelectPersonalityInt.Value = 2; //for testing a specific personality
            maskedFocus = Focus.Items;
        }

        public Personality maskedPersonality;

        public Personality lastMaskedPersonality;

        public Focus maskedFocus;

        public Focus lastMaskedFocus;

        public Activity maskedActivity;

        public bool mustChangeFocus, mustChangeActivity;

        public bool ignoringPersonality;

        public bool focusingPersonality;

        public AISearchRoutine searchForItems;

        public float stopAndTbagTimer = 1.1f;

        public float stopAndTbagCooldown;

        public int randomPose;

        public bool isHoldingObject;

        public bool heldTwoHanded;

        public bool moveSpecial;

        public EnemyAI __instance;

        public MaskedPlayerEnemy maskedEnemy;

        public Animator creatureAnimator;

        public NavMeshAgent agent;

        public bool checkDestination;

        //public bool wantItems = true; //not needed currently

        public GrabbableObject closestGrabbable;

        public GrabbableObject nearestGrabbable, heldGrabbable; //only for status report!

        private bool targetPlayerReachable;

        private bool closestGrabbableReachable;

        private bool nearestGrabbableReachable;

        private bool justPickedUpItem = false;

        public CheckItemCollision itemSystem;

        public float enterTermianlCodeTimer;

        public float transmitMessageTimer; //for transmitting a signal translator message

        public float transmitPauseTimer; //for adding a delay between messages

        public int enterTermianlSpecialCodeTime;

        public LethalNetworkVariable<int> enterTermianlSpecialCodeInt = new LethalNetworkVariable<int>("enterTermianlSpecialCodeInt");

        public LethalNetworkVariable<bool> isCrouched = new LethalNetworkVariable<bool>("isCrouched");

        public LethalNetworkVariable<bool> dropItem = new LethalNetworkVariable<bool>("dropItem");

        public LethalNetworkVariable<bool> isDancing = new LethalNetworkVariable<bool>("isDancing");

        public LethalNetworkVariable<bool> useWalkie = new LethalNetworkVariable<bool>("useWalkie");

        public LethalNetworkVariable<bool> isJumped = new LethalNetworkVariable<bool>("isJumped");

        public LethalNetworkVariable<int> SelectPersonalityInt = new LethalNetworkVariable<int>("SelectPersonalityInt");

        public LethalNetworkVariable<int> maxDanceCount = new LethalNetworkVariable<int>("maxDanceCount");

        public LethalNetworkVariable<float> terminalTimeFloat = new LethalNetworkVariable<float>("terminalTimeFloat");

        public LethalNetworkVariable<float> delayMaxTime = new LethalNetworkVariable<float>("delayMaxTime");

        public float jumpTime = 1f;

        private float dropTimerB;

        private Vector3 prevPos;

        private float velX;

        private float velZ;

        public float closetTimer;

        private bool enableDance;

        public float shovelTimer;

        public float hornTimer;

        public bool stunThrowed;

        public float angle1;

        public float angle2;

        public float dropTimer;

        public float shootTimer;

        public float rotationTimer;

        public float rotationCooldown;

        public bool itemDroped;

        public bool droppingItem;

        public Terminal terminal;

        public bool isUsingTerminal;

        public bool noMoreItems = false;

        public bool noMoreTerminal = false;

        public float dropShipTimer;

        public bool isDeliverEmptyDropship;

        public GameObject itemHolder;

        public float upperBodyAnimationsWeight;

        public float grabbableTime;

        public float distanceToPlayer = 1000f;

        private float terminalDistance = 1000f;

        private bool terminalReachable;

        private float breakerBoxDistance = 1000f;

        private float closestGrabbableDistance = 1000f;

        private float nearestGrabbableDistance = 1000f;

        private bool breakerBoxReachable;

        private float bushDistance = 1000f;

        public bool isStaminaDowned;

        public Vector3 originDestination;

        public bool walkieUsed;

        public bool walkieVoiceTransmitted;

        public float walkieTimer;

        public float walkieCooldown;

        public float originTimer;

        private BreakerBox breakerBox;

        public bool isUsingBreakerBox; //needed?

        public bool noMoreBreakerBox = false;

        private AnimatedObjectTrigger powerBox;

        private GameObject[] bushes;

        private ItemDropship dropship;

        private TerminalAccessibleObject[] terminalAccessibleObject;

        private float lookTimer;

        private bool lookedPlayer;

        public bool notGrabClosestItem;

        public bool notGrabItems;

        public bool isReeledWithShovel;

        public bool isHittedWithShovel;

        public bool shovelHitConfirm;

        public PlayerControllerB nearestPlayer;

        public bool canGoThroughItem;

        public bool isDroppedShotgunAvailable;

        public string maskedId = null;

        private string lastDebugModeStatus;

        private string maskedGoal = "";

        private bool stopStatusReporting = false;

        float breakerClosestPoint, terminalClosestPoint, grabbableClosestPoint;

        Vector3 destination;
        float distanceToDestination;

        int delayNumber = 0;
        int delayMax = Plugin.debugStatusDelay;

        private bool delayUpdate()
        {
            //return true;
            delayNumber++;
            if (delayNumber >= delayMax)
            {
                delayNumber = 0;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void MaskedStatusReport()
        {
            if (Plugin.DebugMode && !stopStatusReporting && delayUpdate())
            {
                string ng = "null";
                string hg = "null";
                string ngd = "null";
                string cg = "null";
                if (nearestGrabbable != null)
                {
                    ng = nearestGrabbable.name.ToString();
                    ngd = nearestGrabbableDistance.ToString();

                }
                if (heldGrabbable != null)
                {
                    hg = heldGrabbable.name.ToString();
                }
                if (closestGrabbable != null)
                {
                    cg = closestGrabbable.name.ToString();
                }
                string bbcp = breakerClosestPoint.ToString();
                string tcp = terminalClosestPoint.ToString();
                string bbd = breakerBoxDistance.ToString();
                string td = terminalDistance.ToString();

                destination = maskedEnemy.destination;
                distanceToDestination = Vector3.Distance(maskedEnemy.transform.position, destination);

                string focusStart = "\n------------ Focus Details ------------";
                string focusDetails = "";
                if (maskedFocus == Focus.Items)
                {
                    focusDetails = focusStart +
                        "\nnearestGrabbableReachable = " + nearestGrabbableReachable +
                        "\nnearestGrabbable = " + ng +
                        "\nclosestGrabbable = " + cg;
                }
                else if (maskedFocus == Focus.BreakerBox)
                {
                    focusDetails = focusStart +
                        "\nBreakerBoxReachable = " + breakerBoxReachable +
                        "\nbreakerBox.isPowerOn = " + breakerBox.isPowerOn;
                }
                else if (maskedFocus == Focus.Terminal)
                {
                    focusDetails = focusStart +
                    "\nTerminalReachable = " + terminalReachable;
                }
                string debugMsg =
                "\n===== MaskedStatusReport() Start =====" +
                "\nMaskedID = " + maskedId +
                "\nMaskedPersonality = " + maskedPersonality.ToString() +
                "\nMaskedFocus = " + maskedFocus.ToString() +
                "\nMustChangeFocus = " + mustChangeFocus.ToString() +
                "\nMaskedActivity (Focus=None) = " + maskedActivity.ToString() +
                "\nMustChangeActivity = " + mustChangeActivity.ToString() +
                //"\nDestination = " + destination.ToString() +
                //"\nDistance to Destination = " + distanceToDestination.ToString() +
                //"\nNavMeshPathStatus = " + nms +
                "\nMoon = " + currentMoon +
                "\nInterior = " + currentInterior +
                "\nisDead = " + maskedEnemy.isEnemyDead +
                "\n\nisOutside = " + maskedEnemy.isOutside +
                "\nisInsidePlayerShip = " + maskedEnemy.isInsidePlayerShip +
                "\nheldGrabbable = " + hg +
                "\nMaskedGoal = " + maskedGoal +
                "\n\nnoMoreBreakerBox = " + noMoreBreakerBox +
                "\nnoMoreTerminal = " + noMoreTerminal +
                "\nnoMoreItems = " + noMoreItems +
                "\nnotGrabClosestItem = " + notGrabClosestItem +
                focusDetails +
                "\n===== MaskedStatusReport() End =======";
                if (debugMsg != lastDebugModeStatus)
                {
                    lastDebugModeStatus = debugMsg;
                    Plugin.mls.LogInfo(debugMsg);
                }
                if (maskedEnemy.isEnemyDead)
                {
                    Plugin.mls.LogInfo("Masked " + maskedPersonality.ToString() + "(" + maskedId + ") is now dead, status reporting will cease for this masked");
                    stopStatusReporting = true;
                }
            }
        }

        string currentMoon = null;
        string currentInterior = null;
        List<Personality> useablePersonalities;

        private void selectAvailablePersonalities()
        {
            useablePersonalities = new List<Personality>();
            if (!Plugin.enableMaskedAggressive && !Plugin.enableMaskedCunning && !Plugin.enableMaskedDeceiving && !Plugin.enableMaskedInsane && !Plugin.enableMaskedStealthy)
            {
                Plugin.mls.LogWarning("Bad Config!! - All masked personalities are turned off so turning off whole Masked AI Module.");
            }
            else
            {
                if (Plugin.enableMaskedAggressive)
                {
                    useablePersonalities.Add(Personality.Aggressive);
                }
                if (Plugin.enableMaskedStealthy)
                {
                    useablePersonalities.Add(Personality.Stealthy);
                }
                if (Plugin.enableMaskedCunning)
                {
                    useablePersonalities.Add(Personality.Cunning);
                }
                if (Plugin.enableMaskedDeceiving)
                {
                    useablePersonalities.Add(Personality.Deceiving);
                }
                if (Plugin.enableMaskedInsane)
                {
                    useablePersonalities.Add(Personality.Insane);
                }
            }
        }

        public void Start()
        {
            selectAvailablePersonalities();
            currentMoon = RoundManager.Instance.currentLevel.PlanetName;
            currentInterior = RoundManager.Instance.dungeonGenerator.Generator.DungeonFlow.name.ToString();
            //IL_00ed: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f7: Expected O, but got Unknown
            //IL_0169: Unknown result type (might be due to invalid IL or missing references)
            //IL_018e: Unknown result type (might be due to invalid IL or missing references)
            if (GameNetworkManager.Instance.isHostingGame)
            {
                maskedId = this.GetInstanceID().ToString();
                enterTermianlSpecialCodeInt.Value = Random.Range(0, Enum.GetNames(typeof(Personality)).Length);
            }
            if ((Object)(object)GameObject.FindGameObjectWithTag("Bush") != (Object)null)
            {
                if (bushes != GameObject.FindGameObjectsWithTag("Bush"))
                {
                    bushes = GameObject.FindGameObjectsWithTag("Bush");
                }
                GameObject[] array = bushes;
                foreach (GameObject val in array)
                {
                    if ((Object)(object)val.GetComponent<BushSystem>() == (Object)null)
                    {
                        val.AddComponent<BushSystem>();
                    }
                }
            }
            terminal = Object.FindObjectOfType<Terminal>();
            breakerBox = Object.FindObjectOfType<BreakerBox>();
            __instance = (EnemyAI)(object)((Component)this).GetComponent<MaskedPlayerEnemy>();
            maskedEnemy = ((Component)this).GetComponent<MaskedPlayerEnemy>();
            creatureAnimator = ((Component)((Component)this).transform.GetChild(0).GetChild(3)).GetComponent<Animator>();
            itemHolder = new GameObject("ItemHolder");
            itemHolder.transform.parent = ((Component)__instance).transform.GetChild(0).GetChild(3).GetChild(0)
                .GetChild(0)
                .GetChild(0)
                .GetChild(0)
                .GetChild(1)
                .GetChild(0)
                .GetChild(0)
                .GetChild(0);
            itemHolder.transform.localPosition = new Vector3(-0.002f, 0.036f, -0.042f);
            itemHolder.transform.localRotation = Quaternion.Euler(-3.616f, -2.302f, 0.145f);
            if (GameNetworkManager.Instance.isHostingGame)
            {
                maxDanceCount.Value = Random.Range(2, 4);
            }
            if ((Object)(object)creatureAnimator.runtimeAnimatorController != (Object)(object)Plugin.MaskedAnimController)
            {
                creatureAnimator.runtimeAnimatorController = Plugin.MaskedAnimController;
            }
            if ((Object)(object)((Component)((Component)__instance).transform.GetChild(3).GetChild(0)).GetComponent<Animator>().runtimeAnimatorController != (Object)(object)Plugin.MapDotRework)
            {
                ((Component)((Component)__instance).transform.GetChild(3).GetChild(0)).GetComponent<Animator>().runtimeAnimatorController = Plugin.MapDotRework;
            }
            dropship = Object.FindObjectOfType<ItemDropship>();
            terminalAccessibleObject = Object.FindObjectsOfType<TerminalAccessibleObject>();
        }

        private void Jump(bool enable)
        {
            if (jumpTime > 0f && !isJumped.Value)
            {
                jumpTime -= Time.deltaTime;
            }
            if (!isCrouched.Value && !isJumped.Value && jumpTime < 1f && jumpTime > 0.9f)
            {
                isJumped.Value = true;
                creatureAnimator.SetTrigger("FakeJump");
            }
        }

        private void Dance(bool enabled)
        {
            if (enabled)
            {
                isDancing.Value = true;
                __instance.movingTowardsTargetPlayer = false;
                agent.speed = 0f;
            }
            else
            {
                isDancing.Value = false;
            }
        }

        private void InvokeAllClientsSynced()
        {
            Plugin.mls.LogWarning((object)"InvokeAllClientsSynced");
        }

        private void InvokeOtherClientsSynced()
        {
            Plugin.mls.LogWarning((object)"InvokeOtherClientsSynced");
        }

        private void SelectPersonality(int num)
        {
            switch (num)
            {
                case 0:
                    maskedPersonality = Personality.Aggressive;
                    break;
                case 1:
                    maskedPersonality = Personality.Cunning;
                    break;
                case 2:
                    maskedPersonality = Personality.Deceiving;
                    break;
                case 3:
                    maskedPersonality = Personality.Stealthy;
                    break;
                case 4:
                    maskedPersonality = Personality.Insane;
                    break;
            }
        }

        private void SyncTermianlInt(int num)
        {
            enterTermianlSpecialCodeTime = num;
        }

        Vector3 terminalPosition, breakerPosition, lockerPosition, apparatusPosition, fireExitPosition, mainEntrancePosition; //positions to save

        float lockerDistance;

        float lockerClosestPoint;

        bool lockerReachable;

        int itemsToHide = 5;

        private void CalculatingVariables()
        {
            //update reachable variables
            NavMeshPath nmpBreaker = new NavMeshPath(), nmpTerminal = new NavMeshPath(), nmpLocker = new NavMeshPath();
            NavMeshHit hitBreaker, hitTerminal, hitLocker;
            try
            {
                if (NavMesh.SamplePosition(breakerBox.transform.position, out hitBreaker, 10, -1))
                {
                    breakerBoxReachable = agent.CalculatePath(hitBreaker.position, nmpBreaker);
                    //breakerBoxDistance = Vector3.Distance(((Component)this).transform.position, hitBreaker.position);
                    breakerBoxDistance = Vector3.Distance(((Component)this).transform.position, ((Component)breakerBox).transform.position);
                    breakerClosestPoint = Vector3.Distance(hitBreaker.position, breakerBox.transform.position);
                    breakerPosition = hitBreaker.position;
                }
                else
                {
                    breakerBoxReachable = false;
                }
            }
            catch(NullReferenceException nre)
            {
                //Plugin.mls.LogDebug("BreakerBox NullReferenceException() Caught!\n" + nre.Message);
                breakerBoxReachable = false;
                noMoreBreakerBox = true;
            }
            if (NavMesh.SamplePosition(terminal.transform.position, out hitTerminal, 10, -1))
            {
                terminalReachable = agent.CalculatePath(hitTerminal.position, nmpTerminal);
                terminalDistance = Vector3.Distance(((Component)this).transform.position, ((Component)terminal).transform.position);
                terminalClosestPoint = Vector3.Distance(hitTerminal.position, terminal.transform.position);
                terminalPosition = hitTerminal.position;
            }
            else
            {
                terminalReachable = false;
            }
            //this isnt working right now.. it routes the masked on to above the ship..
            lockerPosition = GameObject.Find("LockerAudio").transform.position; // so use this for now
            /*if (NavMesh.SamplePosition(GameObject.Find("LockerAudio").transform.position,out hitLocker,10,-1))
            {
                lockerReachable = agent.CalculatePath(hitTerminal.position, nmpLocker);
                lockerDistance = Vector3.Distance(((Component)this).transform.position, GameObject.Find("LockerAudio").transform.position);
                lockerClosestPoint = Vector3.Distance(hitTerminal.position, terminal.transform.position);
                lockerPosition = hitTerminal.position;
            }*/
            setNearestGrabbable();
            if (!breakerBoxReachable)
            {
                breakerBoxDistance = 1000f;
            }
            if (!terminalReachable)
            {
                terminalDistance = 1000f;
            }
            //this is in setNearestGrabbable();
            /*if (!closestGrabbableReachable)
            {
                nearestGrabbableDistance = 1000f;
            }*/
        }

        private void SetFocus()
        {
            lastMaskedFocus = maskedFocus;

            //new line
            if (maskedPersonality == Personality.Cunning && breakerBoxDistance < 40f && lastMaskedFocus != Focus.BreakerBox && breakerBoxReachable && !noMoreBreakerBox)
            //this was a cunning only line
            //if (breakerBoxDistance < terminalDistance && breakerBoxDistance < closestGrabbableDistance && lastMaskedFocus != Focus.BreakerBox && breakerBoxReachable && !noMoreBreakerBox && maskedPersonality==Personality.Cunning)
            {
                //cunning only
                maskedFocus = Focus.BreakerBox;
                maskedActivity = Activity.None;
                mustChangeActivity = true;
            }
            //new line
            else if ((maskedPersonality == Personality.Cunning || maskedPersonality == Personality.Deceiving || maskedPersonality == Personality.Insane) && terminalDistance < 40f && lastMaskedFocus != Focus.Terminal && terminalReachable && !noMoreTerminal)
            //this was a cunning only line
            //else if (terminalDistance < breakerBoxDistance && terminalDistance < closestGrabbableDistance && lastMaskedFocus != Focus.Terminal && terminalReachable && !noMoreTerminal && (maskedPersonality==Personality.Cunning || maskedPersonality == Personality.Deceiving || maskedPersonality == Personality.Insane))
            {
                //cunning, deceiving, insane
                maskedFocus = Focus.Terminal;
                maskedActivity = Activity.None;
                mustChangeActivity = true;
            }
            //new line
            else if (nearestGrabbableDistance < 40f && lastMaskedFocus != Focus.Items && nearestGrabbableReachable && !noMoreItems)
            //this was a cunning only line
            //else if (closestGrabbableDistance < breakerBoxDistance && closestGrabbableDistance < terminalDistance && lastMaskedFocus != Focus.Items && closestGrabbableReachable && !noMoreItems)
            {
                //all masked
                maskedFocus = Focus.Items;
                maskedActivity = Activity.None;
                mustChangeActivity = true;
            }
            else if (__instance.targetPlayer != null && lastMaskedFocus != Focus.Hiding && maskedPersonality == Personality.Stealthy)
            {
                maskedFocus = Focus.Hiding;
                maskedActivity = Activity.None;
                mustChangeActivity = true;
            }
            else if (__instance.targetPlayer != null && lastMaskedFocus != Focus.Mimicking && maskedPersonality == Personality.Stealthy)
            {
                maskedFocus = Focus.Mimicking;
                maskedActivity = Activity.None;
                mustChangeActivity = true;
            }
            else if (__instance.targetPlayer != null && lastMaskedFocus != Focus.Player && (maskedPersonality == Personality.Aggressive || maskedPersonality == Personality.Deceiving))
            {
                maskedFocus = Focus.Player;
                maskedActivity = Activity.None;
                mustChangeActivity = true;
            }
            else
            {
                //all masked
                maskedFocus = Focus.None;
                if (mustChangeActivity == true)
                {
                    SetActivity();
                    mustChangeActivity = false;
                }
            }
            mustChangeFocus = false;
        }

        private void SetActivity()
        {
            //int start = 0;
            int random;
            if(maskedEnemy.isOutside && maskedActivity!=Activity.ItemLocker)
            {
                maskedActivity = Activity.ItemLocker;
            }
            else if(!maskedEnemy.isOutside && maskedActivity!=Activity.BreakerBox)
            {
                maskedActivity = Activity.BreakerBox;
            }
            else if(maskedActivity!=Activity.MainEntrance && maskedActivity!=Activity.FireExit)
            {
                random = UnityEngine.Random.RandomRangeInt(0, 2);
                switch (random)
                {
                    case 0:
                        maskedActivity = Activity.MainEntrance;
                        break;
                    case 1:
                        maskedActivity = Activity.FireExit;
                        break;
                }
            }
            else
            {
                //do idle stuff i guess.
                //maskedEnemy.LookAndRunRandomly();
            }
            /*if(Vector3.Distance(maskedEnemy.transform.position, maskedEnemy.mainEntrancePosition) < 10f)
            {
                start = 2;
            }    
            if (maskedEnemy.isInsidePlayerShip && start<2)
            {
                random = UnityEngine.Random.RandomRangeInt(0, 2);
                switch (random)
                {
                    case 0:
                        maskedActivity = Activity.MainEntrance;
                        break;
                    case 1:
                        maskedActivity = Activity.FireExit;
                        break;
                }
            }
            else if (maskedEnemy.isOutside && !maskedEnemy.isInsidePlayerShip)
            {
                random = UnityEngine.Random.RandomRangeInt(start, 3);
                switch (random)
                {
                    case 0:
                        maskedActivity = Activity.MainEntrance;
                        break;
                    case 1:
                        maskedActivity = Activity.FireExit;
                        break;
                    case 2:
                        maskedActivity = Activity.ItemLocker;
                        break;
                }
            }
            else if (!maskedEnemy.isOutside)
            {
                random = UnityEngine.Random.RandomRangeInt(start, 3);
                switch (random)
                {
                    case 0:
                        maskedActivity = Activity.MainEntrance;
                        break;
                    case 1:
                        maskedActivity = Activity.FireExit;
                        break;
                    case 2:
                        maskedActivity = Activity.BreakerBox;
                        break;
                }
            }
            else
            { 
                ///idk be lazy, do nothing.
            }*/
        }

        public void Update()
        {
            if (Plugin.imperiumFound)
            {
                ImperiumPatches.maskedVisualization(maskedPersonality, maskedActivity, maskedFocus);
            }
            MaskedStatusReport();
            if (((EnemyAI)maskedEnemy).isEnemyDead && isHoldingObject)
            {
                maskedGoal = "died, dropping objects!";
                closestGrabbable.parentObject = null;
                closestGrabbable.isHeld = false;
                closestGrabbable.isHeldByEnemy = false;
                isHoldingObject = false;
            }
            if (__instance.isEnemyDead)
            {
                maskedGoal = "none, he died!";
                ((Behaviour)agent).enabled = false;
                return; //stops them doing anything if they are dead?
            }
            if (useWalkie.Value)
            {
                maskedGoal = "use walkie";
                HoldWalkie();
                useWalkie.Value = false;
            }
            if (GameNetworkManager.Instance.isHostingGame)
            {
                //for testing purposes only
                //TestConfig();
                if (maskedPersonality == Personality.None)
                {
                    //SelectPersonalityInt.Value = Random.Range(0, Enum.GetNames(typeof(Personality)).Length);
                    SelectPersonalityInt.Value = Random.Range(0, useablePersonalities.Count);
                    //Plugin.mls.LogError(useablePersonalities.Count);
                }
                maskedPersonality = useablePersonalities[SelectPersonalityInt.Value];
                /*if (SelectPersonalityInt.Value == 0)
                {
                    maskedPersonality = Personality.Aggressive;
                }
                else if (SelectPersonalityInt.Value == 1)
                {
                    maskedPersonality = Personality.Cunning;
                }
                else if (SelectPersonalityInt.Value == 2)
                {
                    maskedPersonality = Personality.Deceiving;
                }
                else if (SelectPersonalityInt.Value == 3)
                {
                    maskedPersonality = Personality.Stealthy;
                }
                else if (SelectPersonalityInt.Value == 4)
                {
                    maskedPersonality = Personality.Insane;
                }*/
                if (lastMaskedPersonality != maskedPersonality)
                {
                    maskedGoal = "selecting new personality";
                    lastMaskedPersonality = maskedPersonality;
                    if (maskedPersonality == Personality.Deceiving)
                    {
                        SyncTermianlInt(15);
                    }
                    else if (maskedPersonality == Personality.Insane)
                    {
                        SyncTermianlInt(10);
                    }
                    Plugin.mls.LogInfo("Masked '" + maskedId + "' personality changed to '" + maskedPersonality.ToString() + "'");
                    mustChangeFocus = true;
                    mustChangeActivity = true;
                }
            }
            if (!((Component)this).TryGetComponent<NavMeshAgent>(out agent))
            {
                agent = ((Component)this).GetComponent<NavMeshAgent>();
            }
            if ((Plugin.wendigosIntegrated || Plugin.skinWalkersIntegrated) && ((NetworkBehaviour)this).IsHost && (maskedPersonality == Personality.Deceiving || maskedPersonality == Personality.Insane || maskedPersonality==Personality.Stealthy))
            {
                maskedGoal = "using walkietalkie! (skinwalkers/Wendigos integration)";
                useWalkie.Value = true;
            }
            if ((Object)(object)creatureAnimator == (Object)null)
            {
                Plugin.mls.LogError((object)"VariableDeclarationClass.Update():  creatureAnimator is null!");
                return;
            }
            if ((Object)(object)agent == (Object)null)
            {
                Plugin.mls.LogError((object)"VariableDeclarationClass.Update():  __agent is null!");
                return;
            }
            if ((Object)(object)__instance == (Object)null)
            {
                Plugin.mls.LogError((object)"VariableDeclarationClass.Update():  __instance is null!");
                return;
            }
            MovementAnimationSelector(creatureAnimator, maskedEnemy);
            ItemAnimationSelector(creatureAnimator, __instance);
            CalculatingVariables();
            DetectAndSelectRandomPlayer();
            TargetAndKillPlayer(); //potentially, this should change the focus to NONE and "Activity" to killing the player..depending on the personality, this should cancel current activity as well.
            if (mustChangeFocus)
            {
                SetFocus();
            }
            if (maskedFocus != Focus.None)
            {
                if (maskedPersonality == Personality.Cunning)
                {
                    maskedGoal = "trying to be cunning...";
                    MaskedCunning();
                }
                else if (maskedPersonality == Personality.Deceiving)
                {
                    maskedGoal = "trying to be deceiving...";
                    MaskedDeceiving();
                }
                else if (maskedPersonality == Personality.Aggressive)
                {
                    maskedGoal = "trying to be aggressive...";
                    MaskedAggressive();
                }
                else if (maskedPersonality == Personality.Stealthy)
                {
                    maskedGoal = "trying to be stealthy...";
                    MaskedStealthy();
                }
                else if (maskedPersonality == Personality.Insane)
                {
                    maskedGoal = "trying to be insane...";
                    MaskedInsane();
                }
                else
                {
                    Plugin.mls.LogError("Personality = None???");
                }
            }
            else if(maskedFocus == Focus.None && maskedActivity!=Activity.None)
            {
                if (maskedActivity == Activity.ItemLocker)
                //if (maskedPersonality == Personality.Deceiving || maskedPersonality == Personality.Cunning || maskedPersonality == Personality.Insane)
                {
                    maskedGoal = "pathing to ship locker";
                    findLockerAudio();
                }
                else if (maskedActivity == Activity.MainEntrance)
                {
                    maskedGoal = "pathing to main entrance";
                    findMainEntrance();
                }
                else if (maskedActivity == Activity.FireExit)
                {
                    maskedGoal = "pathing to main entrance (because fire exits arent implemented yet :D)";
                    //not done this yet
                    maskedActivity = Activity.MainEntrance;
                    //findFireExit();
                }
                else if (maskedActivity == Activity.BreakerBox)
                {
                    maskedGoal = "pathing to breaker box";
                    findBreakerBox();
                }
                else if (maskedActivity == Activity.RandomPlayer)
                {
                    maskedGoal = "finding random player";
                    findRandomPlayer();
                }
            }
                /*if (Plugin.useTerminal && (maskedPersonality == Personality.Cunning || maskedPersonality == Personality.Deceiving || maskedPersonality == Personality.Insane) && !noMoreTerminal)
                {
                //moved to fit under individual personalities
                    maskedGoal = "useTerminal";
                    //Plugin.mls.LogDebug("Masked '" + maskedId + "' is '" + maskedPersonality.ToString() + "' so can use the terminal."); //too much spam!
                    UsingTerminal();
                }*/
                /*if (((EnemyAI)maskedEnemy).isInsidePlayerShip && isHoldingObject && maskedPersonality == Personality.Cunning)
                {
                    dropItem.Value = true; //cunning shouldent be dropping items in the ship WTF
                }*/

                /*          else if (!__instance.isInsidePlayerShip && !isHoldingObject)
                          {
                              if (maskedPersonality != Personality.Cunning)
                              {
                                  maskedGoal = "(not cunning)grabbing an item";
                                  GrabItem();
                              }
                          }
                      }*/
            //when should they tbag? disabling as they tbag at random times rn..
            /*if (maskedEnemy.stopAndStareTimer >= 0f && stopAndTbagCooldown <= 0f && !__instance.isEnemyDead)
            {
                maskedGoal = "stop, stare and tbag";
                if (GameNetworkManager.Instance.isHostingGame)
                {
                    if (stopAndTbagTimer <= 0f)
                    {
                        randomPose = Random.Range(0, 4);
                    }
                    stopAndTbagTimer -= Time.deltaTime;
                    if (randomPose == 0)
                    {
                        if (stopAndTbagTimer < 1f && stopAndTbagTimer > 0.8f)
                        {
                            isCrouched.Value = true;
                        }
                        else if (stopAndTbagTimer < 0.8f && stopAndTbagTimer > 0.6f)
                        {
                            isCrouched.Value = false;
                        }
                        else if (stopAndTbagTimer < 0.6f && stopAndTbagTimer > 0.4f)
                        {
                            isCrouched.Value = true;
                        }
                        else if (stopAndTbagTimer > 0f && stopAndTbagTimer < 0.4f)
                        {
                            isCrouched.Value = false;
                            stopAndTbagCooldown = 10f;
                        }
                    }
                    else if (randomPose == 1 && maskedPersonality != Personality.Aggressive)
                    {
                        if (stopAndTbagTimer < 1.1f && stopAndTbagTimer > 0.8f)
                        {
                            isDancing.Value = true;
                        }
                        else if (stopAndTbagTimer < 0.7f && stopAndTbagTimer > 0.2f)
                        {
                            isDancing.Value = false;
                            isCrouched.Value = true;
                        }
                        else if (stopAndTbagTimer > 0f && stopAndTbagTimer < 0.2f)
                        {
                            stopAndTbagCooldown = 10f;
                            isCrouched.Value = false;
                        }
                    }
                }
            }
            else
            {
                maskedGoal = "else part of tbagging";
                stopAndTbagTimer = 2.5f;
                if (stopAndTbagCooldown > 0f)
                {
                    stopAndTbagCooldown -= Time.deltaTime;
                }
            }*/
            /*if (maskedPersonality == Personality.Cunning)
            {
                maskedGoal = "if cunning.."; //well way too late for this...
                lookTimer += Time.deltaTime;
                if (lookTimer < 1f && !lookedPlayer)
                {
                    LookAtPos(((Component)((EnemyAI)maskedEnemy).targetPlayer).transform.position, 2.5f);
                    lookedPlayer = true;
                }
                if (lookTimer > 5f)
                {
                    lookTimer = 0f;
                    lookedPlayer = false;
                }
            }*/
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), "OnCollideWithPlayer")]
        [HarmonyPrefix]
        private static bool OnCollideWithPlayer_Prefix()
        {
            return false;
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), "LookAtPosition")]
        [HarmonyPrefix]
        private static bool LookAtPosition_Prefix()
        {
            return false;
        }

        public void LookAtPos(Vector3 pos, float lookAtTime = 1f)
        {
            maskedGoal = "lookatpos";
            //IL_0006: Unknown result type (might be due to invalid IL or missing references)
            //IL_0023: Unknown result type (might be due to invalid IL or missing references)
            //IL_0024: Unknown result type (might be due to invalid IL or missing references)
            //IL_003b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0040: Unknown result type (might be due to invalid IL or missing references)
            //IL_0047: Unknown result type (might be due to invalid IL or missing references)
            //IL_004c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0057: Unknown result type (might be due to invalid IL or missing references)
            //IL_0068: Unknown result type (might be due to invalid IL or missing references)
            bool canSeePos = __instance.CheckLineOfSightForPosition(pos, 160f, 40, -1, null);
            if (canSeePos)
            {
                maskedGoal = $"Look at position {pos} called! lookatpositiontimer setting to {lookAtTime}";
                maskedEnemy.focusOnPosition = pos;
                maskedEnemy.lookAtPositionTimer = lookAtTime;
                float num = Vector3.Angle(((Component)this).transform.forward, pos - ((Component)this).transform.position);
                if (pos.y - maskedEnemy.headTiltTarget.position.y < 0f)
                {
                    num *= -1f;
                }
                maskedEnemy.verticalLookAngle = num;
            }
            else
            {
                maskedGoal = $"Look at position {pos} failed! Cannot see target, walking normally!";
                //these lines seemingly are not needed, as the default directioning is enough.
                //maskedEnemy.LookAtDirection(originDestination);
                //maskedEnemy.lookAtPositionTimer = lookAtTime;
            }
        }

        [HarmonyPatch(typeof(MaskedPlayerEnemy), "LookAtPlayerServerRpc")]
        [HarmonyPrefix]
        private static bool LookAtPlayerServerRpc_Prefix()
        {
            return false;
        }

        private void setNearestGrabbable()
        {
            NavMeshHit hitGrabbable;
            NavMeshPath nmpNearGrabbable = new NavMeshPath();
            List<GrabbableObject> allItemsList;
            float num = float.PositiveInfinity;
            /*if (maskedPersonality == Personality.Cunning)
            {
                //cunning items
                //filtering out only items cunning wants (in the ship, this doesnt work as "in the ship" only takes from items in the ship before the round starts.)
                //allItemsList = GlobalItemList.Instance.allitems.FindAll(item => item.isInShipRoom == true).ToList();
                //filtering out only items cunning wants (near the ship)
                allItemsList = GlobalItemList.Instance.allitems.Where(item => (Vector3.Distance(item.transform.position, ((Component)terminal).transform.position) < 15f || item.name == "Walkie-talkie")).ToList();
            }
            else if (maskedPersonality == Personality.Deceiving)
            {
                allItemsList = GlobalItemList.Instance.allitems.Where(item => (Vector3.Distance(item.transform.position, ((Component)terminal).transform.position) > 30f || item.name=="Walkie-talkie")).ToList();
            }
            else
            {*/
                //all items
                allItemsList = GlobalItemList.Instance.allitems;
            //}

            if (allItemsList.Count == 0)
            {
                Plugin.mls.LogDebug("allItemsList is empty so setting no more items=true");
                noMoreItems = true;
                mustChangeFocus = true;
            }

            //items to ignore because they shouldent be touched.
            /*allItemsList.Remove(allItemsList.Find(cbm => cbm.name == "ClipboardManual")); //removing clipboard from the list
            allItemsList.Remove(allItemsList.Find(sn => sn.name == "StickyNoteItem")); //removing sticky note on the wall from the list)
            allItemsList.RemoveAll(rlh => rlh.name == "RedLocustHive");*/ //removing red locust bee hive as its held by an enemy by default and cunning just stands there looking at it
                                                                        //allItemsList.Remove(allItemsList.Find(rlh => rlh.name == "RedLocustHive")); 
                                                                        //closestGrabbable = null;
                                                                        //allItemsList.OrderBy(x => x.scrapValue); // dont use but this allows sorting by scrap value in future.


            //wont work.. needs a lot of recoding to make work i think but should be better going forward
            //  closestGrabbable = allItemsList.Aggregate((curMin, x) => (
            //    curMin == null ||
            //    Vector3.Distance(this.transform.position, curMin.transform.position) > 20f ||
            //    curMin.isHeld ||
            //    curMin.isHeldByEnemy ||
            //    //need to check it item is in a bush here.. needs some recoding..
            //    (Vector3.Distance(this.transform.position, x.transform.position)) < Vector3.Distance(this.transform.position, curMin.transform.position)
            //   ) ? x : curMin);
            foreach (GrabbableObject allitem in allItemsList)
            {
                //Plugin.mls.LogDebug("allitem = "+allitem);
                //null reference exception fix here
                if ((Component)this == null)
                {
                    //Plugin.mls.LogDebug("SetNearestGrabbable() NullReferenceFix - Masked Entity was NULL");
                    return;
                }
                else if (((Component)this).transform.position == null)
                {
                    //Plugin.mls.LogDebug("SetNearestGrabbable() NullReferenceFix - Masked Entity position was NULL");
                    //Plugin.mls.LogDebug("this.name = " + ((Component)this).name.ToString());
                    return;
                }
                if ((Component)allitem == null)
                {
                    //Plugin.mls.LogDebug("SetNearestGrabbable() NullReferenceFix - Item Entity was NULL");
                    continue;
                }
                else if (((Component)allitem).transform.position == null)
                {
                    //Plugin.mls.LogDebug("SetNearestGrabbable() NullReferenceFix - Item Entity position was NULL");
                    //Plugin.mls.LogDebug("allitem.name = " + ((Component)allitem).name.ToString());
                    continue;
                }
                //null reference exception fix above

                //ignore items from the list after checking if they are null.
                if(allitem.name== "ClipboardManual" || allitem.name == "StickyNoteItem")
                {
                    continue;
                }
                if (allitem.name == "RedLocustHive")
                {
                    continue;
                }

                //ignore non-"cunning" items if cunning
                if(maskedPersonality == Personality.Cunning && Vector3.Distance(allitem.transform.position, ((Component)terminal).transform.position) >= 15f)
                {
                    continue;
                }

                //ignore non-deceiving items if deceiving
                if (maskedPersonality == Personality.Deceiving && Vector3.Distance(allitem.transform.position, ((Component)terminal).transform.position) <= 30f)
                {
                    continue;
                }


                //end removing items

                float num2 = Vector3.Distance(((Component)this).transform.position, ((Component)allitem).transform.position);
                if (allitem.GetComponent<CheckItemCollision>() != null)
                {
                    itemSystem = allitem.GetComponent<CheckItemCollision>();
                }
                if (itemSystem.hidedByMasked)
                {
                    //Plugin.mls.LogDebug("item was hidden by the masked, ignore this item");
                    continue;
                }
                bool isReachable = agent.CalculatePath(allitem.transform.position, nmpNearGrabbable); //because we only want items from the FLOOR
                if (!isReachable)
                {
                    //Plugin.mls.LogDebug("item is not reachable and closestgrabbable is null");
                    continue;
                }
                if (!(num2 < num) || !(num2 <= 30f) || allitem.isHeld || allitem.isHeldByEnemy || notGrabClosestItem)
                {
                    //Plugin.mls.LogDebug("item is either, not closest, further than 30f away, is held, is held by an enemy, or told not to grab closest item");
                    continue;
                }

                num = num2;
                nearestGrabbable = allitem;
                if (NavMesh.SamplePosition(nearestGrabbable.transform.position, out hitGrabbable, 10, -1))
                {
                    //closestGrabbableReachable = agent.CalculatePath(hitGrabbable.position, nmpGrabbable);
                    nearestGrabbableReachable = agent.CalculatePath(nearestGrabbable.transform.position, nmpNearGrabbable); 
                    nearestGrabbableDistance = Vector3.Distance(((Component)this).transform.position, ((Component)nearestGrabbable).transform.position);
                    //grabbableClosestPoint = Vector3.Distance(hitGrabbable.position, nearestGrabbable.transform.position);
                }
                else
                {
                    nearestGrabbableReachable = false;
                    nearestGrabbableDistance = 1000f;
                }
            }
        }

        private void DetectAndSelectRandomPlayer()
        {
            PlayerControllerB[] players = maskedEnemy.GetAllPlayersInLineOfSight(160f, 60, null, -1, -1);
            if(players==null)
            {
                return;
            }
            if (maskedFocus == Focus.Player || (maskedFocus == Focus.None && maskedActivity == Activity.RandomPlayer) || maskedEnemy.inSpecialAnimation || players.Length == 0)
            {
                //dont use this to find or focus on players as you are already found a player
                return;
            }
            maskedGoal = "Detecting Players";
            int extras = 0;
            if (maskedPersonality == Personality.Cunning) { extras = players.Length; }
            else if (maskedPersonality == Personality.Deceiving) { extras = players.Length * 2; }
            else if (maskedPersonality == Personality.Stealthy) { extras = players.Length * 4; }
            else if (maskedPersonality == Personality.Insane) { extras = 2; }
            else if (maskedPersonality == Personality.Aggressive) { extras = 1; }
            int random = UnityEngine.Random.RandomRangeInt(0,players.Length+extras);
            if(random >=players.Length)
            {
                //target no one..
            }
            else
            {
                //target the random player
                mustChangeActivity = false;
                mustChangeFocus = false;
                maskedFocus = Focus.None;
                maskedActivity = Activity.RandomPlayer;
                mustChangeActivity = false;
                mustChangeFocus = false;
                __instance.targetPlayer = players[random];
            }
        }

        public void DetectEnemy()
        {
            maskedGoal = "detectEnemy";
            //IL_0026: Unknown result type (might be due to invalid IL or missing references)
            //IL_0031: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_0084: Unknown result type (might be due to invalid IL or missing references)
            foreach (GrabbableObject allitem in GlobalItemList.Instance.allitems)
            {
                float num = Vector3.Distance(((Component)this).transform.position, ((Component)allitem).transform.position);
                if (num < float.PositiveInfinity && num <= 10f && ((NetworkBehaviour)this).IsHost)
                {
                    if (num > 0.5f)
                    {
                        __instance.SetDestinationToPosition(((Component)closestGrabbable).transform.position, true);
                        __instance.moveTowardsDestination = true;
                        __instance.movingTowardsTargetPlayer = false;
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                        __instance.moveTowardsDestination = false;
                    }
                }
            }
        } //not sure this is needed, we shall see.

        public void ManuelGrabItem(GrabbableObject item)
        {
            maskedGoal = "manuelGrabItem";
            //IL_0018: Unknown result type (might be due to invalid IL or missing references)
            //IL_0023: Unknown result type (might be due to invalid IL or missing references)
            //IL_0049: Unknown result type (might be due to invalid IL or missing references)
            //IL_0059: Unknown result type (might be due to invalid IL or missing references)
            //IL_0069: Unknown result type (might be due to invalid IL or missing references)
            //IL_006e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0084: Unknown result type (might be due to invalid IL or missing references)
            //IL_0099: Unknown result type (might be due to invalid IL or missing references)
            if (isHoldingObject)
            {
                return;
            }
            float num = Vector3.Distance(((Component)this).transform.position, ((Component)item).transform.position);
            if (num < 0.9f)
            {
                float num2 = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
                if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
                {
                    num2 *= -1f;
                }
                maskedEnemy.verticalLookAngle = num2;
                item.parentObject = itemHolder.transform;
                item.hasHitGround = false;
                item.isHeld = true;
                item.isHeldByEnemy = true;
                item.grabbable = false;
                isHoldingObject = true;
                itemDroped = false;
                item.GrabItemFromEnemy(__instance);
            }
            if (num < 4f && !isHoldingObject && maskedPersonality != Personality.Aggressive && ((NetworkBehaviour)this).IsHost)
            {
                isCrouched.Value = true;
            }
        }

        public void ForceGrabCustomItem(GrabbableObject item)
        {
            maskedGoal = "forcegrabcustomitem";
            //IL_000c: Unknown result type (might be due to invalid IL or missing references)
            //IL_001c: Unknown result type (might be due to invalid IL or missing references)
            //IL_002c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0031: Unknown result type (might be due to invalid IL or missing references)
            //IL_0047: Unknown result type (might be due to invalid IL or missing references)
            //IL_005c: Unknown result type (might be due to invalid IL or missing references)
            float num = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
            if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
            {
                num *= -1f;
            }
            maskedEnemy.verticalLookAngle = num;
            item.parentObject = itemHolder.transform;
            item.hasHitGround = false;
            item.isHeld = true;
            item.isHeldByEnemy = true;
            item.grabbable = false;
            isHoldingObject = true;
            itemDroped = false;
            item.GrabItemFromEnemy(__instance);
        }

        //pre-focus methods that should always run

        public void MovementAnimationSelector(Animator creatureAnimator,MaskedPlayerEnemy maskedEnemy)
        {
            /*if(Vector3.Distance(maskedEnemy.transform.position,maskedEnemy.destination)<2.5f)
            {
                maskedEnemy.running = false; // trying to prevent a masked stampede!
            }*/
            //selecting animation
            if (isCrouched.Value)
            {
                //crouched
                isDancing.Value = false;
                creatureAnimator.ResetTrigger("Dancing");
                maskedEnemy.running = false;
                creatureAnimator.ResetTrigger("Running");
                creatureAnimator.SetTrigger("Crouching");
                agent.speed = 1.9f;
            }
            else if (maskedEnemy.running)
            {
                //running
                isCrouched.Value = false;
                creatureAnimator.ResetTrigger("Crouching");
                isDancing.Value = false;
                creatureAnimator.ResetTrigger("Dancing");
                creatureAnimator.SetTrigger("Running");
                maskedEnemy.staminaTimer -= Time.deltaTime * 0.05f;
                agent.speed = 7f;
            }
            else if (isDancing.Value)
            {
                //dancing
                isCrouched.Value = false;
                creatureAnimator.ResetTrigger("Crouching");
                maskedEnemy.running = false;
                creatureAnimator.ResetTrigger("Running");
                creatureAnimator.SetTrigger("Dancing");
                __instance.SetDestinationToPosition(((Component)__instance).transform.position, false);
                agent.speed = 0f;
                Plugin.mls.LogInfo((object)"Dancing");
            }
            else
            {
                //normal
                isCrouched.Value = false;
                creatureAnimator.ResetTrigger("Crouching");
                isDancing.Value = false;
                creatureAnimator.ResetTrigger("Dancing");
                maskedEnemy.running = false;
                creatureAnimator.ResetTrigger("Running");
                agent.speed = 3.8f;
            }
            if (maskedEnemy.staminaTimer >= 5f && !isStaminaDowned) //is this jumping???
            {
                if (!isJumped.Value)
                {

                    isJumped.Value = true;
                }
                else
                {
                    creatureAnimator.ResetTrigger("FakeJump");
                }
                maskedEnemy.running = true;
            }
            if (maskedEnemy.staminaTimer < 0f)
            {
                isStaminaDowned = true;
                maskedEnemy.running = false;
                ((EnemyAI)maskedEnemy).creatureAnimator.SetTrigger("Tired");
            }
            if (isStaminaDowned)
            {
                maskedEnemy.staminaTimer += Time.deltaTime * 0.2f;
                if (maskedEnemy.staminaTimer < 3f)
                {
                    isStaminaDowned = false;
                    ((EnemyAI)maskedEnemy).creatureAnimator.ResetTrigger("Tired");
                }
            }
            if(__instance.targetPlayer!=null)
            {
                LookAtPos(((Component)((EnemyAI)maskedEnemy).targetPlayer).transform.position, 0.5f);
            }
            //crouched
            //running
            //dancing
            //normal
            //old animation logic
            /*if (isCrouched.Value)
            {
                creatureAnimator.SetTrigger("Crouching");
            }
            else
            {
                creatureAnimator.ResetTrigger("Crouching");
            }
            if (isCrouched.Value && !maskedEnemy.running)
            {
                agent.speed = 1.9f;
            }
            else if (maskedEnemy.running)
            {
                //((EnemyAI)maskedEnemy).creatureAnimator.SetBool("Running", true);
                ((EnemyAI)maskedEnemy).creatureAnimator.SetTrigger("Running"); //issue#3 fix? masked run normally now?
                MaskedPlayerEnemy obj = maskedEnemy;
                obj.staminaTimer -= Time.deltaTime * 0.05f;
                agent.speed = 7f;
            }
            if (isDancing.Value)
            {
                creatureAnimator.ResetTrigger("Crouching");
                ((EnemyAI)maskedEnemy).creatureAnimator.ResetTrigger("Running"); //issue#3 fix?
                creatureAnimator.SetTrigger("Dancing");
                __instance.SetDestinationToPosition(((Component)__instance).transform.position, false);
                agent.speed = 0f;
                Plugin.mls.LogInfo((object)"Dancing");
            }
            else if (!maskedEnemy.running && !isCrouched.Value)
            {
                agent.speed = 3.8f;
                creatureAnimator.ResetTrigger("Dancing");
                ((EnemyAI)maskedEnemy).creatureAnimator.ResetTrigger("Running"); //issue#3 fix?
            }*/
        }

        public void ItemAnimationSelector(Animator creatureAnimator, EnemyAI __instance)
        {
            if (isHoldingObject)
            {
                upperBodyAnimationsWeight = Mathf.Lerp(upperBodyAnimationsWeight, 0.9f, 25f * Time.deltaTime);
                creatureAnimator.SetLayerWeight(creatureAnimator.GetLayerIndex("Item"), upperBodyAnimationsWeight);
            }
            else
            {
                upperBodyAnimationsWeight = Mathf.Lerp(upperBodyAnimationsWeight, 0f, 25f * Time.deltaTime);
                creatureAnimator.SetLayerWeight(creatureAnimator.GetLayerIndex("Item"), upperBodyAnimationsWeight);
                creatureAnimator.SetLayerWeight(creatureAnimator.GetLayerIndex("Item"), upperBodyAnimationsWeight);
            }
            if (isHoldingObject && heldGrabbable.itemProperties.twoHandedAnimation && !(heldGrabbable is ShotgunItem))
            {
                creatureAnimator.SetTrigger("HoldLung");
                creatureAnimator.ResetTrigger("HoldFlash");
                creatureAnimator.ResetTrigger("HoldShotgun");
                creatureAnimator.ResetTrigger("HoldOneItem");
            }
            else if (isHoldingObject && !heldGrabbable.itemProperties.twoHandedAnimation && heldGrabbable is FlashlightItem)
            {
                creatureAnimator.SetTrigger("HoldFlash");
                creatureAnimator.ResetTrigger("HoldLung");
                creatureAnimator.ResetTrigger("HoldShotgun");
                creatureAnimator.ResetTrigger("HoldOneItem");
            }
            else if (isHoldingObject && !heldGrabbable.itemProperties.twoHandedAnimation && !(heldGrabbable is ShotgunItem) && !(heldGrabbable is FlashlightItem) && !(heldGrabbable is Shovel))
            {
                creatureAnimator.SetTrigger("HoldOneItem");
                creatureAnimator.ResetTrigger("HoldFlash");
                creatureAnimator.ResetTrigger("HoldLung");
                creatureAnimator.ResetTrigger("HoldShotgun");
            }
            else if (isHoldingObject && !heldGrabbable.itemProperties.twoHandedAnimation && heldGrabbable is Shovel)
            {
                creatureAnimator.SetTrigger("HoldLung");
                creatureAnimator.ResetTrigger("HoldOneItem");
                creatureAnimator.ResetTrigger("HoldFlash");
                creatureAnimator.ResetTrigger("HoldShotgun");
            }
            else if (isHoldingObject && heldGrabbable is ShotgunItem)
            {
                creatureAnimator.SetTrigger("HoldShotgun");
                creatureAnimator.ResetTrigger("HoldFlash");
                creatureAnimator.ResetTrigger("HoldLung");
                creatureAnimator.ResetTrigger("HoldOneItem");
            }
            else if (!isHoldingObject)
            {
                creatureAnimator.ResetTrigger("HoldFlash");
                creatureAnimator.ResetTrigger("HoldLung");
                creatureAnimator.ResetTrigger("HoldShotgun");
                creatureAnimator.ResetTrigger("HoldOneItem");
            }
        }

        public void CheckPathRotating(NavMeshAgent agent, EnemyAI __instance)
        {
            maskedGoal = "checkpathrotating";
            //IL_000f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0015: Expected O, but got Unknown
            //IL_0044: Unknown result type (might be due to invalid IL or missing references)
            //IL_0049: Unknown result type (might be due to invalid IL or missing references)
            //IL_0098: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ad: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ea: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ef: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_00f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ca: Unknown result type (might be due to invalid IL or missing references)
            //IL_0168: Unknown result type (might be due to invalid IL or missing references)
            //IL_0264: Unknown result type (might be due to invalid IL or missing references)
            //IL_027a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0284: Unknown result type (might be due to invalid IL or missing references)
            //IL_0222: Unknown result type (might be due to invalid IL or missing references)
            if (!((NetworkBehaviour)this).IsHost)
            {
                return;
            }
            NavMeshPath val = new NavMeshPath();
            int num = 1;
            if (agent.pathPending)
            {
                return;
            }
            if (agent.remainingDistance < agent.stoppingDistance)
            {
                if (!agent.hasPath)
                {
                    return;
                }
                Vector3 velocity = agent.velocity;
                if (((Vector3)(velocity)).sqrMagnitude == 0f)
                {
                    return;
                }
            }
            if (num >= agent.path.corners.Length || !((NetworkBehaviour)this).IsHost)
            {
                return;
            }
            Vector3 val2 = agent.path.corners[num] - ((Component)agent).transform.position;
            if (num + 1 < agent.path.corners.Length)
            {
                Vector3 val3 = agent.path.corners[num + 1] - agent.path.corners[num];
                float num2 = Vector3.Angle(val2, val3);
                if (num2 > 30f)
                {
                    rotationTimer += Time.deltaTime;
                    if (rotationTimer > 0f && rotationTimer < 0.5f)
                    {
                        if (angle1 == 0f)
                        {
                            angle1 = ((Component)__instance).transform.localEulerAngles.y + 10f;
                        }
                        Plugin.mls.LogInfo((object)("angle1: " + angle1));
                        ((Component)__instance).transform.localEulerAngles = new Vector3(((Component)__instance).transform.localEulerAngles.x, angle1, ((Component)__instance).transform.localEulerAngles.z);
                    }
                    else if ((double)rotationTimer > 0.5 && rotationTimer < 1.1f)
                    {
                        if (angle2 == 0f)
                        {
                            angle1 = ((Component)__instance).transform.localEulerAngles.y - 5f;
                        }
                        Plugin.mls.LogInfo((object)("angle2: " + angle2));
                        ((Component)__instance).transform.localEulerAngles = new Vector3(((Component)__instance).transform.localEulerAngles.x, angle1, ((Component)__instance).transform.localEulerAngles.z);
                    }
                    //original log message in korean
                    //LethalIntelligence.mls.LogWarning((object)"곧 30도이상 회전");
                    Plugin.mls.LogWarning((object)"Soon to rotate more than 30 degrees");   
                }
                else
                {
                    rotationTimer = 0f;
                    angle1 = 0f;
                    angle2 = 0f;
                }
            }
            if (((Vector3)(val2)).magnitude <= agent.stoppingDistance)
            {
                //original log message in korean
                //LethalIntelligence.mls.LogWarning((object)"코너에 거의 도착했으며 다음 코너를 검사하기 위해 인덱스 증가");
                Plugin.mls.LogWarning((object)"Almost reached corner, increase index to check next corner");
                num++;
            }
        }

        private void HoldWalkie()
        {
            maskedGoal = "holdwalkie";
            if ((Plugin.wendigosIntegrated || Plugin.skinWalkersIntegrated) && isHoldingObject && closestGrabbable is WalkieTalkie)
            {
                WalkieTalkie component = ((Component)closestGrabbable).GetComponent<WalkieTalkie>();
                walkieCooldown += Time.deltaTime;
                if (walkieCooldown < 1f)
                {
                    creatureAnimator.ResetTrigger("UseWalkie");
                    walkieUsed = false;
                    walkieVoiceTransmitted = false;
                    walkieTimer = 0f;
                }
                else if (walkieCooldown < 4f && walkieCooldown > 3f && !((GrabbableObject)component).isBeingUsed)
                {
                    ((GrabbableObject)component).isBeingUsed = true;
                }
                if (walkieCooldown > 10f)
                {
                    UseWalkie();
                }
            }
        }
        #region wendigosCompatibility
        int timeToPlay = 20;
        int currentTime = 0;
        private AudioClip wendigosClip()
        {
            AudioClip clipToPlay;
            List<AudioClip> clips = Wendigos.Plugin.audioClips[maskedEnemy.mimickingPlayer.playerClientId];
            int totalClips = clips.Count;
            int randomNumber = UnityEngine.Random.RandomRangeInt(0, totalClips);
            clipToPlay = clips[randomNumber];
            return clipToPlay;
        }
        private bool wendigosTimer()
        {
            if (currentTime >= timeToPlay)
            {
                currentTime = 0;
                return true;
            }
            else
            {
                currentTime++;
                return false;
            }
        }
        #endregion wendigosCompatibility

        public void UseWalkie()
        {
            maskedGoal = "usewalkie";
            if ((!Plugin.wendigosIntegrated && !Plugin.skinWalkersIntegrated) || !isHoldingObject || !(closestGrabbable is WalkieTalkie))
            {
                return;
            }
            walkieTimer += Time.deltaTime;
            WalkieTalkie component = ((Component)closestGrabbable).GetComponent<WalkieTalkie>();
            if (walkieTimer > 1f && !walkieUsed)
            {
                if (!((GrabbableObject)component).isBeingUsed)
                {
                    ((GrabbableObject)component).isBeingUsed = true;
                    component.EnableWalkieTalkieListening(true);
                    ((Renderer)((GrabbableObject)component).mainObjectRenderer).sharedMaterial = component.onMaterial;
                    ((Behaviour)component.walkieTalkieLight).enabled = true;
                    component.thisAudio.PlayOneShot(component.switchWalkieTalkiePowerOn);
                }
                walkieUsed = true;
            }
            if (walkieTimer > 1.5f && !walkieVoiceTransmitted)
            {
                Plugin.mls.LogInfo((object)"Walkie Voice Transmitted!");
                foreach (WalkieTalkie allWalkieTalky in GlobalItemList.Instance.allWalkieTalkies)
                {
                    if (((GrabbableObject)allWalkieTalky).isBeingUsed)
                    {
                        allWalkieTalky.thisAudio.PlayOneShot(allWalkieTalky.startTransmissionSFX[Random.Range(0, allWalkieTalky.startTransmissionSFX.Length + 1)]);
                    }
                    if ((Object)(object)((Component)closestGrabbable).gameObject != (Object)(object)((Component)allWalkieTalky).gameObject && ((GrabbableObject)allWalkieTalky).isBeingUsed)
                    {
                        if (Plugin.skinWalkersIntegrated)
                        {
                            allWalkieTalky.target.PlayOneShot(SkinwalkerModPersistent.Instance.GetSample());
                        }
                        else if(Plugin.wendigosIntegrated)
                        {
                            if (wendigosTimer()) //should play an audio clip at random once every 20 updates, maybe thats too quick..
                            {
                                allWalkieTalky.target.PlayOneShot(wendigosClip());
                            }
                        }
                    }
                }
                creatureAnimator.SetTrigger("UseWalkie");
                walkieVoiceTransmitted = true;
            }
            if (!(walkieTimer > 5f))
            {
                return;
            }
            foreach (WalkieTalkie allWalkieTalky2 in GlobalItemList.Instance.allWalkieTalkies)
            {
                if (((GrabbableObject)allWalkieTalky2).isBeingUsed)
                {
                    allWalkieTalky2.thisAudio.PlayOneShot(allWalkieTalky2.stopTransmissionSFX[Random.Range(0, allWalkieTalky2.stopTransmissionSFX.Length + 1)]);
                }
                if ((Object)(object)((Component)closestGrabbable).gameObject == (Object)(object)((Component)allWalkieTalky2).gameObject && ((GrabbableObject)allWalkieTalky2).isBeingUsed)
                {
                    ((GrabbableObject)component).isBeingUsed = false;
                    component.EnableWalkieTalkieListening(false);
                    ((Renderer)((GrabbableObject)component).mainObjectRenderer).sharedMaterial = component.offMaterial;
                    ((Behaviour)component.walkieTalkieLight).enabled = false;
                    component.thisAudio.PlayOneShot(component.switchWalkieTalkiePowerOff);
                }
            }
            creatureAnimator.ResetTrigger("UseWalkie");
            walkieCooldown = 0f;
            walkieTimer = 0f;
        }

        public void AwayFromPlayer()
        {
            maskedGoal = "awayfromplayer() method";
            //IL_0024: Unknown result type (might be due to invalid IL or missing references)
            //IL_002f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0059: Unknown result type (might be due to invalid IL or missing references)
            //IL_0064: Unknown result type (might be due to invalid IL or missing references)
            //IL_01be: Unknown result type (might be due to invalid IL or missing references)
            //IL_009d: Unknown result type (might be due to invalid IL or missing references)
            //IL_00a8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ad: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c1: Unknown result type (might be due to invalid IL or missing references)
            //IL_00cb: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d5: Unknown result type (might be due to invalid IL or missing references)
            //IL_00d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_00e3: Unknown result type (might be due to invalid IL or missing references)
            //IL_0107: Unknown result type (might be due to invalid IL or missing references)
            //IL_0112: Unknown result type (might be due to invalid IL or missing references)
            //IL_00fb: Unknown result type (might be due to invalid IL or missing references)
            //IL_0100: Unknown result type (might be due to invalid IL or missing references)
            //IL_01aa: Unknown result type (might be due to invalid IL or missing references)
            //IL_018b: Unknown result type (might be due to invalid IL or missing references)
            //IL_016f: Unknown result type (might be due to invalid IL or missing references)
            PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;
            foreach (PlayerControllerB val in allPlayerScripts)
            {
                float num = Vector3.Distance(((Component)__instance).transform.position, ((Component)val).transform.position);
                if (!(num < float.PositiveInfinity))
                {
                    continue;
                }
                nearestPlayer = val;
                float num2 = Vector3.Distance(((Component)this).transform.position, ((Component)val).transform.position);
                if (num2 < 4f && (Object)(object)__instance.targetPlayer != (Object)null)
                {
                    Vector3 val2 = ((Component)this).transform.position - ((Component)val).transform.position;
                    Vector3 val3 = ((Component)this).transform.position + ((Vector3)(val2)).normalized * 5f;
                    if (originDestination != agent.destination)
                    {
                        originDestination = agent.destination;
                    }
                    if (Vector3.Distance(originDestination, agent.destination) < 1.5f)
                    {
                        originTimer += Time.deltaTime;
                    }
                    if (originTimer > 3.5f)
                    {
                        if (__instance.isOutside)
                        {
                            __instance.SetDestinationToPosition(maskedEnemy.shipHidingSpot, true);
                        }
                        else
                        {
                            __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                        }
                        originTimer = 0f;
                    }
                    __instance.SetDestinationToPosition(val3, true);
                }
                else
                {
                    __instance.SetDestinationToPosition(originDestination, true);
                }
            }
        }

        public void PlayerLikeAction()
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0017: Unknown result type (might be due to invalid IL or missing references)
            //IL_016f: Unknown result type (might be due to invalid IL or missing references)
            //IL_004c: Unknown result type (might be due to invalid IL or missing references)
            //IL_005c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0061: Unknown result type (might be due to invalid IL or missing references)
            //IL_0066: Unknown result type (might be due to invalid IL or missing references)
            //IL_006d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0074: Unknown result type (might be due to invalid IL or missing references)
            //IL_007e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0083: Unknown result type (might be due to invalid IL or missing references)
            //IL_0088: Unknown result type (might be due to invalid IL or missing references)
            //IL_008a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0095: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c4: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ad: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b2: Unknown result type (might be due to invalid IL or missing references)
            //IL_015c: Unknown result type (might be due to invalid IL or missing references)
            //IL_013d: Unknown result type (might be due to invalid IL or missing references)
            //IL_0121: Unknown result type (might be due to invalid IL or missing references)
            float num = Vector3.Distance(((Component)this).transform.position, ((Component)nearestPlayer).transform.position);
            if (num < 4f && (Object)(object)__instance.targetPlayer != (Object)null)
            {
                Vector3 val = ((Component)this).transform.position - ((Component)nearestPlayer).transform.position;
                Vector3 val2 = ((Component)this).transform.position + ((Vector3)(val)).normalized * 5f;
                if (originDestination != agent.destination)
                {
                    originDestination = agent.destination;
                }
                if (Vector3.Distance(originDestination, agent.destination) < 1.5f)
                {
                    originTimer += Time.deltaTime;
                }
                if (originTimer > 3.5f)
                {
                    if (__instance.isOutside)
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.shipHidingSpot, true);
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                    }
                    originTimer = 0f;
                }
                __instance.SetDestinationToPosition(val2, true);
            }
            else
            {
                __instance.SetDestinationToPosition(originDestination, true);
            }
        }

        //personality methods

        public void MaskedCunning() //cunning's special ability
        {
            //IL_0052: Unknown result type (might be due to invalid IL or missing references)
            //IL_005d: Unknown result type (might be due to invalid IL or missing references)
            /*breakerBoxReachable = maskedEnemy.SetDestinationToPosition(breakerBox.transform.position, true);
            breakerBoxDistance = Vector3.Distance(((Component)this).transform.position, ((Component)breakerBox).transform.position);
            terminalDistance = Vector3.Distance(((Component)this).transform.position, ((Component)terminal).transform.position);
            terminalReachable = maskedEnemy.SetDestinationToPosition(terminal.transform.position, true);*/
            //if (breakerBoxReachable)
            //{
            if (breakerBox.isPowerOn)
            {
                noMoreBreakerBox = false;
            }
            //}
            //else
            //{
            //    noMoreBreakerBox = true;
            //}
            if (maskedFocus == Focus.Terminal)
            {
                if (!noMoreTerminal && Plugin.useTerminal)
                {
                    //usingterminal
                    UsingTerminal();
                }
                else
                {
                    mustChangeFocus = true;
                }
            }
            else if (maskedFocus == Focus.BreakerBox)
            {
                if (!noMoreBreakerBox)
                {
                    //breakerbox
                    if (breakerBoxDistance < 40f || noMoreTerminal) //add logic for breaker box being turned off so if breaker box is turned OFF, then do nothing.
                    {
                        maskedGoal = "walking to breaker box";
                        //turn off the breaker box.
                        //noMoreTerminal = true;
                        //noMoreItems = true;
                        //focusingPersonality = true;
                        maskedEnemy.SetDestinationToPosition(breakerBox.transform.position);
                        maskedEnemy.moveTowardsDestination = true;

                        if (breakerBoxDistance < 4.0f && !isUsingBreakerBox)
                        {
                            maskedGoal = "using breaker box";
                            maskedEnemy.LookAtFocusedPosition();
                            isUsingBreakerBox = true;
                            creatureAnimator.ResetTrigger("Crouching");
                            //LookAtPos(breakerBox.transform.position, 8.5f);
                            //((Component)maskedEnemy.headTiltTarget).gameObject.SetActive(false);
                            isCrouched.Value = false;
                            agent.speed = 0f;
                            creatureAnimator.ResetTrigger("IsMoving");
                            if (breakerBox.isPowerOn)
                            {
                                breakerBoxSwitchLogic(breakerBoxDistance, false);
                            }
                            else
                            {
                                breakerBoxSwitchLogic(breakerBoxDistance, true);
                                maskedEnemy.SetDestinationToPosition(terminal.transform.position);
                            }
                        }
                    }
                }
                else
                {
                    mustChangeFocus = true;
                }
            }
            else if (maskedFocus == Focus.Items)
            {
                if (!noMoreItems)
                {
                    if (itemsToHide == 0)
                    {
                        maskedGoal = "changing focus as we have stolen enough items";
                        noMoreItems = true;
                        mustChangeFocus = true;
                        return;
                    }
                    //items
                    if (!isHoldingObject)
                    {
                        if (nearestGrabbable == null)
                        {
                            maskedGoal = "cant find any grabbable items, switching focus";
                            noMoreItems = true;
                            mustChangeFocus = true;
                        }
                        else
                        {
                            maskedGoal = "grabbing item from inside ship";
                            GrabItem();
                        }
                    }
                    else
                    {
                        isCrouched.Value = false; //stop crouching after picking up an item
                        if (!isHoldingObject || !((EnemyAI)maskedEnemy).isOutside || !((NetworkBehaviour)this).IsHost || bushes == null)
                        {
                            if (isHoldingObject && ((EnemyAI)maskedEnemy).isOutside && bushes == null)
                            {
                                Plugin.mls.LogDebug("Masked Cunning cannot find any bushes on this moon!");
                                noMoreItems = true;
                                mustChangeFocus = true;
                            }
                            return;
                        }
                        GameObject[] array = bushes;
                        array.ToList().RemoveAll(bush => bush.GetComponent<BushSystem>().bushWithItem); //removing all bushes that contain and item
                        if (array.Length == 0)
                        {
                            Plugin.mls.LogDebug("all bushes have an item, dropping item and changing focus");
                            dropItem.Value = true;
                            DropItem();
                            noMoreItems = true;
                            mustChangeFocus = true;
                        }
                        foreach (GameObject val in array)
                        {
                            bushDistance = Vector3.Distance(((Component)__instance).transform.position, val.transform.position);
                            //Plugin.mls.LogDebug("bushName = " + val.name + " / bush distance = " + bushDistance + " / bushwithitem = " + val.GetComponent<BushSystem>().bushWithItem);
                            if (bushDistance < float.PositiveInfinity && !val.GetComponent<BushSystem>().bushWithItem)
                            {
                                if (bushDistance > 2f && bushDistance < float.PositiveInfinity && !val.GetComponent<BushSystem>().bushWithItem)
                                {
                                    maskedGoal = "walking to bush";
                                    maskedEnemy.SetDestinationToPosition(val.transform.position, true);
                                    moveSpecial = true;
                                }
                                if (bushDistance < 2f)
                                {
                                    maskedGoal = "hiding item in bush";
                                    Plugin.mls.LogDebug("Cunning Is Hiding an Item");
                                    itemSystem.hidedByMasked = true;
                                    dropItem.Value = true;
                                    DropItem();
                                    itemsToHide--;
                                    mustChangeFocus = true;
                                    val.GetComponent<BushSystem>().bushWithItem = true;
                                    //findLockerAudio();
                                    //maskedEnemy.SetDestinationToPosition(terminal.transform.position); //stealing multiple items
                                    //focusingPersonality = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    mustChangeFocus = true;
                }
            }
        }

        public void MaskedDeceiving() //deceivings special ability
        {
            if (maskedFocus == Focus.Terminal)
            {
                if (!noMoreTerminal && Plugin.useTerminal)
                {
                    //usingterminal
                    UsingTerminal();
                }
                else
                {
                    mustChangeFocus = true;
                }
            }
            else if (maskedFocus == Focus.Items)
            {
                if (!noMoreItems)
                {
                    //items
                    if (!isHoldingObject)
                    {
                        if (closestGrabbable == null)
                        {
                            maskedGoal = "locating new grabbable item";
                            noMoreItems = true;
                            mustChangeFocus = true;
                        }
                        else
                        {
                            maskedGoal = "grabbing nearest item";
                            GrabItem();
                        }
                    }
                    else
                    {
                        isCrouched.Value = false; //stop crouching after picking up an item
                        if (maskedEnemy.isInsidePlayerShip)
                        {
                            float num2 = Vector3.Distance(((Component)this).transform.position, ((Component)terminal).transform.position);
                            if(num2>6f)
                            {
                                maskedGoal = "heading to player ship to drop an item";
                                maskedEnemy.SetDestinationToPosition(terminal.transform.position);
                            }
                            else if (num2 < 6f)
                            {
                                maskedGoal = "dropping item in player ship";
                                dropItem.Value = true;
                                DropItem();
                            }
                        }
                        else
                        {
                            findLockerAudio();
                        }
                    }
                }
                else
                {
                    mustChangeFocus = true;
                }
            }
            else if(maskedFocus == Focus.Player)
            {
                if (distanceToPlayer >= 17f)
                {
                    //find player
                    PlayerControllerB player = __instance.GetClosestPlayer();
                    __instance.SetMovingTowardsTargetPlayer(player);
                }
                else
                {
                    if (enableDance)
                    {
                        isDancing.Value = true;
                        maskedEnemy.stopAndStareTimer = 0.9f;
                        agent.speed = 0f;
                    }
                    if (distanceToPlayer < 17f && __instance.targetPlayer.performingEmote && maxDanceCount.Value > 0)
                    {
                        maskedGoal = "emoting with nearby player";
                        if (GameNetworkManager.Instance.isHostingGame && !enableDance)
                        {
                            LethalNetworkVariable<int> obj3 = maxDanceCount;
                            obj3.Value -= 1;
                            randomPose = 1;
                            enableDance = true;
                        }
                        stopAndTbagTimer = 0.9f;
                        __instance.agent.speed = 0f;
                    }
                    else if (isDancing.Value && GameNetworkManager.Instance.isHostingGame)
                    {
                        isDancing.Value = false;
                        stopAndTbagTimer = 0.4f;
                        randomPose = 1;
                        enableDance = false;
                    }
                    mustChangeFocus = true;
                }
            }
        }

        public void MaskedStealthy() //stealthys special ability
        {
            if(maskedFocus == Focus.Hiding)
            {
                if(((NetworkBehaviour)this).IsHost)
                {
                    maskedGoal = "going AwayFromPlayer()";
                    AwayFromPlayer();
                }
            }
            else if(maskedFocus == Focus.Mimicking)
            {
                if (((NetworkBehaviour)this).IsHost)
                {
                    maskedGoal = "doing PlayerLikeAction()";
                    PlayerLikeAction();
                }
            }
        }

        public void MaskedAggressive() //aggressives special ability
        {
            //nothing to put here yet
            if (maskedFocus == Focus.Items)
            {
                if (!noMoreItems)
                {
                    if (GlobalItemList.Instance.isShotgun)
                    {
                        maskedGoal = "grabbing a shotgun";
                        FindAndGrabShotgun();
                        if (isHoldingObject && closestGrabbable is ShotgunItem)
                        {
                            maskedGoal = "using a shotgun";
                            UseItem(((EnemyAI)maskedEnemy).targetPlayer, distanceToPlayer);
                        }
                    }
                    else
                    {
                        noMoreItems = true;
                    }
                }
            }
            else if (maskedFocus == Focus.Player)
            {
                //find the nearest player and kill them as fast as you can i guess? should change focus once player is dead
                PlayerControllerB pt = __instance.GetClosestPlayer();
                if (pt == null) 
                {
                    mustChangeFocus = true;
                    //maskedFocus = Focus.None;
                    mustChangeActivity = true;
                    return; 
                }
                __instance.SetMovingTowardsTargetPlayer(pt);
                if(Vector3.Distance(__instance.transform.position, pt.transform.position)<1f || pt.isPlayerDead)
                {
                    mustChangeFocus = true;
                    //maskedFocus = Focus.None;
                    mustChangeActivity = true;
                }
            }
        }

        public void MaskedInsane() //insanes special ability
        {
            if (maskedFocus == Focus.Terminal)
            {
                if (!noMoreTerminal && Plugin.useTerminal)
                {
                    //usingterminal
                    UsingTerminal();
                }
                else
                {
                    mustChangeFocus = true;
                }
            }
        }

        //Focused Activities

        //find and kill players

        private void TargetAndKillPlayer()
        {
            if ((Object)(object)__instance.targetPlayer == (Object)null && isHoldingObject)
            {
                maskedGoal = "not targeting player, holding object";
                dropTimer = 0f;
            }
            if (__instance.targetPlayer!=null)
            {
                maskedFocus = Focus.Player;

                if ((Object)(object)__instance.targetPlayer != (Object)null && isHoldingObject && !(closestGrabbable is Shovel) && !(closestGrabbable is ShotgunItem) && maskedPersonality == Personality.Aggressive)
                {
                    maskedGoal = "[Aggressive]targetting player while holding object(not shotgun/shovel)";
                    dropTimer += Time.deltaTime;
                    if (((NetworkBehaviour)this).IsHost)
                    {
                        if (!itemDroped)
                        {
                            dropTimerB = Random.Range(0.2f, 4f);
                            itemDroped = true;
                        }
                        if (dropTimer > dropTimerB)
                        {
                            dropItem.Value = true;
                        }
                    }
                }
                else if ((Object)(object)__instance.targetPlayer != (Object)null && isHoldingObject && (maskedPersonality == Personality.Deceiving || maskedPersonality == Personality.Insane))
                {
                    maskedGoal = "[Deceiving/Insane]targetting player while holding object";
                    dropTimer += Time.deltaTime;
                    if (((NetworkBehaviour)this).IsHost)
                    {
                        if (!itemDroped)
                        {
                            dropTimerB = Random.Range(0.2f, 4f);
                            itemDroped = true;
                        }
                        if (dropTimer > dropTimerB)
                        {
                            dropItem.Value = true;
                        }
                    }
                }
                if (isHoldingObject && (Object)(object)__instance.targetPlayer != (Object)null && closestGrabbable is StunGrenadeItem && maskedPersonality == Personality.Aggressive)
                {
                    maskedGoal = "[Aggressive]targetting player while holding object (stun grenade)";
                    StunGrenadeItem component = ((Component)closestGrabbable).GetComponent<StunGrenadeItem>();
                    if (distanceToPlayer < 8f && !stunThrowed)
                    {
                        stunThrowed = true;
                        creatureAnimator.SetTrigger("StunPin");
                        component.inPullingPinAnimation = true;
                        ((GrabbableObject)component).playerHeldBy.activatingItem = true;
                        ((GrabbableObject)component).playerHeldBy.doingUpperBodyEmote = 1.16f;
                        component.itemAnimator.SetTrigger("pullPin");
                        component.itemAudio.PlayOneShot(component.pullPinSFX);
                        WalkieTalkie.TransmitOneShotAudio(component.itemAudio, component.pullPinSFX, 0.8f);
                        component.inPullingPinAnimation = false;
                        component.pinPulled = true;
                        ((GrabbableObject)component).itemUsedUp = true;
                        ((GrabbableObject)component).grabbable = false;
                        Vector3 position = ((Component)component).transform.position;
                        component.grenadeThrowRay = new Ray(((Component)((GrabbableObject)component).playerHeldBy.gameplayCamera).transform.position, ((Component)((GrabbableObject)component).playerHeldBy.gameplayCamera).transform.forward);
                        position = !Physics.Raycast(component.grenadeThrowRay, out component.grenadeHit, 12f, StartOfRound.Instance.collidersAndRoomMaskAndDefault) ? ((Ray)(component.grenadeThrowRay)).GetPoint(10f) : ((Ray)(component.grenadeThrowRay)).GetPoint(((RaycastHit)(component.grenadeHit)).distance - 0.05f);
                        Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
                        component.grenadeThrowRay = new Ray(position, Vector3.down);
                        Vector3 val2 = !Physics.Raycast(component.grenadeThrowRay, out component.grenadeHit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault) ? ((Ray)(component.grenadeThrowRay)).GetPoint(30f) : (((RaycastHit)(component.grenadeHit)).point + Vector3.up * 0.05f);
                        closestGrabbable.parentObject = null;
                        ((Component)closestGrabbable).transform.SetParent(StartOfRound.Instance.propsContainer, true);
                        closestGrabbable.EnablePhysics(true);
                        closestGrabbable.fallTime = 0f;
                        closestGrabbable.startFallingPosition = ((Component)closestGrabbable).transform.parent.InverseTransformPoint(((Component)closestGrabbable).transform.position);
                        closestGrabbable.targetFloorPosition = ((Component)closestGrabbable).transform.parent.InverseTransformPoint(val2);
                        closestGrabbable.floorYRot = -1;
                        isHoldingObject = false;
                        closestGrabbable.DiscardItemFromEnemy();
                    }
                }
                if (!__instance.isEnemyDead && isHoldingObject && (Object)(object)__instance.targetPlayer != (Object)null && (maskedPersonality != Personality.Aggressive || (!(closestGrabbable is Shovel) && !(closestGrabbable is ShotgunItem))))
                {
                    maskedGoal = "not dead, holding object, targetting player (is not aggressive OR closest item is not a shovel/shotgun)";
                    if (__instance.isOutside)
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.shipHidingSpot, false);
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, false); //i really wish there was less sections of code doing this sort of thing :D
                    }
                }
                if ((Object)(object)__instance.targetPlayer != (Object)null)
                {
                    //targetPlayerReachable = __instance.SetDestinationToPosition(__instance.targetPlayer.transform.position, true); //needed when going towards a player
                    //checking distance to target player
                    distanceToPlayer = Vector3.Distance(((Component)creatureAnimator).transform.position, ((Component)__instance.targetPlayer).transform.position);
                    maskedEnemy.lookAtPositionTimer = 0f;
                }
                if (!((EnemyAI)maskedEnemy).isEnemyDead && !isUsingTerminal && (maskedPersonality != Personality.Aggressive || !isHoldingObject || (!(closestGrabbable is Shovel) && !(closestGrabbable is ShotgunItem))))
                {
                    PlayerControllerB[] allPlayerScripts = StartOfRound.Instance.allPlayerScripts;
                    foreach (PlayerControllerB val in allPlayerScripts)
                    {
                        float num = Vector3.Distance(((Component)val).transform.position, ((Component)this).transform.position);
                        if (num < 1f)
                        {
                            maskedGoal = "attempting to kill a player";
                            PlayerControllerB collidePlayer = maskedEnemy.MeetsStandardPlayerCollisionConditions(val.playerCollider, maskedEnemy.inKillAnimation || maskedEnemy.startingKillAnimationLocalClient || !maskedEnemy.enemyEnabled, false);
                            if (collidePlayer != null)
                            {
                                maskedEnemy.KillPlayerAnimationServerRpc((int)val.playerClientId);
                                maskedEnemy.startingKillAnimationLocalClient = true;
                                if (val.isCrouching)
                                {
                                    val.Crouch(false);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                //do nothing
            }
        }

            

        //terminal
        #region terminal
        private void UsingTerminal()
        {
            //IL_0007: Unknown result type (might be due to invalid IL or missing references)
            //IL_0017: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ab: Unknown result type (might be due to invalid IL or missing references)
            //IL_01bb: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d0: Unknown result type (might be due to invalid IL or missing references)
            //IL_01da: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f6: Unknown result type (might be due to invalid IL or missing references)
            //IL_0211: Unknown result type (might be due to invalid IL or missing references)
            //IL_022c: Unknown result type (might be due to invalid IL or missing references)
            //IL_023c: Unknown result type (might be due to invalid IL or missing references)
            //IL_00de: Unknown result type (might be due to invalid IL or missing references)
            float num = Vector3.Distance(((Component)this).transform.position, ((Component)terminal).transform.position);
            if (num < 40f)
            //if (num < 60)
            {
                dropItem.Value = true;
                if (num < 15f && num > 3.5f && Plugin.isTerminalBeingUsed)
                {
                    Plugin.mls.LogDebug("Terminal is being used by another entity");
                    mustChangeFocus = true;
                    return;
                }
                if (num < 5.5f && isHoldingObject)
                {
                    DropItem(); //drop inside the ship if you will use the terminal
                }

                if (!terminal.terminalInUse && !noMoreTerminal && num < 3.5f)
                {
                    Plugin.isTerminalBeingUsed = true;
                    if (!isUsingTerminal)
                    {
                        terminal.terminalAudio.PlayOneShot(terminal.enterTerminalSFX);
                    }
                    isUsingTerminal = true;
                }
                if (!terminal.terminalInUse && !noMoreTerminal && !__instance.isEnemyDead)
                {
                    terminalReachable = __instance.SetDestinationToPosition(((Component)terminal).transform.position, true);
                    if (!terminalReachable)
                    {
                        return; //cant reach terminal
                    }
                    maskedGoal = "walking to terminal";
                    __instance.SetDestinationToPosition(((Component)terminal).transform.position, false);
                    __instance.moveTowardsDestination = true;
                    //noMoreItems = true;
                    this.ignoringPersonality = true;
                }
            }
            isCrouched.Value = false;
            creatureAnimator.ResetTrigger("Crouching"); //temp fix for crouching animation issue
            if (isUsingTerminal)
            {
                maskedGoal = "using terminal";
                creatureAnimator.SetTrigger("Terminal");
                __instance.inSpecialAnimation = true;
                this.ignoringPersonality = false;
                terminal.placeableObject.inUse = true;
                ((Behaviour)terminal.terminalLight).enabled = true;
                __instance.movingTowardsTargetPlayer = false;
                __instance.targetPlayer = null;
                ((Component)maskedEnemy.headTiltTarget).gameObject.SetActive(false);
                agent.speed = 0f;
                creatureAnimator.ResetTrigger("IsMoving");
                ((Component)this).transform.LookAt(new Vector3(((Component)terminal).transform.position.x, ((Component)this).transform.position.y, ((Component)terminal).transform.position.z));
                ((Component)this).transform.localPosition = new Vector3(((Component)terminal).transform.localPosition.x + 7f, ((Component)terminal).transform.localPosition.y + 0.25f, ((Component)terminal).transform.localPosition.z + -14.8f);
                if (maskedPersonality == Personality.Cunning)
                {
                    if (terminal.numberOfItemsInDropship <= 0 && !dropship.shipLanded && dropship.shipTimer <= 0f && !isDeliverEmptyDropship && !noMoreTerminal)
                    {
                        dropShipTimer += Time.deltaTime;
                        if (dropShipTimer > 10f)
                        {
                            /*if (dropship.IsSpawned == false)
                            {
                                dropship.ShipLeave();
                            }*/
                            dropship.LandShipOnServer();
                            isDeliverEmptyDropship = true;
                        }
                    }
                    else if (isDeliverEmptyDropship && dropShipTimer <= 12f && !noMoreTerminal)
                    {
                        dropShipTimer += Time.deltaTime;

                    }
                    if (dropShipTimer > 12f)
                    {
                        Plugin.mls.LogDebug("Masked '" + maskedId + "' is '" + maskedPersonality.ToString() + " called in an item dropship.");
                        terminal.terminalAudio.PlayOneShot(terminal.leaveTerminalSFX);
                        __instance.inSpecialAnimation = false;
                        //noMoreItems = false;
                        this.ignoringPersonality = false;
                        __instance.SwitchToBehaviourState(2);
                        terminal.placeableObject.inUse = false;
                        terminal.terminalLight.enabled = false;
                        creatureAnimator.ResetTrigger("Terminal");
                        maskedEnemy.headTiltTarget.gameObject.SetActive(true);
                        //GrabItem();
                        //__instance.SetDestinationToPosition(GameObject.Find("ItemShip").transform.position, false); //doesnt actually route to the item ship, just uses it as a hook to get off i guess.
                        isUsingTerminal = false;
                        noMoreTerminal = true;
                        dropShipTimer = 0;
                    }
                }
                else if (maskedPersonality == Personality.Insane)
                {
                    if (!TerminalPatches.Transmitter.IsSignalTranslatorUnlocked())
                    {
                        //should play a cry/laugh here instead of doing nothing??
                        Plugin.mls.LogDebug("SignalTranslator is NOT unlocked :(");
                        terminal.terminalAudio.PlayOneShot(terminal.leaveTerminalSFX);
                        __instance.inSpecialAnimation = false;
                        this.ignoringPersonality = false;
                        __instance.SwitchToBehaviourState(2);
                        terminal.placeableObject.inUse = false;
                        terminal.terminalLight.enabled = false;
                        creatureAnimator.ResetTrigger("Terminal");
                        maskedEnemy.headTiltTarget.gameObject.SetActive(true);
                        //__instance.SetDestinationToPosition(GameObject.Find("ItemShip").transform.position, false); //doesnt actually route to the item ship, just uses it as a hook to get off i guess.
                        isUsingTerminal = false;
                        noMoreTerminal = true;
                        Plugin.isTerminalBeingUsed = false;
                        return;
                    }
                    transmitMessageTimer += Time.deltaTime;

                    if (GameNetworkManager.Instance.isHostingGame)
                    {
                        terminalTimeFloat.Value = Random.Range(5.2f, 12.5f);
                        delayMaxTime.Value = Random.Range(15f, 45f);
                    }
                    if (transmitMessageTimer > terminalTimeFloat.Value && transmitPauseTimer > delayMaxTime.Value)
                    {
                        Plugin.mls.LogDebug("Masked '" + maskedId + "' is '" + maskedPersonality.ToString() + "' and sending a message using the signal translator");
                        string sentMessage = InsaneTransmitMessageSelection();
                        TerminalPatches.Transmitter.SendMessage(sentMessage);
                        Plugin.mls.LogDebug("Message sent is: " + sentMessage + " (" + enterTermianlSpecialCodeTime + " message sends remaining)");
                        enterTermianlSpecialCodeTime--;
                        transmitMessageTimer = 0f;
                        transmitPauseTimer = 0f;
                    }
                    if (transmitMessageTimer <= delayMaxTime.Value)
                    {
                        transmitPauseTimer += Time.deltaTime;
                    }
                    if (enterTermianlSpecialCodeTime == 0)
                    {
                        terminal.terminalAudio.PlayOneShot(terminal.leaveTerminalSFX);
                        __instance.inSpecialAnimation = false;
                        this.ignoringPersonality = false;
                        __instance.SwitchToBehaviourState(2);
                        terminal.placeableObject.inUse = false;
                        terminal.terminalLight.enabled = false;
                        creatureAnimator.ResetTrigger("Terminal");
                        maskedEnemy.headTiltTarget.gameObject.SetActive(true);
                        //__instance.SetDestinationToPosition(GameObject.Find("ItemShip").transform.position, false); //doesnt actually route to the item ship, just uses it as a hook to get off i guess.
                        isUsingTerminal = false;
                        noMoreTerminal = true;
                        Plugin.isTerminalBeingUsed = false;
                    }

                }
                else
                {
                    if (maskedPersonality != Personality.Deceiving)
                    {
                        Plugin.mls.LogDebug("Personality is not Deceiving :( (I am " + maskedPersonality.ToString() + ")");
                        terminal.terminalAudio.PlayOneShot(terminal.leaveTerminalSFX);
                        __instance.inSpecialAnimation = false;
                        this.ignoringPersonality = false;
                        __instance.SwitchToBehaviourState(2);
                        terminal.placeableObject.inUse = false;
                        terminal.terminalLight.enabled = false;
                        creatureAnimator.ResetTrigger("Terminal");
                        maskedEnemy.headTiltTarget.gameObject.SetActive(true);
                        //__instance.SetDestinationToPosition(GameObject.Find("ItemShip").transform.position, false); //doesnt actually route to the item ship, just uses it as a hook to get off i guess.
                        isUsingTerminal = false;
                        noMoreTerminal = true;
                        Plugin.isTerminalBeingUsed = false;
                        return;
                    }
                    float num2 = Random.Range(0.2f, 1.5f);
                    enterTermianlCodeTimer += Time.deltaTime;
                    if (enterTermianlCodeTimer > terminalTimeFloat.Value && enterTermianlSpecialCodeTime > 0)
                    {
                        if (GameNetworkManager.Instance.isHostingGame)
                        {
                            terminalTimeFloat.Value = Random.Range(2.2f, 8.5f);
                        }
                        TerminalAccessibleObject obj = terminalAccessibleObject[Random.Range(0, terminalAccessibleObject.Length)];
                        string code = obj.objectCode;
                        if (obj != null)
                        {
                            Plugin.mls.LogDebug("Masked '" + maskedId + "' is '" + maskedPersonality.ToString() + "' and broadcasting a terminal code");
                            terminal.CallFunctionInAccessibleTerminalObject(code);
                            Plugin.mls.LogDebug("Code broadcasted is " + code + " (" + enterTermianlSpecialCodeTime + " code entries remaining)");
                            terminal.terminalAudio.PlayOneShot(terminal.codeBroadcastSFX);
                        }
                        enterTermianlSpecialCodeTime--;
                        enterTermianlCodeTimer = 0f;
                    }
                    if (enterTermianlSpecialCodeTime == 0)
                    {
                        terminal.terminalAudio.PlayOneShot(terminal.leaveTerminalSFX);
                        __instance.inSpecialAnimation = false;
                        this.ignoringPersonality = false;
                        __instance.SwitchToBehaviourState(2);
                        terminal.placeableObject.inUse = false;
                        terminal.terminalLight.enabled = false;
                        creatureAnimator.ResetTrigger("Terminal");
                        maskedEnemy.headTiltTarget.gameObject.SetActive(true);
                        //__instance.SetDestinationToPosition(GameObject.Find("ItemShip").transform.position, false); //doesnt actually route to the item ship, just uses it as a hook to get off i guess.
                        isUsingTerminal = false;
                        noMoreTerminal = true;
                        Plugin.isTerminalBeingUsed = false;
                    }
                }
            }
            else if (!((Component)maskedEnemy.headTiltTarget).gameObject.activeSelf)
            {
                //is this even needed anymore? its repeatedly used above.
                terminal.placeableObject.inUse = false;
                ((Behaviour)terminal.terminalLight).enabled = false;
                creatureAnimator.ResetTrigger("Terminal");
                ((Component)maskedEnemy.headTiltTarget).gameObject.SetActive(true);
            }
        }

        public string InsaneTransmitMessageSelection()
        {
            string msg = null;
            int m = Random.Range(0, 10);
            switch (m)
            {
                case 0:
                    msg = "safe";
                    break;
                case 1:
                    msg = "danger";
                    break;
                case 2:
                    //entities
                    int e = Random.Range(0, 7);
                    switch (e)
                    {
                        case 0:
                            msg = "dogs";
                            break;
                        case 1:
                            msg = "bracken";
                            break;
                        case 2:
                            msg = "giant";
                            break;
                        case 3:
                            msg = "worm";
                            break;
                        case 4:
                            msg = "coilhead";
                            break;
                        case 5:
                            msg = "mine";
                            break;
                        case 6:
                            msg = "turret";
                            break;
                    }
                    break;
                case 3:
                    msg = "7pm";
                    break;
                case 4:
                    msg = "9pm";
                    break;
                case 5:
                    msg = "behind u";
                    break;
                case 6:
                    msg = "left";
                    break;
                case 7:
                    msg = "right";
                    break;
                case 8:
                    msg = "go back";
                    break;
                case 9:
                    msg = "watch out";
                    break;
            }
            //msg.Substring(0, 10);
            return msg;
        }
        #endregion terminal

        //breakerbox
        #region breakerbox
        private async void breakerBoxSwitchLogic(float distanceToBox,bool on)
        {
            isUsingBreakerBox = true;
            __instance.inSpecialAnimation = true;
            __instance.movingTowardsTargetPlayer = false;
            __instance.targetPlayer = null;

            //on means you are turning it on, not on means you are turning it off
            if (!on)
            {
                RoundManager.Instance.FlickerLights(false, false);
                await Task.Delay(2000);
                RoundManager.Instance.FlickerLights(false, false);
                await Task.Delay(1000);
                RoundManager.Instance.FlickerLights(false, false);
                await Task.Delay(500);
                //int switchesToChange = RoundManager.Instance.BreakerBoxRandom.Next(1, breakerBox.breakerSwitches.Length);
                int switchesToChange = breakerBox.breakerSwitches.Length; //all switches for now
                for (int i = 0; i < switchesToChange; i++)
                {
                    if (i != 0)
                    {
                        //Plugin.mls.LogDebug("waiting 1500ms");
                        await Task.Delay(1500);
                    }
                    //int pickedSwitch = RoundManager.Instance.BreakerBoxRandom.Next(0, breakerBox.breakerSwitches.Length);
                    //int i = 0;
                    //powerBox = breakerBox.breakerSwitches[pickedSwitch].gameObject.GetComponent<AnimatedObjectTrigger>();
                    powerBox = breakerBox.breakerSwitches[i].gameObject.GetComponent<AnimatedObjectTrigger>();
                    if (!powerBox.boolValue)
                    {
                        Plugin.mls.LogDebug("switch already turned off");
                        continue;
                    }
                    breakerBox.breakerSwitches[i].SetBool("turnedLeft", value: false);
                    breakerBox.thisAudioSource.PlayOneShot(breakerBox.switchPowerSFX);
                    powerBox.boolValue = false;
                    powerBox.setInitialState = false;
                    breakerBox.leversSwitchedOff++;
                    if (breakerBox.leversSwitchedOff > 0 && breakerBox.isPowerOn==true)
                    {
                        RoundManager.Instance.PowerSwitchOffClientRpc();
                        breakerBox.breakerBoxHum.Stop();
                        breakerBox.isPowerOn = false;
                    }
                }
                noMoreBreakerBox = true;
            }
            else
            {
                //int switchesToChange = RoundManager.Instance.BreakerBoxRandom.Next(1, breakerBox.breakerSwitches.Length);
                int switchesToChange = breakerBox.breakerSwitches.Length; //all switches for now
                for (int i = 0; i < switchesToChange; i++)
                {
                    if (i != 0)
                    {
                        //Plugin.mls.LogDebug("waiting 1500ms");
                        await Task.Delay(1500);
                    }
                    //int pickedSwitch = RoundManager.Instance.BreakerBoxRandom.Next(0, breakerBox.breakerSwitches.Length);
                    //int i = 0;
                    //powerBox = breakerBox.breakerSwitches[pickedSwitch].gameObject.GetComponent<AnimatedObjectTrigger>();
                    powerBox = breakerBox.breakerSwitches[i].gameObject.GetComponent<AnimatedObjectTrigger>();
                    if (powerBox.boolValue)
                    {
                        Plugin.mls.LogDebug("switch already turned on");
                        continue;
                    }
                    breakerBox.breakerSwitches[i].SetBool("turnedLeft", value: true);
                    breakerBox.thisAudioSource.PlayOneShot(breakerBox.switchPowerSFX);
                    powerBox.boolValue = true;
                    powerBox.setInitialState = false;
                    breakerBox.leversSwitchedOff--;
                    if (breakerBox.leversSwitchedOff == 0)
                    {
                        RoundManager.Instance.PowerSwitchOnClientRpc(); //switch on after all are on.
                        breakerBox.breakerBoxHum.Play();
                        breakerBox.isPowerOn = true;
                    }
                }
                noMoreBreakerBox = false;
            }
            //noMoreTerminal = false;
            isUsingBreakerBox = false;
            focusingPersonality = false;
            __instance.inSpecialAnimation = false;
        }
        #endregion breakerbox

        #region items
        public void UseItem(PlayerControllerB target, float distance)
        {
            maskedGoal = "use item";
            //IL_0349: Unknown result type (might be due to invalid IL or missing references)
            //IL_035e: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ea: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c7: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0478: Unknown result type (might be due to invalid IL or missing references)
            //IL_049f: Unknown result type (might be due to invalid IL or missing references)
            //IL_04a9: Unknown result type (might be due to invalid IL or missing references)
            //IL_04ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_04b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_04d7: Unknown result type (might be due to invalid IL or missing references)
            //IL_04dc: Unknown result type (might be due to invalid IL or missing references)
            //IL_04f0: Unknown result type (might be due to invalid IL or missing references)
            //IL_04f2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0514: Unknown result type (might be due to invalid IL or missing references)
            //IL_0516: Unknown result type (might be due to invalid IL or missing references)
            shovelTimer += Time.deltaTime;
            if (isHoldingObject)
            {
                if (closestGrabbable is FlashlightItem)
                {
                    if (shovelTimer < 0.7f)
                    {
                        ((GrabbableObject)((Component)closestGrabbable).GetComponent<FlashlightItem>()).ItemActivate(false, true);
                    }
                    else if (shovelTimer > 0.7f && shovelTimer < 1.4f)
                    {
                        ((GrabbableObject)((Component)closestGrabbable).GetComponent<FlashlightItem>()).ItemActivate(true, true);
                    }
                    else if (shovelTimer > 1f)
                    {
                        shovelTimer = 0f;
                    }
                }
                if (closestGrabbable is Shovel)
                {
                    if (shovelTimer < 0.5f)
                    {
                        if (!isReeledWithShovel)
                        {
                            creatureAnimator.SetTrigger("ShovelUp");
                            ((Component)closestGrabbable).GetComponent<Shovel>().shovelAudio.PlayOneShot(((Component)closestGrabbable).GetComponent<Shovel>().reelUp);
                            isReeledWithShovel = true;
                        }
                    }
                    else if (shovelTimer > 0.5f && shovelTimer < 0.7f)
                    {
                        if (!isHittedWithShovel)
                        {
                            creatureAnimator.ResetTrigger("ShovelUp");
                            if (distance < 3f)
                            {
                                target.movementAudio.PlayOneShot(StartOfRound.Instance.hitPlayerSFX);
                                if (target.health > 20)
                                {
                                    target.DamagePlayer(20, true, true, (CauseOfDeath)1, 0, false, default(Vector3));
                                    Plugin.mls.LogInfo((object)"Damage With Shovel");
                                }
                                else
                                {
                                    target.KillPlayer(Vector3.zero, true, (CauseOfDeath)1, 0);
                                    ((EnemyAI)maskedEnemy).targetPlayer = null;
                                    maskedEnemy.lastPlayerKilled = null;
                                    ((EnemyAI)maskedEnemy).inSpecialAnimation = false;
                                    Plugin.mls.LogInfo((object)"Killed With Shovel");
                                }
                            }
                            ((Component)closestGrabbable).GetComponent<Shovel>().shovelAudio.PlayOneShot(((Component)closestGrabbable).GetComponent<Shovel>().swing);
                            isHittedWithShovel = true;
                        }
                    }
                    else if ((double)shovelTimer > 1.5)
                    {
                        shovelTimer = 0f;
                        isReeledWithShovel = false;
                        isHittedWithShovel = false;
                    }
                }
                if ((Object)(object)((EnemyAI)maskedEnemy).targetPlayer != (Object)null && closestGrabbable is ShotgunItem && maskedPersonality == Personality.Aggressive)
                {
                    Plugin.mls.LogInfo((object)"Shotgun Guy targeted player");
                    ShotgunItem component = ((Component)closestGrabbable).GetComponent<ShotgunItem>();
                    if (component.shellsLoaded > 0)
                    {
                        if (((Component)closestGrabbable).GetComponent<ShotgunItem>().shellsLoaded > 0)
                        {
                            if (shootTimer > 0f)
                            {
                                shootTimer -= Time.deltaTime;
                            }
                            float num = Vector3.Distance(((Component)creatureAnimator).transform.position, ((Component)__instance.targetPlayer).transform.position);
                            if (component.safetyOn && num < 8f)
                            {
                                component.safetyOn = false;
                                component.gunAudio.PlayOneShot(component.switchSafetyOffSFX);
                                WalkieTalkie.TransmitOneShotAudio(component.gunAudio, component.switchSafetyOffSFX, 1f);
                                Plugin.mls.LogInfo((object)"Safety On");
                            }
                            else if (!component.safetyOn && num > 12f)
                            {
                                component.safetyOn = true;
                                component.gunAudio.PlayOneShot(component.switchSafetyOnSFX);
                                WalkieTalkie.TransmitOneShotAudio(component.gunAudio, component.switchSafetyOnSFX, 1f);
                                Plugin.mls.LogInfo((object)"Safety Off");
                            }
                            if (num < 10f && shootTimer <= 0f)
                            {
                                Vector3 val = ((Component)((Component)__instance).transform.GetChild(0).GetChild(3).GetChild(3)).transform.position - ((Component)((Component)__instance).transform.GetChild(0).GetChild(3).GetChild(3)).transform.up * 0.45f;
                                Vector3 forward = ((Component)((Component)__instance).transform.GetChild(0).GetChild(3).GetChild(3)).transform.forward;
                                Plugin.mls.LogInfo((object)"Calling shoot gun....");
                                component.ShootGun(val, forward);
                                Plugin.mls.LogInfo((object)"Calling shoot gun and sync");
                                component.localClientSendingShootGunRPC = true;
                                component.ShootGunServerRpc(val, forward);
                                shootTimer = 3f;
                            }
                            if ((Object)(object)__instance.targetPlayer != (Object)null && num > 10f && shootTimer <= 0f)
                            {
                                maskedEnemy.running = true;
                            }
                        }
                        else if (!component.safetyOn && component.shellsLoaded > 0 && (Object)(object)__instance.targetPlayer == (Object)null)
                        {
                            component.safetyOn = true;
                            component.gunAudio.PlayOneShot(component.switchSafetyOnSFX);
                            WalkieTalkie.TransmitOneShotAudio(component.gunAudio, component.switchSafetyOnSFX, 1f);
                        }
                    }
                }
            }
            if (dropItem.Value)
            {
                DropItem();
            }
        }

        private async void NotGrabItemsTime()
        {
            notGrabItems = true;
            await Task.Delay(9000);
            notGrabItems = false;
        }

        private void OldDropItem()
        {
            //IL_0086: Unknown result type (might be due to invalid IL or missing references)
            //IL_008b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0090: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c4: Unknown result type (might be due to invalid IL or missing references)
            if ((Object)(object)heldGrabbable != (Object)null && isHoldingObject)
            {
                NotGrabItemsTime();
                closestGrabbable.parentObject = null;
                ((Component)closestGrabbable).transform.SetParent(StartOfRound.Instance.propsContainer, true);
                closestGrabbable.EnablePhysics(true);
                closestGrabbable.fallTime = 0f;
                closestGrabbable.startFallingPosition = ((Component)closestGrabbable).transform.parent.InverseTransformPoint(((Component)closestGrabbable).transform.position);
                closestGrabbable.targetFloorPosition = ((Component)closestGrabbable).transform.parent.InverseTransformPoint(closestGrabbable.GetItemFloorPosition(default(Vector3)));
                closestGrabbable.floorYRot = -1;
                closestGrabbable.isHeld = false;
                closestGrabbable.isHeldByEnemy = false;
                isHoldingObject = false;
                closestGrabbable.DiscardItemFromEnemy();
                closestGrabbable.hasHitGround = true;
                closestGrabbable.grabbable = true;
                heldGrabbable = null;
                isHoldingObject = false;
                itemDroped = true;
                PlayerControllerB targetPlayer = __instance.CheckLineOfSightForClosestPlayer(70f, 50, 1, 3f);
                __instance.movingTowardsTargetPlayer = true;
                __instance.targetPlayer = targetPlayer;
                __instance.SwitchToBehaviourState(2);
            }
        }

        private void DropItem()
        {
            //IL_0086: Unknown result type (might be due to invalid IL or missing references)
            //IL_008b: Unknown result type (might be due to invalid IL or missing references)
            //IL_0090: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_00b9: Unknown result type (might be due to invalid IL or missing references)
            //IL_00ba: Unknown result type (might be due to invalid IL or missing references)
            //IL_00bf: Unknown result type (might be due to invalid IL or missing references)
            //IL_00c4: Unknown result type (might be due to invalid IL or missing references)
            if ((Object)(object)heldGrabbable != (Object)null && isHoldingObject)
            {
                heldGrabbable.parentObject = null;
                ((Component)heldGrabbable).transform.SetParent(StartOfRound.Instance.propsContainer, true);
                heldGrabbable.EnablePhysics(true);
                heldGrabbable.fallTime = 0f;
                heldGrabbable.startFallingPosition = ((Component)heldGrabbable).transform.parent.InverseTransformPoint(((Component)heldGrabbable).transform.position);
                heldGrabbable.targetFloorPosition = ((Component)heldGrabbable).transform.parent.InverseTransformPoint(heldGrabbable.GetItemFloorPosition(default(Vector3)));
                heldGrabbable.floorYRot = -1;
                heldGrabbable.isHeld = false;
                heldGrabbable.isHeldByEnemy = false;
                isHoldingObject = false;
                heldGrabbable.DiscardItemFromEnemy();
                heldGrabbable.hasHitGround = true;
                NotGrabItemsTime();
                heldGrabbable.grabbable = true;
                isHoldingObject = false;
                itemDroped = true;
                PlayerControllerB targetPlayer = __instance.CheckLineOfSightForClosestPlayer(70f, 50, 1, 3f);
                __instance.movingTowardsTargetPlayer = true;
                __instance.targetPlayer = targetPlayer;
                __instance.SwitchToBehaviourState(2);
                heldGrabbable = null;
            }
        } //new version, trying to revert to original


        //this picks up multiple items if they are stacked on each other, maybe re-work to be like hoarder bug works..
        public void GrabItem()
        {
            //IL_0045: Unknown result type (might be due to invalid IL or missing references)
            //IL_0050: Unknown result type (might be due to invalid IL or missing references)
            //IL_016c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0138: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_020c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0211: Unknown result type (might be due to invalid IL or missing references)
            //IL_0228: Unknown result type (might be due to invalid IL or missing references)
            //IL_023d: Unknown result type (might be due to invalid IL or missing references)
            if (isHoldingObject || noMoreItems || notGrabItems)
            {
                return;
            }

            NavMeshHit hitGrabbable;
            NavMeshPath nmpGrabbable = new NavMeshPath();

            float num = float.PositiveInfinity;
            List<GrabbableObject> allItemsList;
            /*if (maskedPersonality == Personality.Cunning)
            {
                //cunning items
                //allItemsList = null;
                //filtering out only items cunning wants (in the ship, this doesnt work as "in the ship" only takes from items in the ship before the round starts.)
                //allItemsList = GlobalItemList.Instance.allitems.FindAll(item => item.isInShipRoom == true).ToList();
                //filtering out only items cunning wants (near the ship)
                allItemsList = GlobalItemList.Instance.allitems.FindAll(item => (Vector3.Distance(item.transform.position, ((Component)terminal).transform.position) < 15f || item.name == "Walkie-talkie")).ToList();
            }
            else if (maskedPersonality == Personality.Deceiving)
            {
                //allItemsList = null;
                allItemsList = GlobalItemList.Instance.allitems.FindAll(item => (Vector3.Distance(item.transform.position, ((Component)terminal).transform.position) > 30f || item.name == "Walkie-talkie")).ToList();
            }
            else
            {*/
                //all items
                allItemsList = GlobalItemList.Instance.allitems;
            //}

            if (allItemsList.Count == 0)
            {
                Plugin.mls.LogDebug("allItemsList is empty so setting no more items=true");
                noMoreItems = true;
                mustChangeFocus = true;
                return;
            }

            //items to ignore because they shouldent be touched.
            /*allItemsList.Remove(allItemsList.Find(cbm => cbm.name == "ClipboardManual")); //removing clipboard from the list
            allItemsList.Remove(allItemsList.Find(sn => sn.name == "StickyNoteItem")); //removing sticky note on the wall from the list)
            allItemsList.RemoveAll(rlh => rlh.name == "RedLocustHive");*/ //removing red locust bee hive as its held by an enemy by default and cunning just stands there looking at it
                                                                        //allItemsList.OrderBy(x => x.scrapValue); // dont use but this allows sorting by scrap value in future.

            //wont work.. needs a lot of recoding to make work i think but should be better going forward
            /*closestGrabbable = allItemsList.Aggregate((curMin, x) => (
                curMin == null ||
                Vector3.Distance(this.transform.position, curMin.transform.position) > 20f ||
                curMin.isHeld ||
                curMin.isHeldByEnemy ||
                //need to check it item is in a bush here.. needs some recoding..
                (Vector3.Distance(this.transform.position, x.transform.position)) < Vector3.Distance(this.transform.position, curMin.transform.position)
                ) ? x : curMin);*/

            foreach (GrabbableObject allitem in allItemsList)
            {
                //null reference exception fix here
                if ((Component)this == null)
                {
                    //Plugin.mls.LogDebug("GrabItem() NullReferenceFix - Masked Entity was NULL");
                    return;
                }
                else if (((Component)this).transform.position == null)
                {
                    //Plugin.mls.LogDebug("GrabItem() NullReferenceFix - Masked Entity position was NULL");
                    //Plugin.mls.LogDebug("this.name = " + ((Component)this).name.ToString());
                    return;
                }
                if ((Component)allitem == null)
                {
                    //Plugin.mls.LogDebug("GrabItem() NullReferenceFix - Item Entity was NULL");
                    continue;
                }
                else if (((Component)allitem).transform.position == null)
                {
                    //Plugin.mls.LogDebug("GrabItem() NullReferenceFix - Item Entity position was NULL");
                    //Plugin.mls.LogDebug("allitem.name = " + ((Component)allitem).name.ToString());
                    continue;
                }
                //null reference exception fix above
                //Plugin.mls.LogDebug("CurrentItemName = "+allitem.name.ToString()); //for debugging only.
                //removing items from the list after checking if they are null.
                if (allitem.name == "ClipboardManual" || allitem.name == "StickyNoteItem")
                {
                    continue;
                }
                if (allitem.name == "RedLocustHive")
                {
                    continue;
                }

                //ignore non-"cunning" items if cunning
                if (maskedPersonality == Personality.Cunning && Vector3.Distance(allitem.transform.position, ((Component)terminal).transform.position) >= 15f)
                {
                    continue;
                }

                //ignore non-deceiving items if deceiving
                if (maskedPersonality == Personality.Deceiving && Vector3.Distance(allitem.transform.position, ((Component)terminal).transform.position) <= 30f)
                {
                    continue;
                }


                //end removing items

                float closestGrabbableDistance = Vector3.Distance(((Component)this).transform.position, ((Component)allitem).transform.position);
                if (allitem.GetComponent<CheckItemCollision>() != null)
                {
                    itemSystem = allitem.GetComponent<CheckItemCollision>();
                }
                if (itemSystem.hidedByMasked)
                {
                    continue;
                }
                closestGrabbableReachable = agent.CalculatePath(allitem.transform.position, nmpGrabbable);
                if (!closestGrabbableReachable)
                {
                    continue;
                }
                if (!(closestGrabbableDistance < num) || !(closestGrabbableDistance <= 30f) || allitem.isHeld || allitem.isHeldByEnemy || notGrabClosestItem)
                {
                    continue;
                }
                num = closestGrabbableDistance;
                closestGrabbable = allitem;

                if (((NetworkBehaviour)this).IsHost)
                {
                    if (closestGrabbableDistance < 1.5f && !isHoldingObject)
                    {
                        isCrouched.Value = true;
                    }
                    else
                    {
                        isCrouched.Value = false;
                    }
                    if (closestGrabbableDistance > 0.5f)
                    {
                        __instance.SetDestinationToPosition(((Component)closestGrabbable).transform.position, true);
                        __instance.moveTowardsDestination = true;
                        __instance.movingTowardsTargetPlayer = false;
                    }
                    /*else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                        __instance.moveTowardsDestination = false;
                    }*/
                }
                if (closestGrabbableDistance > 0.5f && closestGrabbableDistance < 3f)
                {
                    maskedEnemy.focusOnPosition = ((Component)closestGrabbable).transform.position;
                    maskedEnemy.lookAtPositionTimer = 1.5f;
                }
                if (closestGrabbableDistance < 0.9f)
                {
                    if (isHoldingObject)
                    {
                        return;
                    }
                    //NotGrabItemsTime(); //may not be a fix
                    float num3 = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
                    if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
                    {
                        num3 *= -1f;
                    }
                    maskedEnemy.verticalLookAngle = num3;
                    heldGrabbable = closestGrabbable;
                    closestGrabbable.parentObject = itemHolder.transform;
                    closestGrabbable.hasHitGround = false;
                    closestGrabbable.isHeld = true;
                    closestGrabbable.isHeldByEnemy = true;
                    closestGrabbable.grabbable = false;
                    isHoldingObject = true;
                    itemDroped = false;
                    closestGrabbable.GrabItemFromEnemy(__instance);
                }
                //not needed...
                /*if (closestGrabbableDistance < 1f && !isHoldingObject && ((NetworkBehaviour)this).IsHost)
                {
                    isCrouched.Value = true;
                }*/
            }
        }

        public void GrabItemNewOne()
        {
            //IL_0045: Unknown result type (might be due to invalid IL or missing references)
            //IL_0050: Unknown result type (might be due to invalid IL or missing references)
            //IL_016c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0138: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b3: Unknown result type (might be due to invalid IL or missing references)
            //IL_01b8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ec: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fc: Unknown result type (might be due to invalid IL or missing references)
            //IL_020c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0211: Unknown result type (might be due to invalid IL or missing references)
            //IL_0228: Unknown result type (might be due to invalid IL or missing references)
            //IL_023d: Unknown result type (might be due to invalid IL or missing references)
            if (isHoldingObject || noMoreItems)
            {
                return;
            }
            //setClosestGrabbable();
            if (!nearestGrabbableReachable || nearestGrabbable == null)
            {
                //setNearestGrabbable();
                if (nearestGrabbable == null)
                {
                    noMoreItems = true;
                    return;
                }
            }
            /*if (closestGrabbableDistance < 5f && maskedPersonality == Personality.Cunning)
            {
                //focusingPersonality = true;
                //return;
            }*/
            if (((NetworkBehaviour)this).IsHost)
            {
                if (closestGrabbableDistance < 1.5f && !isHoldingObject)
                {
                    isCrouched.Value = true;
                }
                else
                {
                    isCrouched.Value = false;
                }
                if (closestGrabbableDistance > 0.5f)
                {
                    __instance.SetDestinationToPosition(closestGrabbable.transform.position, false);
                    __instance.moveTowardsDestination = true;
                    __instance.movingTowardsTargetPlayer = false;
                }
                /*else if(maskedPersonality == Personality.Cunning)
                {
                    MaskedCunning();
                }*/
                /*else
                {
                    if (!__instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, false))
                    {
                        return;
                    }
                    __instance.moveTowardsDestination = false;
                }*/
            }
            if (closestGrabbableDistance > 0.5f && closestGrabbableDistance < 3f)
            {
                maskedEnemy.focusOnPosition = ((Component)closestGrabbable).transform.position;
                maskedEnemy.lookAtPositionTimer = 1.5f;
            }
            if (closestGrabbableDistance < 0.9f)
            {
                float num3 = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
                if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
                {
                    num3 *= -1f;
                }
                maskedEnemy.verticalLookAngle = num3;
                heldGrabbable = closestGrabbable;
                heldGrabbable.parentObject = itemHolder.transform;
                heldGrabbable.hasHitGround = false;
                heldGrabbable.isHeld = true;
                heldGrabbable.isHeldByEnemy = true;
                heldGrabbable.grabbable = false;
                isHoldingObject = true;
                itemDroped = false;
                heldGrabbable.GrabItemFromEnemy(__instance);
            }
            if (closestGrabbableDistance < 1f && !isHoldingObject && ((NetworkBehaviour)this).IsHost)
            {
                isCrouched.Value = true;
            }
        } //new version, trying to revert to original

        public void GrabShotgunItem()
        {
            maskedGoal = "grabshotgunitem";
            //IL_0058: Unknown result type (might be due to invalid IL or missing references)
            //IL_0063: Unknown result type (might be due to invalid IL or missing references)
            //IL_0162: Unknown result type (might be due to invalid IL or missing references)
            //IL_012e: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a9: Unknown result type (might be due to invalid IL or missing references)
            //IL_01ae: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e2: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f2: Unknown result type (might be due to invalid IL or missing references)
            //IL_0202: Unknown result type (might be due to invalid IL or missing references)
            //IL_0207: Unknown result type (might be due to invalid IL or missing references)
            //IL_021e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0233: Unknown result type (might be due to invalid IL or missing references)
            if (isHoldingObject || noMoreItems)
            {
                return;
            }
            float num = float.PositiveInfinity;
            foreach (GrabbableObject allitem in GlobalItemList.Instance.allitems)
            {
                if (!(allitem is ShotgunItem))
                {
                    continue;
                }
                float num2 = Vector3.Distance(((Component)this).transform.position, ((Component)allitem).transform.position);
                if (!(num2 < num) || !(num2 <= 10f) || allitem.isHeld || allitem.isHeldByEnemy || notGrabClosestItem)
                {
                    continue;
                }
                num = num2;
                closestGrabbable = allitem;
                if ((Object)(object)closestGrabbable != (Object)null && (Object)(object)((Component)closestGrabbable).GetComponent<CheckItemCollision>() != (Object)null)
                {
                    itemSystem = ((Component)closestGrabbable).GetComponent<CheckItemCollision>();
                }
                if (itemSystem.hidedByMasked)
                {
                    continue;
                }
                if (((NetworkBehaviour)this).IsHost)
                {
                    if (num2 > 0.5f)
                    {
                        __instance.SetDestinationToPosition(((Component)closestGrabbable).transform.position, true);
                        __instance.moveTowardsDestination = true;
                        __instance.movingTowardsTargetPlayer = false;
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                        __instance.moveTowardsDestination = false;
                    }
                }
                if (num2 > 0.5f && num2 < 3f)
                {
                    maskedEnemy.focusOnPosition = ((Component)closestGrabbable).transform.position;
                    maskedEnemy.lookAtPositionTimer = 1.5f;
                }
                if (num2 < 0.9f)
                {
                    float num3 = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
                    if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
                    {
                        num3 *= -1f;
                    }
                    maskedEnemy.verticalLookAngle = num3;
                    heldGrabbable = closestGrabbable;
                    closestGrabbable.parentObject = itemHolder.transform;
                    closestGrabbable.hasHitGround = false;
                    closestGrabbable.isHeld = true;
                    closestGrabbable.isHeldByEnemy = true;
                    closestGrabbable.grabbable = false;
                    isHoldingObject = true;
                    itemDroped = false;
                    closestGrabbable.GrabItemFromEnemy(__instance);
                }
                if (num2 < 4f && !isHoldingObject && maskedPersonality != Personality.Aggressive && ((NetworkBehaviour)this).IsHost)
                {
                    isCrouched.Value = true;
                }
            }
        }

        private void FindAndGrabShotgun()
        {
            //IL_015b: Unknown result type (might be due to invalid IL or missing references)
            //IL_016d: Unknown result type (might be due to invalid IL or missing references)
            //IL_017e: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e0: Unknown result type (might be due to invalid IL or missing references)
            if (!(closestGrabbable is ShotgunItem) && isHoldingObject && !isDroppedShotgunAvailable)
            {
                Plugin.mls.LogInfo((object)"Drop Item!");
                dropItem.Value = true;
            }
            bool shotgunFound = false;
            foreach (GrabbableObject allitem in GlobalItemList.Instance.allitems)
            {
                if (!(allitem is ShotgunItem))
                {
                    continue;
                }
                shotgunFound = true;
                if (allitem.isHeld)
                {
                    if (!allitem.isHeldByEnemy)
                    {
                        HandleShotgunHeldByPlayer();
                        Plugin.mls.LogInfo((object)"Held Shotgun Found!");
                        isDroppedShotgunAvailable = false;
                    }
                }
                else
                {
                    GrabShotgunItem();
                    isDroppedShotgunAvailable = true;
                }
            }
            if (!shotgunFound)
            {
                noMoreItems = true;
                mustChangeFocus = true;
            }
            if (!isHoldingObject || !(closestGrabbable is Shovel))
            {
                return;
            }
            foreach (GrabbableObject allitem2 in GlobalItemList.Instance.allitems)
            {
                if (allitem2 is ShotgunItem && (Object)(object)allitem2.playerHeldBy != (Object)null)
                {
                    __instance.SetDestinationToPosition(((Component)allitem2.playerHeldBy).transform.position, true);
                    float num = Vector3.Distance(((Component)this).transform.position, ((Component)allitem2.playerHeldBy).transform.position);
                    maskedEnemy.stopAndStareTimer = 0f;
                    if (num < float.PositiveInfinity && num < 4f)
                    {
                        maskedEnemy.headTiltTarget.LookAt(((Component)allitem2.playerHeldBy).transform);
                        LookAtPos(((Component)allitem2.playerHeldBy).transform.position, 0.2f);
                    }
                    if (num < 3.2f)
                    {
                        UseItem(allitem2.playerHeldBy, num);
                    }
                    else
                    {
                        maskedEnemy.running = true;
                    }
                }
            }
        }

        private void HandleShotgunHeldByPlayer()
        {
            //IL_0048: Unknown result type (might be due to invalid IL or missing references)
            //IL_0053: Unknown result type (might be due to invalid IL or missing references)
            //IL_0158: Unknown result type (might be due to invalid IL or missing references)
            //IL_0124: Unknown result type (might be due to invalid IL or missing references)
            //IL_019f: Unknown result type (might be due to invalid IL or missing references)
            //IL_01a4: Unknown result type (might be due to invalid IL or missing references)
            //IL_01d8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01e8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01f8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0214: Unknown result type (might be due to invalid IL or missing references)
            //IL_0229: Unknown result type (might be due to invalid IL or missing references)
            if (isHoldingObject)
            {
                return;
            }
            foreach (GrabbableObject allitem in GlobalItemList.Instance.allitems)
            {
                if (!(allitem is Shovel))
                {
                    continue;
                }
                float num = Vector3.Distance(((Component)this).transform.position, ((Component)allitem).transform.position);
                if (!(num < float.PositiveInfinity) || !(num <= 10f) || allitem.isHeld || allitem.isHeldByEnemy || notGrabClosestItem)
                {
                    continue;
                }
                closestGrabbable = allitem;
                if ((Object)(object)closestGrabbable != (Object)null && (Object)(object)((Component)closestGrabbable).GetComponent<CheckItemCollision>() != (Object)null)
                {
                    itemSystem = ((Component)closestGrabbable).GetComponent<CheckItemCollision>();
                }
                if (itemSystem.hidedByMasked)
                {
                    continue;
                }
                if (((NetworkBehaviour)this).IsHost)
                {
                    if ((double)num > 0.5)
                    {
                        __instance.SetDestinationToPosition(((Component)closestGrabbable).transform.position, true);
                        __instance.moveTowardsDestination = true;
                        __instance.movingTowardsTargetPlayer = false;
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                        __instance.moveTowardsDestination = false;
                    }
                }
                if (num > 0.5f && num < 3f)
                {
                    maskedEnemy.focusOnPosition = ((Component)closestGrabbable).transform.position;
                    maskedEnemy.lookAtPositionTimer = 1.5f;
                }
                if (num < 0.9f)
                {
                    float num2 = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
                    if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
                    {
                        num2 *= -1f;
                    }
                    maskedEnemy.verticalLookAngle = num2;
                    closestGrabbable.parentObject = itemHolder.transform;
                    closestGrabbable.hasHitGround = false;
                    closestGrabbable.isHeld = true;
                    closestGrabbable.isHeldByEnemy = true;
                    closestGrabbable.grabbable = false;
                    isHoldingObject = true;
                    itemDroped = false;
                    closestGrabbable.GrabItemFromEnemy(__instance);
                }
                if (num < 4f && !isHoldingObject && maskedPersonality != Personality.Aggressive && GameNetworkManager.Instance.isHostingGame)
                {
                    isCrouched.Value = true;
                }
            }
        }

        private void HandleShotgunNotHeld()
        {
            //IL_003e: Unknown result type (might be due to invalid IL or missing references)
            //IL_0049: Unknown result type (might be due to invalid IL or missing references)
            //IL_014a: Unknown result type (might be due to invalid IL or missing references)
            //IL_0116: Unknown result type (might be due to invalid IL or missing references)
            //IL_01c8: Unknown result type (might be due to invalid IL or missing references)
            //IL_01cd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0200: Unknown result type (might be due to invalid IL or missing references)
            //IL_0210: Unknown result type (might be due to invalid IL or missing references)
            //IL_0220: Unknown result type (might be due to invalid IL or missing references)
            //IL_0225: Unknown result type (might be due to invalid IL or missing references)
            //IL_023c: Unknown result type (might be due to invalid IL or missing references)
            //IL_0251: Unknown result type (might be due to invalid IL or missing references)
            notGrabClosestItem = true;
            foreach (GrabbableObject allitem in GlobalItemList.Instance.allitems)
            {
                if (!(allitem is ShotgunItem))
                {
                    continue;
                }
                float num = Vector3.Distance(((Component)this).transform.position, ((Component)allitem).transform.position);
                if (!(num < float.PositiveInfinity) || !(num <= 10f) || allitem.isHeld || allitem.isHeldByEnemy || notGrabClosestItem)
                {
                    continue;
                }
                closestGrabbable = allitem;
                if ((Object)(object)closestGrabbable != (Object)null && (Object)(object)((Component)closestGrabbable).GetComponent<CheckItemCollision>() != (Object)null)
                {
                    itemSystem = ((Component)closestGrabbable).GetComponent<CheckItemCollision>();
                }
                if (itemSystem.hidedByMasked)
                {
                    continue;
                }
                if (((NetworkBehaviour)this).IsHost)
                {
                    if ((double)num > 0.5)
                    {
                        __instance.SetDestinationToPosition(((Component)closestGrabbable).transform.position, true);
                        __instance.moveTowardsDestination = true;
                        __instance.movingTowardsTargetPlayer = false;
                    }
                    else
                    {
                        __instance.SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
                        __instance.moveTowardsDestination = false;
                    }
                }
                if (num < 3f && isHoldingObject && !(closestGrabbable is ShotgunItem))
                {
                    dropItem.Value = true;
                }
                if (num > 0.5f && num < 3f)
                {
                    maskedEnemy.focusOnPosition = ((Component)closestGrabbable).transform.position;
                    maskedEnemy.lookAtPositionTimer = 1.5f;
                }
                if (num < 0.9f)
                {
                    float num2 = Vector3.Angle(((Component)__instance).transform.forward, ((Component)closestGrabbable).transform.position - ((Component)__instance).transform.position);
                    if (((Component)closestGrabbable).transform.position.y - maskedEnemy.headTiltTarget.position.y < 0f)
                    {
                        num2 *= -1f;
                    }
                    maskedEnemy.verticalLookAngle = num2;
                    closestGrabbable.parentObject = itemHolder.transform;
                    closestGrabbable.hasHitGround = false;
                    closestGrabbable.isHeld = true;
                    closestGrabbable.isHeldByEnemy = true;
                    closestGrabbable.grabbable = false;
                    isHoldingObject = true;
                    itemDroped = false;
                    closestGrabbable.GrabItemFromEnemy(__instance);
                }
                if (num < 4f && !isHoldingObject && maskedPersonality != Personality.Aggressive && ((NetworkBehaviour)this).IsHost)
                {
                    isCrouched.Value = true;
                }
            }
        }
        #endregion items
        //Focus = none Activities
        #region generic activities
        private void findLockerAudio()
        {
            //__instance.targetPlayer = null;
            //is outside, not in the ship, not dead, (still can use terminal OR if cunning, still can pick up items and can reach that item in the ship).
            //if (__instance.isOutside && !__instance.isInsidePlayerShip && !__instance.isEnemyDead && (!noMoreTerminal || (maskedPersonality == Personality.Cunning && !noMoreItems && closestGrabbableReachable)))
            //now only if outside, not in player ship, and not dead)
            //if (__instance.isOutside && !__instance.isInsidePlayerShip && !__instance.isEnemyDead)
            //{
                maskedGoal = "walking to ships locker";
                maskedEnemy.lostLOSTimer = 0f;
                maskedEnemy.stopAndStareTimer = 0f;
            bool reachable = ((EnemyAI)maskedEnemy).SetDestinationToPosition(lockerPosition, true);
            if (!reachable)
            {
                mustChangeActivity = true;
            }
            //__instance.moveTowardsDestination = true;
            //}
            float distanceToLockerAudio = Vector3.Distance(maskedEnemy.transform.position, lockerPosition);
            if (distanceToLockerAudio <= 2f)
            {
                mustChangeFocus = true;
                mustChangeActivity = true;
            }
        }

        private void findBreakerBox()
        {
            //if (!__instance.isOutside && !__instance.isEnemyDead)
            //{
                maskedGoal = "walking to breaker box";
                maskedEnemy.lostLOSTimer = 0f;
                maskedEnemy.stopAndStareTimer = 0f;
            bool reachable = ((EnemyAI)maskedEnemy).SetDestinationToPosition(breakerPosition, true);
            //__instance.moveTowardsDestination = true;
            if(!reachable)
            {
                mustChangeActivity = true;
            }
            //}
            if (breakerBoxDistance <= 5f)
            {
                mustChangeFocus = true;
                mustChangeActivity = true;
            }
        }

        private void findMainEntrance()
        {
            maskedGoal = "going to main entrance";
            //maskedEnemy.LookAndRunRandomly(true, true);
            ((EnemyAI)maskedEnemy).SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true);
            //__instance.moveTowardsDestination = true;
            if (Vector3.Distance(maskedEnemy.transform.position, maskedEnemy.mainEntrancePosition) < 1f)
            {
                maskedEnemy.running = false; //to stop them running.
                mustChangeFocus = true;
                mustChangeActivity = true;
            }
        }

        private void findFireExit()
        {
            maskedGoal = "going to a random fire exit";
            //maskedEnemy.LookAndRunRandomly(true, true);
            ((EnemyAI)maskedEnemy).SetDestinationToPosition(maskedEnemy.mainEntrancePosition, true); // for now this still heads to the main entrance, need to set variable to find fire exits!
            //__instance.moveTowardsDestination = true;
            if (Vector3.Distance(maskedEnemy.transform.position, maskedEnemy.mainEntrancePosition) < 1f)
            {
                maskedEnemy.running = false; //to stop them running.
                mustChangeFocus = true;
                mustChangeActivity = true;
            }
        }

        float followTime = 0f;

        private void findRandomPlayer()
        {
            if (__instance.targetPlayer == null)
            {
                mustChangeFocus = true;
                mustChangeActivity = true;
                return;
            }
            PlayerControllerB randomPlayer = __instance.targetPlayer;
            maskedGoal = "finding "+randomPlayer.name.ToString();
            Vector3 pos = randomPlayer.transform.position;
            bool canSeePos = __instance.CheckLineOfSightForPosition(pos, 160f, 40, -1, null);
            if (canSeePos)
            {
                followTime = 20f;
            }
            else
            {
                followTime -= 0.1f;
            }
            if(followTime==0f)
            {
                mustChangeFocus = true;
                mustChangeActivity = true;
            }
        }
        #endregion generic activites
    }
    [HarmonyPatch(typeof(ShotgunItem))]
    internal class ShotgunItemPatch
    {
        public bool GetVar(ref bool ___localClientSendingShootGunRPC)
        {
            return ___localClientSendingShootGunRPC;
        }
    }
    [HarmonyPatch(typeof(MaskedPlayerEnemy))]
    internal class MaskedPlayerEnemyPatch
    {
        public static MaskedAIRevamp vd;

        [HarmonyPrefix]
        [HarmonyPatch("Awake")]
        private static void Awake_Prefix(EnemyAI __instance)
        {
            if (Plugin.enableMaskedFeatures)
            {
                vd = ((Component)__instance).gameObject.AddComponent<MaskedAIRevamp>();
            }
            else if ((Object)(object)((Component)((Component)__instance).transform.GetChild(3).GetChild(0)).GetComponent<Animator>().runtimeAnimatorController != (Object)(object)Plugin.MapDotRework)
            {
                ((Component)((Component)__instance).transform.GetChild(3).GetChild(0)).GetComponent<Animator>().runtimeAnimatorController = Plugin.MapDotRework;
            }
        }
    }
}
