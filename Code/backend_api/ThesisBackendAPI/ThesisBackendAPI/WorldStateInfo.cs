using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ThesisBackendAPI;


public struct CharacterStats
{
    [JsonInclude]
    [Required]
    [JsonPropertyName("fighting")]
    public int fighting;
    [JsonInclude]
    [Required]
    [JsonPropertyName("athletics")]
    public int athletics;
    [JsonInclude]
    [Required]
    [JsonPropertyName("surviving")]
    public int surviving;
    [JsonInclude]
    [Required]
    [JsonPropertyName("tinkering")]
    public int tinkering;
    [JsonInclude]
    [Required]
    [JsonPropertyName("talking")]
    public int talking;
    [JsonInclude]
    [Required]
    [JsonPropertyName("observing")]
    public int observing;
    [JsonInclude]
    [Required]
    [JsonPropertyName("spellcasting")]
    public int spellcasting;
    [JsonInclude]
    [Required]
    [JsonPropertyName("thinking")]
    public int thinking;
}

public class WorldStateInfo
{
    public static readonly List<string> EQUIPMENT_SLOTS = new List<string> 
    { 
        "right_hand", 
        "left_hand", 
        "armor", 
        "head", 
        "accessory" 
    };
    public static Character character;
    public static CharacterStats stats;
    public static Dictionary<string, Equip?> characterEquips = new Dictionary<string, Equip?>();
    public static List<Equip?> inventory = new List<Equip?>();
    public static List<Feature> features = new List<Feature>();
    public static List<NPC> party = new List<NPC>();
    public static string objective;
    public static string motivation;
    public static int act = 1;
    public static void addCharacter(Character ch)
    {
        character = ch;
        if (!character.equipment.Equals(null))
        {
            Debug.Print("Equipment not null");
            PropertyInfo[] fields = typeof(Equipment).GetProperties();
            Debug.Print($"Total fields in Equipment: {fields.Length}");
            var stringFields = fields.Where(f => f.PropertyType == typeof(string)).Count();
            Debug.Print($"String fields in Equipment: {stringFields}");
            PropertyInfo[] characteristics = typeof(Characteristics).GetProperties();
            int i = 0;
            foreach (PropertyInfo field in fields)
            {
                if (field.PropertyType == typeof(string) && field.GetValue(character.equipment) != null)
                {
                    Equip? equip = AppData.GetEquipByName((string)field.GetValue(character.equipment));
                    if (equip != null)
                    { 
                        Debug.Print($"Equip {i}: {equip.Value.name} of {field.Name}");
                        characterEquips[field.Name] = equip;
                        PropertyInfo[] c = characteristics.Where(x => equip.Value.characteristic == x.Name).ToArray();
                        var tempCharacteristics = character.characteristics;
                        var matchingField = c.First();
                        int currentValue = (int)matchingField.GetValue(tempCharacteristics);
                        Debug.Print($"Current value of characteristic {matchingField}: {currentValue}");
                        matchingField.SetValue(tempCharacteristics, currentValue + equip.Value.bonus);
                        character.characteristics = tempCharacteristics;
                    }
                }
                i++;
            }
            Debug.Print("Loops for equipments: " + i);
        }
        if (character.features != null && character.features.Count > 0)
        {
            Debug.Print("Features not null");
            foreach (string feature in character.features)
            {
                if (string.IsNullOrEmpty(feature)) continue;
                Feature? f = AppData.GetFeatureByName(feature);
                if (f != null)
                {
                    Debug.Print($"Feature {feature} not null");
                    features.Add(f.Value);
                }
            }
        }
            computeStats();
    }

    private static void computeStats()
    {
        Dictionary<string, int> statsBonuses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["fighting"] = 0,
            ["athletics"] = 0,
            ["surviving"] = 0,
            ["tinkering"] = 0,
            ["talking"] = 0,
            ["observe"] = 0,
            ["spellcasting"] = 0,
            ["thinking"] = 0
        };

