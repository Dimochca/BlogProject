using Microsoft.EntityFrameworkCore;
using BlogProject.Data;
using BlogProject.Services.Interfaces;
using BlogProject.Services.Implementations;
using BlogProject.Models;
using BlogProject.Middleware;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllersWithViews();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IPostService, PostService>();
    builder.Services.AddScoped<ITagService, TagService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<ILogService, LogService>();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Cookies";
        options.DefaultSignInScheme = "Cookies";
        options.DefaultChallengeScheme = "Cookies";
    })
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
    });

    builder.Services.AddAuthorization();

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseStatusCodePages(async context =>
    {
        var response = context.HttpContext.Response;
        if (response.StatusCode == 403)
        {
            response.Redirect("/Home/AccessDenied");
        }
        else if (response.StatusCode == 404)
        {
            response.Redirect("/Home/NotFound");
        }
    });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "user",
        pattern: "User/{username}",
        defaults: new { controller = "User", action = "Details" });

    // ========== ИНИЦИАЛИЗАЦИЯ БАЗЫ ДАННЫХ ==========
    var initLogger = LogManager.GetCurrentClassLogger();

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            try
            {
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    initLogger.Info("Применяются миграции...");
                    await dbContext.Database.MigrateAsync();
                    initLogger.Info("Миграции применены успешно");
                }
            }
            catch (Exception ex)
            {
                initLogger.Error(ex, "Ошибка при применении миграций");
            }

            try
            {
                if (!dbContext.Roles.Any())
                {
                    initLogger.Info("Начинается инициализация базы данных...");

                    // 1. Создаём права (Permissions)
                    var permissions = new[]
                    {
                        new Permission { Name = "DeleteComments", Description = "Удаление комментариев", IsSystem = true },
                        new Permission { Name = "DeletePosts", Description = "Удаление постов", IsSystem = true },
                        new Permission { Name = "EditPosts", Description = "Редактирование постов", IsSystem = true },
                        new Permission { Name = "ManageRoles", Description = "Управление ролями", IsSystem = true },
                        new Permission { Name = "ManageUsers", Description = "Управление пользователями", IsSystem = true },
                        new Permission { Name = "ViewAllPosts", Description = "Просмотр всех постов (включая приватные)", IsSystem = true },
                        new Permission { Name = "CreateRoles", Description = "Создание ролей", IsSystem = true }
                    };

                    foreach (var perm in permissions)
                    {
                        if (!dbContext.Permissions.Any(p => p.Name == perm.Name))
                            dbContext.Permissions.Add(perm);
                    }
                    await dbContext.SaveChangesAsync();

                    // 2. Создаём системные роли
                    var roles = new[]
                    {
                        new Role { Name = "Owner", Color = "#ff0000", Position = 100, IsSystem = true },
                        new Role { Name = "Admin", Color = "#ff6b00", Position = 80, IsSystem = true },
                        new Role { Name = "Moderator", Color = "#007bff", Position = 60, IsSystem = true },
                        new Role { Name = "User", Color = "#6c757d", Position = 40, IsSystem = true }
                    };

                    foreach (var role in roles)
                    {
                        if (!dbContext.Roles.Any(r => r.Name == role.Name))
                            dbContext.Roles.Add(role);
                    }
                    await dbContext.SaveChangesAsync();

                    // 3. Назначаем права ролям
                    var allPermissions = dbContext.Permissions.ToList();
                    var ownerRole = dbContext.Roles.First(r => r.Name == "Owner");
                    var adminRole = dbContext.Roles.First(r => r.Name == "Admin");
                    var moderatorRole = dbContext.Roles.First(r => r.Name == "Moderator");
                    var userRole = dbContext.Roles.First(r => r.Name == "User");

                    // Owner - все права
                    foreach (var perm in allPermissions)
                    {
                        if (!dbContext.RolePermissions.Any(rp => rp.RoleId == ownerRole.Id && rp.PermissionId == perm.Id))
                            dbContext.RolePermissions.Add(new RolePermission { RoleId = ownerRole.Id, PermissionId = perm.Id });
                    }

                    // Admin - все права кроме CreateRoles
                    foreach (var perm in allPermissions.Where(p => p.Name != "CreateRoles"))
                    {
                        if (!dbContext.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == perm.Id))
                            dbContext.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = perm.Id });
                    }

                    // Moderator - DeleteComments, DeletePosts, EditPosts, ViewAllPosts
                    var modPermissions = new[] { "DeleteComments", "DeletePosts", "EditPosts", "ViewAllPosts" };
                    foreach (var permName in modPermissions)
                    {
                        var perm = allPermissions.First(p => p.Name == permName);
                        if (!dbContext.RolePermissions.Any(rp => rp.RoleId == moderatorRole.Id && rp.PermissionId == perm.Id))
                            dbContext.RolePermissions.Add(new RolePermission { RoleId = moderatorRole.Id, PermissionId = perm.Id });
                    }

                    await dbContext.SaveChangesAsync();

                    // 4. Назначаем роль "User" всем пользователям, у кого нет ролей
                    var allUsers = await userService.GetAllAsync();
                    foreach (var user in allUsers)
                    {
                        var userRoles = await roleService.GetUserRolesAsync(user.Id);
                        if (!userRoles.Any() && userRole != null)
                        {
                            await roleService.AssignRoleToUserAsync(user.Id, userRole.Id);
                            initLogger.Info($"Пользователю {user.UserName} назначена роль User");
                        }
                    }

                    initLogger.Info("Инициализация базы данных успешно завершена");
                }
                else
                {
                    initLogger.Info("База данных уже инициализирована");
                }
            }
            catch (Exception ex)
            {
                initLogger.Error(ex, "Ошибка при инициализации данных");
            }
        }
        catch (Exception ex)
        {
            initLogger.Error(ex, "Критическая ошибка при инициализации базы данных");
        }
    }

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Ошибка при запуске приложения");
    throw;
}
finally
{
    LogManager.Shutdown();
}