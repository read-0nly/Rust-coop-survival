
using Convert = System.Convert;
using Network;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Rust.Ai;
using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using Oxide.Core;
using System.ComponentModel;
using System.Diagnostics;
using Oxide.Plugins;
using Oxide.Core.Plugins;
using System.Collections.Specialized;
using UnityEngine.Rendering;
//Requires: Omninasty
namespace Oxide.Plugins
{
    [Info("DataTest", "obsol", "0.0.1")]
    [Description("Testing data shit")]
    public class DataTest : CovalencePlugin
    {

        string[] sewers = { "sewer_tunnel_300_ladder", "sewer_tunnel_300_high_ladder" };
        string[] trainTops = { "corridor_train_tunnel_entrance" };
        string[] trainBottom = { "tunnel-station" };
        string[] customMonuments = { "tunnel-station", "sewer_bigroom" };
        List<GameObject> SewerLadders = new List<GameObject>();
        List<GameObject> TrainTops = new List<GameObject>();
        List<GameObject> TrainBottoms = new List<GameObject>();
        List<GameObject> CustomMonuments = new List<GameObject>();

        //TODO: Remove
        List<GameObject> targets = new List<GameObject>();
        public struct MonumentDefinition
        {
            public int radius;
            public Vector3 centerOffset;
            public int monumentMin;
            public int monumentMax;
        }
        #region config
        public static Configuration config = new Configuration();
        public static Core.Configuration.DynamicConfigFile configRef;
        public class Configuration
        {
            [JsonProperty("monumentDelay", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float monumentDelay = 20;
            [JsonProperty("roadDelay", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float roadDelay = 3;
            [JsonProperty("monumentLoopMin", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int monumentLoopMin = 3;
            [JsonProperty("monumentLoopMax", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int monumentLoopMax = 15;
            [JsonProperty("monumentRoadDistance", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float monumentRoadDistance = 50;
            [JsonProperty("roadRoadDistance", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float roadRoadDistance = 30;
            [JsonProperty("stepSize", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int stepSize = 10;
            [JsonProperty("nodeStuck", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int nodeStuck = 20;
            [JsonProperty("HomeList", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<string>> HomeList = new Dictionary<string, List<string>>();
            [JsonProperty("MonumentDefinitions", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, MonumentDefinition> MonumentDefinitions = new Dictionary<string, MonumentDefinition>();


            public string ToJson() => JsonConvert.SerializeObject(this);
            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }
        protected override void LoadDefaultConfig() => config = new Configuration();
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new JsonException();
                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    Puts("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                Puts($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();

            }
        }
        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }
        #endregion

        public static Omninasty omninasty;
        void Loaded()
        {
            omninasty = (Omninasty)Manager.GetPlugin("Omninasty");
        }

        #region Classes
        public class Connection
        {
            public Node start;
            public Node parent;
            public Node finish;
            public Connection(Node parentNode, Node startNode, Node finishNode)
            {
                start = startNode;
                finish = finishNode;
                parent = parentNode;
            }
            public virtual int Step(List<Connection> cnxs, int idx, BaseEntity entity, out string logging)
            {
                return (start.Step(cnxs, idx, entity, out logging));
            }
            public virtual Vector3 GetPoint(List<Connection> cnxs, int idx, BaseEntity entity, out string logging, bool connected = true)
            {
                return start.GetPoint(cnxs, idx, entity, out logging);
            }
        }
        public class Node
        {
            public string name;
            public List<Vector3> points = new List<Vector3>();
            public Dictionary<Node, int> links = new Dictionary<Node, int>();
            public Dictionary<string, Dictionary<long, float>> seats = new Dictionary<string, Dictionary<long, float>>();
            public int seatMin;
            public int seatMax;
            public static HashSet<Node> nodePool = new HashSet<Node>();
            public static HashSet<Connection> connectionPool = new HashSet<Connection>();

            public Node(string n, Vector3[] pts, int min, int max)
            {
                name = n;
                points = pts.ToList<Vector3>();
                links = new Dictionary<Node, int>();
                seatMin = min; seatMax = max;

            }
            public virtual int Step(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {
                logging = "";
                while (connections.Count() > 1 && connections[1].start != connections[0].parent)
                {
                    connections.RemoveAt(1);
                }
                if (connections.Count() == 1) {
                    connections[0].finish.Extend(connections, idx, entity, out logging);
                }

                {
                string faction = omninasty.getFaction(entity);
                if (!seats.ContainsKey(faction))
                {
                    seats.Add(faction, new Dictionary<long, float>());
                }
                if (seats[faction].Count() < seatMax)
                    if (seats[faction].ContainsKey((long)(entity as BasePlayer).userID))
                    {
                        seats[faction][(long)(entity as BasePlayer).userID] = UnityEngine.Time.time;
                    }
                    else
                    {
                        seats[faction].Add((long)(entity as BasePlayer).userID, UnityEngine.Time.time);
                    }
                }
                return -1;
            }
            public virtual int Extend(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {
                logging = "";
                //Find hights demand, create connection, returnv
                int highestDemand = 0;
                int i = 2;
                if(links==null||links.Keys.Count()==0) return -1;
                if (this.GetDemand(omninasty.getFaction(entity)) < 0) return -1;
                Node foundConn = links.Keys.ToArray()[UnityEngine.Random.Range(0,links.Values.Count())];
                if (connections.Count() > 0) { connections.Add(new Connection(this, connections.Last().parent, foundConn)); }
                else { connections.Add(new Connection(this, this, foundConn)); }
                
                return 1;
            }
            public virtual Vector3 GetPoint(List<Connection> connections, int idx, BaseEntity entity, out string logging, bool connected = true)
            {
                logging = "";

                return points[UnityEngine.Random.Range(0, points.Count())];
            }
            public virtual Vector3 GetPoint()
            {
                return points[UnityEngine.Random.Range(0, points.Count())];
            }

            public virtual int GetDemand(string faction, Node refNode = null) { return 0; }
            public virtual void CleanSeats()
            {
                if (seats == null)
                {
                    seats = new Dictionary<string, Dictionary<long, float>>();
                }
                if(seats.Count()>0)
                    foreach (string s in seats.Keys.ToArray())
                    {

                        if (seats[s] == null)
                        {
                            seats[s] = new Dictionary<long, float>();
                        }
                        if (seats[s].Count()>0)
                        foreach (long l in seats[s].Keys.ToArray())
                        {
                            if (seats[s][l] < UnityEngine.Time.time - (config.monumentDelay))
                            {
                                seats[s].Remove(l);
                            }
                        }
                    }
            }
        }
        public class NodeTemplate : Node
        {
            public NodeTemplate(string n, Vector3[] pts, int min, int max) : base(n, pts, min, max)
            {
            }
            public override int Step(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {
                int baseStep = base.Step(connections, idx, entity, out logging);
                //TODO:Remove
                return baseStep;
            }
            public override int Extend(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {
                //TODO:Remove
                return base.Extend(connections, idx, entity, out logging);
            }
            public override Vector3 GetPoint(List<Connection> connections, int idx, BaseEntity entity, out string logging, bool connected = true)
            {
                //TODO:Remove
                return base.GetPoint(connections, idx, entity, out logging);
            }
            public override void CleanSeats() { base.CleanSeats(); }

        }
        public class NodeTransit : Node
        {
            public float lastCached = 0;
            public float lastDemand = 0;
            public NodeTransit(string n, Vector3[] pts, int min, int max) : base(n, pts, min, max)
            {
            }
            public override int Step(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {
                int baseStep = base.Step(connections, idx, entity, out logging);
                if (connections.Count() == 0)
                    connections.Add(new Connection(this, this, this));
                if (!links.ContainsKey(connections[0].finish))
                {
                    connections[0].finish = links.Keys.ToArray()[UnityEngine.Random.Range(0, links.Keys.Count())];
                }
                if (idx == links[connections[0].finish])
                {
                    connections[1].finish.Extend(connections, idx, entity, out logging);
                    int newIdx = connections[0].finish.links[connections[0].parent];
                    connections.RemoveAt(0);
                    return newIdx;
                }
                else
                {
                    int diff = links[connections[0].finish] - idx;
                    int dist = Math.Abs(diff);
                    int sign = dist / diff;
                    if (dist > config.stepSize)
                    {
                        return idx + (sign * config.stepSize);
                    }
                    else
                    {
                        return idx + diff;
                    }
                }
                //TODO:Remove
                return base.Step(connections, idx, entity, out logging);
            }
            public override Vector3 GetPoint(List<Connection> connections, int idx, BaseEntity entity, out string logging, bool connected = true)
            {
                //points[(idx+points.count())]%poiunts.count()
                logging = "";
                return points[(idx + points.Count()) % points.Count()];
                //TODO:Remove
                return base.GetPoint(connections, idx, entity, out logging);
            }

            public override int Extend(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {
                logging = "";
                //Find hights demand, create connection, return
                Node foundConn = null;
                int highestDemand = 0;
                string faction = omninasty.getFaction(entity);
                int i = 2;
                if (links == null || links.Keys.Count() == 0) return -1;
                foreach (Node c in links.Keys)
                {
                    int thisDemand = c.GetDemand(faction, this);
                    if ((thisDemand >= highestDemand))
                    {
                        if (thisDemand == highestDemand && UnityEngine.Random.Range(0, i) == 0)
                            continue;
                        foundConn = c;
                        highestDemand = thisDemand;
                        i++;
                    }
                }
                if (foundConn == null)
                {
                    foundConn = links.Keys.ToArray()[UnityEngine.Random.Range(0, links.Keys.Count())];
                    foundConn.Extend(connections, idx, entity, out logging);
                }
                if (connections.Count() > 0) { connections.Add(new Connection(this, connections.Last().parent, foundConn)); }
                else { connections.Add(new Connection(this, this, foundConn)); }

                if (foundConn.seats == null)
                {
                    foundConn.seats = new Dictionary<string, Dictionary<long, float>>();
                }
                if (foundConn.seats.ContainsKey(faction))
                    if(foundConn.seats[faction].ContainsKey((long)(entity as BasePlayer).userID))
                        foundConn.seats[faction][(long)(entity as BasePlayer).userID]= UnityEngine.Time.time;
                    else
                        foundConn.seats[faction].Add((long)(entity as BasePlayer).userID, UnityEngine.Time.time);
                else
                {
                    foundConn.seats.Add(faction, new Dictionary<long, float>());
                    foundConn.seats[faction].Add((long)(entity as BasePlayer).userID, UnityEngine.Time.time);

                }
                return 1;
            }
            public override int GetDemand(string faction, Node refNode = null)
            {
                //Find hights demand, create connection, return
                Node foundConn = null;
                int totalDemand = 0;
                int i = 1;
                if (lastCached < UnityEngine.Time.time - 5)
                {
                    CleanSeats();
                    lastCached = UnityEngine.Time.time;
                    foreach (Node c in links.Keys)
                    {
                        if (refNode != null && c == refNode) continue;
                        float scale = 1;
                        if (c is NodeTransit) {
                           if( (c as NodeTransit).lastCached == 0)
                            continue;
                            scale = 0.7f;
                        }

                        int thisDemand = (int)(c.GetDemand(faction) * scale);
                        foundConn = c;
                        totalDemand += thisDemand;
                        i++;
                    }
                    if (!seats.ContainsKey(faction))
                        seats.Add(faction, new Dictionary<long, float>());
                    lastDemand = (totalDemand / i) - seats[faction].Count();
                    return (int)lastDemand;
                }
                else
                {
                    return (int)lastDemand;
                }
                //TODO:Remove
            }

        }
        public class NodeWarp : NodeTransit
        {
            public Transform A;
            public Transform B;

            public NodeWarp(Transform t1,Transform t2):base("Subway Entrance " + ((int)t1.position.x) + ":" + ((int)t1.position.z), new Vector3[] { t1.position,t2.position},1,3)
            {
                A = t1;
                B = t2;
            }
        }
        public class NodeMonument:NodeInterest
        {
            public NodeMonument(MonumentInfo mn, int min, int max) : base(mn.displayPhrase.translated.Replace("\r", "").Replace("\n", "").Replace("\t", ""), new Vector3[] { mn.transform.position}, min, max,mn.Bounds,mn.transform)
            {
                if (points==null||points.Count() == 0)
                {
                    points = new List<Vector3>();
                    points.Add(mn.transform.position);
                }
                name = mn.displayPhrase.translated.Replace("\r", "").Replace("\n", "").Replace("\t", "");
                if (config.MonumentDefinitions.ContainsKey(name))
                {
                    MonumentDefinition md = config.MonumentDefinitions[name];
                    radius = md.radius;
                    seatMax = md.monumentMax;
                    seatMin = md.monumentMin;
                    Vector3 newPoint = (mn.transform.localToWorldMatrix * md.centerOffset);
                    points[0] += newPoint;
                }
                else
                {
                    MonumentDefinition md = new MonumentDefinition();
                    md.radius = 50;
                    md.monumentMax = 5;
                    md.monumentMin = 2;
                    md.centerOffset = new Vector3(0, 0, 0);

                    config.MonumentDefinitions.Add(name, md);
                }
            }

        }
        public class NodeCustomInterest:NodeInterest
        {
            public NodeCustomInterest(string n, Vector3[] pts, int min, int max, Transform t) : base(n, pts, min, max,new Bounds(),t)
            {

            }
        }
        public enum WarpStates {
            A,
            WarpToB,
            LeaveB,
            B,
            WarpToA,
            LeaveA
        }
        public class NodeInterest : Node
        {
            public float radius = 20;
            public Vector3 center;
            public NodeInterest(string n, Vector3[] pts, int min, int max, Bounds B, Transform t) : base(n, pts, min, max)
            {
                radius = B.max.x;
                //center = (t.localToWorldMatrix*B.center);
                center += t.transform.position;
                //ColorWrite(B.center.ToString(), ConsoleColor.Yellow);
            }
            public static void findNearest(List<Connection> queue, Vector3 position, string faction="")
            {
                Node found = null;
                float NodeDistance = -1;
                List<NodeInterest> list = new List<NodeInterest>();
                foreach(Node n in Node.nodePool)
                {
                    if(n is NodeInterest && (position.y < 0) == (n.points[0].y<0))
                    {
                        NodeInterest ni = n as NodeInterest;
                        float newDistance = Vector3.Distance(ni.points[0], position);
                        list.Add(ni);
                        if ((faction==null||faction == "" || (
                                n.seats.ContainsKey(faction) &&(n.seats[faction].Count()>0)
                                )
                            )
                            && (NodeDistance == -1 || newDistance < NodeDistance)) {
                            found = ni;
                            NodeDistance = newDistance;
                        }

                    }
                }
                //ColorWrite(position.ToString() + " picked: " + found.name, ConsoleColor.Cyan);
                string l = "";
                if (found == null) { found = list[UnityEngine.Random.Range(0, list.Count())]; }
                found.Extend(queue, 0, null, out l);
            }
            public override int Step(List<Connection> connections, int idx, BaseEntity entity, out string logging)
            {

                /*
                 * base
                 * 
                 * get demand of target
                 * get self demand
                 * if thisdemand < 0 | next > this
                 *  next.extend
                 *  nextidx=next.fromidx
                 *  connections.remove me
                 *  return nextidx
                 * if seats contains ent
                 *  if(seats.count < max)
                 *      update ent timestamp
                 * else
                 *  if(seats.count < max)
                 *      add ent timestamp
                 *  else
                 *      next.extend
                 *      nextidx=next.fromidx
                 *      remove self
                 *      return nextidx
                 *  return idx++;
                 * 
                 * 
                 * */
                logging = "";
                base.Step(connections, idx, entity, out logging);
                string faction = omninasty.getFaction(entity);
                float targetDemand  = connections[0].finish.GetDemand(faction, this);
                float thisDemand = GetDemand(faction, this);
                int nextIdx = idx;
                if (targetDemand > thisDemand && thisDemand<1) {
                    if(!seats.ContainsKey(faction))
                    {
                        seats.Add(faction, new Dictionary<long, float>());
                    }
                    if (seats[faction].ContainsKey((long)(entity as BasePlayer).userID)) seats[faction].Remove((long)(entity as BasePlayer).userID);
                    if (connections.Count()==1)
                    {
                        logging += "Extending next step";
                        connections[0].finish.Extend(connections,idx, entity, out logging);
                        if (connections.Count() > 1)
                        {                            
                            if (!connections[1].finish.seats.ContainsKey(faction))
                                        connections[1].finish.seats.Add(faction, new Dictionary<long, float>());
                            if (connections[1].finish.seats[faction].ContainsKey((long)(entity as BasePlayer).userID))
                                connections[1].finish.seats[faction][(long)(entity as BasePlayer).userID] = UnityEngine.Time.time;
                            else
                                connections[1].finish.seats[faction].Add((long)(entity as BasePlayer).userID, UnityEngine.Time.time);
                        }
                    }
                    if(connections[0].finish.links.ContainsKey(this))
                        nextIdx = connections[0].finish.links[this];

                    logging += "; After extend, count = "+connections.Count();
                    logging += "; Removing This";
                    connections.RemoveAt(0);
                    return nextIdx;
                }
                if (seats[faction].Count() < seatMax && seats[faction].ContainsKey((long)(entity as BasePlayer).userID))
                {
                    nextIdx = idx + 1;
                }
                else
                {
                    logging += "Don't Switch Before extend, count = " + connections.Count();
                    if (connections.Count() == 1)
                    {
                        logging += ";Extending next step";
                        connections[0].finish.Extend(connections, idx, entity, out logging);
                        nextIdx = idx + 1;
                    }
                    if (connections[0].finish.links.ContainsKey(this))
                        nextIdx = connections[0].finish.links[this];
                    logging += "; After extend, count = " + connections.Count();
                    logging += "; Removing This";

                    if (connections.Count() > 1)
                        connections.RemoveAt(0);
                }
                return nextIdx;

            }
            public override Vector3 GetPoint(List<Connection> connections, int idx, BaseEntity entity, out string logging, bool connected = true)
            {
                /*
                 * pick from points at random until in range or nearest
                 * 
                 * */
                logging = "";
                return base.GetPoint(connections, idx, entity, out logging);
            }
            public override int GetDemand(string faction, Node refNode =null) {
                CleanSeats();
                if (faction == null || faction == "")
                    return 0;
                if (!seats.ContainsKey(faction)) seats.Add(faction,new Dictionary<long, float>());
                int seatCount = seats[faction].Count();
                if (seatCount > seatMax)
                {
                    return seatMax - seatCount;
                }
                if (seatCount < seatMin)
                {
                    return seatMin - seatCount;
                }
                return 0;
            }
        }
        #endregion 

        #region prefabDetails
        /*********************************************************************A
            assets/content/structures/sewers/sewer_tunnel_300_high_ladder.prefab
                x -1.5	y +9.5
                x -1.5	y +0.5	
            assets/content/structures/sewers/sewer_tunnel_300_ladder.prefab
                x -1.5	y +6.5
                x -1.5	y +0.5	
            assets/bundled/prefabs/autospawn/tunnel-station/station-sn-0.prefab
            assets/bundled/prefabs/autospawn/tunnel/straight-sn-4.prefab (4 and 5 are rooms)

            assets/content/structures/train_tunnels/corridor_prefabs/modules/corridor_train_tunnel_entrance_a.prefab
            assets/content/structures/train_tunnels/corridor_prefabs/modules/corridor_train_tunnel_entrance_b.prefab
         A**********************************************************************/
        #endregion


        #region Chat Commands
        [Command("send.warp")]
        void setOrderToPlayerPos(IPlayer player, string command, string[] args)
        {
            foreach(Node n in Node.nodePool)
            {
                if (n is NodeInterest)
                {
                    ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { 60, Color.red, (n as NodeInterest).center, "[M]" });
                }
                else if (n is NodeTransit)
                {
                    foreach (Vector3 v in (n as NodeTransit).points) { 
                        ConsoleNetwork.BroadcastToAllClients("ddraw.text", new object[] { 60, Color.green, v, "@" });
                    }
                }
            }
        }
        #endregion

        NodeMonument startNode = null;
        List<NodeMonument> visitedNodes = new List<NodeMonument>();
        List<Connection> globalQueue= new List<Connection>();

        #region Server Commands
        [Command("resetGlobal")]
        void resetGlobal(IPlayer player, string command, string[] args)
        {
            ColorWrite("Clearing.", ConsoleColor.Red);
            globalQueue.Clear();
            foreach (Node n in Node.nodePool)
            {
                if (n is NodeMonument && n != startNode && !visitedNodes.Contains(n)) { ColorWrite(n.name, ConsoleColor.Green); visitedNodes.Add(startNode); startNode = n as NodeMonument; break; }
            }
            return;
        }
        [Command("stepMon")]
        void stepMon(IPlayer player, string command, string[] args)
        {
            resetGlobal(player, command, args);
            for (int z = 0; z < 3000; z++)
            {
                if (startNode == null) return;
                string l = "";
                if (globalQueue.Count() == 0) { startNode.Extend(globalQueue, 0, null, out l); }
                else { 
                    if (!globalQueue.Last().parent.seats.ContainsKey("Bandits")) { globalQueue.Last().parent.seats.Add("Bandits", new Dictionary<long, float>()); }
                    globalQueue.Last().finish.Extend(globalQueue, 0, null, out l); globalQueue.Last().parent.seats["Bandits"].Add(UnityEngine.Random.Range(0, 2000000000), UnityEngine.Time.time + 200); }
                if (globalQueue.Count() == 0||globalQueue[0].finish == null)
                {
                    resetGlobal(player, command, args);
                }
            }
            ColorWrite("Next connection: " + (globalQueue[0].start != null ? globalQueue[0].start.name : "nullval") + " > " +(globalQueue[0].parent != null ? globalQueue[0].parent.name : "nullval")+" > "+(globalQueue[0].finish != null ? globalQueue[0].finish.name : "nullval"), ConsoleColor.Yellow);
            if (globalQueue.Count() > 1)
            {
                for (int i = 1; i < globalQueue.Count(); i++)
                {
                    ConsoleColor conscol = (globalQueue[i].start.name.Contains("Bandit") || globalQueue[i].start.name.StartsWith("Out") ? ConsoleColor.Magenta : ConsoleColor.Cyan);
                    conscol = (globalQueue[i].start.name.Contains("Rail") ? ConsoleColor.Red : conscol);
                    //if(conscol== ConsoleColor.Cyan) { continue; }
                    ColorWrite("Future connection: " + (globalQueue[i].start != null ? globalQueue[i].start.name : "nullval") + " > " + (globalQueue[i].parent != null ? globalQueue[i].parent.name : "nullval") + " > " + (globalQueue[i].finish != null ? globalQueue[i].finish.name : "nullval"),conscol);
                }
            }
        }
        [Command("listmons")]
        void ListMons(IPlayer player, string command, string[] args)
        {
            foreach (Node n in Node.nodePool)
            {
                if (n is NodeMonument) { ColorWrite(n.name, ConsoleColor.Green); startNode = n as NodeMonument; }
                else if (n is NodeWarp) ColorWrite(n.name, ConsoleColor.Red);
                else if (n is NodeCustomInterest) ColorWrite(n.name, ConsoleColor.Yellow);
                else if (n is NodeInterest) ColorWrite(n.name, ConsoleColor.Cyan);
                else if (n is NodeTransit) ColorWrite(n.name, ConsoleColor.Magenta);
            }
        }
        #endregion

        public static void ColorWrite(string s, ConsoleColor c)
        {
            System.Console.ForegroundColor = c;
            System.Console.WriteLine(s);
            System.Console.ResetColor();
        }
        #region Initialization        
        #region Hooks
        void OnServerInitialized()
        {
            SetupNetwork();
        }
        #endregion
        #region Functions
        void ScanAllPrefabs()
        {
            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go.activeInHierarchy)
                {
                    //Object parentObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                    //string path = UnityEditor.AssetDatabase.GetAssetPath(go);
                    string path = go.transform.name;
                    bool isGood = false;
                    foreach (string s in sewers)
                    {
                        isGood = (isGood || path.Contains(s));
                        if(path.Contains(s)) SewerLadders.Add(go);
                    }
                    foreach (string s in trainTops)
                    {
                        isGood = (isGood || path.Contains(s));
                        if (path.Contains(s)) TrainTops.Add(go);
                    }
                    foreach (string s in trainBottom)
                    {
                        isGood = (isGood || path.Contains(s));
                        if (path.Contains(s)) TrainBottoms.Add(go);
                    }
                    foreach (string s in customMonuments)
                    {
                        isGood = (isGood || path.Contains(s));
                        if (path.Contains(s)) CustomMonuments.Add(go);
                    }
                    if (isGood)
                    {
                        targets.Add(go);
                    }
                }
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(targets.Count() + " targets found");
            Console.ResetColor();
        }
        void SetupNetwork()
        {
            ScanAllPrefabs();
            SetupCustomMonument();
            ScanMonuments();
            LinkSubway();
            ScanRoads();
            ScanInterestConnection();
            ScanTransitConnection();
            SaveConfig();
        }
        void SetupCustomMonument()
        {
            List<NodeCustomInterest> nodes = new List<NodeCustomInterest>();
            foreach (GameObject t1 in CustomMonuments)
            {
                NodeCustomInterest nw = new NodeCustomInterest("Subway "+ ((int)t1.transform.position.x) + ":" + ((int)t1.transform.position.z),new Vector3[] { t1.transform.position},2,3,t1.transform);
                nodes.Add(nw);
                Node.nodePool.Add(nw);
            }
            foreach (NodeCustomInterest nci in nodes)
            {
                foreach (NodeCustomInterest nci2 in nodes)
                {
                    if (Math.Abs(nci.points[0].y - nci2.points[0].y) < 10 && nci.points[0].y < 0)
                    {
                        if(!(nci.links.ContainsKey(nci2)))
                            nci.links.Add(nci2, 0);
                    }
                }
            }

        }
        void LinkSubway()
        {
            foreach (GameObject t1 in TrainTops)
            {
                GameObject bottomHalf=null;
                float bottomDistance=-1;
                foreach (GameObject t2 in TrainBottoms)
                {
                    float newDistance = Vector3.Distance(t1.transform.position, t2.transform.position);
                    if (newDistance < bottomDistance || bottomHalf == null)
                    {
                        bottomDistance= newDistance; bottomHalf = t2;
                    }
                }
                NodeWarp nw = new NodeWarp(t1.transform, bottomHalf.transform);
                Node.nodePool.Add(nw);

                Node topNodeResult = null;
                Node bottomNodeResult = null;
                float topNodeDistance = -1;
                float bottomNodeDistance = -1;
                foreach (Node n in Node.nodePool)
                {
                    NodeMonument topNode = n as NodeMonument;
                    if (topNode == null) continue;
                    if (topNodeResult == null || (topNode.radius > 20 && (Vector3.Distance(topNode.points[0], nw.A.position) < topNodeDistance)))
                    {
                        topNodeDistance = Vector3.Distance(topNode.points[0], nw.A.position);
                        topNodeResult = topNode;
                    }
                }
                foreach (Node n in Node.nodePool)
                {
                    NodeCustomInterest bottomNode = n as NodeCustomInterest;
                    if (bottomNode == null) continue;
                    if (bottomNodeResult == null || Vector3.Distance(bottomNode.points[0], nw.B.position) < bottomNodeDistance)
                    {
                        bottomNodeDistance = Vector3.Distance(bottomNode.points[0], nw.B.position);
                        bottomNodeResult = bottomNode;
                    }
                }
                if (topNodeResult != null)
                {
                    if (!topNodeResult.links.ContainsKey(nw))
                        topNodeResult.links.Add(nw, 0);
                    if (!nw.links.ContainsKey(topNodeResult))
                        nw.links.Add(topNodeResult, 0);
                    ColorWrite("Subway tied to monument " + topNodeResult.name, ConsoleColor.Cyan);
                }
                if (bottomNodeResult != null)
                {
                    if (!bottomNodeResult.links.ContainsKey(nw))
                        bottomNodeResult.links.Add(nw, 0);
                    if (!nw.links.ContainsKey(bottomNodeResult))
                        nw.links.Add(bottomNodeResult, 0);

                    ColorWrite("Subway tied to monument " + topNodeResult.name, ConsoleColor.Cyan);
                }

            }

        }
        void ScanMonuments()
        {
            ColorWrite("Scanning Monuments", ConsoleColor.DarkMagenta);
            foreach (MonumentInfo mi in TerrainMeta.Path.Monuments.ToArray())
            {
                string key = mi.displayPhrase.translated.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "");
                if (mi.transform.position != (new Vector3(0, 0, 0)) && !key.Contains("ighthouse") && key != "")
                {
                    NodeMonument monument = new NodeMonument(mi, 1, 2);
                    Node.nodePool.Add(monument);
                }
            }
        }
        void ScanRoads()
        {
            ColorWrite("Scanning Roads", ConsoleColor.Magenta);
            foreach (PathList mi in TerrainMeta.Path.Roads.ToArray())
            {
                Vector3[] roadPoints = mi.Path.Points.ToArray();
                NodeTransit road = new NodeTransit("Road " + roadPoints.Count(), roadPoints, 1, 3);
                Node.nodePool.Add(road);
            }
            foreach (PathList mi in TerrainMeta.Path.Rails.ToArray())
            {
                Vector3[] roadPoints = mi.Path.Points.ToArray();
                //ColorWrite("Found a rail with " + roadPoints.Count(),ConsoleColor.Green);
                NodeTransit road = new NodeTransit("Rail " + roadPoints.Count(), roadPoints, 1, 3);
                Node.nodePool.Add(road);
            }
            /*
            foreach (PathList mi in TerrainMeta.Path.Powerlines.ToArray())
            {
                Vector3[] roadPoints = mi.Path.Points.ToArray();
                NodeTransit road = new NodeTransit("Power " + roadPoints.Count(), roadPoints, 1, 3);
                Node.nodePool.Add(road);
            }
            */
        }
        void ScanInterestConnection()
        {
            ColorWrite("Scanning Monument Connection",ConsoleColor.Magenta);
            foreach (Node mn3 in Node.nodePool)
            {
                if (!(mn3 is NodeInterest)) continue;
                NodeInterest mn = (mn3 as NodeInterest);
                string key = mn.name;
                if (key.Contains("derwater") || key.Contains("ilRig") || key == "") continue;
                bool isOrphan = true;
                LinkInterestsToTransit(mn, 1, out isOrphan);
                if (isOrphan)
                    LinkInterestsToTransit(mn, 2, out isOrphan);
                if (isOrphan)
                    LinkInterestsToTransit(mn, 4, out isOrphan);
                if (isOrphan)
                    LinkInterestsToTransit(mn, 8, out isOrphan);
                if (isOrphan)
                    ColorWrite(mn.name.Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace(" ", "") + " is orphaned", ConsoleColor.DarkRed);
            }
        }
        void LinkInterestsToTransit(NodeInterest mn, float scale, out bool isOrphan)
        {
            isOrphan = true;
            foreach (Node r2 in Node.nodePool.ToArray())
            {
                if (!(r2 is NodeTransit)) continue;
                NodeTransit r = r2 as NodeTransit;
                int bestPoint = -1; float bestDist = -1; Vector3[] points = r.points.ToArray();
                for (int i = 0; i < points.Count(); i++)
                {
                    if (r.links.ContainsKey(mn)) continue;
                    if (Vector3.Distance(points[i], mn.points[0]) < ((config.monumentRoadDistance * scale) + (mn.radius)) && (points[i].y < 0 == mn.points[0].y < 0))
                    {
                        if ((bestDist == -1 || Vector3.Distance(points[i], mn.points[0]) < bestDist))
                        {
                            bestPoint = i;
                            bestDist = Vector3.Distance(points[i], mn.points[0]);
                        }
                    }
                }
                if (bestDist > -1)
                {
                    isOrphan = false;
                    mn.links.Add(r, bestPoint); r.links.Add(mn, bestPoint);
                }
            }
        }
        void ScanTransitConnection()
            {
                ColorWrite("Scanning transit intersects",ConsoleColor.Magenta);
                foreach (Node mn1 in Node.nodePool)
                {
                    if(!(mn1 is NodeTransit)  ) continue;
                    NodeTransit mn = mn1 as NodeTransit;
                    bool isOrphan = true; int linkCount = 0;
                    foreach (Node r2 in Node.nodePool)
                    {
                        if (!(r2 is NodeTransit)) continue;
                        NodeTransit r = r2 as NodeTransit;
                        if (mn.links.ContainsKey(r) || r == mn) { continue; }
                        int bestPointMe = -1; int bestPointOther = -1; float bestDist = -1;
                        Vector3[] points = r.points.ToArray(); Vector3[] selfPoints = mn.points.ToArray();
                        for (int i = 0; i < points.Count(); i++){ for (int j = 0; j < selfPoints.Count(); j++){
                            if (Vector3.Distance(points[i], selfPoints[j]) < config.roadRoadDistance)
                                if (bestDist == -1 || Vector3.Distance(points[i], selfPoints[j]) < bestDist)
                                {
                                    bestPointMe = j; bestPointOther = i;
                                    bestDist = Vector3.Distance(points[i], selfPoints[j]);
                                }
                        }}
                        if (bestDist > -1)
                        {
                            mn.links.Add(r,bestPointMe);
                            isOrphan = false; linkCount++;
                        }
                    }
                    if (isOrphan)
                        ColorWrite("Road " + mn.points.Count().ToString() + " is orphaned",ConsoleColor.DarkRed);
                }
            }
            #endregion
        #endregion


    }
}