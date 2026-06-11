using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ThesisBackendAPI;

public class Narrator
{
    private static readonly object _stateLock = new object();
    private static string _lastScene = "";
    private static string _storySoFar = "";
    private static string _totalStory = "";
    
    public static string last_scene 
    { 
        get 
        {
            lock (_stateLock) return _lastScene;
        }
        set 
        {
            lock (_stateLock) _lastScene = value;
        }
    }

    public static string Capitalize(string s)
    {
        string result = Regex.Replace(s, @"_([a-zA-Z])", m => " " + m.Groups[1].Value.ToUpper());
        result = result[0].ToString().ToUpper() + result.Substring(1);
        return result;
    } 

    public static string BuildBasicPrompt(List<string> actions, List<FormattedAction> formattedActions, List<string> context, int path)
    {
        Debug.WriteLine($"STORY SO FAR: {_storySoFar}");
        Debug.WriteLine($"TOTAL STORY: {_totalStory}");
        bool descriptive = path != 0 && !RoomManager.currentRoom.pathBlocked();
        Debug.Print($"Path: {path}");
        Debug.Print($"PathBlocked: {RoomManager.currentRoom.pathBlocked()}");
        Debug.Print($"Descriptive: {descriptive}");
        bool next_act = false;
        if (descriptive && path != -3) next_act = RoomManager.MoveRoom(path);
        Debug.Print($"NextAct: {next_act}");
        if (next_act) return BuildTransitionPrompt(actions);
        string prompt;
        prompt = descriptive ? "SCENE TYPE: descriptive scene\n\n" : "SCENE TYPE: responsive scene\n\n";
        Debug.Print($"Context: {context?.Aggregate(prompt, (current, c) => $"{current}, {c}")}");
        if (context != null && context.Count > 0)
        {
            Debug.Print("Context not null or empty");
            prompt += "CONTEXT. Use these information to shape the story and the character's behaviour. DO NOT convey these information directly to the player as plain text, rather use character's dialogues and behaviours or environmental storytelling.";
                    prompt += "\n\n";
                    prompt += context.Aggregate("", (current, action) => current + "\n" + action);
                    prompt += "\n\n\n";
        }
        if (_storySoFar != null && _storySoFar.Replace(" ", "") != "")
        {
            Debug.Print("story so far not null or empty");
            prompt += "STORY IN THIS NARRATIVE ACT";
            prompt += "\n\n";
            prompt += _storySoFar;
            prompt += "\n\n\n";
        }
        Debug.Print($"Last scene: {last_scene ?? ""}");
        if (last_scene != null && last_scene != "")
        {
            Debug.Print("Last scene not null or empty");
            prompt += "LAST SCENE";
            prompt += "\n\n";
            prompt += last_scene;
            prompt += "\n\n\n";
        }

        prompt += "LOCATION: " + AppData.getActLocation(WorldStateInfo.act) + "\n\n";
        prompt += BuildCharacterInformation();
        prompt += "\n\n";
        prompt += "ROOM ELEMENTS";
        prompt += "\n\n";
        prompt += "Description: " + RoomManager.currentRoom.description + "\n";
        prompt += "Paths (from right to left): " + RoomManager.currentRoom.paths.Aggregate("", (current, path) => current + ", " + path + (RoomManager.currentRoom.connections[RoomManager.currentRoom.paths.IndexOf(path)] == RoomManager.lastRoomId ? " (the character come from this path)" : "" )) + "\n";
        //if (RoomManager.currentRoom.visited) prompt += "already described\n";
        RoomManager.currentRoom.visited = true;
        prompt += "\n";
        if (RoomManager.currentRoom.npcs != null && RoomManager.currentRoom.npcs.Count > 0 )
            prompt += "Characters: " + RoomManager.currentRoom.npcs.Aggregate("", (current, npc) => current + "\n" + Capitalize(npc.name) + ": " + npc.description) + "\n";
        if (RoomManager.currentRoom.events != null && RoomManager.currentRoom.events.Count > 0)
        {
            prompt += "Events: ";
            foreach (var e in RoomManager.currentRoom.events)
            {
                prompt += "\t" + e.name + ": " + e.description + "\n";
            }
        }
        
        prompt += "\n";
        if (!descriptive)
        {
            prompt += "The protagonist want to perform the following actions:\n";
                    prompt += actions.Aggregate("", (current, action) => current + "\n" + action);
                    prompt += "\n";
                    if (formattedActions.Count > 0)
                    {
                        prompt += "In particular, they:\n";
                                    foreach (var action in formattedActions)
                                        prompt += (action.success
                                            ? "Successfully performed"
                                            : "Failed in performing") + " the action: \"" + action.information + "\" with target: " +
                                              action.target + (action.success && action.prizes != null && action.prizes != "" ? ". The success results in the PROTAGONIST obtaining/achieving: " + action.prizes + "\n" :"\n");
                                    prompt += "\n";
                    }
            
                    prompt += "\n";
        }
        
        
        prompt += "ACT: " + WorldStateInfo.act;
        prompt +=
            "\n\nRemember:\nreply with only the story, no additional elements;\nalways narrate in third person;\nnever address the user directly.\nnever comment the story in any way";
        return prompt;
    }

