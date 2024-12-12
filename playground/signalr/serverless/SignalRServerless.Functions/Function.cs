using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace SignalRServerless.Functions;

// Reference: https://github.com/aspnet/AzureSignalR-samples/tree/main/samples/DotnetIsolated-BidirectionChat
public class Functions
{
    private readonly ILogger _logger;

    public Functions(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Functions>();
    }

    [Function("index")]
    public HttpResponseData GetWebPage([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteString(File.ReadAllText("content/index.html"));
        response.Headers.Add("Content-Type", "text/html");
        return response;
    }

    [Function("Negotiate")]
    public static HttpResponseData Negotiate([HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "serverless", UserId = "{query.userid}")] string signalRConnectionInfo)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(signalRConnectionInfo);
        return response;
    }

    [Function("OnConnected")]
    [SignalROutput(HubName = "serverless")]
    public SignalRMessageAction OnConnected([SignalRTrigger("serverless", "connections", "connected")] SignalRInvocationContext invocationContext)
    {
        invocationContext.Headers.TryGetValue("Authorization", out var auth);
        _logger.LogInformation($"{invocationContext.ConnectionId} has connected");
        return new SignalRMessageAction("newConnection")
        {
            Arguments = new object[] { new NewConnection(invocationContext.ConnectionId, auth.FirstOrDefault() ?? string.Empty) },
        };
    }

    [Function("Broadcast")]
    [SignalROutput(HubName = "serverless")]
    public SignalRMessageAction Broadcast([SignalRTrigger("serverless", "messages", "Broadcast", "message")] SignalRInvocationContext invocationContext, string message)
    {
        return new SignalRMessageAction("newMessage")
        {
            Arguments = new object[] { new NewMessage(invocationContext, message) }
        };
    }

    [Function("SendToGroup")]
    [SignalROutput(HubName = "serverless")]
    public SignalRMessageAction SendToGroup([SignalRTrigger("serverless", "messages", "SendToGroup", "groupName", "message")] SignalRInvocationContext invocationContext, string groupName, string message)
    {
        return new SignalRMessageAction("newMessage")
        {
            GroupName = groupName,
            Arguments = new object[] { new NewMessage(invocationContext, message) }
        };
    }

    [Function("SendToUser")]
    [SignalROutput(HubName = "serverless")]
    public SignalRMessageAction SendToUser([SignalRTrigger("serverless", "messages", "SendToUser", "userName", "message")] SignalRInvocationContext invocationContext, string userName, string message)
    {
        return new SignalRMessageAction("newMessage")
        {
            UserId = userName,
            Arguments = new object[] { new NewMessage(invocationContext, message) }
        };
    }

    [Function("SendToConnection")]
    [SignalROutput(HubName = "serverless")]
    public SignalRMessageAction SendToConnection([SignalRTrigger("serverless", "messages", "SendToConnection", "connectionId", "message")] SignalRInvocationContext invocationContext, string connectionId, string message)
    {
        return new SignalRMessageAction("newMessage")
        {
            ConnectionId = connectionId,
            Arguments = new object[] { new NewMessage(invocationContext, message) }
        };
    }

    [Function("JoinGroup")]
    [SignalROutput(HubName = "serverless")]
    public SignalRGroupAction JoinGroup([SignalRTrigger("serverless", "messages", "JoinGroup", "connectionId", "groupName")] SignalRInvocationContext invocationContext, string connectionId, string groupName)
    {
        return new SignalRGroupAction(SignalRGroupActionType.Add)
        {
            GroupName = groupName,
            ConnectionId = connectionId
        };
    }

    [Function("LeaveGroup")]
    [SignalROutput(HubName = "serverless")]
    public SignalRGroupAction LeaveGroup([SignalRTrigger("serverless", "messages", "LeaveGroup", "connectionId", "groupName")] SignalRInvocationContext invocationContext, string connectionId, string groupName)
    {
        return new SignalRGroupAction(SignalRGroupActionType.Remove)
        {
            GroupName = groupName,
            ConnectionId = connectionId
        };
    }

    [Function("JoinUserToGroup")]
    [SignalROutput(HubName = "serverless")]
    public SignalRGroupAction JoinUserToGroup([SignalRTrigger("serverless", "messages", "JoinUserToGroup", "userName", "groupName")] SignalRInvocationContext invocationContext, string userName, string groupName)
    {
        return new SignalRGroupAction(SignalRGroupActionType.Add)
        {
            GroupName = groupName,
            UserId = userName
        };
    }

    [Function("LeaveUserFromGroup")]
    [SignalROutput(HubName = "serverless")]
    public SignalRGroupAction LeaveUserFromGroup([SignalRTrigger("serverless", "messages", "LeaveUserFromGroup", "userName", "groupName")] SignalRInvocationContext invocationContext, string userName, string groupName)
    {
        return new SignalRGroupAction(SignalRGroupActionType.Remove)
        {
            GroupName = groupName,
            UserId = userName
        };
    }

    [Function("OnDisconnected")]
    [SignalROutput(HubName = "serverless")]
    public void OnDisconnected([SignalRTrigger("serverless", "connections", "disconnected")] SignalRInvocationContext invocationContext)
    {
        _logger.LogInformation($"{invocationContext.ConnectionId} has disconnected");
    }

    public class NewConnection
    {
        public string ConnectionId { get; }

        public string Authentication { get; }

        public NewConnection(string connectionId, string auth)
        {
            ConnectionId = connectionId;
            Authentication = auth;
        }
    }

    public class NewMessage
    {
        public string ConnectionId { get; }
        public string Sender { get; }
        public string Text { get; }

        public NewMessage(SignalRInvocationContext invocationContext, string message)
        {
            Sender = string.IsNullOrEmpty(invocationContext.UserId) ? string.Empty : invocationContext.UserId;
            ConnectionId = invocationContext.ConnectionId;
            Text = message;
        }
    }
}
