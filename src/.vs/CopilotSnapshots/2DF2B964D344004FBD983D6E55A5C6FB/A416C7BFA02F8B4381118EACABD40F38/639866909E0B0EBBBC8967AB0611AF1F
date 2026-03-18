using App.API.Middlewares;
using App.Business.Helpers;
using App.Core.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace App.API
{
    public static class ApiDependencyInjection
    {
        public static void AddJwt(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = configuration.GetValue<string>("JwtConfiguration:SecretKey")!;
            var issuer = configuration.GetValue<string>("JwtConfiguration:Issuer");
            var audience = configuration.GetValue<string>("JwtConfiguration:Audience");

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    LifetimeValidator = (notBefore, expires, tokenToValidate, tokenValidationParameters) =>
                    {
                        return expires != null && expires > DateTime.UtcNow;
                    }
                };
            });
        }

        public static void AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                // Yalnız Administrator
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireRole(EUserRole.Administrator.ToString()));

                // Administrator + Mühasib (maliyyə əməliyyatları)
                options.AddPolicy("AdminOrAccountant", policy =>
                    policy.RequireRole(
                        EUserRole.Administrator.ToString(),
                        EUserRole.Accountant.ToString()));

                // Administrator + Qəbul üzrə əməkdaş (uşaq idarəetməsi)
                options.AddPolicy("AdminOrAdmission", policy =>
                    policy.RequireRole(
                        EUserRole.Administrator.ToString(),
                        EUserRole.AdmissionStaff.ToString()));

                // Administrator + Müəllim (davamiyyət yazma)
                options.AddPolicy("AttendanceWrite", policy =>
                    policy.RequireRole(
                        EUserRole.Administrator.ToString(),
                        EUserRole.Teacher.ToString()));

                // Administrator + Mühasib + Qəbul üzrə əməkdaş (ödəniş baxış)
                options.AddPolicy("PaymentView", policy =>
                    policy.RequireRole(
                        EUserRole.Administrator.ToString(),
                        EUserRole.Accountant.ToString(),
                        EUserRole.AdmissionStaff.ToString()));

                // Bütün işçilər (ümumi baxış)
                options.AddPolicy("AllStaff", policy =>
                    policy.RequireRole(
                        EUserRole.Administrator.ToString(),
                        EUserRole.Accountant.ToString(),
                        EUserRole.Teacher.ToString(),
                        EUserRole.AdmissionStaff.ToString()));
            });
        }

        public static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" };

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder.WithOrigins(origins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
        }

        public static void AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "KinderGarden CRM API",
                    Version = "v1",
                    Description = "Kindergarten Management CRM System API"
                });

                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });
                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                opt.SchemaFilter<EnumSchemaFilter>();
            });
        }

        public static void AddMiddlewares(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<RequestLoggingMiddleware>();
            builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
            builder.UseMiddleware<XSSProtectionMiddleware>();
        }
    }
}
