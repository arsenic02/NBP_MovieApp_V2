using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {

        private readonly IGraphClient _client;

        public PersonController(IGraphClient client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePerson([FromBody] Person person)
        {
            if (person == null)
            {
                return BadRequest("Podaci o osobi nisu prosleđeni.");
            }

            await _client.Cypher
                .Create("(p:Person {id: apoc.create.uuid(), Ime: $Ime, Prezime: $Prezime, GodinaRodjenja: $GodinaRodjenja})")
                .WithParams(new
                {
                    Ime = person.Ime,
                    Prezime = person.Prezime,
                    GodinaRodjenja = person.GodinaRodjenja
                })
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Osoba uspešno dodata!" });
        }

        [HttpPut]
        public async Task<IActionResult> UpdatePerson([FromBody] Person personUpdate)
        {
            if (personUpdate == null || string.IsNullOrEmpty(personUpdate.id))
            {
                return BadRequest("ID osobe je obavezan za ažuriranje.");
            }
          
            var query = _client.Cypher
                .Match("(person:Person)")
                .Where("person.id = $id")
                .WithParam("id", personUpdate.id);

            if (!string.IsNullOrEmpty(personUpdate.Ime))
            {
                query = query.Set("person.Ime = $Ime").WithParam("Ime", personUpdate.Ime);
            }

            if (!string.IsNullOrEmpty(personUpdate.Prezime))
            {
                query = query.Set("person.Prezime = $Prezime").WithParam("Prezime", personUpdate.Prezime);
            }

            if (personUpdate.GodinaRodjenja > 0)
            {
                query = query.Set("person.GodinaRodjenja = $GodinaRodjenja").WithParam("GodinaRodjenja", personUpdate.GodinaRodjenja);
            }

            if (query == null)
            {
                return BadRequest("Nijedan atribut za ažuriranje nije prosleđen.");
            }

            await query.ExecuteWithoutResultsAsync();

            return Ok("Osoba je uspešno ažurirana.");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPerson(string id)
        {
            var persons = await _client.Cypher
                .Match("(p:Person)")
                .Where((Person p) => p.id == id)
                .Return(p => p.As<Person>())
                //.FirstOrDefaultAsync();
                .ResultsAsync;

            var person = persons.FirstOrDefault();

            if (person == null)
            {
                return NotFound("Osoba nije pronađena.");
            }

            return Ok(person);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePerson(string id)
        {
            await _client.Cypher
                .Match("(p:Person)")
                .Where((Person p) => p.id == id)
                .Delete("p")
                .ExecuteWithoutResultsAsync();

            return Ok("Osoba je uspešno obrisana.");
        }

        [HttpGet("uloge/{id}")]
        public async Task<IActionResult> GetUloge(string id)
        {
            var result = await _client.Cypher
                .Match("(p:Person)-[a:GLUMI_U]->(m:Movie)")
                .Where("p.id = $id")
                .WithParam("id", id)
                .Return(m => m.As<Movie>())
                .ResultsAsync;

            var filmovi = result.ToList();
            if (filmovi.Count == 0)
            {
                return NotFound(new { Message = $"Nije pronadjem ni jedan film" });
            }

            return Ok(filmovi);
        }
    }
}
