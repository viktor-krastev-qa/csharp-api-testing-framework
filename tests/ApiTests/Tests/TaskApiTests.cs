using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NUnit.Framework;
using TaskBoard.Api;
using ApiTests.Infrastructure;

namespace ApiTests.Tests;

[TestFixture,NonParallelizable]
public class TaskApiTests : ApiTestBase
{
    [Test] public async Task Health_ReturnsHealthyJson()
    {
        using var response=await Send(HttpMethod.Get,"/health");
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType!.MediaType,Is.EqualTo("application/json"));
        using var body=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("status").GetString(),Is.EqualTo("healthy"));
    }
    [Test] public async Task FreshServer_HasEmptyList()
    {
        using var response=await Send(HttpMethod.Get,"/api/tasks");
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadFromJsonAsync<TaskItem[]>(),Is.Empty);
    }
    [Test] public async Task Create_Returns201LocationAndDefaults()
    {
        using var response=await Post(new {title="First task"});
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.Created));
        var task=(await response.Content.ReadFromJsonAsync<TaskItem>())!;
        Assert.That(task.Id,Is.Not.EqualTo(Guid.Empty));
        Assert.That(task.Title,Is.EqualTo("First task"));
        Assert.That(task.Description,Is.EqualTo(""));
        Assert.That(task.Priority,Is.EqualTo("normal"));
        Assert.That(task.IsCompleted,Is.False);
        Assert.That(response.Headers.Location!.ToString(),Is.EqualTo($"/api/tasks/{task.Id}"));
        using var fetched=await Send(HttpMethod.Get,response.Headers.Location.ToString());
        Assert.That(await fetched.Content.ReadFromJsonAsync<TaskItem>(),Is.EqualTo(task));
    }
    [Test] public async Task Create_AllFieldsRoundTrip()
    {
        using var response=await Post(new {title="Complete task",description="Detailed task",priority="high",isCompleted=true});
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.Created));
        var item=(await response.Content.ReadFromJsonAsync<TaskItem>())!;
        using var fetched=await Send(HttpMethod.Get,$"/api/tasks/{item.Id}");
        Assert.That(fetched.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await fetched.Content.ReadFromJsonAsync<TaskItem>(),Is.EqualTo(item));
        Assert.That(item.Description,Is.EqualTo("Detailed task"));
        Assert.That(item.Priority,Is.EqualTo("high"));Assert.That(item.IsCompleted,Is.True);
    }
    [TestCase(1)] [TestCase(70)] public async Task TitleBoundaries_Accepted(int length)
    {
        var item=await Create(new string('a',length));Assert.That(item.Title.Length,Is.EqualTo(length));
    }
    [TestCase("")] [TestCase("   ")] public async Task BlankTitle_Rejected(string title)
    {
        using var response=await Post(new {title});await AssertValidation(response,"title_length");
        await AssertEmpty();
    }
    [Test] public async Task Title71_Rejected()
    {
        using var response=await Post(new {title=new string('a',71)});await AssertValidation(response,"title_length");
    }
    [Test] public async Task TitleTrimmed()
    {
        var item=await Create("  Trimmed task  ");Assert.That(item.Title,Is.EqualTo("Trimmed task"));
    }
    [Test] public async Task Description400_Accepted()
    {
        using var response=await Post(new {title="Boundary",description=new string('a',400)});
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.Created));
        Assert.That((await response.Content.ReadFromJsonAsync<TaskItem>())!.Description.Length,Is.EqualTo(400));
    }
    [Test] public async Task Description401_Rejected()
    {
        using var response=await Post(new {title="Boundary",description=new string('a',401)});await AssertValidation(response,"description_length");
    }
    [TestCase("""{"title":123}""","invalid_title")]
    [TestCase("""{}""","title_length")]
    [TestCase("""{"title":"Task","priority":"urgent"}""","invalid_priority")]
    [TestCase("""{"title":"Task","description":null}""","invalid_description")]
    [TestCase("""{"title":"Task","isCompleted":"true"}""","invalid_isCompleted")]
    [TestCase("""{"title":"Task","id":"user-supplied"}""","unknown_field")]
    [TestCase("""{"title":"Task","title":"Other"}""","duplicate_field")]
    [TestCase("[]","body_must_be_object")]
    [TestCase("null","body_must_be_object")]
    public async Task InvalidSchema_Rejected(string json,string detail)
    {
        using var response=await Send(HttpMethod.Post,"/api/tasks",json);await AssertValidation(response,detail);await AssertEmpty();
    }
    [Test] public async Task MalformedJson_Returns400()
    {
        using var response=await Send(HttpMethod.Post,"/api/tasks","{not-json");await AssertError(response,400,"invalid_json");
    }
    [Test] public async Task UnsupportedMedia_Returns415()
    {
        using var response=await Send(HttpMethod.Post,"/api/tasks","text","text/plain");await AssertError(response,415,"unsupported_media_type");
    }
    [Test] public async Task DuplicateTitle_CaseInsensitiveConflict()
    {
        await Create("Unique title");using var response=await Post(new {title="UNIQUE TITLE"});await AssertError(response,409,"duplicate_title");
        using var list=await Send(HttpMethod.Get,"/api/tasks");Assert.That((await list.Content.ReadFromJsonAsync<TaskItem[]>())!.Length,Is.EqualTo(1));
    }
    [Test] public async Task GetUnknown_Returns404()
    {
        using var response=await Send(HttpMethod.Get,$"/api/tasks/{Guid.NewGuid()}");await AssertError(response,404,"not_found");
    }
    [Test] public async Task InvalidGuidRoute_Returns404()
    {
        using var response=await Send(HttpMethod.Get,"/api/tasks/not-a-guid");Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.NotFound));
    }
    [Test] public async Task Put_ReplacesFieldsAndKeepsId()
    {
        var item=await Create();using var response=await Send(HttpMethod.Put,$"/api/tasks/{item.Id}","""{"title":"Updated","description":"New detail","priority":"low","isCompleted":true}""");
        Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        var updated=(await response.Content.ReadFromJsonAsync<TaskItem>())!;
        Assert.That(updated,Is.EqualTo(new TaskItem(item.Id,"Updated","New detail","low",true)));
        using var fetched=await Send(HttpMethod.Get,$"/api/tasks/{item.Id}");Assert.That(await fetched.Content.ReadFromJsonAsync<TaskItem>(),Is.EqualTo(updated));
    }
    [Test] public async Task InvalidPut_PreservesExistingTask()
    {
        var item=await Create();using var response=await Send(HttpMethod.Put,$"/api/tasks/{item.Id}","""{"title":""}""");await AssertValidation(response,"title_length");
        using var fetched=await Send(HttpMethod.Get,$"/api/tasks/{item.Id}");Assert.That(await fetched.Content.ReadFromJsonAsync<TaskItem>(),Is.EqualTo(item));
    }
    [Test] public async Task PutUnknown_Returns404()
    {
        using var response=await Send(HttpMethod.Put,$"/api/tasks/{Guid.NewGuid()}","""{"title":"Valid title"}""");await AssertError(response,404,"not_found");
    }
    [Test] public async Task PutDuplicateTitle_ConflictWithoutMutation()
    {
        await Create("First");var second=await Create("Second");
        using var response=await Send(HttpMethod.Put,$"/api/tasks/{second.Id}","""{"title":"FIRST"}""");await AssertError(response,409,"duplicate_title");
        using var fetched=await Send(HttpMethod.Get,$"/api/tasks/{second.Id}");Assert.That(await fetched.Content.ReadFromJsonAsync<TaskItem>(),Is.EqualTo(second));
    }
    [Test] public async Task PutSameTitle_DoesNotConflictWithItself()
    {
        var item=await Create();using var response=await Send(HttpMethod.Put,$"/api/tasks/{item.Id}",JsonSerializer.Serialize(new {title=item.Title}));Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));
    }
    [Test] public async Task Delete_RemovesOnlySelectedTask()
    {
        var first=await Create("First");var second=await Create("Second");
        using var deleted=await Send(HttpMethod.Delete,$"/api/tasks/{first.Id}");Assert.That(deleted.StatusCode,Is.EqualTo(HttpStatusCode.NoContent));Assert.That(await deleted.Content.ReadAsStringAsync(),Is.Empty);
        using var fetched=await Send(HttpMethod.Get,$"/api/tasks/{first.Id}");await AssertError(fetched,404,"not_found");
        using var list=await Send(HttpMethod.Get,"/api/tasks");Assert.That(await list.Content.ReadFromJsonAsync<TaskItem[]>(),Is.EqualTo(new[]{second}));
    }
    [Test] public async Task DeleteTwice_Returns404SecondTime()
    {
        var task=await Create();using var first=await Send(HttpMethod.Delete,$"/api/tasks/{task.Id}");using var second=await Send(HttpMethod.Delete,$"/api/tasks/{task.Id}");await AssertError(second,404,"not_found");
    }
    [Test] public async Task Search_EncodedCaseInsensitiveSubstring()
    {
        var task=await Create("Review API & UI");await Create("Other task");
        using var response=await Send(HttpMethod.Get,"/api/tasks?search="+Uri.EscapeDataString("api &"));Assert.That(await response.Content.ReadFromJsonAsync<TaskItem[]>(),Is.EqualTo(new[]{task}));
    }
    [Test] public async Task SearchNoMatch_ReturnsEmptyArray()
    {
        await Create();using var response=await Send(HttpMethod.Get,"/api/tasks?search=missing");Assert.That(response.StatusCode,Is.EqualTo(HttpStatusCode.OK));Assert.That(await response.Content.ReadFromJsonAsync<TaskItem[]>(),Is.Empty);
    }
    [Test] public async Task CombinedFilters_ReturnOnlyMatchingTask()
    {
        var target=await Create("Done high", "high",true);await Create("Open high","high");await Create("Done low","low",true);
        using var response=await Send(HttpMethod.Get,"/api/tasks?completed=true&priority=high");Assert.That(await response.Content.ReadFromJsonAsync<TaskItem[]>(),Is.EqualTo(new[]{target}));
    }
    [TestCase("completed=perhaps","completed_must_be_boolean")]
    [TestCase("priority=urgent","invalid_priority")]
    public async Task InvalidFilter_Returns400(string query,string detail)
    {
        using var response=await Send(HttpMethod.Get,"/api/tasks?"+query);var error=await AssertError(response,400,"invalid_query");Assert.That(error.Details,Does.Contain(detail));
    }
    [Test] public async Task FullCrudLifecycle()
    {
        var item=await Create("Lifecycle");using var put=await Send(HttpMethod.Put,$"/api/tasks/{item.Id}","""{"title":"Finished","isCompleted":true}""");Assert.That(put.StatusCode,Is.EqualTo(HttpStatusCode.OK));
        using var get=await Send(HttpMethod.Get,$"/api/tasks/{item.Id}");Assert.That((await get.Content.ReadFromJsonAsync<TaskItem>())!.IsCompleted,Is.True);
        using var delete=await Send(HttpMethod.Delete,$"/api/tasks/{item.Id}");Assert.That(delete.StatusCode,Is.EqualTo(HttpStatusCode.NoContent));await AssertEmpty();
    }
    private async Task AssertEmpty()
    {
        using var list=await Send(HttpMethod.Get,"/api/tasks");Assert.That(await list.Content.ReadFromJsonAsync<TaskItem[]>(),Is.Empty);
    }
    private static async Task<ApiError> AssertError(HttpResponseMessage response,int status,string error)
    {
        Assert.That((int)response.StatusCode,Is.EqualTo(status));
        Assert.That(response.Content.Headers.ContentType!.MediaType,Is.EqualTo("application/json"));
        var parsed=(await response.Content.ReadFromJsonAsync<ApiError>())!;
        Assert.That(parsed.Error,Is.EqualTo(error));
        return parsed;
    }
    private static async Task AssertValidation(HttpResponseMessage response,string detail)
    {
        var error=await AssertError(response,400,"validation_error");Assert.That(error.Details,Does.Contain(detail));
    }
}
