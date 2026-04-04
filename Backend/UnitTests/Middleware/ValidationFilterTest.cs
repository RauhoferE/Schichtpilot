using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Schichtpilot.Middleware;
using Schichtpilot.Models.Responses;

namespace UnitTests.Middleware;

public class ValidationFilterTest
{
    [Fact]
    public async Task OnActionExecutionAsync_InvalidModelState_SetsBadRequestAndSkipsNext()
    {
        var filter = new ValidationFilter();

        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        modelState.AddModelError("Name", "Name is required.");
        modelState.AddModelError("Age", "Age must be positive.");
        var controller = new object();

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            var executedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller);
            return Task.FromResult(executedContext);
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.False(nextCalled);
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.IsType<ErrorResponse>(result.Value);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidModelState_CallsNextAndDoesNotSetResult()
    {
        var filter = new ValidationFilter();

        var httpContext = new DefaultHttpContext();
        var routeData = new RouteData();
        var actionDescriptor = new ActionDescriptor();
        var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
        var controller = new object();

        var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, modelState);
        var context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);

        var nextCalled = false;
        ActionExecutionDelegate next = () =>
        {
            nextCalled = true;
            var executedContext = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller);
            return Task.FromResult(executedContext);
        };

        await filter.OnActionExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }
}
