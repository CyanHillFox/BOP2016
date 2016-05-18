using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace BOP
{
    // [AuId, Id] and [Id, AuId]
    class Au_Id
    {
        internal Au_Id(MAGHelper _helper)
        {
            helper = _helper;
        }

        // [AuId, Id]
        internal ConcurrentBag<Int64[]> AuToIdPath(Author auth, IEnumerable<Paper> paperByAu1, Int64 id, Paper paper)
        {
            ConcurrentBag<Int64[]> paths = new ConcurrentBag<long[]>();

            // undirected paths

            // 查询id文章信息
            //Paper paper = helper.PaperById(id);

            // 启动新线程计算双向路径
            Task undir = Task.Run(()=> BothDir(auth, paperByAu1, id, paths, paper));

            // directed paths
            if (paper.CC != 0)
            {
                // [AuId, Id, Id]
                foreach (Paper p1 in paperByAu1)
                {
                    if (p1.RIds.Contains(id))
                    {
                        paths.Add(new Int64[] { auth.AuId, p1.Id, id });
                    }
                }

                // [AuId, Id, Id, Id]
                HashSet<Int64> candidates = new HashSet<long>();
                foreach (Paper p1 in paperByAu1)
                {
                    foreach (Int64 rp in p1.RIds)
                    {
                        candidates.Add(rp);
                    }
                }

                HashSet<Int64> refsId2 = helper.IdsRefOther(candidates, paper.Id, paper.CC);

                foreach (Paper p1 in paperByAu1)  // 循环auth写的所有文章
                {
                    foreach (Int64 rp in p1.RIds)  //  这篇文章引用的所有文章
                    {
                        if (refsId2.Contains(rp))  // 如果auth写的文章p1引用的文章rp，引用了id文章
                        {
                            paths.Add(new Int64[] { auth.AuId, p1.Id, rp, id });
                        }
                    }
                }
            }

            undir.Wait();  // 等待子线程完成

            return paths;
        }

        internal ConcurrentBag<Int64[]> IdToAuIdPath(Int64 Id, Author auth, IEnumerable<Paper> paperByAu1, Paper paper)
        {
            ConcurrentBag<Int64[]> paths = new ConcurrentBag<Int64[]>();

            // undirected paths
            //Paper paper = helper.PaperById(Id);
            Task undir = null;

            ConcurrentBag<Int64[]> subPaths = new ConcurrentBag<long[]>();
            undir = Task.Run(() => BothDir(auth, paperByAu1, Id, subPaths, paper))
                            .ContinueWith((antecent) =>
                            {
                                foreach (Int64[] path in subPaths)
                                {
                                    Array.Reverse(path);
                                    paths.Add(path);
                                }
                            });
            

            // directed paths

            //[Id,Id,AuId]
            foreach (Int64 rp in paper.RIds)
            {
                if (auth.Papers.Contains(rp))
                    paths.Add(new Int64[] { Id, rp, auth.AuId });
            }

            //[Id,Id,Id,AuId]
            id_Id_Id_AuId(paper, auth, paths);

            undir.Wait();  // wait for undirected paths.

            return paths;
        }

        private void BothDir(Author auth, IEnumerable<Paper> paperByAu1, Int64 Id, ConcurrentBag<Int64[]> paths, Paper paper)
        {
            // 1-hop, [AuId, Id]
            if (auth.Papers.Contains(Id))
                paths.Add(new long[] { auth.AuId, Id });

            // 3-hop
            // [AuId, Id, CId/JId/FId/AuId, Id]
            foreach (Paper p1 in paperByAu1)  // 循环Auth写的所有文章
            {
                // TODO: 环是否合法？
                if (p1.JId == paper.JId)  //[AuId,Id,JId,Id]
                    paths.Add(new long[] { auth.AuId, p1.Id, p1.JId.JId, paper.Id });
                if (p1.CId == paper.CId)  // [AuId, Id, CId, Id]
                    paths.Add(new long[] { auth.AuId, p1.Id, p1.CId.CId, paper.Id });

                foreach (Int64 fId in p1.FIds)  // [AuId, Id, FId, Id]
                {
                    if (paper.FIds.Contains(fId))
                        paths.Add(new long[] { auth.AuId, p1.Id, fId, paper.Id });
                }

                foreach (Int64 auId in p1.AuIds)  // [AuId, Id, AuId, Id]
                {
                    if (paper.AuIds.Contains(auId))
                        paths.Add(new long[] { auth.AuId, p1.Id, auId, paper.Id });
                }
            }

            // [AuId, AfId, AuId, Id]
            auId_AfId_AuId_Id(auth, paper, paths);
        }

        // [AuId, AfId, AuId, Id]
        // 查id文章的作者里边，曾在auId的机构发过文章的那些
        private void auId_AfId_AuId_Id(Author auth, Paper paper, ConcurrentBag<Int64[]> paths)
        {
            if (auth.Affiliations.Count == 0 || paper.AuIds.Count == 0)
                return;

            List<string> cons = new List<string>();
            foreach (Int64 aid in paper.AuIds)
            {
                foreach (Int64 afid in auth.Affiliations)
                {
                    cons.Add(Queries.PaperByAuAtCond(aid, afid));
                }
            }
            string cond = Queries.Or(cons);
            List<string> urls = new List<string>();
            if (cond.Length > MAGClient.MaxExprLen)
            {
                List<string> auConds = new List<string>();
                foreach (Int64 auId in paper.AuIds)
                {
                    auConds.Add(Queries.PaperByAuIdCond(auId));
                }

                foreach (var cGp in Queries.Group(auConds, MAGClient.MaxExprLen, 5))
                {
                    urls.Add(Queries.LinkAttrib(Queries.Or(cGp), false, false, false, false, true, true, false));
                }
            }
            else
            {
                urls.Add(Queries.LinkAttrib(cond, false, false, false, false, true, true, false));
            }

            HashSet<Tuple<Int64, Int64>> pairs = new HashSet<Tuple<long, long>>();
            // 已经并行化
            Parallel.ForEach(urls, (url) =>
            {
                string responce = MAGClient.Evaluate(url);
                foreach (JToken pT in JObject.Parse(responce)["entities"].Children())
                {
                    if (pT["AA"] != null)
                    {
                        foreach (JToken AA in pT["AA"].Children())
                        {
                            if (AA["AuId"] != null && AA["AfId"] != null)
                            {
                                if (paper.AuIds.Contains((Int64)AA["AuId"]) &&
                                    auth.Affiliations.Contains((Int64)AA["AfId"]))
                                {
                                    pairs.Add(new Tuple<long, long>((Int64)AA["AfId"], (Int64)AA["AuId"]));
                                }
                            }
                        }
                    }
                }
            });

            // now add all paths
            foreach (var pair in pairs)
            {
                paths.Add(new Int64[] { auth.AuId, pair.Item1, pair.Item2, paper.Id });
            }
        }


        private void id_Id_Id_AuId(Paper paper, Author auth, ConcurrentBag<Int64[]> paths)
        {
            if (paper.RIds.Count == 0)
                return;

            // 查询被Id文章引用的所有文章的信息
            List<string> cons = new List<string>();
            foreach (Int64 rp in paper.RIds)
            {
                cons.Add(Queries.PaperByIdCond(rp));
            }
            var condGps = Queries.Group(cons, MAGClient.MaxExprLen, 5);
            List<string> exprs = new List<string>();

            foreach (var gp in condGps)
            {
                exprs.Add(Queries.LinkAttrib(Queries.Or(gp), true, false, false, false, false, false, false));
            }

            List<Paper> rPapers = helper.PapersByOrExprs(exprs);

            foreach (Paper rp in rPapers)
            {
                foreach (Int64 rrp in rp.RIds)
                {
                    if (auth.Papers.Contains(rrp))
                        paths.Add(new Int64[] { paper.Id, rp.Id, rrp, auth.AuId });
                }
            }
        }

        MAGHelper helper;
    }
}
