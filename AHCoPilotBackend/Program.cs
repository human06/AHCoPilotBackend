using AHCoPilotBackend.Policies;
using Microsoft.AspNetCore.Mvc;
using Octokit;
using System.Net;
using Microsoft.Extensions.Options;
using AHCoPilotBackend.Configuration;
using AHCoPilotBackend.Interfaces;
using AHCoPilotBackend.Services;
using System.Text.Json;
using AHCoPilotBackend.Models;
using Octokit.Internal;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("GithubCopilot")
    .AddPolicyHandler(HttpClientPolicies.GetRetryPolicy())
    .AddPolicyHandler(HttpClientPolicies.GetCircuitBreakerPolicy())
    .AddPolicyHandler(HttpClientPolicies.GetTimeoutPolicy());


builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IGithubService, GithubService>();
builder.Services.AddSingleton<ICopilotService, CopilotService>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception occurred");

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "AA some issues . Please try again later."
            }));
        }
    });
});

app.UseHealthChecks("/health");
app.MapGet("/info", () => "Hello to AH Enablement CoPilot!");
app.MapGet("/callback", () => "You can close this window and return to GitHub");


app.MapPost("/", async (
    [FromHeader(Name = "X-Github-Token")] string githubToken,
    [FromBody] Payload payload,
    IGithubService githubService,
    ICopilotService copilotService,
    IOptions<AppSettings> appSettings,
    ILogger<Program> logger) =>
{
    try
    {

        // check the body 
        //string rawBody;
        //using (var reader = new StreamReader(request.Body))
        //{
        //    rawBody = await reader.ReadToEndAsync();
        //}
        //Console.WriteLine(rawBody);


        //var payload = JsonSerializer.Deserialize<Payload>(rawBody);


        if (string.IsNullOrEmpty(githubToken))
        {
            return Results.BadRequest(new { error = "GitHub token is required" });
        }

        if (payload == null || payload.Messages == null)
        {
            return Results.BadRequest(new { error = "Invalid payload" });
        }

        // Get GitHub user and enhance payload with system prompts
        var user = await githubService.GetUserAsync(githubToken);
        logger.LogInformation("Processing request for user: {Login}", user.Login);

        // Add system prompts
        var systemPrompt = appSettings.Value.DynamicSysPrompt ?? appSettings.Value.DefaultSysPrompt;

        payload.Messages.Insert(0, new Message
        {
            Role = "system",
            Content = $"Use user name in the conversation, which is @{user.Name}"
        });

        var lastMessage = payload.Messages.Last();
        if (lastMessage.copilot_references != null && lastMessage.copilot_references.Count > 0)
        {
            var highlightedSnippets = lastMessage.copilot_references
                .Where(f => f.type != null && f.type.Contains("client.selection", StringComparison.OrdinalIgnoreCase))
                .Select(f => $"File: {f.id}\nHighlighted code:\n{f.data.content}")
                .ToList();

            if (highlightedSnippets.Count() > 0)
            {
                systemPrompt += "\n\nThe user has highlighted the following code:\n" + string.Join("\n\n", highlightedSnippets);
            }



            var attachedFile = lastMessage.copilot_references
               .Where(f => f.type != null && f.type.Contains("\"client.file\"", StringComparison.OrdinalIgnoreCase))
               .Select(f => $"File: {f.id}\n Attached File:\n{f.data.content}")
               .ToList();

            if (attachedFile.Count() > 0)
            {
                systemPrompt += "\n\nThe user has attached the following file:\n" + string.Join("\n\n", attachedFile);
            }
        }

        payload.Messages.Insert(0, new Message
        {
            Role = "system",
            Content = systemPrompt
        });





        payload.Stream = true;

        // Get response from Copilot API
        var responseStream = await copilotService.GetCompletionsAsync(githubToken, payload);
        return Results.Stream(responseStream, "application/json");
    }
    catch (RateLimitExceededException ex)
    {
        logger.LogWarning(ex, "GitHub API rate limit exceeded");
        return Results.StatusCode(429);
    }
    catch (AuthorizationException ex)
    {
        logger.LogWarning(ex, "GitHub authorization failed");
        return Results.StatusCode(401);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing request");
        return Results.Problem("An unexpected error occurred. Please try again later.");
    }
});
app.Run();