using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;


using UrlShortener;
using LiteDB;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(builder =>
    {
        builder.ConfigureServices(services =>
        {
            services.AddRouting();

            services.AddSingleton<ILiteDatabase, LiteDatabase>(_ => new LiteDatabase("short-links.db"));
        })
        .Configure(app =>
        {
            app.UseRouting();

            app.UseEndpoints((endpoints) =>
            {
                endpoints.MapGet("/", (ctx) =>
                {
                    return ctx.Response.SendFileAsync("index.html");
                });

                endpoints.MapPost("/shorten", HandleShortenUrl);
                endpoints.MapFallback(HandleRedirect);
            });
        });
    })
    .Build();

await host.RunAsync();

static Task HandleShortenUrl(HttpContext context){
    if(!context.Request.HasFormContentType || !context.Request.Form.ContainsKey("url")){
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Невозможно обработать запрос!");
    }

    context.Request.Form.TryGetValue("url", out var formData);
    var requestedUrl = formData.ToString();

    if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri result))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Не понятная ссылка.");
    }

    var url = result.ToString();

    var liteDB = context.RequestServices.GetService<ILiteDatabase>();
    var links = liteDB.GetCollection<ShortLink>(BsonAutoId.Int32);

    var entry = new ShortLink{
        Url = url
    };

    links.Insert(entry);

    var urlChunk = entry.GetUrlChunk();
    var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
    context.Response.Redirect($"/#{responseUri}");
return Task.CompletedTask;
}

static Task HandleRedirect(HttpContext context){
    var db = context.RequestServices.GetService<ILiteDatabase>();
    var collection = db.GetCollection<ShortLink>();

    var path = context.Request.Path.ToUriComponent().Trim('/');
    var id = ShortLink.GetId(path);
    var entry = collection.Find(p => p.Id == id).FirstOrDefault();

    if (entry != null)
        context.Response.Redirect(entry.Url);
    else
        context.Response.Redirect("/");

    return Task.CompletedTask;
}

