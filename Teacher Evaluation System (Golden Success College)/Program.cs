using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<Teacher_Evaluation_System__Golden_Success_College_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Teacher_Evaluation_System__Golden_Success_College_Context")
        ?? throw new InvalidOperationException("Connection string not found.")));

// MVC + Swagger
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
// Register EmailService
builder.Services.AddScoped<EmailService>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// Authorization Policies (USING CLAIM NAME "RoleName")
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireClaim("RoleName", "Admin", "Super Admin"));

    options.AddPolicy("SuperAdminPolicy", policy =>
        policy.RequireClaim("RoleName", "Super Admin"));

    options.AddPolicy("StudentPolicy", policy =>
        policy.RequireClaim("RoleName", "Student"));
});

var app = builder.Build();

// Development vs Production Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Teacher Evaluation System API V1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// MVC Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapControllers();

app.Run();
