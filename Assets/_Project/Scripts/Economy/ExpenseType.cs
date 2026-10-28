namespace PawsAndCare.Economy
{
    // Categories of business expense, carried by ExpenseIncurredEvent so the economy can deduct it
    // and future reporting can break costs down. Append-only.
    public enum ExpenseType
    {
        SALARY = 0,
        HIRING = 1,
        FURNITURE = 2,
        ROOM_UNLOCK = 3
    }
}
