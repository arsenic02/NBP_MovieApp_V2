using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Neo4jClient;
using System;

namespace HR.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcenaController : ControllerBase
    {
        private readonly IDatabase _redisDb;
        private readonly IServer _redisServer;

        private readonly IGraphClient _client;

        public OcenaController(IConnectionMultiplexer redis, IGraphClient client) 
        {          
            _redisDb = redis.GetDatabase();
            _redisServer = redis.GetServer(redis.GetEndPoints().First()); 
            _client = client;
        }

        [HttpPost("createOcena")]
        public async Task<IActionResult> OceniFilm([FromBody] Ocena ocena)
        {
            if (ocena == null || string.IsNullOrEmpty(ocena.MovieId) || string.IsNullOrEmpty(ocena.UserId))
            {
                return BadRequest("Podaci nisu validni.");
            }

            if (ocena.UnetaOcena < 1 || ocena.UnetaOcena > 10)
            {
                return BadRequest("Ocena mora biti između 1 i 10.");
            }

            string key = $"ocena:{ocena.UserId}:{ocena.MovieId}";

            var existingOcena = await _redisDb.ListRangeAsync(key);
            if (existingOcena.Any())
            {
                return Conflict("Vec ste ocenili ovaj film!");
            }
            
            await _redisDb.ListRightPushAsync(key, ocena.UnetaOcena);
            await _redisDb.StringSetAsync($"{key}_last", ocena.UnetaOcena);
           
            await AzurirajProsecnuOcenuNeo4j(ocena.MovieId, ocena.UnetaOcena);

            return Ok(new { Message = "Ocena je uspešno dodata.", UnetaOcena = ocena.UnetaOcena });
        }

        private async Task AzurirajProsecnuOcenuNeo4j(string movieId, float novaOcena)
        {
            if (_client == null)
            {
                Console.WriteLine("Neo4j klijent nije inicijalizovan!");
                return;
            }

            string keyPattern = $"ocena:*:{movieId}";
            var keys = _redisServer.Keys(pattern: keyPattern).ToList();

            if (!keys.Any())
            {
                Console.WriteLine($"Nema ocena u Redis-u za film {movieId}, Neo4j neće biti ažuriran.");
                return;
            }

            List<int> sveOcene = new List<int>();

            foreach (var ocenaKey in keys)
            {
                var ocene = await _redisDb.ListRangeAsync(ocenaKey);
                sveOcene.AddRange(ocene.Select(o => (int)o));
            }

            if (!sveOcene.Any())
            {
                Console.WriteLine($"Neo4j: Nema dostupnih ocena za {movieId}, ne ažuriram.");
                return;
            }

            float prosecnaOcena = (float)sveOcene.Average();

            Console.WriteLine($"AzurirajProsecnuOcenuNeo4j - MovieId: {movieId}, NovaProsecnaOcena: {prosecnaOcena}");

            try
            {
                await _client.Cypher
                    .Match("(m:Movie {id: $movieId})")
                    .Set("m.ProsecnaOcena = $prosecnaOcena")
                    .WithParams(new
                    {
                        movieId,
                        prosecnaOcena
                    })
                    .ExecuteWithoutResultsAsync();

                Console.WriteLine($"Neo4j ažuriran: {movieId} - nova prosečna ocena: {prosecnaOcena}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Neo4j greška: {ex.Message}");
            }
        }
        
        [HttpGet("prosecnaOcena/{movieId}")]
        public async Task<IActionResult> GetProsecnaOcena(string movieId)
        {
            if (string.IsNullOrEmpty(movieId))
            {
                return BadRequest("MovieId nije validan.");
            }

            string key = $"ocena:*:{movieId}"; // trazi sve ocene za dati film
            var keys = _redisServer.Keys(pattern: key).ToList();

            if (!keys.Any())
            {
                return Ok(new { ProsecnaOcena = "N/A" });
            }

            List<int> sveOcene = new List<int>();

            foreach (var ocenaKey in keys)
            {
                var ocene = await _redisDb.ListRangeAsync(ocenaKey);
                sveOcene.AddRange(ocene.Select(o => (int)o));
            }

            if (sveOcene.Count == 0)
            {
                return Ok(new { ProsecnaOcena = "N/A" });
            }

            float prosecnaOcena = (float)sveOcene.Average();

            return Ok(new
            {
                ProsecnaOcena = prosecnaOcena,
                ListaOcena = sveOcene
            });
        }

        [HttpGet("getUnetaOcena/{userId}/{movieId}")]
        public async Task<IActionResult> GetUnetaOcena(string userId, string movieId)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(movieId))
            {
                return BadRequest("Podaci nisu validni.");
            }

            string key = $"ocena:{userId}:{movieId}";
            var unetaOcenaValue = await _redisDb.StringGetAsync($"{key}_last");
            int? unetaOcena = unetaOcenaValue.HasValue ? (int)unetaOcenaValue : (int?)null;

            return Ok(new
            {
                UnetaOcena = unetaOcena
            });
        }

        [HttpDelete("deleteAllOcene/{movieId}")]
        public async Task<IActionResult> DeleteAllOcene(string movieId)
        {
            if (string.IsNullOrEmpty(movieId))
            {
                return BadRequest("MovieId nije validan.");
            }

            string keyPattern = $"ocena:*:{movieId}";
            var keys = _redisServer.Keys(pattern: keyPattern).ToList();

            if (!keys.Any())
            {
                return NotFound("Nema ocena za ovaj film.");
            }

            // brisanje svih ključeva
            foreach (var key in keys)
            {
                await _redisDb.KeyDeleteAsync(key);
            }

            // brisanje prosecne ocene u Neo4j
            try
            {
                await _client.Cypher
                    .Match("(m:Movie {id: $movieId})")
                    .Set("m.ProsecnaOcena = 0.0")
                    .WithParam("movieId", movieId)
                    .ExecuteWithoutResultsAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Neo4j greška: {ex.Message}");
            }

            return Ok("Sve ocene za film su obrisane.");
        }


    }
}
