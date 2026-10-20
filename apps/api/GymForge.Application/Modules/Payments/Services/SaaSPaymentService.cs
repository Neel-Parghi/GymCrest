using AutoMapper;
using GymForge.Application.Modules.Payments.Interfaces;
using GymForge.Contracts.SaaSPayments;
using GymForge.Contracts.SuperAdmin.Configuration;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Razorpay.Api;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GymForge.Application.Modules.Payments.Services
{
    public class SaaSPaymentService : ISaaSPaymentService
    {
        private readonly ISaaSPaymentRepository _paymentRepository;
        private readonly ISaaSConfigurationRepository _configRepository;
        private readonly IGymManagementRepository _gymManagementRepository;
        private readonly ISaaSPlanRepository _saaSPlanRepository;
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly IMapper _mapper;
        private readonly ILogger<SaaSPaymentService> _logger;

        public SaaSPaymentService(IUnitOfWork uow, ISaaSPaymentRepository saaSPaymentRepository, IGymManagementRepository gymManagementRepository, ISaaSPlanRepository saaSPlanRepository, IConfiguration config, ISaaSConfigurationRepository configRepository, IMapper mapper, ILogger<SaaSPaymentService> logger)
        {
            _logger = logger;
            _uow = uow;
            _paymentRepository = saaSPaymentRepository;
            _gymManagementRepository = gymManagementRepository;
            _saaSPlanRepository = saaSPlanRepository;
            _config = config;
            _configRepository = configRepository;
            _mapper = mapper;
        }

        public async Task<InitiatePaymentResponseDto> InitiateSaaSPaymentAsync(CreatePaymentDto paymentDto)
        {
            Domain.Entities.Plan? plan = await _saaSPlanRepository.GetPlanByIdAsync(paymentDto.PlanId);

            if (plan == null) throw new Exception("Plan not found");

            GymForge.Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(paymentDto.GymId);
            if (gym == null) throw new Exception($"Gym with ID {paymentDto.GymId} not found. Please verify the GymId.");

            string razorPayAPIKeyId = _config["RazorPay:ApiKeyId"]!;
            string razorPayAPIKeySecret = _config["RazorPay:ApiKeySecret"]!;

            RazorpayClient client = new(razorPayAPIKeyId, razorPayAPIKeySecret);
            Dictionary<string, object> options = [];
            options.Add("amount", (int)(plan.Price * 100));
            options.Add("currency", "INR");
            options.Add("receipt", Guid.NewGuid().ToString());

            Order order = client.Order.Create(options);
            string razorpayOrderId = order["id"].ToString();

            SaaSPaymentTransaction transaction = new()
            {
                Id = Guid.NewGuid(),
                GymId = paymentDto.GymId,
                Gym = gym,
                PlanId = plan.Id,
                Amount = plan.Price,
                Currency = "INR",
                Status = SaaSPaymentStatus.Pending,
                GatewayTransactionId = razorpayOrderId
            };

            // The subscription is only changed once the payment is confirmed (verify / webhook).
            // A gym without any subscription yet gets an inactive record to attach the payment to.
            SubscriptionRecord? existingSubscription = await _paymentRepository.GetLatestSubscriptionByGymIdAsync(paymentDto.GymId);
            if (existingSubscription != null)
            {
                transaction.SubscriptionId = existingSubscription.Id;
            }
            else
            {
                SubscriptionRecord pendingSubscription = new()
                {
                    Id = Guid.NewGuid(),
                    GymId = paymentDto.GymId,
                    PlanId = plan.Id,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow,
                    IsActive = false,
                    IsTrial = plan.IsTrial,
                    PriceAtPurchase = plan.Price
                };
                transaction.SubscriptionId = pendingSubscription.Id;
                transaction.Subscription = pendingSubscription;
            }

            await _paymentRepository.AddAsync(transaction);
            
            await _uow.SaveChangesAsync();

            return new InitiatePaymentResponseDto
            {
                TransactionId = transaction.Id,
                RazorpayOrderId = razorpayOrderId,
                Amount = (int)(plan.Price * 100)
            };
        }

        public async Task<bool> CanManageGymAsync(Guid gymId, Guid userId)
        {
            Domain.Entities.Gym? gym = await _gymManagementRepository.GetGymByIdAsync(gymId);
            return gym != null && gym.OwnerUserId == userId;
        }

        public async Task<bool> ProcessSuccessfulPaymentAsync(string orderId, string paymentId, string signature)
        {
            // Initialises the SDK credentials used by the signature check.
            _ = new RazorpayClient(_config["RazorPay:ApiKeyId"]!, _config["RazorPay:ApiKeySecret"]!);

            try
            {
                Dictionary<string, string> attributes = new()
                {
                    ["razorpay_order_id"] = orderId,
                    ["razorpay_payment_id"] = paymentId,
                    ["razorpay_signature"] = signature
                };
                Utils.verifyPaymentSignature(attributes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Razorpay payment signature verification failed for order {OrderId}.", orderId);
                return false;
            }

            return await ActivatePaidOrderAsync(orderId, paymentId);
        }

        public async Task<bool> HandleWebhookAsync(string payload, string signature)
        {
            string? secret = _config["RazorPay:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(secret) || !IsValidWebhookSignature(payload, signature, secret))
            {
                _logger.LogWarning("Rejected Razorpay webhook: missing secret or invalid signature.");
                return false;
            }

            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            string? eventName = root.TryGetProperty("event", out JsonElement evt) ? evt.GetString() : null;

            if (!root.TryGetProperty("payload", out JsonElement body)
                || !body.TryGetProperty("payment", out JsonElement payment)
                || !payment.TryGetProperty("entity", out JsonElement entity))
            {
                return true;
            }

            string? orderId = entity.TryGetProperty("order_id", out JsonElement o) ? o.GetString() : null;
            string? paymentId = entity.TryGetProperty("id", out JsonElement p) ? p.GetString() : null;
            if (string.IsNullOrEmpty(orderId))
            {
                return true;
            }

            switch (eventName)
            {
                case "payment.captured":
                case "order.paid":
                    await ActivatePaidOrderAsync(orderId, paymentId);
                    break;
                case "payment.failed":
                    string? reason = entity.TryGetProperty("error_description", out JsonElement e) ? e.GetString() : null;
                    await MarkOrderFailedAsync(orderId, paymentId, reason);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Marks the order's transaction Paid and extends its subscription by the plan duration.
        /// Idempotent: verify and the webhook can both arrive for the same order.
        /// </summary>
        private async Task<bool> ActivatePaidOrderAsync(string orderId, string? paymentId)
        {
            SaaSPaymentTransaction? transaction = await _paymentRepository.GetByGatewayIdAsync(orderId);
            if (transaction == null) return false;
            if (transaction.Status == SaaSPaymentStatus.Paid) return true;

            SubscriptionRecord subscription = transaction.Subscription;
            Domain.Entities.Plan? plan = await _saaSPlanRepository.GetPlanByIdAsync(transaction.PlanId ?? subscription.PlanId);
            if (plan == null) return false;

            int durationDays = plan.DurationInDays > 0 ? plan.DurationInDays : 30;
            DateTime now = DateTime.UtcNow;

            // Early renewal of a running paid plan stacks on top; trials, placeholders and lapsed plans restart today.
            bool extendCurrent = subscription.IsActive && !subscription.IsTrial && subscription.EndDate > now;
            if (!extendCurrent)
            {
                subscription.StartDate = now;
            }
            subscription.EndDate = (extendCurrent ? subscription.EndDate : now).AddDays(durationDays);
            subscription.PlanId = plan.Id;
            subscription.PriceAtPurchase = plan.Price;
            subscription.IsTrial = plan.IsTrial;
            subscription.IsActive = true;
            subscription.Notes = null;

            transaction.Status = SaaSPaymentStatus.Paid;
            transaction.GatewayResponse = paymentId;
            transaction.FailureReason = null;

            await _uow.SaveChangesAsync();
            return true;
        }

        private async Task MarkOrderFailedAsync(string orderId, string? paymentId, string? reason)
        {
            SaaSPaymentTransaction? transaction = await _paymentRepository.GetByGatewayIdAsync(orderId);
            if (transaction == null || transaction.Status == SaaSPaymentStatus.Paid) return;

            transaction.Status = SaaSPaymentStatus.Failed;
            transaction.GatewayResponse = paymentId;
            transaction.FailureReason = reason;
            await _uow.SaveChangesAsync();
        }

        private static bool IsValidWebhookSignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrWhiteSpace(signature)) return false;

            byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
            byte[] received;
            try
            {
                received = Convert.FromHexString(signature);
            }
            catch (FormatException)
            {
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(expected, received);
        }

        public async Task<List<PaymentTransactionDto>> GetAllTransactionsAsync()
        {
            List<SaaSPaymentTransaction>? transactions = await _paymentRepository.GetTransactionsAsync();

            return [.. transactions.Select(t => new PaymentTransactionDto
            {
                Id = t.Id,
                GymName = t.Gym?.GymName ?? "Unknown",
                PlanName = t.Subscription?.Plan?.Name ?? "Unknown",
                Amount = t.Amount,
                Status = t.Status,
                CreatedAt = t.CreatedOn,
                GatewayTransactionId = t.GatewayTransactionId
            })];
        }

        public async Task<PaymentStatsDto> GetPaymentStatsAsync()
        {
            List<SaaSPaymentTransaction>? transactions = await _paymentRepository.GetTransactionsAsync();
            List<SubscriptionRecord> allSubscriptions = await _paymentRepository.GetActiveSubscriptionsAsync();

            List<SaaSPaymentTransaction> successTxs = transactions
                .Where(t => t.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                .ToList();

            decimal mrr = 0;
            foreach (SubscriptionRecord sub in allSubscriptions)
            {
                if (sub.Plan == null) continue;

                // Check if it's a paid subscription (either marked active or has successful payment)
                bool isPaid = sub.IsActive || successTxs.Any(t => t.SubscriptionId == sub.Id);
                decimal price = sub.PriceAtPurchase > 0 ? sub.PriceAtPurchase : sub.Plan.Price;

                // If it's a trial with 0 price and no payment, we skip it
                if (sub.IsTrial && price <= 0 && !isPaid) continue;
                if (!isPaid) continue;

                decimal contribution = 0;
                if (sub.Plan.DurationInDays >= 360) {
                    contribution = price / 12;
                }
                else if (sub.Plan.DurationInDays >= 28 && sub.Plan.DurationInDays <= 31) {
                    contribution = price;
                }
                else if (sub.Plan.DurationInDays > 0) {
                    contribution = (price / (decimal)sub.Plan.DurationInDays) * 30;
                }

                mrr += contribution;
            }

            return new PaymentStatsDto
            {
                TotalRevenue = successTxs.Sum(t => t.Amount),
                MonthlyRecurringRevenue = Math.Round(mrr, 2),
                ActiveSubscriptions = allSubscriptions.Count(x => x.IsActive || successTxs.Any(t => t.SubscriptionId == x.Id))
            };
        }

        public async Task<GymSubscriptionStatusDto> GetSubscriptionStatusAsync(Guid gymId)
        {
            SubscriptionRecord? sub = await _paymentRepository.GetLatestSubscriptionByGymIdAsync(gymId);
            List<Branch> branches = await _gymManagementRepository.GetBranchesByGymIdAsync(gymId);
            
            int branchesCount = branches?.Count ?? 0;

            if (sub == null)
            {
                // No plan record at all: no access until the owner picks a plan. This used to return a made-up
                // plan ending 15 days from "now", which moved forward on every check and never expired.
                return new GymSubscriptionStatusDto
                {
                    PlanName = "No plan",
                    Price = 0,
                    EndDate = DateTime.UtcNow,
                    IsActive = false,
                    IsTrial = false,
                    BranchUsageCount = branchesCount,
                    BranchLimit = 1
                };
            }

            Domain.Entities.Plan? plan = sub.Plan;
            if (plan == null)
            {
                plan = await _saaSPlanRepository.GetPlanByIdAsync(sub.PlanId);
            }

            if (plan == null)
            {
                List<Domain.Entities.Plan> plans = await _saaSPlanRepository.GetAllPlansAsync();
                plan = plans.FirstOrDefault(p => p.IsActive) 
                       ?? new Domain.Entities.Plan { Name = "GymCrest Pro Plan", Price = 4999, MaxBranches = 10 };
            }

            return new GymSubscriptionStatusDto
            {
                PlanName = plan.Name,
                Price = sub.PriceAtPurchase > 0 ? sub.PriceAtPurchase : plan.Price,
                EndDate = sub.EndDate,
                IsActive = sub.IsActive,
                IsTrial = sub.IsTrial,
                BranchUsageCount = branchesCount,
                BranchLimit = plan.MaxBranches ?? 10
            };
        }

        /// <summary>SuperAdmin-only manual renewal (no gateway payment); recorded as a Manual transaction.</summary>
        public async Task<GymSubscriptionStatusDto> RenewGymSubscriptionAsync(Guid gymId, Guid planId)
        {
            SubscriptionRecord? sub = await _paymentRepository.GetLatestSubscriptionByGymIdAsync(gymId);
            List<Domain.Entities.Branch> branches = await _gymManagementRepository.GetBranchesByGymIdAsync(gymId);
            int branchesCount = branches?.Count ?? 0;

            // Fetch pricing plan dynamically from the database
            Domain.Entities.Plan? plan = await _saaSPlanRepository.GetPlanByIdAsync(planId);
            if (plan == null) throw new Exception("Invalid SaaS plan selected.");

            DateTime newEndDate = DateTime.UtcNow.AddDays(plan.DurationInDays > 0 ? plan.DurationInDays : 30);
            if (sub != null)
            {
                newEndDate = sub.EndDate > DateTime.UtcNow ? sub.EndDate.AddDays(plan.DurationInDays > 0 ? plan.DurationInDays : 30) : DateTime.UtcNow.AddDays(plan.DurationInDays > 0 ? plan.DurationInDays : 30);
                sub.EndDate = newEndDate;
                sub.IsActive = true;
                sub.PriceAtPurchase = plan.Price;
                sub.PlanId = plan.Id;
                sub.Notes = $"Manual renewal: {plan.Name}";
            }
            else
            {
                sub = new SubscriptionRecord
                {
                    Id = Guid.NewGuid(),
                    GymId = gymId,
                    PlanId = plan.Id,
                    StartDate = DateTime.UtcNow,
                    EndDate = newEndDate,
                    IsActive = true,
                    IsTrial = false,
                    PriceAtPurchase = plan.Price,
                    Notes = $"Manual renewal: {plan.Name}"
                };
                await _gymManagementRepository.AddGymSubscriptionAsync(sub);
            }

            SaaSPaymentTransaction tx = new()
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                SubscriptionId = sub.Id,
                Amount = plan.Price,
                Currency = "INR",
                PlanId = plan.Id,
                Status = SaaSPaymentStatus.Manual,
                GatewayTransactionId = "manual_" + Guid.NewGuid().ToString("N")[..12],
                CreatedOn = DateTime.UtcNow
            };
            await _paymentRepository.AddAsync(tx);
            await _uow.SaveChangesAsync();

            int branchLimit = plan.MaxBranches ?? 10;

            return new GymSubscriptionStatusDto
            {
                PlanName = plan.Name,
                Price = plan.Price,
                EndDate = newEndDate,
                IsActive = true,
                IsTrial = false,
                BranchUsageCount = branchesCount,
                BranchLimit = branchLimit
            };
        }

        public async Task<List<PaymentTransactionDto>> GetGymTransactionsAsync(Guid gymId)
        {
            List<SaaSPaymentTransaction>? transactions = await _paymentRepository.GetTransactionsAsync();
            List<Domain.Entities.Plan> plans = await _saaSPlanRepository.GetAllPlansAsync();

            return [.. transactions
                .Where(t => t.GymId == gymId)
                .Select(t => {
                    decimal amt = t.Amount;
                    string planName = t.Subscription?.Plan?.Name 
                                      ?? plans.FirstOrDefault(p => p.Price == amt)?.Name 
                                      ?? "GymCrest Pro Plan";
                    return new PaymentTransactionDto
                    {
                        Id = t.Id,
                        GymName = t.Gym?.GymName ?? "Unknown",
                        PlanName = planName,
                        Amount = amt,
                        Status = t.Status,
                        CreatedAt = t.CreatedOn,
                        GatewayTransactionId = t.GatewayTransactionId
                    };
                })];
        }
    }
}
