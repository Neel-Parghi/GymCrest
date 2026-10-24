using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Gym.Billing;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;
using GymCrest.Shared.Enums;

namespace GymCrest.Application.Modules.Gym.Services
{
    public class MemberBillingService : IMemberBillingService
    {

        private readonly IMemberBillingRepository _billingRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MemberBillingService(IMemberBillingRepository billingRepository, IUnitOfWork unitOfWork)
        {
            _billingRepository = billingRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> CreateCustomInvoiceAsync(Guid gymId, Guid? branchId, CreateCustomInvoiceRequest request)
        {
            CustomInvoice invoice = new()
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                BranchId = branchId,
                MemberId = request.MemberId,
                TrainerId = request.TrainerId,
                BillingType = request.BillingType,
                Description = $"{request.BillingType} Invoice",
                Amount = request.Amount,
                AmountPaid = request.Status == "Paid" ? request.Amount : 0,
                TaxRate = (request.BillingType == "Personal Training" ||
                           request.BillingType == "Rehab & Therapy" ||
                           request.BillingType == "Other Charges") ? 0 : 18,
                PaymentMethod = request.Status == "Pending" ? "Pending-" + request.PaymentMethod : request.PaymentMethod,
                Status = request.Status,
                TransactionDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(15),
                CreatedOn = DateTime.UtcNow
            };

            await _billingRepository.AddCustomInvoiceAsync(invoice);
            int saved = await _unitOfWork.SaveChangesAsync();

            return saved > 0;
        }

