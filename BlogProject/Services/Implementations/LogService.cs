using NLog;
using BlogProject.Services.Interfaces;

namespace BlogProject.Services.Implementations
{
    public class LogService : ILogService
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public void LogAction(string action, string details = "")
        {
            var user = System.Security.Claims.ClaimsPrincipal.Current?.Identity?.Name ?? "Anonymous";
            var message = $"User: {user} | Action: {action}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | Details: {details}";
            }
            _logger.Info(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.Error(ex, message);
            else
                _logger.Error(message);
        }

        public void LogWarning(string message)
        {
            _logger.Warn(message);
        }

        public void LogInfo(string message)
        {
            _logger.Info(message);
        }
    }
}