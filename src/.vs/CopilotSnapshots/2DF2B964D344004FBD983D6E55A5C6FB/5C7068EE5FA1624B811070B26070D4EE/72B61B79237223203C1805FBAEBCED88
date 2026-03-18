using App.Business.DTOs.Commons;
using App.Business.Helpers;
using App.Business.Services.ExternalServices.Abstractions;
using App.Business.Services.ExternalServices.Interfaces;
using App.Business.Validators.Commons;
using App.Shared.Implementations;
using App.Shared.Interfaces;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace App.Business
{
    public static class BusinessDependencyInjection
    {
        public static IServiceCollection AddBusiness(this IServiceCollection services)
        {
            services.AddServices();
            services.RegisterAutoMapper();
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
            services.AddScoped<IClaimService, ClaimService>();

            // External Services 
            services.AddScoped<IFileManagerService, FileManagerService>();
        }

        private static void RegisterAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(BusinessDependencyInjection));
        }
    }
}
