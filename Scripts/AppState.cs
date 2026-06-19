public static class AppState
{
    public static int UserId;
    public static string Username = "";
    public static string FullName = "";
    public static string Role = "";

    public static string CurrentScenarioCode = "";
    public static string CurrentScenarioTitle = "";

    public static bool IsLoggedIn
    {
        get { return UserId > 0; }
    }

    public static bool IsAdmin
    {
        get { return Role == "admin"; }
    }

    public static void ClearUser()
    {
        UserId = 0;
        Username = "";
        FullName = "";
        Role = "";
        CurrentScenarioCode = "";
        CurrentScenarioTitle = "";
    }
}
