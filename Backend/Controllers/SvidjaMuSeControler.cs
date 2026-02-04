using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SvidjaMuSeController : ControllerBase
    {

        private readonly IGraphClient _client;
        private readonly IDatabase _redisClient;//Dodaj
        public SvidjaMuSeController(IGraphClient client, IDatabase redisClient)
        {
            _client = client;
            _redisClient = redisClient;
        }

        [HttpPost("svidjaMuSe")]
        public async Task<IActionResult> CreateSvidjaMuSe([FromBody] Svidja_Mu_Se svidjaMuSe)
        {
            if (svidjaMuSe == null || string.IsNullOrEmpty(svidjaMuSe.KorisnikID) || string.IsNullOrEmpty(svidjaMuSe.FilmID))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }

            try
            {
                // proveri da li već postoji veza SVIDJA_MU_SE
                var postojiSvidjaMuSe = await _client.Cypher
                    .Match("(korisnik:User)-[rel:SVIDJA_MU_SE]->(film:Movie)")
                    .Where("korisnik.id = $korisnikID AND film.id = $filmID")
                    .WithParams(new
                    {
                        korisnikID = svidjaMuSe.KorisnikID,
                        filmID = svidjaMuSe.FilmID
                    })
                    .Return(rel => rel.Count())
                    .ResultsAsync;

                if (postojiSvidjaMuSe.FirstOrDefault() > 0)
                {                   
                    return BadRequest(new { Message = "Već ste označili da vam se sviđa ovaj film." });
                }

                // kreiraj vezu SVIDJA_MU_SE
                var rezultat = await _client.Cypher
                    .Match("(korisnik:User)", "(film:Movie)")
                    .Where("korisnik.id = $korisnikID AND film.id = $filmID")
                    .Create("(korisnik)-[:SVIDJA_MU_SE {id: apoc.create.uuid(), javna: $javna}]->(film)")
                    .WithParams(new
                    {
                        korisnikID = svidjaMuSe.KorisnikID,
                        filmID = svidjaMuSe.FilmID,
                        javna = svidjaMuSe.javna
                    })
                    .Return((korisnik, film) => new
                    {
                        KorisnickoIme = korisnik.As<User>().KorisnickoIme,
                        FilmIme = film.As<Movie>().Naslov
                    })
                    .ResultsAsync;

                var podatak = rezultat.FirstOrDefault();
                if (svidjaMuSe.javna == true && podatak != null)
                {
                    string poruka = $"U listu {podatak.KorisnickoIme} je dodat novi film: {podatak.FilmIme}";
                    _redisClient.StreamAdd(
                        podatak.KorisnickoIme,
                        new NameValueEntry[]
                        {
                    new NameValueEntry("message", poruka)
                        });
                }

                return Ok(new { Message = "Film je uspešno dodat na listu omiljenih filmova!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");

            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteSvidjaMuSe([FromQuery] string korisnikID, [FromQuery] string filmID)
        {

            await _client.Cypher
                .Match("(korisnik:User)-[rel:SVIDJA_MU_SE]->(film:Movie)") 
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
        public async Task<IActionResult> GetSvidjaMuSe(string korisnikID)
        {
            try
            {
                var omiljeniFilmovi = await _client.Cypher
                    .Match("(korisnik:User)-[rel:SVIDJA_MU_SE]->(film:Movie)")
                    .Where("korisnik.id = $korisnikID")
                    .WithParam("korisnikID", korisnikID) 
                    .Return(film => film.As<Movie>())
                    .ResultsAsync;

                if (!omiljeniFilmovi.Any())
                {
                    return NotFound("Korisnik nema omiljenih filmova.");
                }

                return Ok(omiljeniFilmovi);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }

        [HttpGet("{korisnikID}/{filmID}")]
        public async Task<IActionResult> ProveriDaLiJeLajkovao(string korisnikID, string filmID)
        {
            try
            {
                var postojiSvidjanje = await _client.Cypher
                    .Match("(korisnik:User)-[rel:SVIDJA_MU_SE]->(film:Movie)")
                    .Where("korisnik.id = $korisnikID AND film.id = $filmID")
                    .WithParams(new { korisnikID, filmID })
                    .Return(rel => rel.Count())
                    .ResultsAsync;

                bool lajkovano = postojiSvidjanje.FirstOrDefault() > 0;

                return Ok(new { lajkovano });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Greška na serveru: {ex.Message}" });
            }
        }


        [HttpPut("UpdateJavna")]
        public async Task<IActionResult> UpdateJavna([FromBody] Svidja_Mu_Se svidjaMuSe)
        {
            if (svidjaMuSe == null || string.IsNullOrEmpty(svidjaMuSe.KorisnikID) || string.IsNullOrEmpty(svidjaMuSe.FilmID))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }

            await _client.Cypher
                .Match("(korisnik:User)-[rel:SVIDJA_MU_SE]->(film:Movie)") // Pronađi relaciju između korisnika i filma
                .Where("korisnik.id = $korisnikID AND film.id = $filmID")   
                .Set("rel.javna = $javna")                                 // azuriraj vrednost javna
                .WithParams(new
                {
                    korisnikID = svidjaMuSe.KorisnikID,
                    filmID = svidjaMuSe.FilmID,
                    javna = svidjaMuSe.javna
                })
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Vrednost javna je uspešno ažurirana!" });
        }

        [HttpPut("UpdateJavna2/{korisnikID}/{filmID}/{javna}")]
        public async Task<IActionResult> UpdateJavna(string korisnikID, string filmID, bool javna)
        {
            if (string.IsNullOrEmpty(korisnikID) || string.IsNullOrEmpty(filmID))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }

            await _client.Cypher
                .Match("(korisnik:User)-[rel:SVIDJA_MU_SE]->(film:Movie)") 
                .Where("korisnik.id = $korisnikID AND film.id = $filmID")   
                .Set("rel.javna = $javna")                                
                .WithParams(new
                {
                    korisnikID,
                    filmID,
                    javna
                })
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Vrednost javna je uspešno ažurirana!" });
        }
    }
}
