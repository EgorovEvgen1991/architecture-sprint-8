using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

using Microsoft.OpenApi.Models;
using ReportMaker.Midleware;
using System.Reflection;
using System.Security.Claims;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakWebApi(builder.Configuration, options =>
    {
        options.RequireHttpsMetadata = false;
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            NameClaimType = "preferred_username",
            RoleClaimType = builder.Configuration["Keycloak:RoleClaimType"],
        };
    });

builder.Services
    .AddAuthorization()
    .AddKeycloakAuthorization(options =>
    {
        options.EnableRolesMapping = RolesClaimTransformationSource.Realm;
        options.VerifyTokenAudience = false;
        options.RoleClaimType = "realm_access.roles";
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("default", policy =>
    {
        policy.WithOrigins(builder.Configuration["AllowedOrigins"]?.Split(',') ?? Array.Empty<string>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Token-Expired");
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BionicProtezReporterAPI",
        Version = "v1",
        Description = "API for Bionic protez reports."

    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {

                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("default");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();

var summaries = new[]
{
    "OK","Good", "Warn", "Fail"
};

app.MapGet("/api/Reports", [Authorize(Roles = "prothetic_user")] () =>
{

    return summaries[0];
})
.WithName("ReportMaker")
.WithOpenApi();

// Тестовый апи для прповерки клоак 
app.MapGet("/",(ClaimsPrincipal user, HttpContext context) =>
{

    app.Logger.LogInformation("Identities");
    foreach (var indt in user.Identities)
    {
        app.Logger.LogInformation($"Name {indt.Name} Label  {indt.Label} Actor {indt.Actor}");
    }
    app.Logger.LogInformation("Claims");
    foreach (var clainm in user.Claims)
    {
        app.Logger.LogInformation($"{clainm.ValueType} {clainm.Value} {clainm.Type}");
    }
    var login = context.User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
    var role = context.User.FindFirst(ClaimsIdentity.DefaultRoleClaimType);
    app.Logger.LogInformation("HTTP CONTEXT");
    app.Logger.LogInformation($"Name: {login?.Value} Role: {role?.Value}");
}).RequireAuthorization();


app.Run();

