namespace SImulator.ViewModel.ButtonManagers
{
    /// <summary>
    /// Менеджер кнопок, работающий вхолостую (кнопки используются вне программы)
    /// </summary>
    internal sealed class EmptyButtonManager : ButtonManagerBase
    {
        public override bool Run()
        {
            return true;
        }

        public override void Stop()
        {
            
        }

        public override void Dispose()
        {
            
        }
    }
}
