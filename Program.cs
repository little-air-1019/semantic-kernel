using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using dotenv.net;

internal class Program
{
    private static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        // Load .env file
        DotEnv.Load();

        // OpenAI 設定
        var ModelId = Environment.GetEnvironmentVariable("OPENAI_DEPLOY_NAME") ?? "gpt-4o";
        var ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";

        // Create a new kernel builder
        var builder = Kernel.CreateBuilder()
                    .AddOpenAIChatCompletion(ModelId, ApiKey);
        builder.Plugins.AddFromType<LeaveRequestPlugin>(); // Add the LeaveRequestPlugin
        builder.Plugins.AddFromType<TimePlugin>();         // Add the TimePlugin
        Kernel kernel = builder.Build();

        // Create chat history 物件，並且加入
        var history = new ChatHistory();
        history.AddSystemMessage(
            @"你是企業的請假助理，可以協助員工進行請假，或是查詢請假天數等功能。
                 若員工需要請假，你需要蒐集請假起始日期、天數、請假事由、代理人、請假者姓名等資訊。最後呼叫 LeaveRequest Method。
                 若員工需要查詢請假天數，你需要蒐集請假者姓名，最後呼叫 GetLeaveRecordAmount Method。
                 --------------
                 * 所有對談請用正體中文回答
                 * 請以口語化的方式來回答，要適合對談機器人的角色
                ");

        // Get chat completion service
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        // 開始對談
        Console.Write("User > ");
        string? userInput;
        while (!string.IsNullOrEmpty(userInput = Console.ReadLine()))
        {
            // Add user input
            history.AddUserMessage(userInput);

            // Enable auto function calling
            OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel);

            // Print the results
            Console.WriteLine("Assistant > " + result);

            // Add the message from the agent to the chat history
            history.AddMessage(result.Role, result.Content ?? string.Empty);

            // Get user input again
            Console.Write("User > ");
        }
    }
}

// 請假功能 Plugin
public class LeaveRequestPlugin
{
    [KernelFunction]
    [Description("取得請假天數")]
    public int GetLeaveRecordAmount([Description("要查詢請假天數的員工名稱")] string employeeName)
    {
        //修改顯示顏色
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n [action]查詢 {employeeName} 請假天數。\n");
        //還原顯示顏色
        Console.ResetColor();

        if (employeeName.ToLower() == "david")
            return 5;
        else if (employeeName.ToLower() == "eric")
            return 8;
        else
            return 3;
    }

    [KernelFunction]
    [Description("進行請假")]
    public bool LeaveRequest([Description("請假起始日期")] DateTime 請假起始日期, [Description("請假天數")] string 天數, [Description("請假事由")] string 請假事由, [Description("代理人")] string 代理人,
    [Description("請假者姓名")] string 請假者姓名)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n [action]建立假單:  {請假者姓名} 請假 {天數}天，從 {請假起始日期} 開始，事由為 {請假事由}，代理人 {代理人}\n");
        //還原顯示顏色
        Console.ResetColor();

        return true;
    }
}

// 時間功能 Plugin
public class TimePlugin
{
    [KernelFunction]
    [Description("取得今天日期")]
    public DateTime GetCurrentDate()
    {
        return DateTime.UtcNow.AddHours(8);
    }
}
