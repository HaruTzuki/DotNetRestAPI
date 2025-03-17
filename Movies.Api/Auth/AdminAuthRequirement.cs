using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Movies.Api.Auth;

public class AdminAuthRequirement : IAuthorizationRequirement, IAuthorizationHandler
{
	private readonly string _apiKey;

	public AdminAuthRequirement(string apiKey)
	{
		_apiKey = apiKey;
	}

	public Task HandleAsync(AuthorizationHandlerContext context)
	{
		if (context.User.HasClaim(AuthConstants.AdminUserClaimName, "true"))
		{
			context.Succeed(this);
			return Task.CompletedTask;
		}
		
		var httpContext = context.Resource as HttpContext;
		
		if (httpContext is null)
		{
			return Task.CompletedTask;
		}
		
		if (!httpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var extractedApiKey))
		{
			context.Fail();
			return Task.CompletedTask;
		}
		
		if (_apiKey != extractedApiKey)
		{
			context.Fail();
			return Task.CompletedTask;
		}
		
		var identity = (ClaimsIdentity)context.User.Identity!;
		identity.AddClaim(new Claim("userid", Guid.Parse("C7FEFB8C-E930-4E7A-97C0-2EFF1994D439").ToString()));
		context.Succeed(this);
		return Task.CompletedTask;
	}
}