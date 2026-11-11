namespace CoworkingMobile.Data;

public static class Constants
{
    public const string DatabaseFilename = "AppSQLite.db3";

    public static string DatabasePath =>
        $"Data Source={Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename)}";

    // Set to true if you want to test against your local API instead of Azure
    public const bool UseLocalApi = false;

    public const string ProductionApiUrl = "https://coworkingapi-e8dbgdbgdsfea9bv.canadacentral-01.azurewebsites.net";

    public static string LocalApiUrl => DeviceInfo.Platform == DevicePlatform.Android 
        ? "http://10.0.2.2:5200" 
        : "https://localhost:7200";

    public static string ApiBaseUrl => UseLocalApi ? LocalApiUrl : ProductionApiUrl;
}
