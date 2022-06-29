using Microsoft.AspNetCore.Mvc;
using CacheService.Models;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CacheService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        // GET: api/<ValuesController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id?}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ValuesController>
        
        public class TelegramRequest
        {

            public string node { get; set; } = "0000";
            public string? type { get; set; } = "CMD";
            public string? addr1 { get; set; }
            public string? addr2 { get; set; }
            public int id { get; set; } = 0;
            public string? barcode { get; set; }
            public string? reserved { get; set; }

        }
        [HttpPost]
        public IActionResult Post([FromBody] TelegramRequest value)
        {

            return Ok(new Telegram54()
                .Node(value.node)
                .Type(value.type)
                .Addr1(value.addr1)
                .Addr2(value.addr2)
                .SequenceNo(value.id)
                .Barcode(value.barcode)
                .Build().GetString());
            
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
