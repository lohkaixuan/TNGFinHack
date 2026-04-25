namespace ApiApp.AI;

public interface IAiService
{
    Task<string> AskAsync(string prompt);
}
