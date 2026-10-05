using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TaskBoard.Api;

namespace ApiTests.Infrastructure;

public abstract class ApiTestBase
{
    private WebApplication? app;
    protected HttpClient Client=null!;
    private readonly List<object> evidence=new();

    [SetUp]
    public async Task StartApi()
    {
        evidence.Clear();
        app=ApiHost.Build([]);
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        Client=new HttpClient{BaseAddress=new Uri(address),Timeout=TimeSpan.FromSeconds(10)};
    }

    protected async Task<HttpResponseMessage> Send(HttpMethod method,string path,string? json=null,string mediaType="application/json")
    {
        using var request=new HttpRequestMessage(method,path);
        if(json is not null) request.Content=new StringContent(json,Encoding.UTF8,mediaType);
        var response=await Client.SendAsync(request);
        var body=await response.Content.ReadAsStringAsync();
        evidence.Add(new {method=method.Method,path,request=json,status=(int)response.StatusCode,body});
        return response;
    }
    protected Task<HttpResponseMessage> Post(object body) => Send(HttpMethod.Post,"/api/tasks",JsonSerializer.Serialize(body));
    protected async Task<TaskItem> Create(string title="API test task",string priority="normal",bool completed=false)
    {
        using var response=await Post(new {title,priority,isCompleted=completed});
        Assert.That((int)response.StatusCode,Is.EqualTo(201));
        return (await response.Content.ReadFromJsonAsync<TaskItem>())!;
    }

    [TearDown]
    public async Task StopApi()
    {
        try
        {
            if(TestContext.CurrentContext.Result.Outcome.Status==TestStatus.Failed)
            {
                try
                {
                    var folder=Path.Combine(TestContext.CurrentContext.WorkDirectory,"artifacts");
                    Directory.CreateDirectory(folder);
                    var path=Path.Combine(folder,"http-"+Guid.NewGuid().ToString("N")+".json");
                    await File.WriteAllTextAsync(path,JsonSerializer.Serialize(evidence,new JsonSerializerOptions{WriteIndented=true}));
                    TestContext.AddTestAttachment(path,"Synthetic HTTP request/response evidence");
                }
                catch(Exception ex){TestContext.Progress.WriteLine("Evidence capture failed: "+ex.GetType().Name);}
            }
        }
        finally
        {
            Client?.Dispose();
            if(app is not null){await app.StopAsync();await app.DisposeAsync();}
        }
    }
}
