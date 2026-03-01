using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using TodoApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);


// הזרקת DbContext
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoList-DB"), 
    ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("ToDoList-DB"))));
// הגדרת CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()    // מאפשר גישה לכל הכתובות
              .AllowAnyMethod()    // מאפשר כל שיטת HTTP (GET, POST, PUT, DELETE)
              .AllowAnyHeader();   // מאפשר כל כותרת
});
});
// הגדרת JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,//מי הנפיק את הטוקן
            ValidateAudience = false, 
            ValidateLifetime = true,//תוקף
            ValidateIssuerSigningKey = true,//חתימה
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
//הוספת SWAGGER
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TodoApi", Version = "v1" });

    // הגדרת כפתור ה-Authorize בתוך Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var app = builder.Build();
//Swagger הגדרת  
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // מפעיל את Swagger
    app.UseSwaggerUI(); // מפעיל את ממשק המשתמש של Swagger
}
//שימוש ב CORS
app.UseCors("AllowAllOrigins"); 
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");

//רישום
app.MapPost("/register", async (ToDoDbContext db, User user) => 
{
    if (await db.Users.AnyAsync(u => u.Username == user.Username))
    {
        return Results.BadRequest("User already exists.");
    }

    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok("User registered successfully!");
}); 

app.MapPost("/login", async (ToDoDbContext db, User loginUser, IConfiguration config) => 
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == loginUser.Username);
    
    if (user == null || !BCrypt.Net.BCrypt.Verify(loginUser.Password, user.Password))
    {
        return Results.Unauthorized();
    }

    // --- יצירת הטוקן מתחילה כאן ---
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[] {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim("userId", user.Id.ToString())
    };

    var token = new JwtSecurityToken(
        issuer: config["Jwt:Issuer"],//מי הנפיק את הטוקן
        audience: null,//למי מיועד
        claims: claims,//הנתונים
        expires: DateTime.Now.AddHours(2), // תוקף לשעתיים
        signingCredentials: credentials);//החתימה

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    // --- סיום יצירת הטוקן ---

    return Results.Ok(new { token = jwt }); // מחזירים את הטוקן ל-React!
});

//שליפת כל המשימות
app.MapGet("/items", async (ToDoDbContext db, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst("userId")?.Value);
    return await db.Items.Where(i => i.UserId == userId).ToListAsync();
}).RequireAuthorization();

//הוספת משימה חדשה
app.MapPost("/items", async (ToDoDbContext db, Item newItem, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst("userId")?.Value);
    newItem.UserId = userId; // משייכים את המשימה למשתמש ששלח את הבקשה
    
    db.Items.Add(newItem);
    await db.SaveChangesAsync();
    return Results.Ok(newItem);
}).RequireAuthorization();

//עדכון משימה
app.MapPut("/items/{id}", async (ToDoDbContext db, int id, Item updatedItem, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst("userId")?.Value);
    var existingItem = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
    
    if (existingItem is null) return Results.NotFound("Task not found or unauthorized");

    existingItem.IsComplete = updatedItem.IsComplete;
    await db.SaveChangesAsync();
    return Results.Ok(existingItem);
}).RequireAuthorization();

//מחיקת משימה
app.MapDelete("/items/{id}", async (ToDoDbContext db, int id, ClaimsPrincipal user) =>
{
    var userId = int.Parse(user.FindFirst("userId")?.Value);
    var existingItem = await db.Items.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

    if (existingItem is null) return Results.NotFound("Task not found or unauthorized");

    db.Items.Remove(existingItem);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

app.Run();
