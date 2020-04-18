namespace SICore
{
    internal sealed class SimpleViewerComputerLogic : ViewerComputerLogic<SimpleViewer>
    {
        /// <summary>
        /// Создание логики
        /// </summary>
        /// <param name="client">Текущий клиент</param>
        public SimpleViewerComputerLogic(SimpleViewer client, ViewerData data)
            : base(client, data)
        {
            
        }
    }
}
