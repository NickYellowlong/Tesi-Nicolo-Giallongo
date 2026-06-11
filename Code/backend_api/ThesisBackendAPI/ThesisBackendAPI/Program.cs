using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThesisBackendAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();  // Abilita le annotazioni
    c.SwaggerDoc("v1", new() { Title = "Thesis API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:4173")  // URL del tuo Vite
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
    options.AddPolicy("AllowReactApp", policy =>
           {
               policy.WithOrigins("http://localhost:5173")  // URL del tuo Vite
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
           });
});

var app = builder.Build();

app.UseCors("AllowReactApp");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1"); });
}

app.MapGet("/api", () => Results.Ok(new { status = "running", version = "1.0" }));

app.MapGet("/init", (bool architecture) =>
{   
    Debug.WriteLine("Init API");
    Debug.WriteLine($"Architecture: {architecture}");
    string initComplete = AppData.Init();
    AppData.test.architecture = architecture;
    APICaller.Init();
    RoomManager.InitializeRooms();
    APICaller.RebuildIndexes();
    return Results.Ok(new { status = initComplete, user_id = AppData.test.user_id});
});

app.MapPost("/character", (Character character) =>
    {
        Debug.WriteLine("Create new character");
        WorldStateInfo.addCharacter(character);
        
        Debug.WriteLine("==========CHARACTER==========\n");
        foreach (var info in WorldStateInfo.getCharacterInfo())
        {
            Debug.WriteLine(info.Key + " : " + info.Value);   
        }

        AppData.test.character_info.name = character.name;
        AppData.test.character_info.description = character.description;
        AppData.test.character_info.archetype = character.archetype.ToString();
        return Results.Ok(WorldStateInfo.getCharacterInfo());
    })
    .WithName("PostCharacter");

app.MapGet("/character", () =>
    {
        Debug.WriteLine("Get character");
        return Results.Ok(WorldStateInfo.getCharacterInfo());
    })
    .WithName("GetCharacter");

app.MapPost("/objective", (string objective, string motivation) =>
    {
        Debug.WriteLine("Create new objective");
        WorldStateInfo.objective = objective;
        WorldStateInfo.motivation = motivation;
        return Results.Ok();
    })
    .WithName("PostObjective");

