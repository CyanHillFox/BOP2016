using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;

namespace BOP
{
    class Id_Id
    {
        internal Id_Id(MAGHelper _helper)
        {
            helper = _helper;
        }

        internal ConcurrentBag<Int64[]> IdToIdPath(Int64 id1, Int64 id2, Paper paper1, Paper paper2)
        {
            ConcurrentBag<Int64[]> paths = new ConcurrentBag<long[]>();

            /*/ 查出id1,id2的文章的信息
            string cond = Queries.Or(new string[] { Queries.PaperByIdCond(id1),
                Queries.PaperByIdCond(id2) });
            string expr = Queries.LinkAttrib(cond, true, true, true, true, true, false, true);

            //Paper paper1 = null, paper2 = null;

            foreach (Paper p in helper.PapersByExpr(expr))
            {
                if (p.Id == id1)
                    paper1 = p;
                else if (p.Id == id2)
                    paper2 = p;
            }*/

            // 1-hop
            if (paper1.RIds.Contains(id2))
                paths.Add(new Int64[] { id1, id2 });

            // 2-hop except [Id,Id,Id]
            IdId2Hop(paper1, paper2, paths);

            Task<IList<Paper>> rPsTask = Task.Run(() => refedPapers(paper1));   // 新线程查询id1引用的文章的信息
            // [Id,Id,Id] and [Id,Id,any except Id,Id]
            Task forTask = rPsTask.ContinueWith((rpstask) => forward(paper1, paper2, rpstask, paths));

            // 3-hop
            if (paper2.CC != 0)
            {
                int th1 = 5000;
                if (paper2.CC < th1)
                {
                    // 当前线程计算
                    backCombo(paper1, paper2, paths, rPsTask);

                    // 等待线程结束
                    forTask.Wait();
                }
                else
                {
                    // 新线程计算四种情况
                    Task cTask = Task.Run(() => idCIdIdId(paper1.Id, paper1.CId.CId, paper2.Id, paths));
                    Task jTask = Task.Run(() => idJIdIdId(paper1.Id, paper1.JId.JId, paper2.Id, paths));
                    Task fTask = Task.Run(() => idfIdIdId(paper1, paper2.Id, paths));
                    Task auTask = Task.Run(() => idAuIdIdId(paper1, paper2.Id, paths));

                    // 当前线程计算[Id,Id,Id,Id]
                    //[Id,Id,Id,Id]
                    HashSet<Int64> rrIds = new HashSet<long>();

                    IList<Paper> rPs = rPsTask.Result;
                    foreach (Paper rp in rPs)
                    {
                        foreach (Int64 rrp in rp.RIds)
                        {
                            rrIds.Add(rrp);
                        }
                    }

                    var filtered = helper.IdsRefOther(rrIds, paper2.Id, paper2.CC);
                    foreach (Paper rp in rPs)
                    {
                        foreach (Int64 rrp in rp.RIds)
                        {
                            if (filtered.Contains(rrp))
                            {
                                paths.Add(new Int64[] { paper1.Id, rp.Id, rrp, paper2.Id });
                            }
                        }
                    }

                    // 等待线程结束
                    Task.WaitAll(forTask, cTask, jTask, fTask, auTask);
                }
            }

            return paths;
        }

        private IList<Paper> refedPapers(Paper paper1)
        {
            // 查找被id1引用的文章的信息
            List<Paper> rPas = null;
            if (paper1.RIds.Count != 0)
            {
                List<string> cons = new List<string>();
                foreach (Int64 rp in paper1.RIds)
                {
                    cons.Add(Queries.PaperByIdCond(rp));
                }

                var condGps = Queries.Group(cons, MAGClient.MaxExprLen, 5);
                List<string> exprs = new List<string>();

                foreach (var gp in condGps)
                {
                    exprs.Add(Queries.LinkAttrib(Queries.Or(gp), true, true, true, true, true, false, false));
                }

                rPas = helper.PapersByOrExprs(exprs);
            }
            else
                rPas = new List<Paper>();

            return rPas;
        }

