using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ThesisBackendAPI;

public struct SearchResult
{
    public string id { get; set; }
    public string text { get; set; }
    public double score { get; set; }
}

public struct SpecificAction
{
    [JsonPropertyName("action")]
    public string action { get; set; }
    
    [JsonPropertyName("target")]
    public string target { get; set; }
    
    [JsonPropertyName("information")]
    public string information { get; set; }
}
public struct ExtractResult
{
    [JsonPropertyName("action_list")]
    public List<string> action_list { get; set; }
    
    [JsonPropertyName("specific_actions")]
    public List<SpecificAction> specific_actions { get; set; }
}

public struct SearchResponse
{
    public SearchResponse()
    {
        query = null;
        results = new List<SearchResult>();
        processing_time_ms = 0;
        total_found = 0;
    }

    public string query { get; set; }
    public List<SearchResult> results { get; set; }
    public int total_found { get; set; }
    public double processing_time_ms { get; set; }
    
}

public struct InfoExtractionResponse
{
    [JsonPropertyName("extracted_info")]
    public ExtractResult extracted_info { get; set; }
    
    [JsonPropertyName("processing_time_ms")]
    public double processing_time_ms { get; set; }
}

public struct PathfindingResponse
{
    public int value { get; set; }
    public double processing_time_ms { get; set; }
}

public struct GeneratedContent
{
    public string narration { get; set; }
}
public struct GeneratorResponse
{
    public GeneratedContent content { get; set; }
    public double processing_time_ms { get; set; }
}

public struct SummaryResponse
{
    public string content { get; set; }
    public double processing_time_ms { get; set; }
}

public struct HasJoinedResponse
{
    public HasJoinedResponse()
    {
        target = null;
        processing_time_ms = 0;
    }

    public bool joined { get; set; } = false;
    public string target { get; set; }
    public double processing_time_ms { get; set; }
}

public class APICaller
{
    private const string APIBaseUrl = "http://127.0.0.1:8000/api";
    private const bool offlineMode = false;

    private static readonly Dictionary<string, string> roots = new Dictionary<string, string>()
    {
        ["retrieval"] = "/api/search",
        ["retrieval_documents"] = "/api/documents",
        ["retrieval_rebuild_indexes"] = "/api/rebuild/indexes",
        ["info_extraction"] = "/api/extract",
        ["pathfinding"] = "/api/find_path",
        ["text_generation"] = "/api/generate",
        ["summary"] = "/api/generate/summary",
        ["health_check"] = "/api/health",
        ["rebuildIndex"] = "api/indexes/rebuild",
        ["has_joined"] = "/api/has_joined"
    };
    
    private static Dictionary<string, (double Mean, int Count)> mean_processing_times = new ()
    {
        ["retrieval"] = (0.0, 0),
        ["info_extraction"] = (0.0, 0),
        ["text_generation"] = (0.0, 0),
        ["summary"] = (0.0, 0),
        ["pathfinding"] = (0.0, 0),
        ["has_joined"] = (0.0, 0),
    };
    
    
    private static HttpClient client = new HttpClient();

    public static void Init()
    {
        client.BaseAddress = new Uri(APIBaseUrl);
        client.DefaultRequestHeaders.Accept.Clear();
        client.Timeout = TimeSpan.FromSeconds(300);
    }
    
    private static async Task<T> CallAPI<T>(string method, string endpoint, string body = null)
    {
        HttpResponseMessage response;
        if (method == "GET")
        {
            response = await client.GetAsync(endpoint);
        }
        else if (method == "POST")
        {
            string safeBody = body ?? "";
            try
            {
                response = await client.PostAsync(endpoint,
                    new StringContent(safeBody, Encoding.UTF8, "application/json"));
                Debug.WriteLine("RAW RESPONSE:");
                Debug.WriteLine(response);
            }
            catch (Exception ex)
            {
                response = new HttpResponseMessage();
                Debug.WriteLine(ex.Message);
            }
        }
        else throw new Exception("Invalid method");
        string json = await response.Content.ReadAsStringAsync();
        Debug.WriteLine("RESPONSE:");
        Debug.WriteLine(json);
        JsonReader reader = new JsonTextReader(new StringReader(json));
        return JsonSerializer.Create().Deserialize<T>(reader);
    }

