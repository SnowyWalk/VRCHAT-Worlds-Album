using server.Service;
namespace server.Core;

public class Startup
{
    Database db;
    
    public void OnStartup()
    {
        db = new();
        db.LoadFromFile();
    }
}