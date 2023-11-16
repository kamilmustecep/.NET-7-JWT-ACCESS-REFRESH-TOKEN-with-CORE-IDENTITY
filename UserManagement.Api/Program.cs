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

#region JWT TokenValidationParameters Açýklama

/*
 
1-ValidateIssuerSigningKey = true:
Bu durumda, JWT'nin içindeki imzalama anahtarý doðrulanýr. Yani, JWT'nin geçerliliðini doðrulamak için kullanýlan imzalama anahtarý, belirtilen anahtarla eþleþmelidir. Bu, tokenin güvenli bir þekilde imzalandýðýndan emin olunmasýný saðlar.


-----------------------------------------------------


2- ValidateAudience = false:
Bu durumda, JWT'nin içindeki "audience" (hedef) alaný doðrulanmaz. Yani, JWT'nin belirli bir hedefe yönlendirilip yönlendirilmediði kontrol edilmez. Bu genellikle bir kullanýcýnýn tokenini diðer kullanýcýlarla paylaþmasýna olanak tanýr.

Örnek; ValidateAudience = true olarak ayarlandýðýný varsayalým ve token üreten servisimizde Audience'yi belirtelim.

var tokenHandler = new JwtSecurityTokenHandler();
var tokenDescriptor = new SecurityTokenDescriptor
{
    // Diðer token ayarlarý...
    Audience = "hedefA",
};
var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);


------------------------------------------------------


3- ValidateIssuer = false:
Bu durumda, JWT'nin içindeki "issuer" (veren) alaný doðrulanmaz. Yani, JWT'nin hangi servis veya uygulama tarafýndan oluþturulduðu kontrol edilmez. Bu durumda, tokeni üreten servisin kim olduðu önemli deðildir.

Örnek; ValidateIssuer = true: olarak ayarlandýðýný varsayalým ve token üreten servisimizde Issuer'i belirtelim.

var tokenHandler = new JwtSecurityTokenHandler();
var tokenDescriptor = new SecurityTokenDescriptor
{
    // Diðer token ayarlarý...
    Issuer = "myAuthService",
};
var token = tokenHandler.CreateToken(tokenDescriptor);
var tokenString = tokenHandler.WriteToken(token);


------------------------------------------------------


Örnek; ValidIssuer ve ValidAudience deðerleri belirtilerek token'ler ile rolleme iþlemi yapýlabilir.
Bir Schema'ya sahip token farklý bir schema ile configure olmuþ servislere eriþim saðlayamaz.

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
            // Diðer doðrulama parametreleri...
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
            // Diðer doðrulama parametreleri...
        };
    });


// UserController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "UserManagement")]
public class UserController : ControllerBase
{
    // Kullanýcý yönetimi ile ilgili iþlemler...
}


// ContentController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "ContentListing")]
public class ContentController : ControllerBase
{
    // Ýçerik listeleme iþlemleri...
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
                    //Bu ayar, JWT'nin "iat" ve "exp" zaman damgalarýnýn tam olarak eþleþmesini gerektirir.
                    //Yani, belirli bir cihaz veya uygulamadan gönderilen JWT'nin, sunucu saatine göre oluþturulma ve geçerlilik süresi zaman damgalarý tam olarak doðrulanmalýdýr.
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

