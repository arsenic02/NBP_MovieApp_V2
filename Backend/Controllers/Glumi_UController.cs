using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System.Linq;
using System.Threading.Tasks;


    [ApiController]
    [Route("[controller]")]
    public class Glumi_UController : ControllerBase
    {
        private readonly IGraphClient _client;

        public Glumi_UController(IGraphClient client)
        {
            _client = client;
        }

        [HttpPost]
        public async Task<IActionResult> AddGlumiU([FromBody] Glumi_U glumiU)
        {
            var existingRelationship = await _client.Cypher
                .Match("(o:Person)-[r:GLUMI_U]->(f:Movie)")
                .Where("o.id = $OsobaID AND f.id = $FilmID")
                .WithParams(new { OsobaID = glumiU.OsobaID, FilmID = glumiU.FilmID })
                .Return(r => r.As<Glumi_U>())
                .ResultsAsync;

            if (existingRelationship.Any())
            {
                return Conflict(new { Message = "Relationship already exists!" });
            }

            var result = await _client.Cypher
                .Match("(o:Person)", "(m:Movie)")
                .Where("o.id = $OsobaID AND m.id = $FilmID")
                .Create("(o)-[g:GLUMI_U {id: apoc.create.uuid(), imeLika: $imeLika}]->(m)")
                .WithParams(new
                {
                    OsobaID = glumiU.OsobaID,
                    FilmID = glumiU.FilmID,
                    imeLika = glumiU.imeLika
                })
                .Return(g => g.As<Glumi_U>())
                .ResultsAsync;

            if (!result.Any())
            {
                return Conflict(new { Message = "ERROR exists!" });
            }

            return Ok(new { Message = "Uspesno dodata veza GLUMI_U"+result });
        }

        [HttpDelete("del-by-id/{id}")]
        public async Task<IActionResult> DeleteGlumiU(string id)
        {
            await _client.Cypher
                .Match("(o:Person)-[r:GLUMI_U]->(f:Movie)")
                .Where("r.id = $id")
                .WithParam("id", id)
                .Delete("r")
                .ExecuteWithoutResultsAsync();

            return Ok(new { Message = "Relationship successfully deleted!" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGlumiU(string id)
        {
            var result = await _client.Cypher
                .Match("(o:Person)-[r:GLUMI_U]->(f:Movie)")
                .Where("r.id = $id")
                .WithParam("id", id)
                 .Return((r, o, f) => new
                    {
                        RelationshipId = r.As<Glumi_U>().id,
                        imeLika = r.As<Glumi_U>().imeLika,
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