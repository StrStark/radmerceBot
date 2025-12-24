using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Data;
using radmerceBot.Api.Interfaces;
using radmerceBot.Api.Services;
using radmerceBot.Api.Sms;
using radmerceBot.Api.Telegram;


var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
var services = builder.Services;
var env = builder.Environment;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient();

builder.Services.AddSingleton<ippanelService>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var httpClient = provider.GetRequiredService<HttpClient>();
    return new ippanelService(config["Sms:TOKEN"]!,httpClient);
});

builder.Services.AddSingleton<ITelegramBotService>(provider =>
{
    var token = configuration["Telegram:BotToken"]!;
    return new TelegramBotService(token);
});
builder.Services.AddScoped<ISmsService, SmsService>();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.MapControllers();
app.Run();

