namespace server.Core;

public static class Config
{
    public static readonly double WorldMetadataTTLHours = 24;

    // 캐싱 데이터 파일 위치
    public static readonly string DatabaseJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Database", "Data.json");
    public static readonly string DatabaseJsonTempPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Database", "Data.json.tmp");

    // 파일 스캔할 대상 폴더
    public static readonly string ScanFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "static", "worlds");

}