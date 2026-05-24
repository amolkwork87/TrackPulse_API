using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Globalization;
public class RaceRepository : IRaceRepository
{
    private readonly AppDbContext _db;
    private readonly DapperContext _dapper;
    public RaceRepository(AppDbContext db, DapperContext dapper)
    {
        _db = db;
        _dapper = dapper;
    }

    public async Task<Race> GetByRaceDayAndNameAsync(int raceDayId, string raceName)
    {
        return await _db.Races
            .FirstOrDefaultAsync(r => r.RaceDayId == raceDayId && r.RaceName == raceName);
    }

    public async Task<IEnumerable<dynamic>> GetRaceByCityAndDateAsync(string cityName, string raceDate)
    {
        using var conn = _dapper.CreateConnection();

        const string sql = @"SELECT 
                                r.*, rd.city_name, 
                                rd.race_date
                            FROM trackpulse.race_days rd
                            JOIN trackpulse.races r 
                                ON rd.race_day_id = r.race_day_id
                                    WHERE rd.city_name = @CityName
                                    AND rd.race_date = @RaceDate";

        //var races = await conn.QueryAsync(sql, new { CityName = cityName});
        var parsedDate = DateTime.ParseExact(
    raceDate,
    "yyyy-MM-dd",
    CultureInfo.InvariantCulture
).Date;

        var races = await conn.QueryAsync(sql, new { CityName = cityName.ToUpper(), RaceDate = parsedDate });

        return races;
    }

    public async Task<Race> CreateAsync(Race race)
    {
        var existingRace = await _db.Races
            .FirstOrDefaultAsync(r => r.RaceDayId == race.RaceDayId && r.RaceName == race.RaceName);

        if (existingRace != null) return existingRace;

race.StartTime = DateTime.SpecifyKind(race.StartTime.Value, DateTimeKind.Utc);
        _db.Races.Add(race);
        await _db.SaveChangesAsync();
        return race;
    }

    public async Task UpdateStatusAsync(int raceId, string status)
{
    const string sql = @"UPDATE trackpulse.races SET status = @Status WHERE race_id = @RaceId";
    using var conn = _dapper.CreateConnection();
    await conn.ExecuteAsync(sql, new { RaceId = raceId, Status = status });
}
}
