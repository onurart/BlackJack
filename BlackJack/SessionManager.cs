using BlackJack.Service;
using System.Collections.Concurrent;
using BlackJack.Models;
namespace BlackJack;
public class SessionManager
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions;
    private readonly ConcurrentDictionary<Guid, string> _sessionUsers;

    public SessionManager()
    {
        _sessions = new ConcurrentDictionary<Guid, GameSession>();
        _sessionUsers = new ConcurrentDictionary<Guid, string>();
    }

    public Guid StartNewSession(string username)
    {
        var sessionId = Guid.NewGuid();
        _sessionUsers[sessionId] = username;
        _sessions[sessionId] = new GameSession(new CasualMode()); 
        return sessionId;
    }

    public bool ValidateSession(Guid sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    public void UpdateGameSession(Guid sessionId, GameSession gameSession)
    {
        _sessions[sessionId] = gameSession; 
    }

    public GameSession GetGameSession(Guid sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public void EndSession(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        _sessionUsers.TryRemove(sessionId, out _);
    }

    public string GetUsernameBySessionId(Guid sessionId)
    {
        _sessionUsers.TryGetValue(sessionId, out var username);
        return username;
    }
}