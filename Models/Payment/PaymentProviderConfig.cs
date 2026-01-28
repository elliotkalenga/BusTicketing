
using System.ComponentModel.DataAnnotations;
using BusTicketing.Models.Common;

namespace BusTicketing.Models.Payment
{
    public class PaymentProviderConfig : AuditableBranchEntity
    {
        [Required, MaxLength(64)] public string ProviderKey { get; set; } = default!;
        [MaxLength(256)] public string PublicKey { get; set; } = "";
        [MaxLength(256)] public string SecretKey { get; set; } = "";
        [MaxLength(128)] public string MerchantAccount { get; set; } = "";
        public bool IsLive { get; set; }
    }
}
