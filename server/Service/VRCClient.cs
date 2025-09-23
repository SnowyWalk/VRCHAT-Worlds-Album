using server.Core;
using server.Schema;
using System.Net.Http.Headers;
using System.Text.Json;
namespace server.Service;

public class VRCClient
{
    private readonly HttpClient m_httpClient;

    public VRCClient()
    {
        m_httpClient = new HttpClient();
        m_httpClient.BaseAddress = new Uri("https://api.vrchat.cloud/api/1/");
        m_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        m_httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VRCHAT-Worlds-Album", "1.0"));
    }
    
    public async Task<WorldMetadata?> FetchVRCWorldMetadata(string worldId)
    {
        try
        {
            HttpResponseMessage req = await m_httpClient.GetAsync($"worlds/{worldId}");
            req.EnsureSuccessStatusCode();

            string body = await req.Content.ReadAsStringAsync();
            VRCWorldMetadata vrcWorldMetadata = JsonSerializer.Deserialize<VRCWorldMetadata>(body)!;
            
            return new WorldMetadata(vrcWorldMetadata, DateTime.UtcNow);
        }
        catch (HttpRequestException e)
        {
            Log.Error(e);
            return null;
        }
    }
}
