namespace SICore
{
    /// <summary>
    /// Логика клиента для живого игрока
    /// </summary>
    internal abstract class HumanLogic<C, D> : Logic<C, D>
        where C : IActor
        where D : Data
    {
        public HumanLogic(C client, D data)
            : base(client, data)
        {

        }
    }
}
