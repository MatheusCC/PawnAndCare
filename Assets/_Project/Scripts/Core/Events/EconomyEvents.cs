using PawsAndCare.Economy;

namespace PawsAndCare.Core
{
    /// <summary>
    /// Published by EconomyManager whenever the running balance changes. Delta is the signed amount
    /// applied (positive for revenue, negative for spending), so UI can show both the new total and
    /// what just changed.
    /// </summary>
    public readonly struct BalanceChangedEvent
    {
        private readonly float newBalance;
        private readonly float delta;

        public float NewBalance
        {
            get { return newBalance; }
        }

        public float Delta
        {
            get { return delta; }
        }

        public BalanceChangedEvent(float newBalance, float delta)
        {
            this.newBalance = newBalance;
            this.delta = delta;
        }
    }

    /// <summary>
    /// Published by systems that incur a business expense (e.g. StaffOffice payroll and hiring).
    /// EconomyManager subscribes and deducts the amount, mirroring how it adds service revenue.
    /// </summary>
    public readonly struct ExpenseIncurredEvent
    {
        private readonly float amount;
        private readonly ExpenseType type;

        public float Amount
        {
            get { return amount; }
        }

        public ExpenseType Type
        {
            get { return type; }
        }

        public ExpenseIncurredEvent(float amount, ExpenseType type)
        {
            this.amount = amount;
            this.type = type;
        }
    }
}
