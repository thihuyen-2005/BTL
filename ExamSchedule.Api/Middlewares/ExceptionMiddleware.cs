using System.Text.Json;

namespace ExamSchedule.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    public ExceptionMiddleware(RequestDelegate next) { _next = next; }

    public async Task Invoke(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (BusinessException ex) { await Write(ctx, 400, ex.Message); }
        catch (ConflictException ex) { await Write(ctx, 409, ex.Message); }
        catch (NotFoundException ex) { await Write(ctx, 404, ex.Message); }
        catch (Exception ex)         { await Write(ctx, 500, ex.Message); }
    }

    private static async Task Write(HttpContext ctx, int code, string msg)
    {
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = msg }));
    }
}