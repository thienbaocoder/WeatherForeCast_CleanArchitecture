using System.Threading;
using System.Threading.Tasks;
using WeatherWeb.Application.Features.Chat.DTOs;

namespace WeatherWeb.Application.Abstractions;

public interface IChatBot
{
    Task<ChatReply> AskAsync(string message, ChatContext context, CancellationToken ct = default);
}
