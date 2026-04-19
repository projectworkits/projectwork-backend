using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using projectWork.Authentication;
using projectWork.Endpoints;
using projectWork.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAntiforgery();

builder.Services.AddScoped<Authentication>();
builder.Services.AddScoped<PasswordServices>();

builder.Services.AddScoped<ImagesServices>();
builder.Services.AddScoped<UsersServices>();
builder.Services.AddScoped<ProductsServices>();

var keyBytes = Encoding.UTF8.GetBytes(builder.Configuration["jwtSecret"] ?? "deve-essere-di-almeno-32-caratteri");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    // Dice ad ASP.NET di leggere il token dal cookie invece che dall'header Authorization
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            ctx.Token = ctx.Request.Cookies["AccessToken"];
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        // il token scade esattamente quando scade, asp.net lo accetterebbe anche 5 min dopo
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.AddAuthenticationEndpoints();
app.AddUsersEndpoints();
app.AddImagesEndpoints();

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

app.Run();