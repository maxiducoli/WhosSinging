namespace WhosSinging.Web.Configuration;

public class MusicLibraryOptions
{
    public const string SectionName = "MusicLibrary";

    public string BasePath { get; set; } = string.Empty;

    public string DatabaseFileName { get; set; } = "database_juego.json";
}