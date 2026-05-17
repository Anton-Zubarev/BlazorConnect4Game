using Microsoft.Extensions.Caching.Memory;

namespace BlazorConnect4Game.FourOrderGame;

public class FourOrderGameStateManager
{

    private readonly IMemoryCache _cache;

    public FourOrderGameStateManager(IMemoryCache cache) => _cache = cache;

    public GameState Get(string roomNumber)
    {
        var validRoom = RoomNumberValidate(roomNumber);
        if (!string.IsNullOrEmpty(validRoom)) throw new Exception(validRoom);

        return _cache.GetOrCreate(roomNumber, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            return new GameState();
        });
    }

    public void Remove(string roomNumber) => _cache.Remove(roomNumber);

    string RoomNumberValidate(string roomNumber)
    {
        if (string.IsNullOrWhiteSpace(roomNumber)) return "Error! RoomNumber cannot be empty.";
        return string.Empty;
    }

}