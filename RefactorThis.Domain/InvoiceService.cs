using System;
using System.Linq;
using RefactorThis.Persistence;

namespace RefactorThis.Domain
{
    public class InvoiceService
    {
        private readonly InvoiceRepository _invoiceRepository;

        public InvoiceService(InvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public string ProcessPayment(Payment payment)
        {
            var inv = _invoiceRepository.GetInvoice(payment.Reference);
            var responseMessage = string.Empty;
            if (inv == null) throw new InvalidOperationException("There is no invoice matching this payment");
            if (inv.Amount == 0)
            {
                if (inv.Payments == null || !inv.Payments.Any())
                     return "no payment needed";
                else
                    throw new InvalidOperationException(
                        "The invoice is in an invalid state, it has an amount of 0 and it has payments.");
            }

            if (inv.Payments != null && inv.Payments.Any())
            {
                var total = inv.Payments?.Sum(x => x.Amount) ?? 0;
                var remain = inv.Amount - inv.AmountPaid;
            
                if (total == inv.Amount)
                {
                    return  "invoice was already fully paid";
                }
                if (payment.Amount > remain)
                {
                    return "the payment is greater than the partial amount remaining";
                }
                if (inv.Amount - inv.AmountPaid == payment.Amount)
                {
                
                    responseMessage = "final partial payment received, invoice is now fully paid";
                }
                else
                {
                    responseMessage = "another partial payment received, still not fully paid";
                }
                ApplyingTax(inv, payment);

            }
            else
            {
                if (payment.Amount > inv.Amount)
                {
                    return "the payment is greater than the invoice amount";
                }
                else if (inv.Amount == payment.Amount)
                {
                    responseMessage = "invoice is now fully paid";
                }
                else
                {
                    responseMessage = "invoice is now partially paid";
                }
                ApplyingTax(inv, payment, true);

            }
            inv.Save();

            return responseMessage;
        }

        private void ApplyingTax(Invoice inv, Payment payment, Boolean isPaymentEqualThanAmount = false)
        {
            inv.AmountPaid += payment.Amount;
            if (isPaymentEqualThanAmount || inv.Type == InvoiceType.Commercial)
            {
                inv.TaxAmount = payment.Amount * 0.14m;
            }
            
            if (inv.Payments == null)
            {
                inv.Payments = new System.Collections.Generic.List<Payment>();
            }
            inv.Payments.Add(payment);
        }
    }
}