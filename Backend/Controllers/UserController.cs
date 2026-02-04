using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

 [ApiController]
    [Route("[controller]")]
public class UserController:ControllerBase{
        private readonly IGraphClient _client;
            private readonly IDatabase _redisClient;
        public UserController(IGraphClient client,IDatabase redisClient)
        {
            _client = client;
             _redisClient = redisClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest("Podaci o korisniku nisu prosleđeni.");
            }
            var result = await _client.Cypher
                .Create("(p:User {id: apoc.create.uuid(), KorisnickoIme: $KorisnickoIme, Mejl: $Mejl})")
                .WithParams(new
                {
                    KorisnickoIme = user.KorisnickoIme,
                    Mejl = user.Mejl
                })
                .Return<int>("COUNT(p)")
                .ResultsAsync; 
            
            int count = result.FirstOrDefault();

            if (count > 0)
            {

                return Ok(new { Message = "Korisnik je uspešno dodat!" });
            }
            else
            {
                return StatusCode(500, new { Message = "Greška prilikom kreiranja korisnika." });
            }
           
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] User userUpdate)
        {
            if (userUpdate == null || string.IsNullOrEmpty(userUpdate.id))
            {
                return BadRequest("ID korisnika je obavezan za ažuriranje.");
            }
          
            var query = _client.Cypher
                .Match("(user:User)")
                .Where("user.id = $id")
                .WithParam("id", userUpdate.id);

            if (!string.IsNullOrEmpty(userUpdate.Mejl))
            {
                query = query.Set("user.Mejl = $Mejl").WithParam("Mejl", userUpdate.Mejl);
            }

            if (query == null)
            {
                return BadRequest("Nijedan atribut za ažuriranje nije prosleđen.");
            }

            await query.ExecuteWithoutResultsAsync();

            return Ok("Osoba je uspešno ažurirana.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var users = await _client.Cypher
                .Match("(u:User)")
                .Where((User u) => u.id == id)
                .Return(u => u.As<User>())
                .ResultsAsync;

            var user = users.FirstOrDefault();

            if (user == null)
            {
                return NotFound("Korisnik nije pronađen.");
            }

            return Ok(user);
        }
        
        [HttpGet("by-username/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var users = await _client.Cypher
                .Match("(u:User)")
                .Where((User u) => u.KorisnickoIme == username)
                .Return(u => u.As<User>())
                .ResultsAsync;

            var user = users.FirstOrDefault();

            if (user == null)
            {
                return NotFound("Korisnik nije pronađen.");
            }
            string key = $"user:{user.id}:streams";

            // get svih streamova koje korisnik prati
            var streams = _redisClient.HashGetAll(key);
            List<string> messages = new List<string>();
            foreach (var stream in streams){
                StreamEntry[] result = _redisClient.StreamRange(stream.Name.ToString(), stream.Value, "+", count: 100);
                 foreach (StreamEntry entry in result)
                    {
                        if(entry.Id == stream.Value)
                            continue;
                        foreach (var value in entry.Values)
                        {                        
                            messages.Add(value.Value.ToString());
                        }

                     
                    }
                       string newestId = result[result.Length - 1].Id;
                        _redisClient.HashSet(key, stream.Name, newestId); 
            }

            return Ok(new {
                Korisnik = user,
                Poruke = messages
                });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _client.Cypher
                .Match("(u:User)")
                .Where((User u) => u.id == id)
                .Delete("u")
                .ExecuteWithoutResultsAsync();

            return Ok("Osoba je uspešno obrisana.");
        }

        [HttpGet("liked/{id}")]
        public async Task<IActionResult> GetSvidjanja(string id)
        {
            var result = await _client.Cypher
                .Match("(u:User)-[r:SVIDJA_MU_SE]->(f:Movie)")
                .Where("u.id = $id")
                .WithParam("id", id)
                 .Return((r, u, f) => new
                    {
                        RelationshipId = r.As<Svidja_Mu_Se>().id,
                        Javna = r.As<Svidja_Mu_Se>().javna,
                        KorisnikID = u.As<User>().id,
                        FilmID = f.As<Movie>().id
                    })
                .ResultsAsync;

            var relationship = result.FirstOrDefault();
            result = result.ToList();
            if (relationship == null)
            {
                return NotFound(new { Message = "Relationship not found!" });
            }
            return Ok(result);
        }   
        [HttpGet("follow/{id}")]
        public async Task<IActionResult> GetSveListe(string id)
        {
            var result = await _client.Cypher
                .Match("(u:User)-[r:PRATI]->(f:User)")
                .Where("u.id = $id")
                .WithParam("id", id)
                 .Return((r, u, f) => new
                    {
                        RelationshipId = r.As<Prati_Listu>().id,
                        IzvorniKorisnikID = u.As<User>().id,
                        OdredisniKorisnikID = f.As<User>().id
                    })
                .ResultsAsync;

            var relationship = result.FirstOrDefault();
            result = result.ToList();
            if (relationship == null)
            {
                return NotFound(new { Message = "Relationship not found!" });
            }
            return Ok(result);
        }  
        
}