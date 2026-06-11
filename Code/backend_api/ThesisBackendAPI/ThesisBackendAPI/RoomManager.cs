using System.Diagnostics;
using Newtonsoft.Json;

namespace ThesisBackendAPI;

public class RoomManager
{
    public static Room currentRoom;
    public static int lastRoomId;
    public static  Dictionary<int, Room> rooms = new Dictionary<int, Room>();
    private static readonly int ROOM_LIMIT = 999;

    public static void InitializeRooms()
    {
        Debug.WriteLine($"Initializing Rooms of act {WorldStateInfo.act}");
        List<Room> rs = AppData.getActsRooms(WorldStateInfo.act);
        foreach (Room r in rs)
            rooms[r.id] = r;
        if (WorldStateInfo.act == 3)
        {
            Debug.Print("OBJECTIVE: " + WorldStateInfo.objective);
            if (WorldStateInfo.objective ==
                "Retrieve the Mystic Crown of the Fallen King.\nThe Mystic Crown is said to be able to grant a single wish of its bearer.")
                currentRoom = rooms[1];
            else if (WorldStateInfo.objective ==
                "Find the treasure of the Fallen King.\nThis treasure is said to be immense, and contains gems worth more then entire kingdoms.")
                currentRoom = rooms[2];
            else if (WorldStateInfo.objective ==
                "Defeat the evil Shadowmancer.\nThe Shadowmancer is an evil sorcerer which plans to plunge the world in darkness.")
                currentRoom = rooms[3];
            else currentRoom = rooms[2];
            Debug.Print($"CURRENT ROOM ID: {currentRoom.id}");
            return;
        }
        currentRoom = rooms[1];
    }

    public static bool MoveRoom(int id)
    {
        Debug.Print($"Room ID: {id}");
        if (id < 0)
        {
            //handle negative id case
            return false;
        }

        if (id >= ROOM_LIMIT)
        {
            Debug.Print("Room ID is out of range");
            Debug.Print($"Changing act from {WorldStateInfo.act} to {WorldStateInfo.act + 1}");
            WorldStateInfo.act += 1;
            if (WorldStateInfo.act > 3) return true;
            InitializeRooms();
            return true;
        }
        if (currentRoom != null)
            lastRoomId = currentRoom.id;
        currentRoom = rooms[id];
        return false;
    }
}