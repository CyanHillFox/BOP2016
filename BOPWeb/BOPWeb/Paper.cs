using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOP
{
    internal struct ConfId
    {
        internal ConfId(Int64 id)
        {
            CId = id;
        }
        
        public override bool Equals(object obj)
        {
            if (obj is ConfId)
            {
                ConfId rhs = (ConfId)obj;
                if (this.CId == rhs.CId && this.CId != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        public static bool operator ==(ConfId lhs, ConfId rhs)
        {
            if (lhs.CId == rhs.CId && lhs.CId != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(ConfId lhs, ConfId rhs)
        {
            return !(lhs == rhs);
        }

        internal readonly Int64 CId;
    }

    class Paper
    {
        internal Paper(Int64 id, HashSet<Int64> auths, Int64 cId, HashSet<Int64> rIds,
                       Int64 jId, HashSet<Int64> fId, int cc)
        {
            Id = id;
            AuIds = auths;
            RIds = rIds;
            CId = new ConfId(cId);
            JId = new JourId(jId);
            FIds = fId;
            CC = cc;
        }

        public readonly Int64 Id;

        public readonly HashSet<Int64> AuIds;

        public readonly HashSet<Int64> RIds;

        public readonly ConfId CId;

        public readonly JourId JId;

        public readonly HashSet<Int64> FIds;

        public readonly int CC;
    }

    // 我本来想三个属性(FId, JId, CId)用一个类算了，不过考虑可能手滑写错，导致不同属性进行比较，所以仍然分开成三个。

    internal struct JourId
    {
        internal JourId(Int64 id)
        {
            JId = id;
        }

        public override bool Equals(object obj)
        {
            if (obj is JourId)
            {
                JourId rhs = (JourId)obj;
                if (this.JId == rhs.JId && this.JId != 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
                return false;
        }

        public static bool operator ==(JourId lhs, JourId rhs)
        {
            if (lhs.JId == rhs.JId && lhs.JId != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool operator !=(JourId lhs, JourId rhs)
        {
            return !(lhs == rhs);
        }

        internal readonly Int64 JId;
    }
}
