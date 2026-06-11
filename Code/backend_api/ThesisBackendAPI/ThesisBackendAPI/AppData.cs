using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThesisBackendAPI;

public struct Equip
{
    [JsonInclude]
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string name;
    [JsonInclude]
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("description")]
    public string description;
    [JsonInclude]
    [Required]
    [JsonPropertyName("compatibilities")]
    public List<string> compatibilities;
    [JsonInclude]
    [Required]
    [StringLength(10, MinimumLength = 2)]
    [JsonPropertyName("characteristic")]
    public string characteristic;
    [JsonInclude]
    [Required]
    [JsonPropertyName("bonus")]
    public int bonus;
}

public struct Feature
{
    [JsonInclude]
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string name;
    [JsonInclude]
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("description")]
    public string description;
    [JsonInclude]
    [Required]
    [JsonPropertyName("bonuses")]
    public Dictionary<string, int> bonuses;
}

public struct Data
{
    public List<Equip> equipments;
    public List<Feature> features;
    public List<TempEvent> events;
    public List<NPC> npcs;
    public List<TempAct> acts;
}

public class DifficultyData
{
    public int cd { get; set; }
    public object prizes { get; set; }
}

public struct TempEvent
{
    public string name;
    public string description { get; set; }
    public Dictionary<string, DifficultyData> difficulty { get; set; }

    public bool block_path = false;

    public Dictionary<string, string> bonus_object = null;
    
    public string following_event = null;

    public TempEvent()
    {
        name = null;
        description = null;
        difficulty = null;
    }
}

public struct Adventure
{
    public string name;
    public string description;
    public List<Objective> objectives;
    public List<Act> acts;
    public string ending_prompt;
}

public struct TempRoom
{
    public int id;
    public string description;
    public List<string> characters;
    public List<string> events;
    public List<string> paths;
    public List<int> connections;
}

public struct TempAct
{
   public int number;
   public string location;
   public string introductive_prompt;
   public List<TempRoom> rooms;
}

public struct Act
{
    public int number;
    public string location;
    public string introductive_prompt;
    public List<Room> rooms;
}

public struct Objective
{
    public string name;
    public string description;
    public List<Event> events;
}

public struct CharacterInfo
{
    public string name;
    public string description;
    public string archetype;
}

public struct SceneInfo
{
    public string user_input = "";
    public string prompt = "";
    public string generated_text = "";
    public double search_ms = 0.0;
    public double path_finding_ms = 0.0;
    public double extraction_ms = 0.0;
    public double generation_ms = 0.0;
    public double has_joined_ms = 0.0;

    public SceneInfo()
    {
    }
}
public struct Test
{
    public string user_id;
    public bool architecture;
    public CharacterInfo character_info;
    public List<SceneInfo> scenes = new List<SceneInfo>();

    public Test()
    {
        user_id = null;
        architecture = false;
        character_info = default;
    }
}

public struct TestData
{
    [JsonPropertyName("testers_number")]
    public int testers_number;
    [JsonPropertyName("tests")]
    public List<Test> tests;

    public TestData(){}
}

public static class AppData
{
    private static string DataPath = Path.Combine(AppContext.BaseDirectory, "Resources", "AppData.json");
    private static string TestingDataPath = Path.Combine(AppContext.BaseDirectory, "Resources", "TestData.json");
    private static TestData _testData;
    public static Test test = new Test();
    private static string userID;
    private static List<Equip> equips = new List<Equip>();
    private static List<Feature> features = new List<Feature>();
    private static List<Event> events = new List<Event>();
    private static List<NPC> npcs = new List<NPC>();
    private static List<Act> acts = new List<Act>();
    
