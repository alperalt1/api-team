using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using nowClock.Application.Interfaces.Auth;
using nowClock.Infrastructure.Data.Auth;
using nowClock.Infrastructure.Identity;
using nowClock.Infrastructure.Services.Auth;
using System;
using System.Text;

namespace nowClock.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var conexion = configuration.GetConnectionString("PostgresConnection") ?? string.Empty;

            services.AddDbContext<ApplicationDbContext>(opt =>
            {
                if (!string.IsNullOrWhiteSpace(conexion))
                {
                    opt.UseNpgsql(conexion);
                }
            });

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Políticas de contraseñas robustas
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                // Protección contra ataques de fuerza bruta (Lockout)
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;

                // Validación de usuarios: se autentican por Cédula (UserName)
                options.User.RequireUniqueEmail = false;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddSingleton<IDbConnectionFactory>(new NpgsqlConnectionFactory(conexion));
            services.AddScoped<IAuthService, AuthService>();

            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretString = jwtSettings["SecretKey"] ?? jwtSettings["Secret"]
                ?? "Default_Development_Super_Secret_Key_32_Bytes_Long!";

            var secretKey = Encoding.UTF8.GetBytes(secretString);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(opt =>
                {
                    opt.RequireHttpsMetadata = false;
                    opt.SaveToken = true;
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                        ValidateIssuer = !string.IsNullOrEmpty(jwtSettings["Issuer"]),
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidateAudience = !string.IsNullOrEmpty(jwtSettings["Audience"]),
                        ValidAudience = jwtSettings["Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            return services;
        }
    }
}
