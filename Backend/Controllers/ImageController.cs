using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace HR.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : ControllerBase
    {

        private readonly IGraphClient _client;

        public ImageController(IGraphClient client)
        {
            _client = client;
        }

        private string SaveImage(IFormFile imageFile, string movieId)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new ArgumentException("Invalid image file.");
            }
           
            var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
           
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }
            
            var fileName = $"{movieId}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(imagesPath, fileName);

            try
            {
                // cuvanje fajla na disku
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                // vracanje relativne putanje za Neo4j
                return $"/images/{fileName}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred while saving the image.", ex);
            }
        }
        
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile imageFile, [FromForm] string movieId)
        {
            if (string.IsNullOrEmpty(movieId))
            {
                return BadRequest(new { Message = "Movie ID is required." });
            }

            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest(new { Message = "Valid image file is required." });
            }

            try
            {             
                var imagePath = SaveImage(imageFile, movieId);
                
                await _client.Cypher
                    .Match("(m:Movie {id: $movieId})")
                    .Set("m.SlikaURL = $imagePath")
                    .WithParams(new
                    {
                        movieId,
                        imagePath
                    })
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "Image successfully uploaded!", ImagePath = imagePath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

    }
}