    public static string Init()
    {
        Debug.WriteLine("========= Data Path ========");
        Debug.WriteLine(DataPath);
        Debug.WriteLine("============================");
        string json = "";
        Data data;
        if (File.Exists(DataPath)) json = File.ReadAllText(DataPath);
        if (json != "")
        {
            try
            {
                data = JsonConvert.DeserializeObject<Data>(json);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "error in deserialization: " + e.Message;
            }
            string tData = File.ReadAllText(TestingDataPath);
            try
            {
                _testData = JsonConvert.DeserializeObject<TestData>(tData);
                Debug.WriteLine($"Test data path: {TestingDataPath}");
                Debug.WriteLine("TEST NUMBER: " + _testData.testers_number);
                Debug.Print("TESTS NUMBER: " +_testData.tests.Count.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            equips = data.equipments;
            features = data.features;
            DeserializeEvents(data);
            npcs = data.npcs;
            foreach (var npc in npcs)
            {
                Debug.WriteLine("NPC: " + npc.name);
            }
            DeserializeActs(data);
            GenerateUserID();
            return "success";
        }
        return "json non existing or empty at: " + DataPath;
    }

    public static void GenerateUserID()
    {
        string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int n = _testData.testers_number;
        string userID = "";
        do
        {
            userID = chars[n % chars.Length] + userID;
            n /= chars.Length;
        } while (n > 0);
        test.user_id = userID.PadLeft(4, '0');
        _testData.testers_number += 1;
    }

    private static void DeserializeActs(Data data)
    {
        foreach (TempAct tAct in data.acts)
        {
            Act act = new Act();
            act.introductive_prompt = tAct.introductive_prompt;
            act.number = tAct.number;
            act.location = tAct.location;
            act.rooms = new List<Room>();
            foreach (TempRoom tRoom in tAct.rooms)
            {
                Room room = new Room();
                room.id = tRoom.id;
                room.description = tRoom.description;
                room.connections = tRoom.connections;
                room.paths = tRoom.paths;
                room.events = new List<Event>();
                room.npcs = new List<NPC>();
                foreach (string e in tRoom.events)
                {
                    room.events.Add(GetEventByName(e));
                }
                foreach (string c in tRoom.characters)
                {
                    room.npcs.Add(GetNPCByName(c));
                }
                act.rooms.Add(room);
            }
            acts.Add(act);
        }
        
    }

    private static void DeserializeEvents(Data data)
    {
        foreach (TempEvent tEvent in data.events)
        {
            Event e = new Event();
            e.name = tEvent.name;
            e.description = tEvent.description;
            e.difficulty = new Dictionary<string, (int cd, IPrizes prizes)>();
            e.block_path = tEvent.block_path;
            e.bonus_object = tEvent.bonus_object;
            e.following_event = tEvent.following_event;
            foreach (var d in tEvent.difficulty)
            {
                if (d.Value?.prizes == null) 
                {
                    Debug.Print($"Attenzione: prizes è null per {tEvent.name} - {d.Key}");
                    e.difficulty[d.Key] = (d.Value.cd, null);
                    continue;
                }
            
                Debug.Print($"Prize type: {d.Value.prizes.GetType().Name}");
                int cd = d.Value.cd;
                IPrizes prizes = null;
                
                if (d.Value.prizes is string s)
                {
                    prizes = new StringPrizes { Prizes = s };
                }
                else if (d.Value.prizes is JArray jArray)
                {
                    var equipPrizes = new EquipPrizes();
                    equipPrizes.Prizes = new List<Equip>();
                
                    foreach (var prizeToken in jArray)
                    {
                        string prizeName = prizeToken.ToString();
                        Equip? equip = GetEquipByName(prizeName);
                        if (equip.HasValue)
                        {
                            equipPrizes.Prizes.Add(equip.Value);
                        }
                    }
                    prizes = equipPrizes;
                }
            
                e.difficulty[d.Key] = (cd, prizes);
            }
            events.Add(e);
        }
    }
    public static Equip? GetEquipByName(string equipName)
    {
        if (equips != null)
            Debug.Print(equips.Count.ToString());
        else Debug.Print(equips.Count.ToString());
        List<Equip> eqs = equips.Where(x => x.name == equipName)?.ToList();
        if (eqs.Count == 0) return null;
        return eqs.First();
    }
    
    public static Feature? GetFeatureByName(string featureName)
    {
        List<Feature> ft = features.Where(x => x.name == featureName).ToList();
        if (ft.Count == 0) return null;
        return ft.First();
    }

    public static Event? GetEventByName(string eventName)
    {
        List<Event> ev = events.Where(x => x.name == eventName).ToList();
        if (ev.Count == 0) return null;
        return ev.FirstOrDefault();
    }

    public static NPC? GetNPCByName(string npcName, bool tollerance = false)
    {   
        List<NPC> ns = npcs.Where(x => x.name == npcName).ToList();
        if (ns.Count == 0 && !tollerance) return null;
        if (ns.Count != 0)
        {
            return ns.FirstOrDefault();
        }
        Func<string, string> name_variant_lowercase = x => x.ToLower();
        Func<string,List<string>> name_variant_split = x => x.Split(' ').ToList();
        Func<string, string> name_variant_union = x => x.Replace(" ", string.Empty);
        foreach (var npc in npcs)
        {
            if (name_variant_lowercase(npc.name) == npcName) return npc;
        }
        foreach (var npc in npcs)
        {
            if (name_variant_union(npc.name.ToLower()) == npcName.ToLower()) return npc;
        }
        foreach (var npc in npcs)
        {
            if (name_variant_split(npc.name.ToLower()).Contains(npcName.ToLower())) return npc;
        }
        return null;
    }

    public static List<Room> getActsRooms(int actId)
    {
        return acts.Where(act => act.number == actId).ToList().First().rooms.ToList();
    }

    public static string getActPrompt(int actId)
    {
        return acts.Where(act => act.number == actId).Select(a => a.introductive_prompt).First();
    }
    
    public static string getActLocation(int actId)
    {
        return acts.Where(act => act.number == actId).Select(a => a.location).First();
    }

    public static int getActsNumber()
    {
        return acts.Count;
    }

    public static void SaveTestData()
    {
        Debug.WriteLine("Saving Test Data");
        try
        {
            _testData.tests.Add(test);
            Debug.WriteLine(_testData.testers_number.ToString());
            Debug.WriteLine(test.user_id.ToString());
            Debug.WriteLine(test.scenes.Count.ToString());
            Debug.WriteLine($"Data available at Path: {TestingDataPath}");
            File.WriteAllText(TestingDataPath, JsonConvert.SerializeObject(_testData));
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error saving data: " + e.Message);
        }
        
    }
}