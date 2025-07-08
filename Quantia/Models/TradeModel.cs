using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quantia.Models
{
    [Table("trades")]
    public class TradeModel
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("crypto_symbol")]
        public string CryptoSymbol { get; set; } = string.Empty;

        [Column("buy_date")]
        public DateTime BuyDate { get; set; }

        [Column("buy_price")]
        public decimal BuyPrice { get; set; }

        [Column("quantity")]
        public decimal Quantity { get; set; }

        // Partie Vente (null si trade encore "ouvert")
        [Column("sell_date")]
        public DateTime? SellDate { get; set; }

        [Column("sell_price")]
        public decimal? SellPrice { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Open";

        // Calcule le PnL directement dans le modèle (optionnel)
        public decimal? PnL => SellPrice.HasValue ? (SellPrice.Value - BuyPrice) * Quantity : null;
    }

}
