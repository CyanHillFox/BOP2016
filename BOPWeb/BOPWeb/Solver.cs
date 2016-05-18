using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Collections.Concurrent;

namespace BOP
{
    class Solver
    {
        internal Solver()
        {
            helper = new MAGHelper();

            au_au = new Au_Au(helper);
            au_id = new Au_Id(helper);
            id_id = new Id_Id(helper);
        }

        internal string Solve(Int64 id1, Int64 id2)
        {
            // parse request.

            var auInfo = helper.CombinedAuthTest(id1, id2);
            var au1Info = auInfo.Item1;
            var au2Info = auInfo.Item2;

            ConcurrentBag<Int64[]> paths;

            if (au1Info.Item1)
            {
                if (au2Info.Item1)
                {
                    // both ID are AuId.
                    paths = au_au.AuToAuPaths(au1Info.Item2, au2Info.Item2, au1Info.Item3);
                }
                else
                {
                    Paper p2 = au2Info.Item3[0];
                    paths = au_id.AuToIdPath(au1Info.Item2, au1Info.Item3, id2, p2);
                }
            }
            else
            {
                if (au2Info.Item1)
                {
                    Paper p2 = au1Info.Item3[0];
                    paths = au_id.IdToAuIdPath(id1, au2Info.Item2, au2Info.Item3, p2);
                }
                else
                {
                    Paper p1 = au1Info.Item3[0];
                    Paper p2 = au2Info.Item3[0];
                    paths = id_id.IdToIdPath(id1, id2, p1, p2);
                }
            }

            IList<Int64[]> pathsList = new List<Int64[]>(paths.Count);
            while (!paths.IsEmpty)
            {
                Int64[] path;
                if (paths.TryTake(out path))
                {
                    pathsList.Add(path);
                }
            }

            string result = Newtonsoft.Json.JsonConvert.SerializeObject(pathsList);
            /*
            using (FileStream file = new FileStream(@"C:\Users\Elecky\Documents\Result.txt", FileMode.Create))
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(result);
            }*/

            return result;
        }

        private string serial(IList<Int64[]> paths)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach (Int64[] path in paths)
            {
                sb.Append('[');
                var e = path.GetEnumerator();
                e.MoveNext();
                sb.Append(e.Current);
                while (e.MoveNext())
                {
                    sb.Append(',');
                    sb.Append(e.Current);
                }
                sb.Append(']');
            }
            sb.Append(']');
            return sb.ToString();
        }

        private MAGHelper helper;

        private Au_Au au_au;

        private Au_Id au_id;

        private Id_Id id_id;
    }
}
