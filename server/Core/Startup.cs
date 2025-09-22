using server.Service;
namespace server.Core;

public class Startup
{
    public void OnStartup()
    {
        Database.Instance.LoadFromFile();
    }
}