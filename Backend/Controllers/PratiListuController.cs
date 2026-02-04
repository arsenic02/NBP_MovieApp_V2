using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PratiListuController : ControllerBase
    {

        private readonly IGraphClient _client;
        private readonly IDatabase _redisClient;//Dodaj
        public PratiListuController(IGraphClient client, IDatabase redisClient)
        {
            _client = client;
            _redisClient = redisClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePratiListu([FromBody] Prati_Listu pratiLista)
        {
            if (pratiLista == null || string.IsNullOrEmpty(pratiLista.IzvorniKorisnikID.ToString()) || string.IsNullOrEmpty(pratiLista.OdredisniKorisnikID.ToString()))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }
         
            var rezultat = await _client.Cypher
                .Match("(izvor:User)", "(odrediste:User)") 
                .Where("izvor.id = $izvorniKorisnikID AND odrediste.id = $odredisniKorisnikID")
                .Create("(izvor)-[:PRATI {id: apoc.create.uuid()}]->(odrediste)") 
                .WithParams(new
                {
                    izvorniKorisnikID = pratiLista.IzvorniKorisnikID,
                    odredisniKorisnikID = pratiLista.OdredisniKorisnikID
                })
                .Return((odrediste) => new
                {                  
                    KorisnickoImeOdredista = odrediste.As<User>().KorisnickoIme
                })
                .ResultsAsync;
            string imeStreama;
            if (rezultat.FirstOrDefault() != null)
                imeStreama = rezultat.FirstOrDefault().KorisnickoImeOdredista;
            else
                imeStreama = "";

            string id = pratiLista.IzvorniKorisnikID;

            string key = $"user:{id}:streams";
            // Postavljanje vrednosti pozicije za dati stream
            
            var result = _redisClient.Execute("XINFO", "STREAM", imeStreama);
            var responseArray = (RedisResult[])result;
            string lastGeneratedId = null;
          
            for (int i = 0; i < responseArray.Length; i++)
            {
                if (responseArray[i].ToString() == "last-generated-id")
                {                    
                    lastGeneratedId = responseArray[i + 1].ToString();
                    break;
                }
            }

            string position = lastGeneratedId;

            _redisClient.HashSet(key, imeStreama, position);
            return Ok(new { Message = "Korisnik je uspešno zapraćen!" });
        }


        [HttpDelete]
        public async Task<IActionResult> DeletePratiListu([FromBody] Prati_Listu pratiLista)
        {
            if (pratiLista == null || string.IsNullOrEmpty(pratiLista.IzvorniKorisnikID) || string.IsNullOrEmpty(pratiLista.OdredisniKorisnikID))
            {
                return BadRequest("Podaci nisu ispravno prosleđeni.");
            }
            
            await _client.Cypher
                .Match("(izvor:User)-[p:PRATI]->(odrediste:User)") 
                .Where("izvor.id = $izvorniKorisnikID AND odrediste.id = $odredisniKorisnikID")
                .Delete("p") 
                .WithParams(new
                {
                    izvorniKorisnikID = pratiLista.IzvorniKorisnikID,
                    odredisniKorisnikID = pratiLista.OdredisniKorisnikID
                })
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Veza uspešno uklonjena." });
        }

        [HttpGet("{korisnikID}")]
        public async Task<IActionResult> GetPratiListu(string korisnikID)
        {
            try
            {
                var pratiListu = await _client.Cypher
                    .Match("(izvor:User)-[rel:PRATI]->(odrediste:User)") 
                    .Where("izvor.id = $korisnikID") 
                    .WithParam("korisnikID", korisnikID) 
                    .Return((izvor, rel, odrediste) => new
                    {
                        id = rel.As<Prati_Listu>().id,
                        IzvorniKorisnikID = izvor.As<Person>().id,
                        OdredisniKorisnikID = odrediste.As<Person>().id
                    })
                    .ResultsAsync; 

                if (!pratiListu.Any())
                {
                    return NotFound("Korisnik ne prati nijednog korisnika.");
                }

                return Ok(pratiListu);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }
        [HttpGet("followed-list/{korisnikID}")]
        public async Task<IActionResult> GetFilmoveIListe(string korisnikID, int page = 1, int pageSize = 2)
        {
            try
            {
                var skipCount = (page - 1) * pageSize;
                var listeSaFilmovima = await _client.Cypher
                     .Match("(izvor:User)-[:PRATI]->(odrediste:User)")
                     .Where("izvor.id = $korisnikID")
                     .WithParam("korisnikID", korisnikID)
                     .Skip(skipCount)  
                     .Limit(pageSize)  
                     .OptionalMatch("(odrediste)-[rel:SVIDJA_MU_SE]->(film:Movie)")
                     .Where("rel.javna = true")
                     .Return((odrediste, film) => new
                     {
                         VlasnikID = odrediste.As<User>().id,
                         ImeListe = odrediste.As<User>().KorisnickoIme,
                         Film = film.CollectAs<Movie>()
                     })
                     .ResultsAsync;

                if (!listeSaFilmovima.Any())
                {
                    return NotFound("Korisnik ne prati nikoga ili nema filmova.");
                }

                var rezultat = listeSaFilmovima.Select(l => new Lista_Filmovi
                {
                    VlasnikID = l.VlasnikID,
                    ImeListe = l.ImeListe,
                    ListaFilmova = l.Film.ToList() 
                }).ToList();
                return Ok(new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalResults = rezultat.Count,
                    Lista = rezultat
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Greška na serveru: {ex.Message}");
            }
        }

        [HttpGet("recommended-lists")]
        public async Task<IActionResult> GetRecommendedMovieLists([FromQuery] string korisnikID, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            int skip = (page - 1) * size;

            try
            {
                var query = await _client.Cypher
                    .Match("(u:User {id: $userId})-[:SVIDJA_MU_SE]->(f:Movie)")
                    .With("u, f.Zanr AS zanr, COUNT(f) AS brojFilmova")
                    .OrderByDescending("brojFilmova")
                    .Limit(2)
                    .With("u, COLLECT(zanr) AS topZanrovi")

                    .Match("(slican:User)-[:SVIDJA_MU_SE]->(film:Movie)")
                    .Where("film.Zanr IN topZanrovi AND slican.id <> u.id")
                    .With("u, slican, COUNT(film) AS brojSlicnihFilmova, topZanrovi")
                    .OrderByDescending("brojSlicnihFilmova")
                    .Limit(5)

                    .Match("(slican)-[:SVIDJA_MU_SE]->(preporuceniFilm:Movie)")
                    .With("u, topZanrovi, slican, COLLECT(DISTINCT preporuceniFilm) AS preporuceniFilmovi")
                    .WithParam("userId", korisnikID)
                    .Return((slican, preporuceniFilmovi) => new
                    {
                        NajslicnijiKorisnici = slican.As<User>(),
                        PreporuceniFilmovi = preporuceniFilmovi.As<List<Movie>>()
                    })
                    .ResultsAsync;

                // provera da li su podaci null i konverzija u listu
                var queryResult = query?.ToList();

                if (!queryResult.Any())
                {
                    return NotFound(new { Message = "Nema preporučenih filmova za ovog korisnika." });
                }

                var rezultat = queryResult.Select(l => new
                {
                    NajslicnijiKorisnici = new List<object>
            {
                new
                {
                    l.NajslicnijiKorisnici.KorisnickoIme,
                    l.NajslicnijiKorisnici.id,
                    l.NajslicnijiKorisnici.Mejl
                }
            },
                    PreporuceniFilmovi = l.PreporuceniFilmovi?.Select(film => new
                    {
                        film.id,
                        film.Naslov,
                        film.Zanr,
                        film.Kategorija,
                        film.GodinaIzlaska,
                        film.ProsecnaOcena
                    }).ToList()
                }).ToList();

                var totalResults = rezultat.Count;
               
                var pageResults = rezultat.Skip(skip).Take(size).ToList();

                return Ok(new
                {
                    Page = page,
                    PageSize = size,
                    TotalResults = totalResults,
                    RecommendedMovies = pageResults
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška na serveru: {ex.Message}");
                return StatusCode(500, new { Message = $"Greška na serveru: {ex.Message}" });
            }
        }
    }
}