        if (features != null && features.Count > 0)
        {
            Debug.Print("Features not null");
            foreach (var feature in features)
            {
                if (feature.bonuses != null)
                {
                    foreach (var bonus in feature.bonuses)
                    {
                        if (!statsBonuses.ContainsKey(bonus.Key))
                            statsBonuses[bonus.Key] = 0;
                        statsBonuses[bonus.Key] += bonus.Value;
                        Debug.Print($"Bonus {bonus.Key} of {feature.name} is {bonus.Value}");
                    }
                }
            }
        }

        if (party != null && party.Count > 0)
        {
            Debug.Print("Party not null");
            foreach (var npc in party)
            {
                    if (npc.statsBonuses != null)
                    {
                        foreach (var bonus in npc.statsBonuses)
                        {
                            if (!statsBonuses.ContainsKey(bonus.Key))
                                statsBonuses[bonus.Key] = 0;
                            statsBonuses[bonus.Key] += bonus.Value;
                            Debug.Print($"Bonus {bonus.Key} of {npc} is {bonus.Value}");
                        }
                    }
            }
        }
        
        
        stats = new CharacterStats();
        stats.fighting = (int)(Math.Ceiling(((double)(character.characteristics.strength + character.characteristics.agility +
                                                      character.characteristics.resistance) / 3)) + statsBonuses["fighting"]);
        Debug.Print($"Fighting: {stats.fighting}");
        stats.athletics = (int)(Math.Ceiling(((double)(character.characteristics.strength + character.characteristics.agility) / 2)) + statsBonuses["athletics"]);
        Debug.Print($"Athletics: {stats.athletics}");
        stats.surviving = (int)(Math.Ceiling(((double)(character.characteristics.resistance + character.characteristics.instinct) / 2)) + statsBonuses["surviving"]);
        Debug.Print($"Surviving: {stats.surviving}");
        stats.tinkering = (int)(Math.Ceiling(((double)(character.characteristics.agility + character.characteristics.instinct + character.characteristics.knowledge) / 3)) + statsBonuses["tinkering"]);
        Debug.Print($"Tinkering: {stats.tinkering}");
        stats.talking = character.characteristics.charisma + statsBonuses["talking"];
        Debug.Print($"Talking: {stats.talking}");
        stats.observing = character.characteristics.instinct + statsBonuses["observe"];
        Debug.Print($"Observing: {stats.observing}");
        stats.spellcasting = (int)(Math.Ceiling(((double)(character.characteristics.instinct + character.characteristics.knowledge +
                                                          character.characteristics.charisma) / 3)) + statsBonuses["spellcasting"]);
        Debug.Print($"Spellcasting: {stats.spellcasting}");
        stats.thinking = (int)(Math.Ceiling(((double)(character.characteristics.knowledge + character.characteristics.instinct) / 2)) + statsBonuses["thinking"]);
        Debug.Print($"Thinking: {stats.thinking}");
    }

    public static Dictionary<string, object?> getCharacterInfo()
    {
        Dictionary<string, object?> characterInfo = new Dictionary<string, object?>();
        characterInfo["character"] = character;
        characterInfo["equipments"] = characterEquips;
        characterInfo["features"] = features;
        characterInfo["stats"] = stats;
        return characterInfo;
    }

    public static NPC addPartyMember(NPC npc)
    {
        if (npc == null) return null;
        if (party.Contains(npc)) return npc;
        party.Add(npc);
        RoomManager.currentRoom.npcs.Remove(npc);
        computeStats();
        return npc;
    }

    public static void addEquipments(List<Equip> equipments)
    {
        Debug.Print($"Old Inventory: {inventory.Aggregate("", (s, equip) => $"{s} {equip?.name}")}");
        Debug.Print($"Adding equipments: {equipments.Aggregate("", (s, equip) => $"{s} {equip.name}" )}");
        if (equipments == null) return;
        foreach (Equip equipment in equipments)
            inventory.Add(equipment);
        Debug.Print($"Added {equipments.Count} equipments");
        Debug.Print($"New Inventory: {inventory.Aggregate("", (s, equip) => $"{s} {equip?.name}")}");
    }

    public static void switchEquipment(Equip equip, string position)
    {
        bool equipped = false;
        position = position.ToLower();
        Debug.Print($"Inserting equipment {equip.name} at {position}");
        if (!EQUIPMENT_SLOTS.Contains(position)) {Debug.Print($"Slot {position} not found");return;}
        if (!inventory.Contains(equip) && !characterEquips.ContainsValue(equip)) {Debug.Print($"Equip {equip.name} not found"); return;}
        
        Debug.Print($"Position: '{position}', Length: {position.Length}, Bytes: {BitConverter.ToString(Encoding.UTF8.GetBytes(position))}");
        Debug.Print($"Compatibilities bytes:");
        foreach (var comp in equip.compatibilities)
        {
            Debug.Print($"  '{comp}', Length: {comp.Length}, Bytes: {BitConverter.ToString(Encoding.UTF8.GetBytes(comp))}");
        }
        
        bool isCompatible = equip.compatibilities
            ?.Select(c => c?.Trim().ToLower())
            .Contains(position) ?? false;

        if (!isCompatible)
        {
            Debug.Print($"Equipment {equip.name} not compatible with slot {position}");
            Debug.Print($"Normalized compatibilities: [{string.Join(", ", equip.compatibilities?.Select(c => c?.Trim().ToLower()) ?? new List<string>())}]");
            return;
        }        Debug.Print($"Inserting started");
        Equip? oldEquip = characterEquips.ContainsKey(position)? characterEquips[position] : null;
        Debug.Print($"Old equip: {oldEquip?.name} in {position}");
        if (inventory.Contains(equip))
        {
            Debug.Print($"{equip.name} is in inventory");
            Debug.Print($"Current inventory {inventory.Aggregate("", (s, equip) => $"{s} {equip?.name}")}");
            inventory.Remove(equip);
            Debug.Print($"Old equip: {equip.name} removed from inventory");
            Debug.Print($"Current inventory {inventory.Aggregate("", (s, equip) => $"{s} {equip?.name}")}");
            characterEquips[position] = equip;
            Debug.Print($"Now {equip.name} is in {position}");
        }
        else if (characterEquips.ContainsValue(equip))
        {
            if (characterEquips.FirstOrDefault(x => x.Value.Value.name == equip.name).Key == position)
                return;
            Debug.Print($"{equip.name} is in equipment");
            equipped = true;
            var x = characterEquips.FirstOrDefault(x=> ((x.Value).Value.name == equip.name)).Key;
            characterEquips[x] = null;
            characterEquips[position] = equip;
            Debug.Print($"Now {equip.name} is in {position}");
        }
        inventory.Add(oldEquip);
        Debug.Print($"Now {oldEquip?.name} is in inventory");
        PropertyInfo[] characteristics = typeof(Characteristics).GetProperties();
        PropertyInfo[] c_new = characteristics.Where(x => equip.characteristic == x.Name).ToArray();
        PropertyInfo[] c_old = characteristics.Where(x => oldEquip?.characteristic == x.Name).ToArray();
        var tempCharacteristics = character.characteristics;
        var matchingFieldNew = c_new.First();
        Debug.Print($"New equip {equip.name} gives bonus to {matchingFieldNew?.Name}");
        int currentValueNew = (int)matchingFieldNew.GetValue(tempCharacteristics);
        Debug.Print($"Characteristic {matchingFieldNew?.Name} has value {currentValueNew}");
        if (!equipped)
        {
            matchingFieldNew.SetValue(tempCharacteristics, currentValueNew + equip.bonus);
            Debug.Print($"New value for characteristic {matchingFieldNew?.Name} is {currentValueNew} + {equip.bonus} = {matchingFieldNew?.GetValue(tempCharacteristics)}");
        }
        if (oldEquip != null)
        {
            var matchingFieldOld = c_old.First();
            Debug.Print($"Old equip {oldEquip?.name} used to give bonus to {matchingFieldOld?.Name}");
            int currentValueOld = (int)matchingFieldOld.GetValue(tempCharacteristics);
            Debug.Print($"Characteristic {matchingFieldOld?.Name} has value {currentValueOld}");
            matchingFieldOld.SetValue(tempCharacteristics, currentValueOld - oldEquip?.bonus);
            Debug.Print($"New value for characteristic {matchingFieldOld?.Name} is {currentValueOld} - {oldEquip?.bonus} = {matchingFieldOld?.GetValue(tempCharacteristics)}");
        }
        character.characteristics = tempCharacteristics;
        computeStats();
    }
}