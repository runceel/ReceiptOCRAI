// 実験的な機能を使っているときにでる警告を抑止
#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// 構成の読み込み
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var endpoint = configuration["OpenAI:Endpoint"];
var deploymentName = configuration["OpenAI:DeploymentName"];

// 手抜き null チェック
ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));
ArgumentNullException.ThrowIfNull(deploymentName, nameof(deploymentName));

// Kernel を作成
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName,
        endpoint,
        new AzureCliCredential())
    .Build();

var agent = new ChatCompletionAgent
{
    Instructions = """
        あなたは世界最高品質の OCR ツールです。入力画像に対して適切な操作を行い必要最低限の回答をしてください。
        """,
    Kernel = kernel,
    Name = "OCR",
    Arguments = new(new OpenAIPromptExecutionSettings
    {
        Temperature = 0.0,
    }),
};

ChatHistory chatHistory = [];
chatHistory.Add(new ChatMessageContent(
    AuthorRole.User,
    items: [
        new ImageContent(await File.ReadAllBytesAsync("contoso-receipt.png"), "image/jpeg"),
        new TextContent("レシート画像の中にある日付情報だけを抽出して回答してください。他の不要な情報は一切入れないでください。"),
    ]));
await foreach (var message in agent.InvokeAsync(chatHistory))
{
    chatHistory.Add(message);
    Console.WriteLine($"{message.AuthorName}({message.Role}): {message.Content}");
}
