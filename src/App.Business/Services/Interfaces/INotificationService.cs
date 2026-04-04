namespace App.Business.Services.Interfaces
{
    public record SendResult(int Sent, int Failed, IReadOnlyList<string> Errors);

    public interface INotificationService
    {
        /// <summary>Bir uşağın valideyninə ödəniş xatırlatması göndərir.</summary>
        Task<SendResult> SendPaymentReminderAsync(int childId);

        /// <summary>Bütün borclu valideynlərə toplu xatırlatma göndərir.</summary>
        Task<SendResult> SendBulkRemindersToDebtorsAsync();

        /// <summary>Yarın ödəniş günü olan uşaqların valideynlərinə xatırlatma göndərir.</summary>
        Task<SendResult> SendPaymentDueRemindersAsync();

        /// <summary>Ödəniş edilərkən valideynə təsdiqləmə mesajı göndərir.</summary>
        Task<SendResult> SendPaymentConfirmationAsync(int paymentId);
    }
}
