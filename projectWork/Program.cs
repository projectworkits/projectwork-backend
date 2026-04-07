using projectWork.Endpoints;
using projectWork.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ImagesServices>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

app.AddImagesEndpoints();

app.Run();