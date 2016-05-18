using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOP
{
    // [AuId, AuId]
    internal class Au_Au
    {
        internal Au_Au(MAGHelper _helper)
        {
            helper = _helper;
        }

        internal ConcurrentBag<Int64[]> AuToAuPaths(Author auth1, Author auth2, IEnumerable<Paper> paperByAu1)
        {
            ConcurrentBag<Int64[]> paths = new ConcurrentBag<long[]>();

            // no 1-hop path

            // 2-hop path
            // AuId->AfId->AuId
            // 这里暂且用auth1的Affiliations进行循环。实际上，用集合大小更小的进行循环大概更快一些
            foreach (Int64 afId in auth1.Affiliations)
            {
                if (auth2.Affiliations.Contains(afId))
                {
                    paths.Add(new Int64[] { auth1.AuId, afId, auth2.AuId });
                }
            }
            // AuId->Id->AuId
            foreach (Int64 id in auth1.Papers)
            {
                if (auth2.Papers.Contains(id))
                {
                    paths.Add(new Int64[] { auth1.AuId, id, auth2.AuId });
                }
            }

            AuId_Id_Id_AuId(auth1, auth2, paths, paperByAu1);

            return paths;
        }

        /// <summary>
        /// find and add all 3-hop paths of type AuId->Id->Id->AuId into paths.
        /// </summary>
        /// <param name="auId1"></param>
        /// <param name="auId2"></param>
        /// <param name="paths">container to which to write paths</param>
        /// <param name="papers1">information of papers by author 2</param>
        private void AuId_Id_Id_AuId(Author auth1, Author auth2, ConcurrentBag<Int64[]> paths,
                                    IEnumerable<Paper> papers1)
        {
            // 目前使用的方法：
            // 判断ID是否为AuId时顺手查询了他所写的文章信息，直接利用

            foreach (Paper paper in papers1)
            {
                // 检查paper的引用
                foreach (Int64 rId in paper.RIds)
                {
                    if (auth2.Papers.Contains(rId))  // 如果这篇文章是auth2写的
                    {
                        paths.Add(new Int64[] { auth1.AuId, paper.Id, rId, auth2.AuId });
                    }
                }
            }
        }

        MAGHelper helper;
    }
}