app.MapPost("/start", async () =>
    {
    Debug.WriteLine("Start the game");
    SceneInfo sceneInfo = new SceneInfo();
    sceneInfo.user_input = "";
    string prompt = Narrator.BuildIntroductionPrompt();
    sceneInfo.prompt = prompt;
    GeneratorResponse generatedResponse = await APICaller.Generate(prompt);
    GeneratedContent generatedContent = generatedResponse.content;
    sceneInfo.generated_text = generatedContent.narration;
    sceneInfo.generation_ms = generatedResponse.processing_time_ms;
    AppData.test.scenes.Add(sceneInfo);
    Debug.Print($"Generated narration into Program: {generatedContent.narration}");
    _ = Task.Run(async () =>
        {
            try
            {
                Narrator.UpdateWithSummaryAsync(generatedContent.narration);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }
    );
        return Results.Ok(
            new
            {
                narration = generatedContent.narration,
            }
            
            );
    })
    .WithName("Start");

app.MapPost("/first_scene", async () =>
    {
        Debug.WriteLine("First Scene");
        SceneInfo sceneInfo = new SceneInfo();
        string prompt = Narrator.BuildBasicPrompt(["the character continues their journey"], [], null, 1);
        sceneInfo.prompt = prompt;
        GeneratorResponse generatedResponse = await APICaller.Generate(prompt);
        GeneratedContent generatedContent = generatedResponse.content;
        sceneInfo.generated_text = generatedContent.narration;
        sceneInfo.generation_ms = generatedResponse.processing_time_ms;
        AppData.test.scenes.Add(sceneInfo);
        Debug.Print($"Generated narration into Program: {generatedContent.narration}");
        _ = Task.Run(async () =>
            {
                try
                {
                    Narrator.UpdateWithSummaryAsync(generatedContent.narration);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

            }
        );
        return Results.Ok(
            new
            {
                narration = generatedContent.narration,
            }
            
        );
    })
    .WithName("FirstScene");


app.MapPost("/action", async ( ActionRequest action = null) =>
    {
        string content = action?.content;
        Debug.Print($"Action: {content}");
        SceneInfo sceneInfo = new SceneInfo();
        int current_act = WorldStateInfo.act;
        string pathfinderContent = "";
        if (!string.IsNullOrEmpty(content))
        {
            pathfinderContent = "PATHS: {\n";
            List<string> paths = RoomManager.currentRoom.paths.ToList();
            List<int> connections = RoomManager.currentRoom.connections.ToList();
            foreach ((int i, string path) in connections.Zip(paths, (i, p) => (i, p)))
            {
                pathfinderContent += "\t\"" + i + "\" : \"" + path + (RoomManager.currentRoom.connections[RoomManager.currentRoom.paths.IndexOf(path)] == RoomManager.lastRoomId ? " (the character come from this path)" : "") + "\",\n";
            }
    
            pathfinderContent += "}\n\n";
            pathfinderContent += "SCENE:\n" + content;
        }
        List<NPC> currentNPCs = RoomManager.currentRoom.npcs.ToList();
        string searchContent="";
        if (RoomManager.currentRoom.npcs != null || RoomManager.currentRoom.events != null)
            searchContent = RoomManager.currentRoom.npcs.Aggregate("", (current, npc) => current + " " + npc.name) + " " + RoomManager.currentRoom.events.Aggregate("", (current, events) => current + " " + events.name) + WorldStateInfo.party.Aggregate("", (current, npc) => current + " " + npc.name) + "\n";
        if (WorldStateInfo.party != null)
            searchContent += WorldStateInfo.party.Aggregate("", (current, npc) => current + " " + npc.name);
        searchContent += AppData.getActLocation(WorldStateInfo.act);
        string extractContent = "";
        if (!string.IsNullOrEmpty(content))
            extractContent = 
                "PROTAGONIST: " + WorldStateInfo.character.name + "\n" +
                "POSSIBLE TARGETS: " +
                RoomManager.currentRoom.npcs.Aggregate("", (current, npc) => current + "\n" + npc.name) +
                RoomManager.currentRoom.events.Aggregate("", (current, e) => current + "\n" + e.name) + WorldStateInfo.party.Aggregate("", (current, npc) => current + "\n" + npc.name)+ "\n"  +
                content;
        
        Task<SearchResponse> searchTask = APICaller.Search(searchContent);
        Task<InfoExtractionResponse?> extractTask = APICaller.ExtractInfo(extractContent);
        Task<PathfindingResponse> pathfindTask = APICaller.FindPath(pathfinderContent);
        
        await Task.WhenAll(searchTask, extractTask, pathfindTask);
        
        SearchResponse searchResponse = await searchTask;
        InfoExtractionResponse? extractResponse = await extractTask;
        PathfindingResponse pathfindingResponse = await pathfindTask;
        
        sceneInfo.search_ms = searchResponse.processing_time_ms;
        ExtractResult? extractResult = extractResponse?.extracted_info;
        sceneInfo.extraction_ms = extractResponse.Value.processing_time_ms;
        int pathfindResult = pathfindingResponse.value;
        sceneInfo.path_finding_ms = pathfindingResponse.processing_time_ms;
        
        Debug.Print($"Pathfind result: {pathfindResult}");
        Debug.Print($"N. of Actions: {extractResult?.specific_actions.Count}");
        if (extractResult?.specific_actions != null)
            foreach (var a in extractResult?.specific_actions)
            {
                Debug.Print($"Action: {a.action}");
                Debug.Print($"Target: {a.target}");
                Debug.Print($"Information: {a.information}");
            }
        string prompt = Narrator.BuildBasicPrompt(extractResult?.action_list, FormattedAction.FormatActions(extractResult?.specific_actions), searchResponse.results?.Select(r => r.text).ToList(), pathfindResult);
        sceneInfo.prompt = prompt;
        //Debug.Print(prompt);
        GeneratorResponse generatedResponse = await APICaller.Generate(prompt);
        sceneInfo.generation_ms = generatedResponse.processing_time_ms;
        GeneratedContent generatedContent = generatedResponse.content;
        sceneInfo.generated_text = generatedContent.narration;
        Debug.Print($"Generated result:\n{generatedContent.narration}");
        string hasJoinedRequest = Narrator.BuildHasJoinedRequest(sceneInfo.generated_text, currentNPCs);
        NPC joinedCharacter = null;
        HasJoinedResponse hasJoinedResponse;
        if (!string.IsNullOrEmpty(hasJoinedRequest))
        {
            hasJoinedResponse = await APICaller.HasJoined(hasJoinedRequest);
            sceneInfo.has_joined_ms = hasJoinedResponse.processing_time_ms;
            if (hasJoinedResponse.joined)
            {
                Debug.Print($"NPC name: {hasJoinedResponse.target}");
                joinedCharacter = AppData.GetNPCByName(hasJoinedResponse.target, true);
                
            }
            if (joinedCharacter != null && RoomManager.currentRoom.npcs.Select(x => x.name).ToList().Contains(joinedCharacter.name))
            {
                Debug.Print($"NPC FOUND: {joinedCharacter.name}");
                WorldStateInfo.addPartyMember(joinedCharacter);
            }
        }
        AppData.test.scenes.Add(sceneInfo);
        _ = Task.Run(async () =>
            {
                try
                {
                    Narrator.UpdateWithSummaryAsync(sceneInfo.generated_text);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                
            }
        );
        Debug.Print($"CURRENT ACT: {WorldStateInfo.act}");
        if (WorldStateInfo.act >= 4)
            AppData.SaveTestData();
        return Results.Ok(new
        {
            narration = sceneInfo.generated_text,
            change_act = (current_act != WorldStateInfo.act),
            joined_character = joinedCharacter,
            end = (WorldStateInfo.act >= 4)
        }
        );
    })
    .WithName("PostAction");

app.MapPost("/equipment/switch", async (SwitchEquipmentRequest request) =>
    {
        string equipName = request.EquipName;
        string position = request.Position;
        Equip? equip = AppData.GetEquipByName(equipName);
        if(equip == null) return Results.Problem("Equipment not found");
        WorldStateInfo.switchEquipment(equip.Value, position);
        return Results.Ok(new
        {
            character = WorldStateInfo.getCharacterInfo(),
        }
        );
    }
    ).WithName("SwitchEquipment");

app.MapGet("/inventory", async () =>
{
    try
    {
        List<Equip?> inventory = WorldStateInfo.inventory;
        Debug.Print($"Inventory: {inventory.Aggregate("", (s, equip) => $"{s} {equip?.name}")}");
        return Results.Ok(inventory);
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message);
    }
    
}).WithName("GetInventory");

app.Run();