using System.Diagnostics;
using System.Reflection;

namespace ThesisBackendAPI;

public class FormattedAction
{
    public string action;
    public string target;
    public string information;
    public bool success;
    public string prizes;

    public static List<FormattedAction> FormatActions(List<SpecificAction> actions)
    {
        if (actions == null) return null;
        List<FormattedAction> formattedActions = new List<FormattedAction>();
        foreach (var a in actions)
        {
            try
            {
                FormattedAction formattedAction = new FormattedAction();
                Debug.Print($"Action: {a.action}");
                formattedAction.action = a.action;
                Debug.Print($"Target: {a.target}");
                formattedAction.target = a.target;
                Debug.Print($"Information: {a.information}");
                formattedAction.information = a.information;
                formattedAction.prizes = "";
                Debug.Print("possible actions:\n");
                foreach (var property in typeof(CharacterStats).GetFields())
                {
                    Debug.Print($"{property.Name}");
                }

                FieldInfo info = typeof(CharacterStats).GetField(a.action.ToLower());
                if (info == null)
                {
                    Debug.Print($"Action {a.action} not found incorrect");
                    continue;
                }

                Debug.Print($"Info: {info.Name}");
                int? skillPoint = info.GetValue(WorldStateInfo.stats) as int?;
                if (!skillPoint.HasValue) continue;
                Event? target = AppData.GetEventByName(a.target);
                if (target != null && RoomManager.currentRoom.events.Select(x => x.name).ToList().Contains(target.name))
                {
                    int cd;
                    IPrizes p;
                    try
                    {
                        p = AppData.GetEventByName(a.target).difficulty[a.action.ToLower()].prizes;
                        cd = target.difficulty[a.action.ToLower()].cd;
                    }
                    catch (KeyNotFoundException)
                    {
                        cd = 1000;
                        p = null;
                    }

                    if (target.bonus_object != null && target.bonus_object.ContainsKey(a.action.ToLower()))
                    {
                        bool useBonus = false;
                        if (WorldStateInfo.inventory != null && WorldStateInfo.inventory.Count >= 0)
                            foreach (var equip in WorldStateInfo.inventory)
                            {
                                if (equip.HasValue && equip.Value.name == target.bonus_object[a.action.ToLower()])
                                    useBonus = true;
                            }

                        foreach (var equip in WorldStateInfo.characterEquips.Values)
                        {
                            if (equip.HasValue && equip.Value.name == target.bonus_object[a.action.ToLower()])
                                useBonus = true;
                        }

                        if (useBonus) cd -= 5;
                    }

                    formattedAction.success = cd <= skillPoint.Value;
                    if (formattedAction.success)
                    {
                        RoomManager.currentRoom.events.Remove(target);
                        if (target.following_event != null && target.following_event != "")
                        {
                            Event newEvent = AppData.GetEventByName(target.following_event);
                            if (newEvent != null && !RoomManager.currentRoom.events.Select(x => x.name).ToList()
                                    .Contains(newEvent.name))
                            {
                                RoomManager.currentRoom.events.Add(newEvent);
                            }
                        }
                    }

                    string prizes = "";
                    if (p != null)
                        if (p is EquipPrizes equipPrizes)
                            prizes = Narrator.Capitalize(string.Join(", ", equipPrizes.Prizes.Select(e => e.name)));
                        else if (p is StringPrizes stringPrizes)
                            prizes = stringPrizes.Prizes;
                    formattedAction.prizes = prizes;
                    if (formattedAction.success && p != null && p is EquipPrizes ep)
                        WorldStateInfo.addEquipments(ep.Prizes);
                    formattedActions.Add(formattedAction);
                }
                else
                {
                    NPC? target1 = AppData.GetNPCByName(a.target);
                    bool npc_found = target1 != null;
                    if (!npc_found)
                        Debug.Print($"NPC {a.target} not found");
                    else Debug.Print($"NPC {a.target} found: {target1}");
                    if (target1 == null) continue;
                    formattedAction.success = true;
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                continue;
            }
           
        }
        return formattedActions;
    }
}