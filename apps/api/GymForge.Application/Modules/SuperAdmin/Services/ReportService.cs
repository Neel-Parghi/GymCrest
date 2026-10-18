using GymForge.Contracts.Gym.Management;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using System.Text;

namespace GymForge.Application.Modules.SuperAdmin.Services
{
    public interface IReportService
    {
        Task<(byte[] Content, string FileName)> GenerateReportAsync(string type);
    }

    public class ReportService : IReportService
    {
        private readonly IGymManagementRepository _gymRepository;
        private readonly ISaaSPaymentRepository _paymentRepository;
        private readonly ISaaSConfigurationRepository _configRepository;

        public ReportService(
            IGymManagementRepository gymRepository,
            ISaaSPaymentRepository paymentRepository,
            ISaaSConfigurationRepository configRepository)
        {
            _gymRepository = gymRepository;
            _paymentRepository = paymentRepository;
            _configRepository = configRepository;
        }

        public async Task<(byte[] Content, string FileName)> GenerateReportAsync(string type)
        {
            return type.ToLower() switch
            {
                "financial" => await GenerateFinancialReportAsync(),
                "gyms" => await GenerateGymDirectoryReportAsync(),
                "snapshot" => await GenerateSnapshotReportAsync(),
                _ => throw new ArgumentException("Invalid report type")
            };
        }

        private async Task<(byte[] Content, string FileName)> GenerateFinancialReportAsync()
        {
            List<SaaSPaymentTransaction> transactions = await _paymentRepository.GetTransactionsAsync();
            StringBuilder csv = new();
            
            csv.AppendLine("TransactionId,GymName,Amount,Currency,Status,GatewayId,FailureReason,Date");

            foreach (SaaSPaymentTransaction t in transactions)
            {
                string gymName = t.Gym?.GymName?.Replace(",", " ") ?? "Unknown";
                string status = t.Status;
                string reason = t.FailureReason?.Replace(",", ";") ?? "";
                
                csv.AppendLine($"{t.Id},{gymName},{t.Amount},{t.Currency},{status},{t.GatewayTransactionId},{reason},{t.CreatedOn:yyyy-MM-dd HH:mm:ss}");
            }

            return (Encoding.UTF8.GetBytes(csv.ToString()), $"GymCrest_Financials_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        private async Task<(byte[] Content, string FileName)> GenerateGymDirectoryReportAsync()
        {
            List<GymListResponseDto> gyms = await _gymRepository.GetAllGymsAsync();
            StringBuilder csv = new();
            
            csv.AppendLine("GymName,BrandName,Email,Phone,Website,GstNumber,RegNumber,EstablishedDate,Owner,IsVerified,JoinedDate");

            foreach (GymListResponseDto g in gyms)
            {
                string ownerName = g.OwnerName?.Replace(",", " ") ?? "Unknown";
                string established = g.EstablishedDate?.ToString("yyyy-MM-dd") ?? "";
                
                csv.AppendLine($"{g.GymName.Replace(",", " ")},{g.BrandName?.Replace(",", " ")},{g.Email},{g.Phone},{g.WebsiteUrl},{g.GstNumber},{g.RegistrationNumber},{established},{ownerName},{g.IsVerified},{g.CreatedOn:yyyy-MM-dd}");
            }

            return (Encoding.UTF8.GetBytes(csv.ToString()), $"GymCrest_Directory_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        private async Task<(byte[] Content, string FileName)> GenerateSnapshotReportAsync()
        {
            SaaSConfiguration config = await _configRepository.GetConfigurationAsync();
            
            StringBuilder csv = new();
            
            csv.AppendLine("Metric,TargetValue,CurrentStatus,LastUpdated");
            csv.AppendLine($"Yearly Revenue Target,{config.YearlyRevenueTarget},Active,{DateTime.UtcNow:yyyy-MM-dd}");
            csv.AppendLine($"Subscription Target,{config.SubscriptionTarget},Active,{DateTime.UtcNow:yyyy-MM-dd}");
            csv.AppendLine($"Uptime Threshold,{config.UptimeThreshold}%,Nominal,{DateTime.UtcNow:yyyy-MM-dd}");

            return (Encoding.UTF8.GetBytes(csv.ToString()), $"GymCrest_Strategy_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
