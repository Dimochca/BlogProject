using Microsoft.AspNetCore.Mvc;
using BlogProject.Services.Interfaces;

namespace BlogProject.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public GlobalExceptionMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

                    logService.LogError($"Произошла ошибка в {context.Request.Path}", ex);

                    context.Response.Clear();
                    context.Response.StatusCode = 500;
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }
}