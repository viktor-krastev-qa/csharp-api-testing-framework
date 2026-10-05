using System.Text.Json;
namespace TaskBoard.Api;

public static class RequestParser
{
    public static async Task<(TaskInput? Input,IResult? Error)> Parse(HttpRequest request)
    {
        if(!request.HasJsonContentType()) return (null,Results.Json(new ApiError("unsupported_media_type",[]),statusCode:415));
        JsonDocument document;
        try { document=await JsonDocument.ParseAsync(request.Body); }
        catch(JsonException) { return (null,Results.BadRequest(new ApiError("invalid_json",[]))); }
        using(document)
        {
            var root=document.RootElement;
            if(root.ValueKind!=JsonValueKind.Object) return (null,Results.BadRequest(new ApiError("validation_error",["body_must_be_object"])));
            var errors=new List<string>();
            var fields=new Dictionary<string,JsonElement>();
            foreach(var field in root.EnumerateObject())
            {
                if(!fields.TryAdd(field.Name,field.Value)) errors.Add("duplicate_field");
                if(field.Name is not ("title" or "description" or "priority" or "isCompleted")) errors.Add("unknown_field");
            }
            string Text(string name,string fallback)
            {
                if(!fields.TryGetValue(name,out var value)) return fallback;
                if(value.ValueKind!=JsonValueKind.String) { errors.Add("invalid_"+name);return fallback; }
                return value.GetString() ?? fallback;
            }
            var title=Text("title","").Trim();
            var description=Text("description","");
            var priority=Text("priority","normal");
            if(title.Length is <1 or >70) errors.Add("title_length");
            if(description.Length>400) errors.Add("description_length");
            if(priority is not ("low" or "normal" or "high")) errors.Add("invalid_priority");
            var completed=false;
            if(fields.TryGetValue("isCompleted",out var flag))
            {
                if(flag.ValueKind is JsonValueKind.True or JsonValueKind.False) completed=flag.GetBoolean();
                else errors.Add("invalid_isCompleted");
            }
            if(errors.Count>0) return (null,Results.BadRequest(new ApiError("validation_error",errors.Distinct().ToArray())));
            return (new TaskInput(title,description,priority,completed),null);
        }
    }
}
