using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity.Wallets.Templates.Definitions
{
    public enum EnumTransactionType : byte
    {
        // For creating channel
        FUNDING = 0,

        // For RSMC transaction
        RSMC = 0x10,
        COMMITMENT = 0x11,
        REVOCABLE = 0x12,
        BREACHREMEDY = 0x13,

        // For HTLC Transaction
        HTLC = 0x40,
        HCTX = 0x41,
        HDTX = 0x42,
        HRDTX = 0x43,
        HETX = 0x48,
        HEDTX = 0x49,
        HERDTX = 0x4a,
        HTTX = 0x50,
        HTDTX = 0x51,
        HTRDTX = 0x52,

        HTLC2RSMC = 0x80,

        // For closing channel
        SETTLE = 0xFF
    }
}