        // all 2-hop paths between Id and Id except via Id.
        private void IdId2Hop(Paper p1, Paper p2, ConcurrentBag<Int64[]> paths)
        {
            //[Id,CId,Id]
            if (p1.CId == p2.CId)
                paths.Add(new Int64[] { p1.Id, p1.CId.CId, p2.Id });
            //[Id,FId,Id]
            foreach (Int64 fId in p1.FIds)
            {
                if (p2.FIds.Contains(fId))
                    paths.Add(new Int64[] { p1.Id, fId, p2.Id });
            }
            //[Id,JId,Id]
            if (p1.JId == p2.JId)
                paths.Add(new Int64[] { p1.Id, p1.JId.JId, p2.Id });
            //[Id,AuId,Id]
            foreach (Int64 auId in p1.AuIds)
            {
                if (p2.AuIds.Contains(auId))
                {
                    paths.Add(new Int64[] { p1.Id, auId, p2.Id });
                }
            }
        }

        private void forward(Paper p1, Paper p2, Task<IList<Paper>> rPsTask, ConcurrentBag<Int64[]> paths)
        {
            Int64 id1 = p1.Id, id2 = p2.Id;
            IList<Paper> rPs = rPsTask.Result;

            //[Id,Id,Id]
            foreach (Paper rp in rPs)
            {
                if (rp.RIds.Contains(id2))
                    paths.Add(new Int64[] { id1, rp.Id, id2 });
            }

            //[Id,Id,CId/FId/JId/AuId,Id]
            ConcurrentBag<Int64[]> subPath1 = new ConcurrentBag<long[]>();
            foreach (Paper rp in rPs)
            {
                IdId2Hop(rp, p2, subPath1);
            }
            foreach (Int64[] path in subPath1)
            {
                paths.Add(new Int64[] { p1.Id, path[0], path[1], path[2] });
            }
        }

        private void backCombo(Paper p1, Paper p2, ConcurrentBag<Int64[]> paths, Task<IList<Paper>> rPsTask)
        {
            HashSet<Int64> idRefs2 = new HashSet<long>();
            string cond = Queries.PaperRefsOtherCond(p2.Id);
            string expr = Queries.LinkAttrib(cond, false, true, true, true,
                true, false, false);
            IList<Paper> paRefs2 = helper.PapersByExpr(expr);
            foreach (Paper p in paRefs2)
                idRefs2.Add(p.Id);
            //TODO: 并行
            //[Id,CId/FId/JId/AuId,Id,Id]
            ConcurrentBag<Int64[]> subPath2 = new ConcurrentBag<long[]>();
            foreach (Paper rp2 in paRefs2)
            {
                IdId2Hop(p1, rp2, subPath2);
            }
            foreach (Int64[] path in subPath2)
            {
                paths.Add(new Int64[] { path[0], path[1], path[2], p2.Id });
            }

            //[Id,Id,Id,Id]
            IList<Paper> rPs = rPsTask.Result;  // 到这里再wait计算rPs的线程
            foreach (Paper rp in rPs)
            {
                foreach (Int64 rrp in rp.RIds)
                {
                    if (idRefs2.Contains(rrp))
                        paths.Add(new Int64[] { p1.Id, rp.Id, rrp, p2.Id });
                }
            }
        }

        private void idCIdIdId(Int64 id1, Int64 cId, Int64 id2, ConcurrentBag<Int64[]> paths)
        {
            if (cId != 0)
            {
                string cond = Queries.And(new string[] { "Composite(C.CId=" + cId.ToString() + ")",
                            Queries.PaperRefsOtherCond(id2)});
                string expr = Queries.LinkAttrib(cond, false, false, false, false, false, false, false);
                string responce = MAGClient.Evaluate(expr);
                foreach (Int64 id in helper.ReadIds(responce))
                {
                    paths.Add(new Int64[] { id1, cId, id, id2 });
                }
                /*
                foreach (JToken pt in JObject.Parse(responce)["entities"].Children())
                {
                    Int64 id = (Int64)pt["Id"];
                    paths.Add(new Int64[] { id1, cId, id, id2 });
                }*/
            }
        }

