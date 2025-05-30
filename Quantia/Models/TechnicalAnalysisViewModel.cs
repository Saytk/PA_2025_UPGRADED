using System;
using System.Collections.Generic;

namespace Quantia.Models
{
    public class TechnicalAnalysisViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime AnalysisDate { get; set; }
        public List<string> SignalsDetected { get; set; }
        public decimal Entry { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public string RiskReward { get; set; }
    }
}
