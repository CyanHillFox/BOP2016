using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace BOPWeb
{
    public class FinalController : ApiController
    {
        // GET api/<controller>
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<controller>/5
        public string Get(int id)
        {
            return "value";
        }

        // GET api/<controller>/5
        public HttpResponseMessage Get(string id1, string id2)
        {
            BOP.Solver solver = new BOP.Solver();
            try
            {
                string result = solver.Solve(Int64.Parse(id1), Int64.Parse(id2));
                return new HttpResponseMessage()
                {
                    Content = new StringContent(result, System.Text.Encoding.UTF8, "application/json")
                };
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage()
                { Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json") };
            }
            finally
            {
                GC.Collect(0);  //被提醒说2代GC可能导致阻塞，虽然我也不知道怎么操作比较好，不过先这样吧
            }
        }

        // POST api/<controller>
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        public void Delete(int id)
        {
        }
    }
}
