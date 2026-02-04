using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GledaoController : ControllerBase
    {

        private readonly IGraphClient _client;
        private readonly IDatabase _redisClient;
        public GledaoController(IGraphClient client, IDatabase redisClient)
        {
            _client = client;
            _redisClient = redisClient;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGledao([FromBody] Gledao gledao)
        {
            if (gledao == null || string.IsNullOrEmpty(gledao.KorisnikID.ToString()) || string.IsNullOrEmpty(gledao.FilmID.ToString()))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }

            var rezultat = await _client.Cypher
                .Match("(korisnik:User)", "(film:Movie)") 
                .Where("korisnik.id = $korisnikID AND film.id = $filmID")
                .Create("(korisnik)-[:GLEDAO {id: apoc.create.uuid(), Status:$status, DatumPromeneStatusa:$datumPromeneStatusa}]->(film)") 
                .WithParams(new
                {
                    korisnikID = gledao.KorisnikID,
                    filmID = gledao.FilmID,
                    status = gledao.Status,
                    datumPromeneStatusa = DateTime.Now
                })
                .Return((korisnik, film) => new
                {
                    KorisnickoIme = korisnik.As<User>().KorisnickoIme,
                    FilmIme = film.As<Movie>().Naslov
                })
                .ResultsAsync;

            var podatak = rezultat.FirstOrDefault();
            if (podatak.Equals(null))
            {
                return BadRequest("Film nije uspesno dodat na listu odgledanih filmova");
            }
            return Ok(new { Message = "Film je uspešno dodat na listu odgledanih filmova!" });
        }

        [HttpPatch("update")]
        public async Task<IActionResult> PatchStatus([FromBody] Gledao gledao)
        {
            if (gledao == null || string.IsNullOrEmpty(gledao.KorisnikID) || string.IsNullOrEmpty(gledao.FilmID))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }

            try
            {
                await _client.Cypher
                    .Match("(korisnik:User)-[rel:GLEDAO]->(film:Movie)") // nalazenje relacije izmedju korisnika i filma
                    .Where("korisnik.id = $korisnikID AND film.id = $filmID")   
                    //.Where("toString(korisnik.id) = $korisnikID AND toString(film.id) = $filmID")
                    .Set("rel.Status = COALESCE($status, rel.Status),rel.DatumPromeneStatusa = COALESCE($datumPromeneStatusa, rel.DatumPromeneStatusa)")             // Ažuriraj samo ako je vrednost prosleđena
                    .WithParams(new
                    {
                        korisnikID = gledao.KorisnikID,
                        filmID = gledao.FilmID,
                        status = gledao.Status,
                        datumPromeneStatusa = DateTime.Now
                    })
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "Status gledanja je uspesno azuriran!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteGledao([FromQuery] string korisnikID, [FromQuery] string filmID)
        {

            await _client.Cypher
                .Match("(korisnik:User)-[rel:GLEDAO]->(film:Movie)") 
                .Where("korisnik.id = $korisnikID AND film.id = $filmID")   
                .Delete("rel")                                            
                .WithParams(new
                {
                    korisnikID = korisnikID,
                    filmID = filmID
                })
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Relacija je uspešno obrisana." });
        }

        [HttpGet("{korisnikID}")]
        public async Task<IActionResult> GetGledao(string korisnikID)
        {
            try
            {
                var gledaniFilmovi = await _client.Cypher
                    .Match("(korisnik:User)-[rel:GLEDAO]->(film:Movie)")
                    .Where("korisnik.id = $korisnikID")
                    .WithParam("korisnikID", korisnikID) // dodavanje parametra korisnikID
                    .Return(film => film.As<Movie>())
                    .ResultsAsync;

                if (!gledaniFilmovi.Any())
                {
                    return NotFound("Korisnik nema odgledanih filmova.");
                }

                return Ok(gledaniFilmovi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }

        [HttpGet("proveri/{korisnikID}/{filmID}")]
        public async Task<IActionResult> ProveriGledao(string korisnikID, string filmID)
        {
            try
            {
                var postojiRelacija = await _client.Cypher
                    .Match("(korisnik:User)-[rel:GLEDAO]->(film:Movie)")
                    .Where("korisnik.id = $korisnikID AND film.id = $filmID")
                    .WithParams(new
                    {
                        korisnikID = korisnikID,
                        filmID = filmID
                    })
                    .Return(rel => rel.Count()) // broji koliko postoji relacija
                    .ResultsAsync;

                if (postojiRelacija.FirstOrDefault() > 0)
                {
                    return Ok(new { Message = "Korisnik je već gledao ovaj film." });
                }
                else
                {                    
                    return NotFound(new { Message = "Korisnik nije gledao ovaj film." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }

        [HttpGet("{korisnikID}/{statusFilma}")]
        public async Task<IActionResult> GetGledaoByStatus(string korisnikID, string statusFilma)
        {
            try
            {
                var gledaniFilmovi = await _client.Cypher
                            .Match("(korisnik:User)-[rel:GLEDAO]->(film:Movie)")
                            .Where("korisnik.id = $korisnikID AND rel.status = $statusFilma")
                            .WithParams(new
                            {
                                korisnikID,
                                statusFilma
                            })
                            .Return(film => film.As<Movie>())
                            .ResultsAsync;

                if (!gledaniFilmovi.Any())
                {
                    return NotFound("Korisnik nema odgledanih filmova.");
                }

                return Ok(gledaniFilmovi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredMovies(
        [FromQuery] string korisnikID,
        [FromQuery] int? godinaIzlaska,
        [FromQuery] double? ocena,
        [FromQuery] string? status,
        [FromQuery] string? zanr,
        [FromQuery] string? parametarSortiranja, 
        [FromQuery] string? VlasnikParametar, //film ili rel
        [FromQuery] bool? rastuci) // Parametar za smer sortiranja 

        {
            try
            {
                var query = _client.Cypher
                    .Match("(korisnik:User)-[rel:GLEDAO]->(film:Movie)")
                    .Where("korisnik.id = $korisnikID");
                
                if (godinaIzlaska.HasValue)
                {
                    query = query.AndWhere("film.GodinaIzlaska = $godinaIzlaska");
                }
                if (ocena.HasValue)
                {
                    query = query.AndWhere("film.ProsecnaOcena >= $ocena");
                }
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.AndWhere("rel.Status = $status");
                }
                if (!string.IsNullOrEmpty(zanr))
                {
                    query = query.AndWhere("film.Zanr = $zanr");
                }
               
                if (!string.IsNullOrEmpty(parametarSortiranja))
                {                    
                    if (rastuci.HasValue && rastuci.Value)
                    {
                        query = query.OrderBy($"{VlasnikParametar}.{parametarSortiranja}");
                    }
                    else
                    {
                        query = query.OrderByDescending($"{VlasnikParametar}.{parametarSortiranja}");
                    }
                }
                
                var rezultati = await query
                    .WithParams(new
                    {
                        korisnikID = korisnikID,
                        godinaIzlaska = godinaIzlaska,
                        ocena = ocena,
                        status = status,
                        zanr = zanr
                    })
                    .Return(film => film.As<Movie>())
                    .ResultsAsync;
               
                if (!rezultati.Any())
                {
                    return NotFound("Nijedan film nije pronadjen sa zadatim parametrima. ");
                }

                return Ok(rezultati);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greska na serveru: {ex.Message}");
            }
        }

    }
}
