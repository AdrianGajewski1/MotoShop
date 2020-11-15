﻿using AutoMapper;
using AutoMapper.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MotoShop.Services.Implementation;
using MotoShop.Services.Services;
using MotoShop.WebAPI.Attributes.Base;
using MotoShop.WebAPI.AutoMapper.Profiles;
using MotoShop.WebAPI.Configurations;
using MotoShop.WebAPI.Helpers;
using MotoShop.WebAPI.Token_Providers;
using System;
using System.Text;

namespace MotoShop.WebAPI.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, string key)
        {
            var jwtKey = Encoding.UTF8.GetBytes(key);

            var tokentValidationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero
            };

            services.AddAuthentication(options => 
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => 
            {
                options.SaveToken = false;
                options.TokenValidationParameters = tokentValidationParams;
            });

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            //Scoped
            services.AddScoped<IApplicationUserService, ApplicationUserService>();
            services.AddScoped<IAdvertisementService, AdvertisementService>();
            services.AddScoped<IShopItemsService, ShopItemsService>();
            services.AddScoped<DatabaseSeeder>();


            //Singletons
            services.AddSingleton<JsonWebTokenWriter>();
            services.AddSingleton<CacheBase>();
            services.AddSingleton<ICachingService, CachingService>();
            services.AddSingleton<IEmailSenderService, EmailSender>();
            services.AddSingleton<IImageUploadService, ImageUploader>();
            return services;
        }

        public static IServiceCollection SetUpAutoMapper(this IServiceCollection services)
        {
            var mappingConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile<ItemsProfile>();
                mc.AddProfile<ApplicationUserProfile>();
            });

            IMapper mapper = mappingConfig.CreateMapper();
            services.AddSingleton(mapper);

            return services;
        }

        public static IServiceCollection AddCompression(this IServiceCollection services)
        {
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);
            services.Configure<BrotliCompressionProviderOptions>(options => options.Level = System.IO.Compression.CompressionLevel.Fastest);

            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            return services;
        }

        public static IServiceCollection AddCaching(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            var redisOptions = new RedisOptions();
            configuration.GetSection("RedisOptions").Bind(redisOptions);
            services.AddSingleton(redisOptions);

            services.AddStackExchangeRedisCache(setup => 
            {
                setup.Configuration = redisOptions.ConnectionString;
            });

            return services;
        }
    }
}
