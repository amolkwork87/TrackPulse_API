// DAPPER — OddsRepository.cs (high frequency updates)
using Dapper;

public class OddsRepository : IOddsRepository
{
    private readonly DapperContext _dapper;
    public OddsRepository(DapperContext dapper) => _dapper = dapper;

    public async Task UpdateOddsAsync(Odds dto, int adminId)
    {
        using var conn = _dapper.CreateConnection();

        const string sql = @"
            INSERT INTO trackpulse.odds (race_horse_id, win_odds, place_odds, updated_at, updated_by)
            VALUES (@RaceHorseId, @WinOdds, @PlaceOdds, NOW(), @AdminId)";

        await conn.ExecuteAsync(sql, new
        {
            dto.RaceHorseId,
            dto.WinOdds,
            dto.PlaceOdds,
            AdminId = adminId
        });
    }

    // Bulk update all horses in a race at once
    public async Task BulkUpdateOddsAsync(List<Odds> horseOdds)
    {
        if (horseOdds == null || !horseOdds.Any())
            return;

        using var conn = _dapper.CreateConnection();

        // For PostgreSQL (recommended)
        const string sql = @"
        INSERT INTO trackpulse.odds (race_horse_id, win_odds, place_odds, updated_at, updated_by)
        VALUES (@RaceHorseId, @WinOdds, @PlaceOdds, NOW(),1) -- Replace with actual admin ID if needed
        ON CONFLICT (race_horse_id) 
        DO UPDATE SET 
            win_odds = EXCLUDED.win_odds,
            place_odds = EXCLUDED.place_odds,
            updated_at = NOW(),
            updated_by = 1 -- Replace with actual admin ID if needed)
        RETURNING race_horse_id;";

        try
        {
            var result = await conn.ExecuteAsync(sql, horseOdds);
            // result will be the number of affected rows (both updates and inserts)
        }
        catch (Exception ex)
        {
            // Log the exception
            Console.WriteLine($"Error in BulkUpdateOddsAsync: {ex.Message}");
            throw;
        }
    }
}
