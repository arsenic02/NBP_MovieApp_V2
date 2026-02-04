using HR;
using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace HR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilmController : ControllerBase
    {

        private readonly IGraphClient _client;

        public FilmController(IGraphClient client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<IActionResult> AddFilm([FromBody] Movie movie)
        {           
            string unformatedName = movie.Naslov + movie.GodinaIzlaska.ToString();
            string formattedName = string.Concat(unformatedName
           .Split(' ')  
           .Where(s => !string.IsNullOrEmpty(s))  
           .Select((word, index) =>
               index == 0
               ? Char.ToUpper(word[0]) + word.Substring(1).ToLower()  
               : Char.ToUpper(word[0]) + word.Substring(1).ToLower())  
           );

            Console.WriteLine($"Formatted Name: {formattedName}");

            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "MovieCover");
            var searchPattern = formattedName + ".jpg";//.*
            string filePath = Path.Combine(directoryPath, searchPattern);

            Console.WriteLine($"filePath: {filePath}");
      
            var files = Directory.GetFiles(directoryPath, searchPattern);
            if (files.Length <= 0)
            {
                return NotFound(new { Message = "Image not found." });
            }

            var existingFilm = await _client.Cypher
                .Match("(m:Movie)")
                .Where("m.Naslov = $Naslov")
                .WithParam("Naslov", movie.Naslov)
                .Return(m => m.As<Movie>())
                .ResultsAsync;

            if (existingFilm.Any())
            {
                return Conflict(new { Message = "Film with this title already exists!" });
            }

            await _client.Cypher
                .Create("(m:Movie {id: apoc.create.uuid(), Naslov: $Naslov, GodinaIzlaska: $GodinaIzlaska, Zanr: $Zanr, ProsecnaOcena: $ProsecnaOcena, Kategorija: $Kategorija})")
                .WithParams(new
                {
                    Naslov = movie.Naslov,
                    GodinaIzlaska = movie.GodinaIzlaska,
                    Zanr = movie.Zanr,
                    ProsecnaOcena = movie.ProsecnaOcena,
                    Kategorija = movie.Kategorija
                })
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Film successfully added!" });
        }

        [HttpDelete("del-by-id/{id}")]
        public async Task<IActionResult> DeleteFilm(string id)
        {
            await _client.Cypher
                .Match("(m:Movie)")
                .Where("m.id=$id")
                .WithParam("id", id)
                .DetachDelete("m")
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Film successfully deleted!" });
        }
        [HttpDelete("del-by-title/{naslov}")]
        public async Task<IActionResult> DeleteFilmPoNazivu(string naslov)
        {
            await _client.Cypher
                .Match("(m:Movie)")
                .Where("m.Naslov=$Naslov")
                .WithParam("Naslov", naslov)
                .DetachDelete("m")
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Film successfully deleted!" });
        }

        [HttpPatch("{id}/{novaOcena}")]
        public async Task<IActionResult> UpdateProsecnaOcena(string id, float novaOcena)
        {
            await _client.Cypher
                .Match("(m:Movie)")
                .Where((Movie m) => m.id == id)
                .Set("m.ProsecnaOcena = $novaOcena")
                .WithParam("novaOcena", novaOcena)
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Prosečna ocena successfully updated!" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFilm(string id)
        {
            var result = await _client.Cypher
                .Match("(m:Movie)")
                .Where((Movie m) => m.id == id)
                .Return(m => m.As<Movie>())
                .ResultsAsync;

            var film = result.FirstOrDefault();
            if (film == null)
            {
                return NotFound(new { Message = "Film not found!" });
            }

            return Ok(film);
        }

        [HttpGet("by-title/{naslov}")]
        public async Task<IActionResult> GetFilmByTitle(string naslov)
        {
            var result = await _client.Cypher
                .Match("(m:Movie)")
                .Where("m.Naslov = $naslov")
                .WithParam("naslov", naslov)
                .Return(m => m.As<Movie>())
                .ResultsAsync;

            var film = result.FirstOrDefault();
            if (film == null)
            {
                return NotFound(new { Message = $"Film sa naslovom '{naslov}' nije pronađen!" });
            }

            return Ok(film);
        }
        [HttpGet("get-image/{fileName}/{year}")]
        public IActionResult GetImage(string fileName, string year)
        {
            Console.WriteLine($"Ime:{fileName} Godina:{year}");
            var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", "MovieCover");
            var searchPattern = fileName + year + ".*";
            string unformated = fileName + year;
            string formattedName = string.Concat(unformated
            .Split(' ')  
            .Where(s => !string.IsNullOrEmpty(s))  
            .Select((word, index) =>
                index == 0
                ? Char.ToUpper(word[0]) + word.Substring(1).ToLower() 
                : Char.ToUpper(word[0]) + word.Substring(1).ToLower())  
            );
            Console.WriteLine($"Ime:{formattedName}");
            
            var files = Directory.GetFiles(directoryPath, formattedName + ".*");
           
            if (files.Length > 0)
            {
                var imagePath = files[0];
                Console.WriteLine(imagePath);

                var imageBytes = System.IO.File.ReadAllBytes(imagePath);
                var contentType = "image/jpeg"; 

                return File(imageBytes, contentType);
            }
            else
            {
                return NotFound(new { Message = "Image not found." });
            }

        }

        [HttpGet("cast/{id}")]
        public async Task<IActionResult> GetGlumce(string id)
        {
            var result = await _client.Cypher
                .Match("(p:Person)-[a:GLUMI_U]->(m:Movie)")
                .Where("m.id = $id")
                .WithParam("id", id)
                .Return(p => p.As<Person>())
                .ResultsAsync;

            var glumci = result.ToList<Person>();
            if (glumci.Count == 0)
            {
                return NotFound(new { Message = $"Film sa ID-em '{id}' nije pronađen!" });
            }

            return Ok(glumci);
        }


        [HttpGet("all")] //get filmovi sa paginacijom
        public async Task<IActionResult> GetAllFilms([FromQuery] int page = 1, [FromQuery] int size = 10)
        {
            if (page <= 0 || size <= 0)
            {
                return BadRequest(new { Message = "Page and size must be positive integers." });
            }

            var skip = (page - 1) * size;
    
            var result = await _client.Cypher
            .Match("(m:Movie)")
            .Return(m => m.As<Movie>())
            .OrderByDescending("m.GodinaIzlaska")
            .ThenBy("m.Naslov") 
            .Skip(skip)
            .Limit(size)
            .ResultsAsync;

            var totalMovies = await _client.Cypher
                .Match("(m:Movie)")
                .Return(m => m.Count())
                .ResultsAsync;

            var totalCount = totalMovies.FirstOrDefault();

            var response = new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = size,
                Movies = result
            };

            return Ok(response);
        }
        [HttpGet("bestRated/{limit}")]
        public async Task<IActionResult> GetBestRated(int limit)
        {
            var bestRated = await _client.Cypher
                 .Match("(m:Movie)")
                 .OrderByDescending("m.ProsecnaOcena") 
                 .Return(m => m.As<Movie>()) 
                 .Limit(limit) // ogranicava broj rezultata 
                 .ResultsAsync;

            if (!bestRated.Any())
            {
                return NotFound("Nema najbolje ocenjenih filmova.");
            }

            return Ok(bestRated);
        }
        [HttpGet("recommendations")]
        public async Task<IActionResult> GetMovieRecommendations([FromQuery] string korisnikID, [FromQuery] int page = 1, [FromQuery] int size = 10)
        {

            int skip = (page - 1) * size;
            var result = await _client.Cypher
                .Match("(u:User {id: $userId})-[s:SVIDJA_MU_SE]->(f:Movie)")
                .Where("s.javna = true")//dodato
                .With("f.Zanr AS zanr, COUNT(f) AS brojFilmova")
                .OrderByDescending("brojFilmova")
                .Limit(2)
                .With("COLLECT(zanr) AS topZanrovi")
                .Match("(f:Movie)<-[:GLUMI_U]-(g:Person)")
                .Where("f.Zanr IN topZanrovi")
                .With("g, COUNT(f) AS brojUloga, topZanrovi")
                .OrderByDescending("brojUloga")
                .Limit(2)
                .With("COLLECT(g) AS topGlumci, topZanrovi")
                .Match("(f:Movie)<-[:GLUMI_U]-(g:Person), (u:User {id: $userId})")
                .Where("g IN topGlumci AND f.Zanr IN topZanrovi AND NOT (u)-[:GLEDAO]->(f)")
                .WithParam("userId", korisnikID)
                .ReturnDistinct(f => f.As<Movie>())
                .Skip(skip) // Paginacija
                .Limit(size) // Paginacija
                .ResultsAsync;

            var movies = result.ToList();

            if (!movies.Any())
            {
                return NotFound(new { Message = "Nema preporučenih filmova za ovog korisnika." });
            }

            return Ok(new
            {
                Page = page,
                PageSize = size,
                TotalResults = movies.Count,
                Movies = movies
            });
        }
        [HttpGet("autocomplete/{query}")]
        public async Task<IActionResult> Autocomplete(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return BadRequest("Upit mora imati barem 2 slova.");
            }

            var suggestions = await _client.Cypher
                .Match("(m:Movie)")
                .Where("toLower(m.Naslov) STARTS WITH toLower($query)")
                .WithParam("query", query)
                .Return(m => m.As<Movie>().Naslov)
                .Limit(5)
                .ResultsAsync;

            if (!suggestions.Any())
            {
                return NotFound("Nema Rezultata!");
            }

            return Ok(suggestions.ToList());
        }       
    }
}