        public async Task<MemberBillingOverviewDto> GetMemberBillingOverviewAsync(Guid gymId, Guid? branchId, string monthKey)
        {
            int year = int.Parse(monthKey.Split('-')[0]);
            int month = int.Parse(monthKey.Split('-')[1]);

            DateTime startDate = new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime endDate = startDate.AddMonths(1).AddTicks(-1);

            List<MemberInvoiceDto> unifiedInvoices = [];

            GymCrest.Domain.Entities.Gym? gym = await _billingRepository.GetGymByIdAsync(gymId);
            string invoicePrefix = string.IsNullOrWhiteSpace(gym?.InvoicePrefix) ? "GF-" : gym.InvoicePrefix;

            Branch? mainBranch = await _billingRepository.GetMainBranchAsync(gymId);

            IEnumerable<MemberSubscription> subscriptions = await _billingRepository.GetSubscriptionsByMonthAsync(gymId, startDate, endDate);

            foreach (MemberSubscription sub in subscriptions)
            {
                if (branchId.HasValue && sub.Member.BranchId != branchId.Value)
                    continue;

                bool isFirstSubscription = !sub.Member.Subscriptions.Any(prev => prev.StartDate < sub.StartDate);
                string billingCategory = isFirstSubscription ? "Registration" : "Membership Renewal";

                Branch? resolvedBranch = sub.Member.Branch ?? mainBranch;

                unifiedInvoices.Add(new MemberInvoiceDto
                {
                    Id = $"{invoicePrefix}{sub.StartDate:yyyy}-{sub.Id.ToString()[..4].ToUpper()}",
                    MemberId = sub.MemberId,
                    MemberName = $"{sub.Member.FirstName} {sub.Member.LastName}",
                    Email = sub.Member.Email,
                    BillingType = billingCategory,
                    Description = sub.PlanNameSnapshot ?? "Membership Plan Access",
                    Amount = sub.PricePaid,
                    AmountPaid = sub.AmountPaid,
                    Balance = sub.PricePaid - sub.AmountPaid,
                    DateIssued = sub.StartDate,
                    DueDate = sub.StartDate.AddDays(15),
                    Status = sub.PaymentStatus.ToString(),
                    MembershipNumber = sub.Member.MembershipNumber ?? string.Empty,
                    RealRecordId = sub.Id,
                    CreatedOn = sub.CreatedOn,

                    // Branch scoping details
                    BranchId = resolvedBranch?.Id,
                    BranchName = resolvedBranch?.Name ?? "Main Outlet",
                    BranchLine1 = resolvedBranch?.Address?.Address1 ?? string.Empty,
                    BranchLine2 = resolvedBranch?.Address?.Address2 ?? string.Empty,
                    BranchCity = resolvedBranch?.Address?.City ?? string.Empty,
                    BranchState = resolvedBranch?.Address?.State ?? string.Empty,
                    BranchPostalCode = resolvedBranch?.Address?.PostalCode ?? string.Empty
                });
            }

            IEnumerable<SaleTransaction> retailSales = await _billingRepository.GetTransactionsByMonthAsync(gymId, startDate, endDate);

            foreach (SaleTransaction tx in retailSales)
            {
                if (branchId.HasValue && tx.BranchId != branchId.Value)
                    continue;

                Branch? resolvedBranch = tx.Branch ?? mainBranch;
                decimal saleAmount = tx?.TotalAmount ?? 0;
                bool saleIsPaid = !tx!.PaymentMethod.StartsWith("Pending");

                unifiedInvoices.Add(new MemberInvoiceDto
                {
                    Id = $"{invoicePrefix}{tx.TransactionDate:yyyy}-{tx.Id.ToString()[..4].ToUpper()}",
                    MemberId = tx.MemberId,
                    MemberName = $"{tx?.Member?.FirstName} {tx?.Member?.LastName}",
                    Email = tx?.Member?.Email ?? "N/A",
                    BillingType = "Store Purchase",
                    Description = tx?.InventoryItem?.Name ?? "Retail Sale Item",
                    Amount = saleAmount,
                    AmountPaid = saleIsPaid ? saleAmount : 0,
                    Balance = saleIsPaid ? 0 : saleAmount,
                    DateIssued = tx!.TransactionDate,
                    DueDate = tx.TransactionDate,
                    Status = saleIsPaid ? "Paid" : "Pending",
                    MembershipNumber = tx?.Member?.MembershipNumber ?? string.Empty,
                    RealRecordId = tx!.Id,
                    CreatedOn = tx.CreatedOn,

                    BranchId = resolvedBranch?.Id,
                    BranchName = resolvedBranch?.Name ?? "Main Outlet",
                    BranchLine1 = resolvedBranch?.Address?.Address1 ?? string.Empty,
                    BranchLine2 = resolvedBranch?.Address?.Address2 ?? string.Empty,
                    BranchCity = resolvedBranch?.Address?.City ?? string.Empty,
                    BranchState = resolvedBranch?.Address?.State ?? string.Empty,
                    BranchPostalCode = resolvedBranch?.Address?.PostalCode ?? string.Empty
                });
            }

            IEnumerable<CustomInvoice> customInvoices = await _billingRepository.GetCustomInvoicesByMonthAsync(gymId, startDate, endDate);

            foreach (CustomInvoice invoice in customInvoices)
            {
                if (branchId.HasValue && invoice.BranchId != branchId.Value)
                    continue;

                Branch? resolvedBranch = invoice.Branch ?? mainBranch;

                unifiedInvoices.Add(new MemberInvoiceDto
                {
                    Id = $"{invoicePrefix}{invoice.TransactionDate:yyyy}-{invoice.Id.ToString()[..4].ToUpper()}",
                    MemberId = invoice.MemberId,
                    MemberName = $"{invoice?.Member?.FirstName} {invoice?.Member?.LastName}",
                    Email = invoice?.Member?.Email ?? "N/A",
                    BillingType = invoice?.BillingType ?? string.Empty,
                    Description = invoice?.Description ?? invoice?.BillingType ?? string.Empty,
                    Amount = invoice?.Amount ?? 0,
                    AmountPaid = invoice?.AmountPaid ?? 0,
                    Balance = (invoice?.Amount ?? 0) - (invoice?.AmountPaid ?? 0),
                    DateIssued = invoice!.TransactionDate,
                    DueDate = invoice.DueDate,
                    Status = invoice.Status,
                    PaymentMethod = invoice.PaymentMethod,
                    MembershipNumber = invoice?.Member?.MembershipNumber ?? string.Empty,
                    RealRecordId = invoice!.Id,
                    CreatedOn = invoice.CreatedOn,

                    // Branch scoping details
                    BranchId = resolvedBranch?.Id,
                    BranchName = resolvedBranch?.Name ?? "Main Outlet",
                    BranchLine1 = resolvedBranch?.Address?.Address1 ?? string.Empty,
                    BranchLine2 = resolvedBranch?.Address?.Address2 ?? string.Empty,
                    BranchCity = resolvedBranch?.Address?.City ?? string.Empty,
                    BranchState = resolvedBranch?.Address?.State ?? string.Empty,
                    BranchPostalCode = resolvedBranch?.Address?.PostalCode ?? string.Empty
                });
            }

            List<MemberInvoiceDto> sortedInvoices = [..unifiedInvoices.OrderByDescending(x => x.CreatedOn)];

            decimal totalCollected = sortedInvoices.Sum(x => x.AmountPaid);
            decimal pendingReceivables = sortedInvoices.Where(x => x.Status == "Pending" || x.Status == "Partial").Sum(x => x.Balance);
            decimal overdueBalances = sortedInvoices.Where(x => x.Status == "Unpaid" || x.Status == "Overdue").Sum(x => x.Balance);
            decimal totalInvoiced = sortedInvoices.Sum(x => x.Amount);

            return new MemberBillingOverviewDto
            {
                Invoices = sortedInvoices,
                Stats = new MemberBillingStatsDto
                {
                    TotalCollected = totalCollected,
                    PendingReceivables = pendingReceivables,
                    OverdueBalances = overdueBalances,
                    TotalInvoiced = totalInvoiced
                },
                GymName = gym?.GymName ?? string.Empty,
                GymBrandName = gym?.BrandName ?? string.Empty,
                GymGstNumber = gym?.GstNumber ?? string.Empty
            };
        }

