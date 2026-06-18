namespace BlogProject.Services.Interfaces
{
    public interface ILogService
    {
        void LogAction(string action, string details = "");
        void LogError(string message, Exception? ex = null);
        void LogWarning(string message);
        void LogInfo(string message);
    }
}