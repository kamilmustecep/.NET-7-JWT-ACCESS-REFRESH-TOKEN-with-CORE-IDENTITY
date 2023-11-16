using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using UserManagement.Api.Services;
using UserManagement.Data.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddTransient<IAuthService, AuthService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

var _GetConnectionString = builder.Configuration.GetConnectionString("defaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(_GetConnectionString));

// For Identity  
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

// Adding Authentication

#region JWT TokenValidationParameters A��klama

/*
 
1-ValidateIssuerSigningKey = true:
Bu durumda, JWT'nin i�indeki imzalama anahtar� do�rulan�r. Yani, JWT'nin ge�erlili�ini do�rulamak i�in kullan�lan imzalama anahtar�, belirtilen anahtarla e�le�melidir. Bu, tokenin g�venli bir �ekilde imzaland���ndan emin olunmas�n� sa�lar.


-----------------------------------------------------


2- ValidateAudience = false:
Bu durumda, JWT'nin i�indeki "audience" (hedef) alan� do�rulanmaz. Yani, JWT'nin belirli bir hedefe y�nlendirilip y�nlendirilmedi�i kontrol edilmez. Bu genellikle bir kullan�c�n�n tokenini di�er kullan�c�larla payla�mas�na olanak tan�r.

�rnek; ValidateAudience = true olarak ayarland���n� varsayal�m ve token �reten servisimizde Audience'yi belirtelim.

var tokenHandler = new JwtSecurityTokenHandler();
var tokenDescriptor = new SecurityTokenDescriptor
{
    // Di�er token ayarlar�...
    Audience = "hedefA",
};
var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);


------------------------------------------------------


3- ValidateIssuer = false:
Bu durumda, JWT'nin i�indeki "issuer" (veren) alan� do�rulanmaz. Yani, JWT'nin hangi servis veya uygulama taraf�ndan olu�turuldu�u kontrol edilmez. Bu durumda, tokeni �reten servisin kim oldu�u �nemli de�ildir.

�rnek; ValidateIssuer = true: olarak ayarland���n� varsayal�m ve token �reten servisimizde Issuer'i belirtelim.

var tokenHandler = new JwtSecurityTokenHandler();
var tokenDescriptor = new SecurityTokenDescriptor
{
    // Di�er token ayarlar�...
    Issuer = "myAuthService",
};
var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);


------------------------------------------------------


�rnek; ValidIssuer ve ValidAudience de�erleri belirtilerek token'ler ile rolleme i�lemi yap�labilir.
Bir Schema'ya sahip token farkl� bir schema ile configure olmu� servislere eri�im sa�layamaz.

services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer("UserManagement", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["AppSettings:UserManagementTokenKey"])),
            ValidateIssuer = true,
            ValidIssuer = "UserManagementApi",
            ValidateAudience = true,
            ValidAudience = "UserManagementAudience",
            // Di�er do�rulama parametreleri...
        };
    })
    .AddJwtBearer("ContentListing", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["AppSettings:ContentListingTokenKey"])),
            ValidateIssuer = true,
            ValidIssuer = "ContentListingApi",
            ValidateAudience = true,
            ValidAudience = "ContentListingAudience",
            // Di�er do�rulama parametreleri...
        };
    });


// UserController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "UserManagement")]
public class UserController : ControllerBase
{
    // Kullan�c� y�netimi ile ilgili i�lemler...
}


// ContentController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ContentListing")]
public class ContentController : ControllerBase
{
    // ��erik listeleme i�lemleri...
}



*/
#endregion

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWTKey:ValidAudience"],
                    ValidIssuer = builder.Configuration["JWTKey:ValidIssuer"],
                    //ClockSkew = TimeSpan.Zero,
                    //Bu ayar, JWT'nin "iat" ve "exp" zaman damgalar�n�n tam olarak e�le�mesini gerektirir.
                    //Yani, belirli bir cihaz veya uygulamadan g�nderilen JWT'nin, sunucu saatine g�re olu�turulma ve ge�erlilik s�resi zaman damgalar� tam olarak do�rulanmal�d�r.
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWTKey:Secret"]))
                };
            });





var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

