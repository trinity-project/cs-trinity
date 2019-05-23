using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Wallets.Templates.Definitions
{
    public enum TransactionType : byte
    {
        RSMC = 0,
        HTLC = 1,
        HTLC2RSMC=2
    }
}
