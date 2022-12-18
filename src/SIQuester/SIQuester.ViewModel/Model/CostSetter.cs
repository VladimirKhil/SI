using System.Xml.Serialization;

namespace SIQuester.Model;

/// <summary>
/// Информация о назначении стоимостей
/// </summary>
public sealed class CostSetter
{
    [XmlAttribute]
    public int BaseValue { get; set; }

    [XmlAttribute]
    public int Increment { get; set; }

    public CostSetter()
    {

    }

    public CostSetter(int startValue)
    {
        BaseValue = startValue;
        Increment = startValue;
    }
}
