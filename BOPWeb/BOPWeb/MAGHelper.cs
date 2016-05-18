using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;

namespace BOP
{
    // 提供各种的查询方式，通过调用MAGClient来获得json结果，然后解析得.net类型对象。
    class MAGHelper
    {
        // 通过ID获得作者的信息
        // deprecated
        internal Author AuthorByID(Int64 auId)
        {
            string expr = Queries.AuthByAuIdExpr(auId);
            string result = MAGClient.Evaluate(expr);

            JObject jo = JObject.Parse(result);
            var papers = jo["entities"].Children();

            HashSet<Int64> paperSet = new HashSet<long>();
            HashSet<Int64> affSet = new HashSet<long>();

            foreach (JToken paper in papers)
            {
                Int64 id = (Int64)paper["Id"];
                paperSet.Add(id);
                var auths = paper["AA"].Children();
                foreach (JToken auth in auths)
                {
                    Int64 _auId = (Int64)auth["AuId"];
                    if (auId == _auId)
                    {
                        JToken affToken = auth["AfId"];
                        if (affToken != null)
                        {
                            affSet.Add((Int64)affToken);
                        }
                    }
                }
            }

            return new Author(auId, paperSet, affSet);
        }

        internal Paper PaperById(Int64 id)
        {
            string expr = Queries.LinkAttrib(Queries.PaperByIdCond(id));
            var results = PapersByExpr(expr);

            if (results.Count == 0)
            {
                return null;
            }
            else
            {
                return results[0];
            }
        }

        internal List<Paper> PapersByExpr(string expr)
        {
            string result = MAGClient.Evaluate(expr);

            JObject jo = JObject.Parse(result);
            var papers = jo["entities"].Children();

            List<Paper> paperList = new List<Paper>();

            foreach (JToken paper in papers)
            {
                paperList.Add(ParsePaper(paper));
            }

            return paperList;
        }

        /// <summary>
        /// 测试auId是否确实是AuId，同时顺手查询其作者信息以及所写文章信息
        /// </summary>
        /// <param name="auId">要查询的Id</param>
        /// <returns>三元素的Tuple
        /// 1: 是否是AuId
        /// 2: 作者信息, 为 null 当auId不是AuId
        /// 3: 作者所写的文章, 为 null 当auId不是AuId</returns>
        internal Tuple<Tuple<bool, Author, List<Paper>>, Tuple<bool, Author, List<Paper>>>
            CombinedAuthTest(Int64 auId1, Int64 auId2)
        {
            string expr = Queries.LinkAttrib(Queries.Or(new string[]
                            { Queries.PaperByAuIdCond(auId1), Queries.PaperByAuIdCond(auId2),
                              Queries.PaperByIdCond(auId1), Queries.PaperByIdCond(auId2) }),
                            true, true, true, true, true, true, true);
            string responce = MAGClient.Evaluate(expr);

            JObject jo = JObject.Parse(responce);

            JToken entities = jo["entities"];

            List<Paper> papers1 = new List<Paper>();
            List<Paper> papers2 = new List<Paper>();
            HashSet<Int64> ids1 = new HashSet<long>();
            HashSet<Int64> ids2 = new HashSet<long>();
            HashSet<Int64> afIds1 = new HashSet<long>();
            HashSet<Int64> afIds2 = new HashSet<long>();
            Paper p1 = null, p2 = null;

            foreach (JToken pToken in entities.Children())
            {
                var parsedInfo = ParsePaper(pToken);

                if (parsedInfo.AuIds.Contains(auId1))  // AuId1是这篇文章的作者
                {
                    papers1.Add(parsedInfo);
                    ids1.Add(parsedInfo.Id);
                }

                if (parsedInfo.AuIds.Contains(auId2))
                {
                    papers2.Add(parsedInfo);
                    ids2.Add(parsedInfo.Id);
                }

                // affiliations
                if (pToken["AA"] != null)
                {
                    foreach (JToken aa in pToken["AA"].Children())
                    {
                        if (aa["AuId"] != null && aa["AfId"] != null)
                        {
                            Int64 auid = (Int64)aa["AuId"];
                            Int64 afid = (Int64)aa["AfId"];
                            if (auid == auId1)
                                afIds1.Add(afid);
                            if (auid == auId2)
                                afIds2.Add(afid);
                        }
                    }
                }

                if (parsedInfo.Id == auId1)
                    p1 = parsedInfo;
                if (parsedInfo.Id == auId2)
                    p2 = parsedInfo;
            }

            bool isId1Au, isId2Au;
            if (papers1.Count != 0)
            {
                isId1Au = true;
            }
            else
            {
                isId1Au = false;
                papers1.Add(p1);
            }

            if (papers2.Count != 0)
            {
                isId2Au = true;
            }
            else
            {
                isId2Au = false;
                papers2.Add(p2);
            }

            return new Tuple<Tuple<bool, Author, List<Paper>>, Tuple<bool, Author, List<Paper>>>
                (new Tuple<bool, Author, List<Paper>>(isId1Au, new Author(auId1, ids1, afIds1), papers1),
                 new Tuple<bool, Author, List<Paper>>(isId2Au, new Author(auId2, ids2, afIds2), papers2));
        }


