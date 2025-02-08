using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using VirsignAPI.ContextDB;
using VirsignAPI.Interfaces;
using VirsignAPI.MachineStateReader;

namespace VirsignAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            try
            {
                Log.Information("Starting VirsignAPI");

                var jwtSettings = builder.Configuration.GetSection("Jwt");
                var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

                builder.Services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtSettings["Issuer"],
                            ValidAudience = jwtSettings["Audience"],
                            IssuerSigningKey = new SymmetricSecurityKey(key)
                        };
                    });

                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
                    options.AddPolicy("UserOnly", policy => policy.RequireRole("user"));
                });

                builder.Host.UseSerilog();
                
                builder.Services.AddSingleton<MongoDBContext>(sp =>
                // new MongoDBContext("MONGODB_CONNECTION_STRING"));

            
                new MongoDBContext(builder.Configuration.GetSection("MongoDB")["ConnectionString"], builder.Configuration.GetSection("MongoDB")["DatabaseName"]));

                builder.Services.AddControllers().AddNewtonsoftJson();

                builder.Services.AddSingleton<IMachineStateReader, MockMachineStateReader>();
                //builder.Services.AddSingleton<IMachineStateReader, RealMachineStateReader>();
                builder.Services.AddHostedService<MachineStateBackgroundService>();

                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "JWT Authentication",
                        Description = "Enter your JWT token in this field",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    };

                    c.AddSecurityDefinition("Bearer", securityScheme);

                    var securityRequirement = new OpenApiSecurityRequirement
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
                            new string[] {}
                        }
                    };

                    c.AddSecurityRequirement(securityRequirement);
                });

                var app = builder.Build();
                app.UseSerilogRequestLogging();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    app.MapSwagger().RequireAuthorization("admin");
                }

                app.UseHttpsRedirection();

                app.UseAuthorization();

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application dead...");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}