    private static string BuildCharacterInformation()
    {
        string prompt = "";
        prompt += "PROTAGONIST INFO";
        prompt += "\n\n";
        Debug.Print(WorldStateInfo.character.ToString());
        Debug.Print(WorldStateInfo.character.name);
        prompt += "Name: " + WorldStateInfo.character.name + "\n";
        prompt += "Description: " + WorldStateInfo.character.description + "\n";
        prompt += "Objective: " + WorldStateInfo.objective + "\n";
        prompt += "Motivation: " + WorldStateInfo.motivation + "\n\n";
        prompt += "Knowledge: " + WorldStateInfo.character.characteristics.knowledge;
        prompt += "\n\n";
        if (WorldStateInfo.party != null && WorldStateInfo.party.Count > 0)
            prompt += "PARTY: " + WorldStateInfo.party.Aggregate("", (current, party) => current + "\n" + party.name + ": " + party.description) + "\n\n\n";
        if (WorldStateInfo.characterEquips.Values != null && WorldStateInfo.characterEquips.Count > 0)
            prompt += "EQUIPMENTs: " + WorldStateInfo.characterEquips.Values != null ? WorldStateInfo.characterEquips.Values.Aggregate("", (current, equip) => current + ", " + equip.Value.name) : "";
        return prompt;
    }

    public static string BuildIntroductionPrompt()
    {
        string prompt = "SCENE TYPE: introductive scene\n\n";
        prompt += "CONTEXT. Use these information to shape the story and the character's behaviour. DO NOT convey these information directly to the player as plain text, rather use character's dialogues and behaviours or environmental storytelling.";
        prompt += "\n\n";
        Debug.Print($"ACT NOW: {WorldStateInfo.act.ToString()}");
        prompt += AppData.getActPrompt(WorldStateInfo.act);
        prompt += "\n\n\n";
        prompt += BuildCharacterInformation();
        prompt += "\n\n";
        prompt += "ACT: " + WorldStateInfo.act;
        return prompt;
    }

    public static string BuildTransitionPrompt(List<string> actions)
    {
        Debug.Print($"Building transition prompt to act: {WorldStateInfo.act}");
        if (WorldStateInfo.act > 3)
            return BuildEndingPrompt(actions);
        string prompt = "SCENE TYPE: transition scene\n\n";
        prompt += "CONTEXT. Use these information to shape the story and the character's behaviour. DO NOT convey these information directly to the player as plain text, rather use character's dialogues and behaviours or environmental storytelling.";
        prompt += "\n\n";
        prompt += AppData.getActPrompt(WorldStateInfo.act);
        prompt += "\n\n\n";
        prompt += BuildCharacterInformation();
        prompt += "\n";
        if (_storySoFar != null && _storySoFar != "")
        {
            Debug.Print("story so far not null or empty");
            prompt += "STORY IN THIS NARRATIVE ACT";
            prompt += "\n\n";
            prompt += _storySoFar;
            prompt += "\n\n\n";
        }
        UpdateCompleteStory();
        prompt += "The protagonist want to perform the following actions:\n";
        prompt += actions.Aggregate("", (current, action) => current + "\n" + action);
        prompt += "\n";
        prompt += "ACT: " + WorldStateInfo.act;
        return prompt;
    }

    private static string BuildEndingPrompt(List<string> actions)
    {
        Debug.Print("Building ending prompt");
        string prompt = "SCENE TYPE: ending scene\n\n";
        prompt += "CONTEXT";
        prompt += "\n\n";
        prompt += AppData.getActPrompt(WorldStateInfo.act);//ending prompt
        prompt += "\n\n\n";
        prompt += BuildCharacterInformation();
        prompt += "\n";
        UpdateCompleteStory();
        if (_totalStory != null && _totalStory != "")
        {
            Debug.Print("story so far not null or empty");
            prompt += "TOTAL STORY:";
            prompt += "\n\n";
            prompt += _totalStory;
            prompt += "\n\n";
            prompt += "use it to recall the character journey.\n";
        }
        prompt += "The protagonist want to perform the following actions:\n";
        prompt += actions.Aggregate("", (current, action) => current + "\n" + action);
        prompt += "\n";
        return prompt;
    }

    public static async Task UpdateWithSummaryAsync(string newNarration)
    {
        Debug.Print("Start updating story so far");
        string oldLastScene;
        
        lock (_stateLock)
        {
            oldLastScene = _lastScene;
            Debug.Print($"Old last scene: {oldLastScene}");
        }
        
        string summary = null;
        if (!string.IsNullOrEmpty(oldLastScene))
        {
            Debug.Print("Genertaing summary");
            summary = await APICaller.GenerateSummary(oldLastScene);
            Debug.Print($"Summary generated: {summary}");
        }
        
        lock (_stateLock)
        {
            Debug.Print("Updating story so far");
            if (!string.IsNullOrEmpty(summary))
            {
                Debug.Print($"Old Story so far: {_storySoFar}");
                _storySoFar += "\n" + summary;
                Debug.Print($"New Story so far: {_storySoFar}");
            }
            Debug.Print($"Updaying last scene");
            _lastScene = newNarration;
            Debug.Print($"New last scene: {_lastScene}");
        }
    }

    private static void UpdateCompleteStory()
    {
        _totalStory += $"Act {WorldStateInfo.act - 1}:\n";
        _totalStory += _storySoFar + "\n";
        _storySoFar = "";
    }

    public static string BuildHasJoinedRequest(string generatedContentNarration, List<NPC> currentNPCs)
    {
        if (string.IsNullOrEmpty(generatedContentNarration)) return null;
        string prompt = "";
        prompt += "PROTAGONIST: " + WorldStateInfo.character.name + "\n";
        if ( currentNPCs == null && currentNPCs.Count == 0)
            return null;
        prompt += "CHARACTERS: " +currentNPCs.Aggregate("", (current, npc) => current + ", " + npc.name) + "\n";
        prompt += "SCENE: \n" + generatedContentNarration;
        return prompt;
    }
}
