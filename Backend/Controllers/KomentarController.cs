using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace HR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KomentarController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly IServer _redisServer;

        public KomentarController(IConnectionMultiplexer redis) 
        {           
            _redisDb = redis.GetDatabase();
            _redisServer = redis.GetServer(redis.GetEndPoints().First()); 
        }

        [HttpPost("createComment")]
        public async Task<IActionResult> CreateComment([FromBody] Komentar komentar)
        {
            if (komentar == null || string.IsNullOrEmpty(komentar.text) || string.IsNullOrEmpty(komentar.userId) || string.IsNullOrEmpty(komentar.movieId))
            {
                return BadRequest("Komentar ili obavezna polja nisu validna.");
            }

            // generisanje jedinstvenog ID-ja za komentar koristeći Redis INCR
            var newCommentId = await _redisDb.StringIncrementAsync("commentIdCounter");
            komentar.id = newCommentId.ToString(); // postavljanje generisanog ID-ja
            
            var key = $"film:{komentar.movieId}:user:{komentar.userId}";
           
            var serializedComment = System.Text.Json.JsonSerializer.Serialize(komentar);
          
            await _redisDb.StringSetAsync(key, serializedComment);

            return Ok(new { Message = "Komentar je sačuvan.", KomentarId = komentar.id });
        }

        [HttpGet("getKomentariFilma/{idFilma}")]
        public IActionResult GetCommentsByFilmId(string idFilma)
        {
            if (string.IsNullOrEmpty(idFilma))
            {
                return BadRequest("ID filma nije validan.");
            }
           
            var allKeys = _redisServer.Keys().Where(key => key.ToString().StartsWith($"film:{idFilma}:user:")).ToArray();

            if (!allKeys.Any())
            {
                return NotFound("Komentari za dati film nisu pronađeni.");
            }
          
            var comments = allKeys.Select(key =>
            {
                var serializedComment = _redisDb.StringGet(key);
                return System.Text.Json.JsonSerializer.Deserialize<Komentar>(serializedComment);
            }).ToList();

            return Ok(comments);
        }
       
        [HttpDelete("deleteComment/{userId}/{movieId}")]
        public async Task<IActionResult> DeleteComment(string userId, string movieId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId))
            {
                return BadRequest("ID korisnika ili ID filma nisu validni.");
            }
            
            var key = $"film:{movieId}:user:{userId}";
          
            if (!await _redisDb.KeyExistsAsync(key))
            {
                return NotFound("Komentar nije pronađen.");
            }
           
            bool deleted = await _redisDb.KeyDeleteAsync(key);

            if (deleted)
            {
                return Ok(new { Message = "Komentar uspešno obrisan." });
            }
            else
            {
                return StatusCode(500, "Došlo je do greške prilikom brisanja komentara.");
            }
        }

    }
}
