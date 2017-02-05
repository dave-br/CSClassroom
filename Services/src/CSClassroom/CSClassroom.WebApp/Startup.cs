﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using CSC.Common.Infrastructure.Configuration;
using CSC.Common.Infrastructure.Serialization;
using CSC.CSClassroom.Model.Questions;
using CSC.CSClassroom.Repository;
using CSC.CSClassroom.Repository.Configuration;
using CSC.CSClassroom.Service.Configuration;
using CSC.CSClassroom.WebApp.Configuration;
using CSC.CSClassroom.WebApp.Logging;
using CSC.CSClassroom.WebApp.ModelBinders;
using CSC.CSClassroom.WebApp.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Serilog.Events;

namespace CSC.CSClassroom.WebApp
{
	/// <summary>
	/// Startup class for the web application.
	/// </summary>
	public class Startup
	{
		/// <summary>
		/// The DI container for the application.
		/// </summary>
		private IContainer _container;

		/// <summary>
		/// The logger factory.
		/// </summary>
		private readonly ILoggerFactory _loggerFactory;

		/// <summary>
		/// The configuration for the application.
		/// </summary>
		public IConfigurationRoot Configuration { get; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public Startup(IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			_loggerFactory = loggerFactory;

			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.Environment.json", optional: true)
				.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		/// <summary>
		/// Registers all services required for the web application.
		/// This method is called by the runtime.
		/// </summary>
		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddAuthentication(SetupAuthentication);
			services.AddTelemetry(Configuration, typeof(CSClassroomTelemetryInitializer));
			services.AddMvc(SetupMvc);

			var builder = new ContainerBuilder();
			
			builder.RegisterJsonSerialization(GetTypeMaps());
			builder.RegisterCSClassroomWebApp(GetSection("CSClassroom"), GetSection("GitHub"));
			builder.RegisterCSClassroomService(GetSection("CSClassroom"));
			builder.RegisterGitHubClients(GetSection("GitHub"));
			builder.RegisterRemoteBuildService(GetSection("BuildService"));
			builder.RegisterSendGridMailProvider(GetSection("SendGrid"));
			builder.RegisterJobQueueClient();
			builder.RegisterSystem();
			builder.RegisterOperationRunner();
			services.AddCSClassroomDatabase(DatabaseConnectionString);
			services.AddHangfireQueue(DatabaseConnectionString, _loggerFactory);

			builder.Populate(services);
			_container = builder.Build();

			return new AutofacServiceProvider(_container);
		}

		/// <summary>
		/// Configures the HTTP request pipeline. 
		/// This method is called by the runtime.
		/// </summary>
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			ApplyDatabaseMigrations(app);

			app.UseTelemetry(loggerFactory, Configuration, IncludeLogEvent);
			app.UseStaticFiles();
			app.UseStatusCodePages();
			app.UseExceptionHandler("/Error");
			app.UseForwardedHeaders(GetForwardedHeadersOptions());
			app.UseCookieAuthentication(GetCookieAuthenticationOptions());
			app.UseOpenIdConnectAuthentication(GetOpenIdConnectOptions());
			app.UseMvc();
			app.UseHangfireQueueDashboard(_container);
		}

		/// <summary>
		/// Applies any new database migrations, if needed (creating 
		/// and initializing the database if it does not yet exist).
		/// </summary>
		private static void ApplyDatabaseMigrations(IApplicationBuilder app)
		{
			using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
			{
				scope.ServiceProvider.GetService<DatabaseContext>().Database.Migrate();
			}
		}

		/// <summary>
		/// The database connection string.
		/// </summary>
		private string DatabaseConnectionString
			=> GetSection("ConnectionStrings")["PostgresDefaultConnection"];

		/// <summary>
		/// Returns the given configuration section.
		/// </summary>
		private IConfigurationSection GetSection(string sectionName)
		{
			return Configuration.GetSection(sectionName);
		}

