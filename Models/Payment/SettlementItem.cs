
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Payment
{
    public class SettlementItem : AuditableBranchEntity
    {
        public int SettlementBatchId { get; set; }
        public SettlementBatch SettlementBatch { get; set; } = default!;

        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = default!;

        public Money Captured { get; set; } = new Money();
        public Money Refunded { get; set; } = new Money();
        public Money Fees { get; set; } = new Money();
    }
}
