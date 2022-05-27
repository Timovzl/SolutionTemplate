using System.Text;
using __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
	/// <summary>
	/// The current time zone's offset from UTC during January. Useful for replacements in JSON strings to make assertions on.
	/// </summary>
	protected static string TimeZoneUtcOffsetString { get; } = $"+{TimeZoneInfo.Local.GetUtcOffset(DateTime.UnixEpoch):hh\\:mm}";

	protected string UniqueTestName { get; } = $"Test_{Guid.NewGuid():N}";

	protected IHostBuilder HostBuilder { get; set; }

	protected IConfiguration Configuration { get; }
	protected string ConnectionString { get; }

	/// <summary>
	/// <para>
	/// Returns the host, which contains the services.
	/// </para>
	/// <para>
	/// On the first resolution, the host is built and started.
	/// </para>
	/// <para>
	/// If the host is started, it is automatically stopped when the test class is disposed.
	/// </para>
	/// </summary>
	protected IHost Host
	{
		get
		{
			if (this._host is null)
			{
				this._host = this.HostBuilder.Build();
				this._host.Start();
			}
			return this._host;
		}
	}
	private IHost? _host;

	protected IntegrationTestBase()
	{
		this.HostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
			.UseDefaultServiceProvider(provider => provider.ValidateOnBuild = provider.ValidateScopes = true) // Be as strict as ASP.NET Core in Development is
			.ConfigureWebHostDefaults(webBuilder => webBuilder.UseTestServer(options => options.PreserveExecutionContext = true));

		this.Configuration = new ConfigurationBuilder()
		   .AddJsonFile("appsettings.json")
		   .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
		   .AddEnvironmentVariables()
		   .Build();

		this.ConnectionString = $"{this.Configuration["ConnectionStrings:ContextDatabase"]};Database={this.UniqueTestName};DefaultCommandTimeout=120;";
		this.Configuration["ConnectionStrings:ContextDatabase"] = this.ConnectionString;

		this.ConfigureServices(services => services.AddSingleton(this.Configuration));

		this.ConfigureServices(services => services.AddApplicationLayer(this.Configuration));
		this.ConfigureServices(services => services.AddDatabaseInfrastructureLayer(this.Configuration));
	}

	public virtual void Dispose()
	{
		try
		{
			this._host?.StopAsync().GetAwaiter().GetResult();
		}
		finally
		{
			this._host?.Dispose();
		}
	}

	/// <summary>
	/// Adds an action to be executed as part of what would normally be Startup.ConfigureServices().
	/// </summary>
	protected void ConfigureServices(Action<IServiceCollection> action)
	{
		if (this._host is not null) throw new Exception("No more services can be registered once the host is resolved.");

		this.HostBuilder.ConfigureServices(action ?? throw new ArgumentNullException(nameof(action)));
	}

	/// <summary>
	/// Clears any services registered using <see cref="ConfigureServices(Action{IServiceCollection})"/>.
	/// </summary>
	protected void ClearConfigureServices()
	{
		if (this._host is not null) throw new Exception("No more configuration actions can be specified once the host is resolved.");

		this.HostBuilder = new HostBuilder();
	}

	/// <summary>
	/// <para>
	/// Performs a request as if it came from the outside, using the middleware pipeline, for maximum coverage.
	/// </para>
	/// <para>
	/// Throws an <see cref="HttpRequestException"/> if the response has a non-success status code.
	/// </para>
	/// <para>
	/// The request is handled in the current ExecutionContext, with access to test's ambient contexts, such as ClockScope.
	/// </para>
	/// </summary>
	protected async Task GetApiResponse<TRequest>(string url, TRequest request, string verb = "POST")
	{
		using var httpClient = this.Host.GetTestClient();

		var requestJson = JsonConvert.SerializeObject(request);

		using var requestMessage = new HttpRequestMessage(new HttpMethod(verb), url)
		{
			Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
		};

		var response = await httpClient.SendAsync(requestMessage);

		if (!response.IsSuccessStatusCode)
			throw new HttpRequestException(await response.Content.ReadAsStringAsync());
	}

	/// <summary>
	/// <para>
	/// Performs a request as if it came from the outside, using the middleware pipeline, for maximum coverage.
	/// </para>
	/// <para>
	/// Throws an <see cref="HttpRequestException"/> if the response has a non-success status code.
	/// </para>
	/// <para>
	/// The request is handled in the current ExecutionContext, with access to test's ambient contexts, such as ClockScope.
	/// </para>
	/// </summary>
	protected async Task<TResponse?> GetApiResponse<TRequest, TResponse>(string url, TRequest request, string verb = "GET")
	{
		using var httpClient = this.Host.GetTestClient();

		var requestJson = JsonConvert.SerializeObject(request);

		using var requestMessage = new HttpRequestMessage(new HttpMethod(verb), url)
		{
			Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
		};

		var response = await httpClient.SendAsync(requestMessage);

		if (!response.IsSuccessStatusCode)
			throw new HttpRequestException(await response.Content.ReadAsStringAsync());

		var responseJson = await response.Content.ReadAsStringAsync();
		var result = JsonConvert.DeserializeObject<TResponse>(responseJson);

		return result;
	}

	protected Task<int> ExecuteNonQuery(string query)
	{
		throw new NotImplementedException();
	}

	protected Task<object?> ExecuteScalar(string query)
	{
		throw new NotImplementedException();
	}
}
