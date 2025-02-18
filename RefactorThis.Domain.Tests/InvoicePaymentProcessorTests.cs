using System;
using System.Collections.Generic;
using NUnit.Framework;
using RefactorThis.Persistence;

namespace RefactorThis.Domain.Tests
{
    [TestFixture]
    public class InvoicePaymentProcessorTests
    {
        private InvoiceRepository _repo;
        private InvoiceService _paymentProcessor;

        [SetUp]
        public void Setup()
        {
            _repo = new InvoiceRepository();
            _paymentProcessor = new InvoiceService(_repo);
        }

        [Test]
        public void ProcessPayment_Should_ThrowException_When_NoInoiceFoundForPaymentReference()
        {
            Payment payment = new Payment();

            Exception ex = Assert.Throws<InvalidOperationException>(() => _paymentProcessor.ProcessPayment(payment));
            Assert.AreEqual("There is no invoice matching this payment", ex.Message);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_And_Correct_Invoice_Figures_When_NoPaymentNeeded()
        {
            Invoice invoice = SetupInvoice(0, 0, null);

            String result = _paymentProcessor.ProcessPayment(new Payment());

            Assert.AreEqual("no payment needed", result);
            
            Assert.AreEqual(invoice.Amount, 0);
            
            Assert.AreEqual(invoice.AmountPaid, 0);
            
            Assert.AreEqual(invoice.TaxAmount, 0);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_And_Correct_Invoice_Figures_When_InvoiceAlreadyFullyPaid()
        {
            Invoice invoice = SetupInvoice(10, 10, new List<Payment> { new Payment { Amount = 10 } });

            String result = _paymentProcessor.ProcessPayment(new Payment{ Amount = 6 });

            Assert.AreEqual("invoice was already fully paid", result);
            
            Assert.AreEqual(invoice.Payments.Count, 1);
            
            Assert.AreEqual(invoice.Amount, 10);
            
            Assert.AreEqual(invoice.AmountPaid, 10);
            
            Assert.AreEqual(invoice.TaxAmount, 0);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_And_Correct_Invoice_Figures_When_PartialPaymentExistsAndAmountPaidExceedsAmountDue()
        {
            Invoice invoice = SetupInvoice(10, 5, new List<Payment> { new Payment { Amount = 5 } });

            String result = _paymentProcessor.ProcessPayment(new Payment { Amount = 6 });

            Assert.AreEqual("the payment is greater than the partial amount remaining", result);
            
            Assert.AreEqual(invoice.Payments.Count, 1);
            
            Assert.AreEqual(invoice.Amount, 10);
            
            Assert.AreEqual(invoice.AmountPaid, 5);
            
            Assert.AreEqual(invoice.TaxAmount, 0);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnFailureMessage_And_Correct_Invoice_Figures_When_NoPartialPaymentExistsAndAmountPaidExceedsInvoiceAmount()
        {
            Invoice invoice = SetupInvoice(5, 0, new List<Payment>());

            String result = _paymentProcessor.ProcessPayment(new Payment { Amount = 6 });

            Assert.AreEqual("the payment is greater than the invoice amount", result);
            
            Assert.AreEqual(invoice.Payments.Count, 0);
            
            Assert.AreEqual(invoice.Amount, 5);
            
            Assert.AreEqual(invoice.AmountPaid, 0);
            
            Assert.AreEqual(invoice.TaxAmount, 0);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFullyPaidMessage_And_Correct_Invoice_Figures_When_PartialPaymentExistsAndAmountPaidEqualsAmountDue()
        {
            Invoice invoice = SetupInvoice(10, 5, new List<Payment> { new Payment { Amount = 5 } });

            String result = _paymentProcessor.ProcessPayment(new Payment { Amount = 5 });

            Assert.AreEqual("final partial payment received, invoice is now fully paid", result);
            
            Assert.AreEqual(invoice.Payments.Count, 2);
            
            Assert.AreEqual(invoice.Amount, 10);
            
            Assert.AreEqual(invoice.AmountPaid, 5);
            
            Assert.AreEqual(invoice.TaxAmount, 0.7);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnFullyPaidMessage_And_Correct_Invoice_Figures_When_NoPartialPaymentExistsAndAmountPaidEqualsInvoiceAmount()
        {
            Invoice invoice = SetupInvoice(10, 0, new List<Payment> { new Payment { Amount = 10 } });

            String result = _paymentProcessor.ProcessPayment(new Payment { Amount = 10 });

            Assert.AreEqual("invoice was already fully paid", result);
            
            Assert.AreEqual(invoice.Payments.Count, 1);
            
            Assert.AreEqual(invoice.Amount, 10);
            
            Assert.AreEqual(invoice.AmountPaid, 0);
            
            Assert.AreEqual(invoice.TaxAmount, 0);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnPartiallyPaidMessage_And_Correct_Invoice_Figures_When_PartialPaymentExistsAndAmountPaidIsLessThanAmountDue()
        {
            Invoice invoice = SetupInvoice(10, 5, new List<Payment> { new Payment { Amount = 5 } });

            String result = _paymentProcessor.ProcessPayment((new Payment { Amount = 1 }));

            Assert.AreEqual("another partial payment received, still not fully paid", result);
            
            Assert.AreEqual(invoice.Payments.Count, 2);
            
            Assert.AreEqual(invoice.Amount, 10);
            
            Assert.AreEqual(invoice.AmountPaid, 1);
            
            Assert.AreEqual(invoice.TaxAmount, 0.14);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnPartiallyPaidMessage_And_Correct_Invoice_Figures_When_NoPartialPaymentExistsAndAmountPaidIsLessThanInvoiceAmount()
        {
            Invoice invoice = SetupInvoice(10, 0, new List<Payment>());

            String result = _paymentProcessor.ProcessPayment((new Payment { Amount = 1 }));

            Assert.AreEqual("invoice is now partially paid", result);
            
            Assert.AreEqual(invoice.Payments.Count, 1);
            
            Assert.AreEqual(invoice.Amount, 10);
            
            Assert.AreEqual(invoice.AmountPaid, 1);
            
            Assert.AreEqual(invoice.TaxAmount, 0);
        }

        private Invoice SetupInvoice(decimal amount, decimal amountPaid, List<Payment> payments)
        {
            Invoice invoice = new Invoice(_repo)
            {
                Amount = amount,
                AmountPaid = amountPaid,
                Payments = payments
            };

            _repo.Add(invoice);

            return invoice;
        }
    }
}