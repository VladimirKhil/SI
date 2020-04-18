namespace SICore
{
    /// <summary>
    /// Логика компьютерного игрока
    /// </summary>
    internal abstract class ComputerLogic<C, D> : Logic<C, D>
        where C : IActor
        where D : Data
    {
        public ComputerLogic(C client, D data)
            : base(client, data)
        {
        }
    }
}
