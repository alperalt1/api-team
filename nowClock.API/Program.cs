using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using nowClock.API.Extensions;
using nowClock.Infrastructure;
using Scalar.AspNetCore;

namespace nowClock.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.AddInfrastructureServices(builder.Configuration);
            builder.Services.AddWebUIService();

            var app = builder.Build();

            // Middleware global de manejo de excepciones
            app.UseGlobalExceptionHandler();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();

            app.UseCors(ApiExtensions.CorsPolicyName);

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
