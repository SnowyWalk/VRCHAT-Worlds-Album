namespace server.Core;

public static class Config
{
    // 캐싱 데이터 파일 위치
    public static string DatabaseJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Database", "Data.json");
    
    // 파일 스캔할 대상 폴더
    public static string ScanFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "static", "worlds");
    
}