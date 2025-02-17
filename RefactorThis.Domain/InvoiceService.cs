using System;
using System.Collections.Generic;
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
            Invoice inv = _invoiceRepository.GetInvoice(payment.Reference) ??
                          throw new InvalidOperationException("There is no invoice matching this payment");

            string responseMessage = string.Empty;

            //invoice is empty
            if (inv.Amount == 0)
            {
                if (!HasPayments(inv))
                {
                    return "no payment needed";
                }

                throw new InvalidOperationException(
                    "The invoice is in an invalid state, it has an amount of 0 and it has payments.");
            }

            decimal invoiceRemainingAmount = inv.Amount - inv.AmountPaid;
            decimal invoiceAlreadyPaid = !HasPayments(inv) ? 0 : inv.Payments.Sum(x => x.Amount);

            //invoice is populated, needs payment to fulfill it
            //for non first time payments
            if (HasPayments(inv))
            {
                //invoice is already fully paid
                if (inv.Amount == invoiceAlreadyPaid)
                {
                    responseMessage = "invoice was already fully paid";
                }

                //incoming payment is greater than the remaining amount
                else if (payment.Amount > invoiceRemainingAmount)
                {
                    responseMessage = "the payment is greater than the partial amount remaining";
                }

                //can execute the payment, update invoice
                else
                {
                    //payment is equal to the remaining amount
                    if (invoiceRemainingAmount == payment.Amount)
                    {
                        ProcessInvoicePayment(inv, payment);

                        responseMessage = "final partial payment received, invoice is now fully paid";
                    }

                    //payment is less than the remaining amount
                    else
                    {
                        ProcessInvoicePayment(inv, payment);

                        responseMessage = "another partial payment received, still not fully paid";
                    }
                }
            }
            //for first time payments
            else
            {
                //first time payment is greater than the invoice amount
                if (payment.Amount > inv.Amount)
                {
                    responseMessage = "the payment is greater than the invoice amount";
                }

                //first time payment is equal to the invoice amount
                else if (inv.Amount == payment.Amount)
                {
                    ProcessInvoicePayment(inv, payment);

                    responseMessage = "invoice is now fully paid";
                }

                //first time payment is less than the invoice amount
                else
                {
                    ProcessInvoicePayment(inv, payment);

                    responseMessage = "invoice is now partially paid";
                }
            }

            inv.Save();

            return responseMessage;
        }

        private void ProcessInvoicePayment(Invoice invoice, Payment payment)
        {
            if (HasPayments(invoice))
            {
                invoice.AmountPaid = payment.Amount;
                invoice.TaxAmount = payment.Amount * 0.14m;
                invoice.Payments.Add(payment);
            }
            else
            {
                invoice.AmountPaid += payment.Amount;
                invoice.Payments.Add(payment);

                if (invoice.Type == InvoiceType.Commercial)
                {
                    invoice.TaxAmount += payment.Amount * 0.14m;
                }
            }
        }

        private bool HasPayments(Invoice invoice)
        {
            return !(invoice.Payments == null || !invoice.Payments.Any());
        }
    }
}