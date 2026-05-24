// DAPPER — RaceCardRepository.cs (complex join)
using Dapper;

public class RaceCardRepository : IRaceCardRepository
{
    private readonly DapperContext _dapper;
    public RaceCardRepository(DapperContext dapper) => _dapper = dapper;

    public async Task<IEnumerable<Races>> GetRaceByDateAsync(string raceDate)
    {
       using var conn = _dapper.CreateConnection();

        const string sql = @"SELECT  
    r.race_id AS Id,
    r.race_name AS Name,
    CONCAT(rd.city_name , ' ', TO_CHAR(r.start_time, 'HH24:MI')) AS Venue,
    TO_CHAR(r.start_time, 'HH24:MI') AS Time,
    COUNT(rh.horse_id) AS Horses,
    r.status AS Status
FROM trackpulse.race_days rd
JOIN trackpulse.venues v        ON v.venue_id = rd.venue_id
JOIN trackpulse.races r         ON rd.race_day_id = r.race_day_id
JOIN trackpulse.race_horses rh  ON rh.race_id = r.race_id
WHERE DATE(rd.race_date) = CURRENT_DATE
GROUP BY 
    r.race_id, r.race_name, v.venue_name, r.start_time, r.status, rd.city_name
ORDER BY 
    CASE 
        WHEN r.status = 'Live' THEN 1
        WHEN r.status = 'Upcoming' THEN 2
        WHEN r.status = 'Completed' THEN 3
        WHEN r.status = 'Cancelled' THEN 4
        ELSE 5
    END,
    r.start_time;";

        // return await conn.QueryAsync<RaceCardDto>(sql, new { RaceDate = raceDate });
        return await conn.QueryAsync<Races>(sql);
    }

    public async Task<IEnumerable<RaceCardDto>> GetRaceByIdAsync(int raceId)
    {
       using var conn = _dapper.CreateConnection();

        const string sql = @"SELECT
                r.race_id           AS RaceId,                
                r.race_name         AS RaceName,
                rh.race_horse_id    AS RaceHorseId,
                h.horse_id       AS HorseId,
                h.horse_name        AS HorseName,
                h.age               AS Age,
                h.color             AS Color,
                h.gender            AS Gender,
                j.name              AS JockeyName,
                lo.win_odds         AS WinOdds,
                lo.place_odds       AS PlaceOdds,
                rh.position          AS Position,
                t.name              AS TrainerName
            FROM trackpulse.races r
            JOIN trackpulse.race_days rd    ON rd.race_day_id   = r.race_day_id            
            JOIN trackpulse.race_horses rh  ON rh.race_id        = r.race_id
            JOIN trackpulse.horses h        ON h.horse_id        = rh.horse_id
            LEFT JOIN trackpulse.jockeys j  ON j.jockey_id       = rh.jockey_id
            LEFT JOIN trackpulse.trainers t  ON t.id              = rh.trainer_id
            LEFT JOIN LATERAL (
                SELECT win_odds, place_odds
                FROM trackpulse.odds
                WHERE race_horse_id = rh.race_horse_id
                ORDER BY updated_at DESC
                LIMIT 1
            ) lo ON TRUE
            WHERE r.race_id=@RaceId
            ORDER BY rh.position ASC";
var result= await conn.QueryAsync<RaceCardDto>(sql, new { RaceId = raceId });
return result;
    }

    public async Task<IEnumerable<RaceCardDto>> GetTodaysRaceCardAsync()
    {
        using var conn = _dapper.CreateConnection();

        const string sql = @"
            SELECT
                r.race_id           AS RaceId,
                r.race_number       AS RaceNumber,
                r.race_name         AS RaceName,
                r.race_type         AS RaceType,
                r.distance_meters   AS DistanceMeters,
                r.start_time        AS StartTime,
                r.status            AS Status,
                v.venue_name        AS VenueName,
                c.city_name         AS CityName,
                rh.race_horse_id    AS RaceHorseId,
                rh.draw_number      AS DrawNumber,
                rh.weight           AS Weight,
                rh.rating           AS Rating,
                h.horse_name        AS HorseName,
                h.age               AS Age,
                h.color             AS Color,
                h.gender            AS Gender,
                j.name              AS JockeyName,
                lo.win_odds         AS WinOdds,
                lo.place_odds       AS PlaceOdds
            FROM trackpulse.races r
            JOIN trackpulse.race_days rd    ON rd.race_day_id   = r.race_day_id
            JOIN trackpulse.venues v        ON v.venue_id        = rd.venue_id
            JOIN trackpulse.cities c         ON c.city_id         = v.city_id
            JOIN trackpulse.race_horses rh  ON rh.race_id        = r.race_id
            JOIN trackpulse.horses h        ON h.horse_id        = rh.horse_id
            LEFT JOIN trackpulse.jockeys j  ON j.jockey_id       = rh.jockey_id
            LEFT JOIN LATERAL (
                SELECT win_odds, place_odds
                FROM trackpulse.odds
                WHERE race_horse_id = rh.race_horse_id
                ORDER BY updated_at DESC
                LIMIT 1
            ) lo ON TRUE
            WHERE rd.race_date = CURRENT_DATE
            ORDER BY r.race_number, rh.draw_number";

        return await conn.QueryAsync<RaceCardDto>(sql);
    }
}
