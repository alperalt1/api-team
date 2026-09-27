using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using nowClock.API.Middlewares;
using nowClock.Application.Wrappers;
using System.Linq;

namespace nowClock.API.Extensions
{
    public static class ApiExtensions
    {
        public const string CorsPolicyName = "AllowAll";

        public static IServiceCollection AddWebUIService(this IServiceCollection services) 
        {
            services.AddControllers()
                .ConfigureApiBehaviorOptions(opt =>
                {
                    opt.InvalidModelStateResponseFactory = context =>
                    {
                        var errores = context.ModelState.Values
                            .SelectMany(x => x.Errors.Select(e => e.ErrorMessage))
                            .ToList();

                        var response = new ApiResponse<object>("Errores de validación en la petición", errores);
                        return new BadRequestObjectResult(response);
                    };
                });

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicyName, policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            services.AddOpenApi();

            return services;
        }

        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
