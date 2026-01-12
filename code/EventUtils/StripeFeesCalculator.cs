public static class StripeFeeCalculator
{
    
    /// <param name="targetNet">The amount the organizer should keep (e.g., 2000 for $20.00)</param>
    /// <param name="platformProfit">Your desired profit (e.g., 100 for $1.00)</param>
    /// <param name="stripePercent">Default 0.029 (2.9%)</param>
    /// <param name="stripeFixed">Default 30 (30 cents)</param>
    public static (long totalToCharge, long applicationFee) Calculate(
        long targetNet, 
        long platformProfit, 
        double stripePercent = 0.029, 
        long stripeFixed = 30)
    {
        // Formula: Total = (Net + Fixed + Profit) / (1 - Percent)
        double numerator = targetNet + stripeFixed + platformProfit;
        double denominator = 1 - stripePercent;
        
        long totalToCharge = (long)Math.Ceiling(numerator / denominator);
        
        // Your application fee must cover your profit PLUS the stripe fee 
        // if you want the organizer to keep exactly the targetNet.
        long stripeFee = (long)Math.Ceiling(totalToCharge * stripePercent) + stripeFixed;
        long applicationFee = platformProfit + stripeFee;

        return (totalToCharge, applicationFee);
    }
}