    public static async Task<SearchResponse> Search(string query)
    {
        Debug.Print("Search query: " + query);
        if (string.IsNullOrEmpty(query.Replace(" ",""))) return new SearchResponse();
        string method = "POST";
        string endpoint = roots["retrieval"];
        Dictionary<string, object> body = new Dictionary<string, object>()
        {
            ["query"] = query,
            ["top_k"] = 7,
            ["alpha"] = 0.85
        };
        
        SearchResponse response = await CallAPI<SearchResponse>(method, endpoint, JsonConvert.SerializeObject(body));
        
        return response;
    }

    public static async Task<InfoExtractionResponse?> ExtractInfo(string query)
    {
        if (string.IsNullOrEmpty(query)) return null;
        string method = "POST";
        string endpoint = roots["info_extraction"];
        Dictionary<string, string> body = new Dictionary<string, string>()
        {
            ["query"] = query
        };
        Debug.Print(body.ToString());
        InfoExtractionResponse response = await CallAPI<InfoExtractionResponse>(method, endpoint,JsonConvert.SerializeObject(body));
        
        return response;
    }
    
    public static async Task<PathfindingResponse> FindPath(string query)
    {
        Debug.Print("Pathfinding started");
        Debug.Print($"Query: {query}");
        if (string.IsNullOrEmpty(query)) return new PathfindingResponse(){processing_time_ms = 0.0, value = -3};
        string method = "POST";
        string endpoint = roots["pathfinding"];
        Dictionary<string, string> body = new Dictionary<string, string>()
        {
            ["query"] = query
        };
        
        PathfindingResponse response = await CallAPI<PathfindingResponse>(method, endpoint, JsonConvert.SerializeObject(body));

        Debug.Print($"Finished with result {response.value}");
        return response;
    }
    
    public static async Task<GeneratorResponse> Generate(string query)
    {
        string method = "POST";
        string endpoint = roots["text_generation"];
        Debug.Print($"Query: {query}");
        Dictionary<string, string> body = new Dictionary<string, string>()
        {
            ["query"] = query
        };
        
        Debug.Print($"Body: {JsonConvert.SerializeObject(body)}");
        
        GeneratorResponse response = await CallAPI<GeneratorResponse>(method, endpoint, JsonConvert.SerializeObject(body));
        Debug.Print($"Response content: {response.content.ToString()}");
        Debug.Print($"Response narration: {response.content.narration}");
        Debug.Print(response.processing_time_ms.ToString());
        
        return response;
    }

    public static async Task<string> GenerateSummary(string query)
    {
        if (string.IsNullOrEmpty(query)) return null;
        string method = "POST";
        string endpoint = roots["summary"];
        Dictionary<string, string> body = new Dictionary<string, string>()
        {
            ["query"] = query
        };
        Debug.Print(JsonConvert.SerializeObject(body));
        SummaryResponse response = await CallAPI<SummaryResponse>(method, endpoint,JsonConvert.SerializeObject(body));

        double current_mean_time = mean_processing_times["summary"].Mean;
        int current_instances = mean_processing_times["summary"].Count;
        double processing_time = response.processing_time_ms;
        
        mean_processing_times["summary"] = new ((current_mean_time*current_instances + processing_time)/(current_instances + 1), current_instances + 1);
        
        return response.content;
    }

    public static async void RebuildIndexes()
    {
        string method = "POST";
        string endpoint = roots["rebuildIndex"];
        Debug.Print($"endpoint: {endpoint}");
        
        await CallAPI<Dictionary<string, string>>(method, endpoint);
    }

    public static async Task<HasJoinedResponse> HasJoined(string query)
    {
        if (string.IsNullOrEmpty(query)) return default;
        string method = "POST";
        string endpoint = roots["has_joined"];
        Dictionary<string, string> body = new Dictionary<string, string>()
        {
            ["narration"] = query
        };
        Debug.Print(JsonConvert.SerializeObject(body));
        HasJoinedResponse response = await CallAPI<HasJoinedResponse>(method, endpoint,JsonConvert.SerializeObject(body));
        return response;
    }
}