        private void idJIdIdId(Int64 id1, Int64 jId, Int64 id2, ConcurrentBag<Int64[]> paths)
        {
            if (jId != 0)
            {
                string cond = Queries.And(new string[] { "Composite(J.JId=" + jId.ToString() + ")",
                            Queries.PaperRefsOtherCond(id2)});
                string expr = Queries.LinkAttrib(cond, false, false, false, false, false, false, false);
                string responce = MAGClient.Evaluate(expr);
                foreach (Int64 id in helper.ReadIds(responce))
                {
                    paths.Add(new Int64[] { id1, jId, id, id2 });
                }
                /*
                foreach (JToken pt in JObject.Parse(responce)["entities"].Children())
                {
                    Int64 id = (Int64)pt["Id"];
                    paths.Add(new Int64[] { id1, jId, id, id2 });
                }*/
            }
        }

        private void idfIdIdId(Paper p1, Int64 id2, ConcurrentBag<Int64[]> paths)
        {
            if (p1.FIds.Count != 0)
            {
                Parallel.ForEach(p1.FIds, (fId) =>
                {
                    string cond = Queries.And(new string[]
                        { Queries.PaperRefsOtherCond(id2), "Composite(F.FId=" + fId.ToString() + ")" });
                    string expr = Queries.LinkAttrib(cond, false, false, false, false, false, false, false);
                    string responce = MAGClient.Evaluate(expr);
                    foreach (Int64 id in helper.ReadIds(responce))
                    {
                        paths.Add(new Int64[] { p1.Id, fId, id, id2 });
                    }
                }
                );
            }
        }

        private void idAuIdIdId(Paper p1, Int64 id2, ConcurrentBag<Int64[]> paths)
        {
            if (p1.AuIds.Count != 0)
            {
                List<string> conds = new List<string>();
                foreach (Int64 auid in p1.AuIds)
                {
                    conds.Add("Composite(AA.AuId=" + auid.ToString() + ")");
                }

                string refCond = Queries.PaperRefsOtherCond(id2);
                var gpConds = Queries.Group(conds, MAGClient.MaxExprLen - refCond.Length - 6, 5);
                if (gpConds.Count == 1)
                {
                    string expr = Queries.LinkAttrib(Queries.And(new string[] { Queries.Or(gpConds[0]), refCond }),
                                    false, false, false, false, true, false, false);
                    string responce = MAGClient.Evaluate(expr);

                    foreach (JToken pt in JObject.Parse(responce)["entities"].Children())
                    {
                        Int64 id = (Int64)pt["Id"];
                        if (pt["AA"] != null)
                        {
                            foreach (JToken at in pt["AA"].Children())
                            {
                                Int64 auId = (Int64)at["AuId"];
                                if (p1.AuIds.Contains(auId))
                                    paths.Add(new Int64[] { p1.Id, auId, id, id2 });
                            }
                        }
                    }
                }
                else
                {
                    HashSet<Int64> done = new HashSet<long>();  // 已经处理过的文章
                    foreach (IList<string> condGp in gpConds)
                    {
                        string expr = Queries.LinkAttrib(Queries.And(new string[] { Queries.Or(condGp), refCond }),
                            false, false, false, false, true, false, false);
                        string responce = MAGClient.Evaluate(expr);

                        foreach (JToken pt in JObject.Parse(responce)["entities"].Children())
                        {
                            Int64 id = (Int64)pt["Id"];
                            if (!done.Contains(id))
                            {
                                done.Add(id);
                                if (pt["AA"] != null)
                                {
                                    foreach (JToken ft in pt["AA"].Children())
                                    {
                                        Int64 auId = (Int64)ft["AuId"];
                                        if (p1.AuIds.Contains(auId))
                                            paths.Add(new Int64[] { p1.Id, auId, id, id2 });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        MAGHelper helper;
    }
}
