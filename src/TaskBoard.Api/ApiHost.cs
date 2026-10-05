namespace TaskBoard.Api;

public static class ApiHost
{
    public static WebApplication Build(string[] args)
    {
        var builder=WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<TaskStore>();
        var app=builder.Build();
        app.MapGet("/health",()=>Results.Ok(new {status="healthy"}));
        app.MapGet("/api/tasks",(HttpRequest request,TaskStore store)=>
        {
            IEnumerable<TaskItem> items=store.List();
            var completed=request.Query["completed"].ToString();
            if(completed!="")
            {
                if(!bool.TryParse(completed,out var flag)) return Results.BadRequest(new ApiError("invalid_query",["completed_must_be_boolean"]));
                items=items.Where(t=>t.IsCompleted==flag);
            }
            var priority=request.Query["priority"].ToString();
            if(priority!="")
            {
                if(priority is not ("low" or "normal" or "high")) return Results.BadRequest(new ApiError("invalid_query",["invalid_priority"]));
                items=items.Where(t=>t.Priority==priority);
            }
            var search=request.Query["search"].ToString().Trim();
            if(search!="") items=items.Where(t=>t.Title.Contains(search,StringComparison.OrdinalIgnoreCase));
            return Results.Ok(items.ToArray());
        });
        app.MapGet("/api/tasks/{id:guid}",(Guid id,TaskStore store)=>store.Get(id) is {} task ? Results.Ok(task) : Results.NotFound(new ApiError("not_found",[])));
        app.MapPost("/api/tasks",async(HttpRequest request,TaskStore store)=>
        {
            var parsed=await RequestParser.Parse(request);
            if(parsed.Error is not null) return parsed.Error;
            var result=store.Create(parsed.Input!);
            if(result.Error is not null) return Results.Conflict(new ApiError(result.Error,[]));
            return Results.Created($"/api/tasks/{result.Item!.Id}",result.Item);
        });
        app.MapPut("/api/tasks/{id:guid}",async(Guid id,HttpRequest request,TaskStore store)=>
        {
            var parsed=await RequestParser.Parse(request);
            if(parsed.Error is not null) return parsed.Error;
            var result=store.Update(id,parsed.Input!);
            return result.Error switch
            {
                "not_found" => Results.NotFound(new ApiError("not_found",[])),
                "duplicate_title" => Results.Conflict(new ApiError("duplicate_title",[])),
                _ => Results.Ok(result.Item)
            };
        });
        app.MapDelete("/api/tasks/{id:guid}",(Guid id,TaskStore store)=>store.Delete(id) ? Results.NoContent() : Results.NotFound(new ApiError("not_found",[])));
        return app;
    }
}