        public async Task<RecordPaymentResult> RecordPaymentAsync(Guid gymId, Guid? branchId, Guid recordId, RecordPaymentRequest request)
        {
            if (request.Amount <= 0)
                return RecordPaymentResult.Fail("Payment amount must be greater than zero.");

            MemberSubscription? subscription = await _billingRepository.GetSubscriptionByIdAsync(recordId);
            if (subscription != null)
            {
                decimal balance = subscription.PricePaid - subscription.AmountPaid;
                if (request.Amount > balance)
                    return RecordPaymentResult.Fail($"Payment amount exceeds the remaining balance of {balance:0.00}.");

                PaymentRecord ledgerEntry = new()
                {
                    Id = Guid.NewGuid(),
                    MemberId = subscription.MemberId,
                    SourceType = PaymentSourceType.Subscription,
                    SourceId = subscription.Id,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    Notes = request.Notes,
                    PaidAt = DateTime.UtcNow,
                    GymId = gymId,
                    BranchId = branchId,
                    CreatedOn = DateTime.UtcNow
                };
                await _billingRepository.AddPaymentRecordAsync(ledgerEntry);

                subscription.AmountPaid += request.Amount;
                subscription.PaymentStatus = subscription.AmountPaid >= subscription.PricePaid
                    ? PaymentStatus.Paid
                    : PaymentStatus.Partial;
                subscription.ModifiedOn = DateTime.UtcNow;

                int savedSub = await _unitOfWork.SaveChangesAsync();
                return savedSub > 0 ? RecordPaymentResult.Ok() : RecordPaymentResult.Fail("Failed to save payment.");
            }

            SaleTransaction? transaction = await _billingRepository.GetTransactionByIdAsync(recordId);
            if (transaction != null)
            {
                bool isPending = transaction.PaymentMethod.StartsWith("Pending-");
                if (!isPending)
                    return RecordPaymentResult.Fail("This transaction is already fully paid.");

                if (request.Amount != transaction.TotalAmount)
                    return RecordPaymentResult.Fail($"Store purchases must be paid in full ({transaction.TotalAmount:0.00}). Partial payments are not supported for retail sales.");

                transaction.PaymentMethod = transaction.PaymentMethod.Replace("Pending-", "");

                int savedTx = await _unitOfWork.SaveChangesAsync();
                return savedTx > 0 ? RecordPaymentResult.Ok() : RecordPaymentResult.Fail("Failed to save payment.");
            }

            CustomInvoice? invoice = await _billingRepository.GetCustomInvoiceByIdAsync(recordId);
            if (invoice != null)
            {
                decimal balance = invoice.Amount - invoice.AmountPaid;
                if (request.Amount > balance)
                    return RecordPaymentResult.Fail($"Payment amount exceeds the remaining balance of {balance:0.00}.");

                PaymentRecord ledgerEntry = new()
                {
                    Id = Guid.NewGuid(),
                    MemberId = invoice.MemberId,
                    SourceType = PaymentSourceType.CustomInvoice,
                    SourceId = invoice.Id,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    Notes = request.Notes,
                    PaidAt = DateTime.UtcNow,
                    GymId = gymId,
                    BranchId = branchId,
                    CreatedOn = DateTime.UtcNow
                };
                await _billingRepository.AddPaymentRecordAsync(ledgerEntry);

                invoice.AmountPaid += request.Amount;
                invoice.Status = invoice.AmountPaid >= invoice.Amount ? "Paid" : "Partial";
                if (invoice.PaymentMethod.StartsWith("Pending-"))
                {
                    invoice.PaymentMethod = invoice.Status == "Paid"
                        ? invoice.PaymentMethod.Replace("Pending-", "")
                        : invoice.PaymentMethod;
                }
                invoice.ModifiedOn = DateTime.UtcNow;

                int savedInvoice = await _unitOfWork.SaveChangesAsync();
                return savedInvoice > 0 ? RecordPaymentResult.Ok() : RecordPaymentResult.Fail("Failed to save payment.");
            }

            return RecordPaymentResult.Fail("Billing record not found.");
        }

        public async Task<IEnumerable<PaymentRecordDto>> GetPaymentHistoryAsync(Guid recordId)
        {
            IEnumerable<PaymentRecord> records = await _billingRepository.GetPaymentRecordsBySourceAsync(PaymentSourceType.Subscription, recordId);
            if (!records.Any())
            {
                records = await _billingRepository.GetPaymentRecordsBySourceAsync(PaymentSourceType.CustomInvoice, recordId);
            }

            return records.Select(r => new PaymentRecordDto
            {
                Id = r.Id,
                Amount = r.Amount,
                PaymentMethod = r.PaymentMethod,
                Notes = r.Notes,
                PaidAt = r.PaidAt
            });
        }
    }
}