        internal Paper ParsePaper(JToken paper)
        {
            Int64 id = (Int64)(paper["Id"] ?? 0);

            HashSet<Int64> auths = new HashSet<long>();  // 假设：一篇文章一定有作者和引用
            if (paper["AA"] != null)
            {
                foreach (JToken auToken in paper["AA"].Children())
                {
                    Int64 _auId = (Int64)auToken["AuId"];
                    auths.Add(_auId);
                }
            }

            HashSet<Int64> refs = new HashSet<long>();
            if (paper["RId"] != null)
            {
                foreach (JToken refToken in paper["RId"].Children())
                {
                    refs.Add((Int64)refToken);
                }
            }

            Int64 CId, JId;
            CId = (paper["C"] != null ? (Int64)paper["C"]["CId"] : 0);
            JId = (paper["J"] != null ? (Int64)paper["J"]["JId"] : 0);
            int cc;
            cc = (int)(paper["CC"] ?? 0);

            HashSet<Int64> FIds = new HashSet<long>();  // 因为文章可能没有FId, 为了避免意外的
                                                        //null reference exception, 如果没有F域，FId一律为空集合
            if (paper["F"] != null)
            {
                foreach (JToken fToken in paper["F"].Children())
                {
                    FIds.Add((Int64)fToken["FId"]);
                }
            }

            return new Paper(id, auths, CId, refs, JId, FIds, cc);
        }

        internal List<Paper> PapersByOrExprs(List<string> exprs)
        {
            if (exprs.Count == 0)
                return new List<Paper>();
            else if (exprs.Count == 1)
                return PapersByExpr(exprs[0]);
            else
            {
                // 已经并行化
                List<Paper> papers = new List<Paper>();
                Parallel.ForEach(exprs, (expr) =>
                {
                    papers.AddRange(PapersByExpr(expr));
                }
                );
                return papers;
            }
        }

        internal HashSet<Int64> IdsRefOther(HashSet<Int64> candidates, Int64 id2, int cc)
        {
            if (candidates.Count == 0 || cc == 0)
                return new HashSet<long>();

            List<string> rrConds = new List<string>();
            foreach (Int64 rrId in candidates)
            {
                rrConds.Add(Queries.PaperByIdCond(rrId));
            }

            string refId2Cond = Queries.PaperRefsOtherCond(id2);
            var condGp = Queries.Group(rrConds, MAGClient.MaxExprLen - refId2Cond.Length - 6, 5);

            if ((condGp.Count - 1) * 2500 < cc)
            {
                HashSet<Int64> result = new HashSet<long>();
                Parallel.ForEach(condGp, (gp) =>
                {
                    string expr = Queries.LinkAttrib(Queries.And(new string[] { Queries.Or(gp), refId2Cond }),
                                                    false, false, false, false, false, false, false);
                    string responce = MAGClient.Evaluate(expr);

                    result.UnionWith(ReadIds(responce));
                });
                return result;
            }
            else
            {
                string cond = Queries.PaperRefsOtherCond(id2);
                string expr = Queries.LinkAttrib(cond, false, false, false, false, false, false, false);
                string responce = MAGClient.Evaluate(expr);

                candidates.IntersectWith(ReadIds(responce));
                return candidates;
            }
        }

        internal HashSet<Int64> ReadIds(string json)
        {
            HashSet<Int64> ids = new HashSet<long>();

            JsonTextReader reader = new JsonTextReader(new StringReader(json));
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && (string)reader.Value == "Id")
                {
                    if (reader.Read())
                    {
                        try
                        {
                            ids.Add((Int64)reader.Value);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            return ids;
        }
    }
}
