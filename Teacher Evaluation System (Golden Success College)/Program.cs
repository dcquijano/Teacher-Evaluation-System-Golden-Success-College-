using Microsoft.EntityFrameworkCore;
using Teacher_Evaluation_System__Golden_Success_College_.Data;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<Teacher_Evaluation_System__Golden_Success_College_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Teacher_Evaluation_System__Golden_Success_College_Context")
        ?? throw new InvalidOperationException("Connection string not found.")));

// Add services
builder.Services.AddControllersWithViews();  // For MVC views
builder.Services.AddEndpointsApiExplorer();   // For Swagger/OpenAPI
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Development vs Production Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Teacher Evaluation System API V1");
        options.RoutePrefix = "swagger"; // Swagger at /swagger
    });
}
else
{
    app.UseExceptionHandler("/Home/Error"); // Production error page
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API routes
app.MapControllers();

app.Run();
