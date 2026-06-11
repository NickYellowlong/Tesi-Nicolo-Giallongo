namespace ThesisBackendAPI;

public interface IPrizes { }

public class EquipPrizes : IPrizes
{
    public List<Equip> Prizes { get; set; }
}

public class StringPrizes : IPrizes
{
    public string Prizes { get; set; }
}
public class Event
{
    public string name { get; set; }
    public string description { get; set; }
    public Dictionary<string, (int cd, IPrizes prizes)> difficulty { get; set; }
    
    public bool block_path;

    public Dictionary<string, string> bonus_object;
    
    public string following_event { get; set; }


}