using BlackJack.Hubs;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddControllers()
    .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<Program>());

builder.Services.AddScoped<IGameMode, CasualMode>(); 
builder.Services.AddScoped<GameSession>(); 
builder.Services.AddSingleton<SessionManager>();
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.ListenAnyIP(5002);
// });
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true);
    });
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
}
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("CorsPolicy"); 
app.UseRouting();
app.MapControllers();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<BlackJackGameHub>("/gameHub");
});

app.Run();




//using BlackJack.Hubs;
//using BlackJack.Models;
//using BlackJack.Service;

//var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
//builder.Services.AddSignalR();
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("CorsPolicy", builder =>
//    {
//        builder.AllowAnyMethod()
//            .AllowAnyHeader()
//            .AllowCredentials()
//            .SetIsOriginAllowed(origin => true); 
//    });
//});

//builder.Services.AddScoped<GameSession>();
//builder.Services.AddScoped<IGameMode, CasualMode>();

//var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
//app.UseCors("CorsPolicy");
//app.UseRouting();
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//    endpoints.MapHub<BlackJackGameHub>("/gameHub");
//});

//app.Run();


