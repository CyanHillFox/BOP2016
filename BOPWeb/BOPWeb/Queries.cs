using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOP
{
    // 这个类用于帮助生成一些请求字符串，以及进行筛选条件的组合
    static class Queries
    {
        /// <summary>
        /// 生成用于检查某个id是不是AuId的expr
        /// </summary>
        /// <param name="auId">作者ID</param>
        /// <returns></returns>
        static internal string AuthTestExpr(Int64 auId)
        {
            return "Composite(AA.AuId=" + auId.ToString() + ")";
        }

        /// <summary>
        /// 生成用于请求作者信息的字符串，虽然目的是请求作者信息，但是返回的是作者写的文章的信息
        /// </summary>
        /// <param name="auId">作者ID</param>
        /// <returns></returns>
        static internal string AuthByAuIdExpr(Int64 auId)
        {
            return "Composite(AA.AuId=" + auId.ToString() + ")&attributes=Id,AA.AuId,AA.AfId";
        }

        /// <summary>
        /// 生成通过文章ID请求文章信息的字符串
        /// </summary>
        /// <param name="id">文章ID</param>
        /// <returns></returns>
        static internal string PaperByIdCond(Int64 id)
        {
            return "Id=" + id.ToString();
        }

        static internal string PaperRefsOtherCond(Int64 id)
        {
            return "RId=" + id.ToString();
        }

        static internal string PaperByAuAtCond(Int64 auId, Int64 afId)
        {
            return "Composite(And(AA.AuId=" + auId.ToString() + ",AA.AfId=" + afId.ToString() + "))";
        }

        static internal string PaperByAuIdCond(Int64 auId)
        {
            return "Composite(AA.AuId=" + auId.ToString() + ")";
        }

        //辅助连接所需查询的属性的帮助函数:

        /// <summary>
        /// 给筛选条件后边，连接我们通常需要用到的文章属性
        /// </summary>
        /// <param name="condExpr">筛选条件字符串</param>
        /// <returns></returns>
        static internal string LinkAttrib(string condExpr)
        {
            return condExpr + "&attributes=Id,AA.AuId,J.JId,F.FId,C.CId,RId,CC";
        }

        /// <summary>
        /// 向筛选条件后边连接需要的属性，Id是默认添加的
        /// </summary>
        /// <param name="condExpr">筛选条件</param>
        /// <param name="RId"></param>
        /// <param name="FId"></param>
        /// <param name="CId"></param>
        /// <param name="JId"></param>
        /// <param name="AuId"></param>
        /// <param name="AfId"></param>
        /// <returns></returns>
        static internal string LinkAttrib(string condExpr, bool RId, bool FId, bool CId,
                                          bool JId, bool AuId, bool AfId, bool CC)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(condExpr);
            sb.Append("&attributes=");

            sb.Append("Id");
            link(RId, sb, "RId");
            link(FId, sb, "F.FId");
            link(CId, sb, "C.CId");
            link(JId, sb, "J.JId");
            link(AuId, sb, "AA.AuId");
            link(AfId, sb, "AA.AfId");
            link(CC, sb, "CC");

            return sb.ToString();
        }

        static private void link(bool flag, StringBuilder sb, string attrib)
        {
            if (flag)
            {
                sb.Append(',');
                sb.Append(attrib);
            }
        }

        // 组合条件帮助函数:

        static internal string And(IEnumerable<String> conds)
        {
            var e = conds.GetEnumerator();
            if (e.MoveNext())
            {
                StringBuilder sb = new StringBuilder();
                expand(e, sb, "And");
                return sb.ToString();
            }
            else
                return string.Empty;
        }

        static internal string Or(IEnumerable<String> conds)
        {
            var e = conds.GetEnumerator();
            if (e.MoveNext())
            {
                StringBuilder sb = new StringBuilder();
                expand(e, sb, "Or");
                return sb.ToString();
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// expand string in strs, using comma(,) to separate each token, and appending the result to sb
        /// </summary>
        /// <param name="strs">string collection</param>
        /// <param name="sb">the string builder to which to append</param>
        static private void expand(IEnumerator<string> e, StringBuilder sb, string opt)
        {
            string str1 = e.Current;
            if (!e.MoveNext())
            {
                sb.Append(str1);
            }
            else
            {
                sb.Append(opt);
                sb.Append('(');
                sb.Append(str1);
                sb.Append(',');
                expand(e, sb, opt);
                sb.Append(')');
            }
        }

        static internal IList<List<string>> Group(IList<string> conds, int limit, int extra)
        {
            IList<List<string>> groups = new List<List<string>>();
            List<string> cur = new List<string>();
            int len = 0;
            foreach (string cond in conds)
            {
                if (len + cond.Length + extra <= limit)
                {
                    len = len + cond.Length + extra;
                    cur.Add(cond);
                }
                else
                {
                    groups.Add(cur);
                    cur = new List<string>();
                    cur.Add(cond);
                    len = cond.Length + extra;
                }
            }

            if (cur.Count != 0)
            {
                groups.Add(cur);
            }
            return groups;
        }
    }
}
