namespace ThesisBackendAPI;

public class NPC
{
    public string name { get; set; }
    public string description { get; set; }
    public string ability_description { get; set; }
    public string role { get; set; }
    public Dictionary<string, int> statsBonuses { get; set; }
}