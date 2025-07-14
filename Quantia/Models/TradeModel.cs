using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quantia.Models
{
    [Table("trades")]
    public class TradeModel
    {
        // ───── Colonnes réelles (inchangées) ─────────────────────────── //
        [Column("id")] public int Id { get; set; }
        [Column("user_id")] public int UserId { get; set; }
        [Column("crypto_symbol")] public string CryptoSymbol { get; set; } = string.Empty;
        [Column("buy_date")] public DateTime BuyDate { get; set; }
        [Column("buy_price")] public decimal BuyPrice { get; set; }
        [Column("quantity")] public decimal Quantity { get; set; }
        [Column("sell_date")] public DateTime? SellDate { get; set; }
        [Column("sell_price")] public decimal? SellPrice { get; set; }
        [Column("status")] public string Status { get; set; } = "Open";

        // ───── Propriété PnL réalisé (déjà existante) ───────────────── //
        public decimal? PnL => SellPrice.HasValue
            ? (SellPrice.Value - BuyPrice) * Quantity
            : null;

        // ───── NOUVEAU : PnL latent (non stocké) ─────────────────────── //
        [NotMapped] public decimal? UnrealizedPnl { get; set; }

        // ───── NOUVEAU : drapeau gagnant / perdant (non stocké) ─────── //
        [NotMapped]
        public bool? IsWinning =>
            (SellDate is null ? UnrealizedPnl : PnL) switch
            {
                > 0 => true,
                < 0 => false,
                _ => null
            };
    }
}
