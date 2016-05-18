using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BOP
{
    // 负责发送和接受http请求
    static class MAGClient
    {
        static internal string CalcHistogram(string expr)
        {
            string uriStr = Domain + "calchistogram?expr=" + expr + "&count=1000000" + Key;

            bool isSuccess;
            string returnVal = string.Empty;
            do
            {
                try
                {
                    //var start = Environment.TickCount;
                    var response = client.GetAsync(uriStr).Result;
                    isSuccess = true;
                    //var finish = Environment.TickCount;
                    //Console.WriteLine("http responds in {0} ms", finish - start);
                    if (response.IsSuccessStatusCode)
                    {
                        returnVal = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        returnVal = "{\"entities\":[]}";
                    }
                }
                catch (Exception)
                {
                    isSuccess = false;
                }

            } while (!isSuccess);
            return returnVal;
        }

        static internal string Evaluate(string expr)
        {
            string uriStr = Domain + "evaluate?expr=" + expr + "&count=1000000" + Key;

            bool isSuccess;
            string retVal = string.Empty;

            do
            {
                try
                {
                    //var start = Environment.TickCount;
                    var response = client.GetAsync(uriStr).Result;
                    isSuccess = true;
                    //var finish = Environment.TickCount;
                    //Console.WriteLine("http responds in {0} ms", finish - start);
                    if (response.IsSuccessStatusCode)
                    {
                        retVal = response.Content.ReadAsStringAsync().Result;
                    }
                    else
                    {
                        retVal = "{\"entities\":[]}";
                    }
                }
                catch (Exception)
                {
                    isSuccess = false;
                }
            } while (!isSuccess);
            return retVal;
        }

        static string Domain = @"https://oxfordhk.azure-api.net/academic/v1.0/";

        static string Key = @"&subscription-key=f7cc29509a8443c5b3a5e56b0e38b5a6";

        internal static int MaxExprLen = 1800;

        static private HttpClient client = new HttpClient();
    }
}
