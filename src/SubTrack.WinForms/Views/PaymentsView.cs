namespace SubTrack.WinForms.Views;

internal sealed class PaymentsView : ChargesView
{
    public PaymentsView(ViewContext context)
        : base(context, ChargesViewMode.Payments)
    {
    }
}
