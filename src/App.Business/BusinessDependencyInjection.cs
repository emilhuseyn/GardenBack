using App.Business.DTOs.Commons;
using App.Business.Helpers;
using App.Business.Services.ExternalServices.Abstractions;
using App.Business.Services.ExternalServices.Interfaces;
using App.Business.Services.Implementations;
using App.Business.Services.Interfaces;
using App.Business.Validators.Commons;
using App.Shared.Implementations;
using App.Shared.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace App.Business
{
    public static class BusinessDependencyInjection
    {
        public static IServiceCollection AddBusiness(this IServiceCollection services)
        {
            services.AddServices();
            services.RegisterAutoMapper();
            services.AddValidatorsFromAssemblyContaining<BaseEntityValidator<BaseEntityDTO>>();
            services.AddControllers(options =>
            {
                options.Conventions.Add(new PluralizedRouteConvention());
                options.ModelValidatorProviders.Clear();
            })
           .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<BaseEntityValidator<BaseEntityDTO>>())
           .AddJsonOptions(options =>
           {
               options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
           });

            return services;
        }

        private static void AddServices(this IServiceCollection services)
        {
            services.AddHttpClient("WhatsApp");

            services.AddScoped<IClaimService, ClaimService>();
            services.AddScoped<IFileManagerService, FileManagerService>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IChildService, ChildService>();
            services.AddScoped<IAttendanceService, AttendanceService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IGroupService, GroupService>();
            services.AddScoped<IDivisionService, DivisionService>();
            services.AddScoped<IScheduleConfigService, ScheduleConfigService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<IAgreementService, AgreementService>();
        }

        private static void RegisterAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(BusinessDependencyInjection));
        }
    }
}
