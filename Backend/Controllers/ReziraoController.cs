using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System.Linq;
using System.Threading.Tasks;


    [ApiController]
    [Route("[controller]")]
    public class ReziraoController : ControllerBase
    {
        private readonly IGraphClient _client;

        public ReziraoController(IGraphClient client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<IActionResult> AddRezirao([FromBody] Rezirao rezirao)
        {
            var existingRelationship = await _client.Cypher
                .Match("(o:Person)-[r:REZIRAO]->(f:Movie)")
                .Where("o.id = $OsobaID AND f.id = $FilmID")
                .WithParams(new { OsobaID = rezirao.OsobaID, FilmID = rezirao.FilmID })
                .Return(r => r.As<Rezirao>())
                .ResultsAsync;

            if (existingRelationship.Any())
            {
                return Conflict(new { Message = "Relationship already exists!" });
            }

            var result = await _client.Cypher
                .Match("(o:Person)", "(m:Movie)")
                .Where("o.id = $OsobaID AND m.id = $FilmID")
                .Create("(o)-[g:REZIRAO {id: apoc.create.uuid()}]->(m)")
                .WithParams(new
                {
                    OsobaID = rezirao.OsobaID,
                    FilmID = rezirao.FilmID,
                })
                .Return(g => g.As<Glumi_U>())
                .ResultsAsync;

            if (!result.Any())
            {
                return Conflict(new { Message = "ERROR exists!" });
            }

            return Ok(new { Message = "Uspesno dodata veza Rezirao" });
        }

        [HttpDelete("del-by-id/{id}")]
        public async Task<IActionResult> DeleteRezirao(string id)
        {
            await _client.Cypher
                .Match("(o:Person)-[r:REZIRAO]->(f:Movie)")
                .Where("r.id = $id")
                .WithParam("id", id)
                .Delete("r")
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Relationship successfully deleted!" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRezirao(string id)
        {
            var result = await _client.Cypher
                .Match("(o:Person)-[r:REZIRAO]->(f:Movie)")
                .Where("r.id = $id")
                .WithParam("id", id)
                 .Return((r, o, f) => new
                    {
                        RelationshipId = r.As<Glumi_U>().id,      
                        OsobaID = o.As<Person>().id,
                        FilmID = f.As<Movie>().id
                    })
                .ResultsAsync;

            var relationship = result.FirstOrDefault();
            if (relationship == null)
            {
                return NotFound(new { Message = "Relationship not found!" });
            }

            return Ok(relationship);
        }   
    }