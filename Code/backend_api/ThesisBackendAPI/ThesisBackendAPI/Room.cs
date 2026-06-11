using System.Diagnostics;

namespace ThesisBackendAPI;

public class Room
{
    public int id { get; set; }
    public string description { get; set; }
    public List<NPC> npcs { get; set; }
    public List<Event> events { get; set; }
    public List<string> paths { get; set; }
    public List<int> connections { get; set; }

    public bool visited = false;

    public bool pathBlocked()
    {
        bool pathBlocked = false;
        if (events == null || events.Count == 0) return false;
        Debug.Print($"Room ID: {id}, Description: {description}");
        Debug.Print($"Events Count: {events.Count}");
        foreach (var e in events)
        {
            Debug.Print($"Event: {e.name}");
            Debug.Print($"Is block path: {e.block_path == null}");
            Debug.Print($"Block path: {e.block_path}");
            pathBlocked = pathBlocked || e.block_path;
        }
        return pathBlocked;
    }
}