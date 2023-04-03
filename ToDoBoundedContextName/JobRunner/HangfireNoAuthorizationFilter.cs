using Hangfire.Dashboard;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.JobRunner;

/// <summary>
/// Permits unauthorized access to the <see cref="Hangfire"/> dashboard, allowing security be handled purely by network access.
/// </summary>
internal sealed class HangfireNoAuthorizationFilter : IDashboardAuthorizationFilter
{
	public bool Authorize(DashboardContext context)
	{
		return true;
	}
}
