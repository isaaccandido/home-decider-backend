namespace HomeDecider.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        logger.LogInformation("{Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        await next(ctx);
        logger.LogInformation("{Method} {Path} -> {StatusCode}", ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode);
    }
}
