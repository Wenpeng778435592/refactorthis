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
            var payment = new Payment();

            var ex = Assert.Throws<InvalidOperationException>(() => _paymentProcessor.ProcessPayment(payment));
            Assert.AreEqual("There is no invoice matching this payment", ex.Message);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_NoPaymentNeeded()
        {
            SetupInvoice(0, 0, null);

            var result = _paymentProcessor.ProcessPayment(new Payment());

            Assert.AreEqual("no payment needed", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_InvoiceAlreadyFullyPaid()
        {
            SetupInvoice(10, 10, new List<Payment> { new Payment { Amount = 10 } });

            var result = _paymentProcessor.ProcessPayment(new Payment());

            Assert.AreEqual("invoice was already fully paid", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_PartialPaymentExistsAndAmountPaidExceedsAmountDue()
        {
            SetupInvoice(10, 5, new List<Payment> { new Payment { Amount = 5 } });

            var result = _paymentProcessor.ProcessPayment(new Payment { Amount = 6 });

            Assert.AreEqual("the payment is greater than the partial amount remaining", result);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnFailureMessage_When_NoPartialPaymentExistsAndAmountPaidExceedsInvoiceAmount()
        {
            SetupInvoice(5, 0, new List<Payment>());

            var result = _paymentProcessor.ProcessPayment(new Payment { Amount = 6 });

            Assert.AreEqual("the payment is greater than the invoice amount", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFullyPaidMessage_When_PartialPaymentExistsAndAmountPaidEqualsAmountDue()
        {
            SetupInvoice(10, 5, new List<Payment> { new Payment { Amount = 5 } });

            var result = _paymentProcessor.ProcessPayment(new Payment { Amount = 5 });

            Assert.AreEqual("final partial payment received, invoice is now fully paid", result);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnFullyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidEqualsInvoiceAmount()
        {
            SetupInvoice(10, 0, new List<Payment> { new Payment { Amount = 10 } });

            var result = _paymentProcessor.ProcessPayment((new Payment { Amount = 10 }));

            Assert.AreEqual("invoice was already fully paid", result);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnPartiallyPaidMessage_When_PartialPaymentExistsAndAmountPaidIsLessThanAmountDue()
        {
            SetupInvoice(10, 5, new List<Payment> { new Payment { Amount = 5 } });

            var result = _paymentProcessor.ProcessPayment((new Payment { Amount = 1 }));

            Assert.AreEqual("another partial payment received, still not fully paid", result);
        }

        [Test]
        public void
            ProcessPayment_Should_ReturnPartiallyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidIsLessThanInvoiceAmount()
        {
            SetupInvoice(10, 0, new List<Payment>());

            var result = _paymentProcessor.ProcessPayment((new Payment { Amount = 1 }));

            Assert.AreEqual("invoice is now partially paid", result);
        }

        private void SetupInvoice(decimal amount, decimal amountPaid, List<Payment> payments)
        {
            Invoice invoice = new Invoice(_repo)
            {
                Amount = amount,
                AmountPaid = amountPaid,
                Payments = payments
            };

            _repo.Add(invoice);
        }
    }
}