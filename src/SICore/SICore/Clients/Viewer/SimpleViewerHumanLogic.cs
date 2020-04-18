namespace SICore
{
    internal sealed class SimpleViewerHumanLogic : ViewerHumanLogic<SimpleViewer>
    {
        /// <summary>
        /// Создание логики
        /// </summary>
        /// <param name="client">Текущий клиент</param>
        public SimpleViewerHumanLogic(SimpleViewer client, ViewerData data)
            : base(client, data)
        {
            
        }
    }
}
