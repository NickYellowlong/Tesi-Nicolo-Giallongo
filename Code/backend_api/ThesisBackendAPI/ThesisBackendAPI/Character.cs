using System.ComponentModel.DataAnnotations;

namespace ThesisBackendAPI;

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;


public enum Archetype
{
    Warrior,
    Mage,
    Explorer,
    Bard
}
public class Characteristics
{
    [JsonInclude]
    [Required]
    [Range(1, 6)]
    [JsonPropertyName("strength")]
    public int strength { get; set; }
    
    [JsonInclude]
    [Required]
    [Range(1, 6)]
    [JsonPropertyName("agility")]
    public int agility { get; set; }
    
    [JsonInclude] 
    [Required]
    [Range(1, 6)]
    [JsonPropertyName("resistance")]
    public int resistance { get; set; }
    
    [JsonInclude]
    [Required]
    [Range(1, 6)]
    [JsonPropertyName("instinct")]
    public int instinct { get; set; }
    
    [JsonInclude]
    [Required]
    [Range(1, 6)]
    [JsonPropertyName("knowledge")]
    public int knowledge { get; set; }
    
    [JsonInclude]
    [Required]
    [Range(1, 6)]
    [JsonPropertyName("charisma")]
    public int charisma { get; set; }
}

public class Equipment
{
    [JsonInclude]
    [StringLength(50, MinimumLength = 0)]
    [JsonPropertyName("left_hand")]
    public string left_hand { get; set; }
    [JsonInclude]
    [StringLength(50, MinimumLength = 0)]
    [JsonPropertyName("right_hand")]
    public string right_hand { get; set; }
    [JsonInclude]
    [StringLength(50, MinimumLength = 0)]
    [JsonPropertyName("armor")]
    public string armor { get; set; }
    [JsonInclude]
    [StringLength(50, MinimumLength = 0)]
    [JsonPropertyName("head")]
    public string head { get; set; }
    [JsonInclude]
    [StringLength(50, MinimumLength = 0)]
    [JsonPropertyName("accessory")]
    public string accessory { get; set; }
}
public class Character
{
    [JsonInclude]
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [JsonPropertyName("name")]
    public string name { get; set; }
    
    [JsonInclude] 
    [Required]
    [StringLength(150, MinimumLength = 10)]
    [JsonPropertyName("description")]
    public string description { get; set; }
    
    [JsonInclude]
    [Required]
    [JsonPropertyName("characteristics")]
    public Characteristics characteristics { get; set; }
    
    [JsonInclude]
    [Required]
    [JsonPropertyName("archetype")]
    public Archetype archetype { get; set; }
    
    [JsonInclude]
    [Required]
    [JsonPropertyName("equipment")]
    public Equipment equipment { get; set; }
    
    [JsonInclude]
    [Required]
    [JsonPropertyName("features")]
    public List<string>? features { get; set; }
    
}