		/// <summary>
		/// Sets up authentication.
		/// </summary>
		private static void SetupAuthentication(SharedAuthenticationOptions authOptions)
		{
			authOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
		}

		/// <summary>
		/// Adds model binders to the MVC configuration.
		/// </summary>
		private void SetupMvc(MvcOptions options)
		{
			// Support binding to abstract models for a set of whitelisted types.
			options.ModelBinderProviders.Insert(0, new AbstractModelBinderProvider
			(
				new List<Type>()
				{
						typeof(Question),
						typeof(CodeQuestionTest),
						typeof(QuestionSubmission)
				})
			);

			options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider
			(
				_container.Resolve<ITimeZoneProvider>())
			);
		}

		/// <summary>
		/// Returns whether or not to include a given log event.
		/// </summary>
		private bool IncludeLogEvent(LogEvent logEvent)
		{
			if (logEvent.Properties.ContainsKey("RequestPath"))
			{
				string requestPath = logEvent.Properties["RequestPath"]
					.ToString()
					.Substring(1);

				if (requestPath.StartsWith("/css")
					|| requestPath.StartsWith("/images")
					|| requestPath.StartsWith("/js")
					|| requestPath.StartsWith("/lib")
					|| requestPath.StartsWith("/markdown")
					|| requestPath.StartsWith("/favicon.ico"))
				{
					return false;
				}
			}

			if (logEvent.Properties.ContainsKey("SourceContext")
					&& logEvent.Properties["SourceContext"]
						.ToString()
						.StartsWith("\"Microsoft.EntityFrameworkCore")
					&& logEvent.Level == LogEventLevel.Information)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns a set of type maps that permit deserializing JSON objects into
		/// abstract types.
		/// </summary>
		private ITypeMapCollection GetTypeMaps()
		{
			return new TypeMapCollection()
			{
				[typeof(Question)] = GetTypeMap(typeof(Question)),
				[typeof(QuestionSubmission)] = GetTypeMap(typeof(QuestionSubmission))
			};
		}

		/// <summary>
		/// Returns a type map for a given base type.
		/// </summary>
		private IReadOnlyDictionary<string, Type> GetTypeMap(Type baseType)
		{
			return baseType.GetTypeInfo()
				.Assembly
				.GetTypes()
				.Where
				(
					type => !type.GetTypeInfo().IsAbstract
							&& baseType.IsAssignableFrom(type)
				).ToDictionary(type => type.Name, type => type);
		}

		/// <summary>
		/// Returns options for forwarded headers.
		/// </summary>
		private static ForwardedHeadersOptions GetForwardedHeadersOptions()
		{
			return new ForwardedHeadersOptions
			{
				ForwardedHeaders = ForwardedHeaders.XForwardedProto
			};
		}

		/// <summary>
		/// Returns options for cookie authentication.
		/// </summary>
		private static CookieAuthenticationOptions GetCookieAuthenticationOptions()
		{
			return new CookieAuthenticationOptions()
			{
				ExpireTimeSpan = TimeSpan.FromHours(8)
			};
		}

		/// <summary>
		/// Returns options for OpenId Connect authentication.
		/// </summary>
		/// <returns></returns>
		private OpenIdConnectOptions GetOpenIdConnectOptions()
		{
			return new OpenIdConnectOptions()
			{
				ClientId = Configuration["Authentication:AzureAd:ClientId"],
				Authority = Configuration["Authentication:AzureAd:AADInstance"] + "Common",
				CallbackPath = Configuration["Authentication:AzureAd:CallbackPath"],

				TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = false,
				},
				Events = new OpenIdConnectEvents
				{
					OnTicketReceived = (context) =>
					{
						context.Ticket.Properties.ExpiresUtc = null;

						return Task.FromResult(0);
					},
					OnAuthenticationFailed = (context) =>
					{
						context.Response.Redirect("/Home/Error");
						context.HandleResponse(); // Suppress the exception
						return Task.FromResult(0);
					},
				}
			};
		}
	}
}