using Newtonsoft.Json;
using Notions;
using SICore.BusinessLogic;
using SICore.Network.Contracts;
using SICore.PlatformSpecific;
using SIData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace SICore
{
    /// <summary>
    /// Информация о компьютерном игроке
    /// </summary>
    [DataContract]
    public class ComputerAccount : Account, IAccountInfo, IComparable<ComputerAccount>, IComparable
    {
        public void SetPicture(string photoUri)
        {
            if (!string.IsNullOrEmpty(Picture) && CoreManager.Instance.FileExists(Picture))
                return;

            if (string.IsNullOrEmpty(Name))
                return;

            Picture = Path.Combine(photoUri, Name.Translit()) + ".jpg";
        }

        /// <summary>
        /// Стиль игры
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public PlayerStyle Style { get; set; }

        // Все вероятности лежат в интервале 0 - 100

        private int _v1 = 0;
        /// <summary>
        /// Вероятность того, что игрок будет выбирать согласно номеру темы
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V1
        {
            get { return _v1; }
            set { _v1 = value; OnPropertyChanged(); }
        }

        // Иначе - согласно стоимости вопроса

        // Если согласно номеру темы

        private int _v2 = 0;
        /// <summary>
        /// Тема та же, что и была
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V2
        {
            get { return _v2; }
            set { _v2 = value; OnPropertyChanged(); OnPropertyChanged(nameof(V3Max)); OnPropertyChanged(nameof(Rest1)); }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public int V2Max
        {
            get { return 100 - V3; }
        }

        private int _v3 = 0;

        /// <summary>
        /// Тема согласно приоритету тем
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V3
        {
            get { return _v3; }
            set { _v3 = value; OnPropertyChanged(); OnPropertyChanged(nameof(V2Max)); OnPropertyChanged(nameof(Rest1)); }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public int V3Max
        {
            get { return 100 - V2; }
        }

        private char[] _p1 = null;

        /// <summary>
        /// Приоритет тем (например "123456")
        /// </summary>
        [DataMember]
        public char[] P1
        {
            get { return _p1; }
            set { _p1 = value; OnPropertyChanged(); }
        }

        // Иначе - случайная тема
        [XmlIgnore]
        [IgnoreDataMember]
        public int Rest1
        {
            get { return 100 - V2 - V3; }
        }

        // Если согласно номеру вопроса
        private int _v4 = 0;

        /// <summary>
        /// Вопрос дешевле, чем был
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V4
        {
            get { return _v4; }
            set
            {
                _v4 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(V5Max));
                OnPropertyChanged(nameof(V6Max));
                OnPropertyChanged(nameof(V7Max));
                OnPropertyChanged(nameof(Rest2));
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public int V4Max
        {
            get { return 100 - V5 - V6 - V7; }
        }

        private int _v5 = 0;
        /// <summary>
        /// Вопрос дороже, чем был
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V5
        {
            get { return _v5; }
            set
            {
                _v5 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(V4Max));
                OnPropertyChanged(nameof(V6Max));
                OnPropertyChanged(nameof(V7Max));
                OnPropertyChanged(nameof(Rest2));
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public int V5Max
        {
            get { return 100 - V4 - V6 - V7; }
        }

        private int _v6 = 0;
        /// <summary>
        /// Вопрос той же цены
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V6
        {
            get { return _v6; }
            set
            {
                _v6 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(V4Max));
                OnPropertyChanged(nameof(V5Max));
                OnPropertyChanged(nameof(V7Max));
                OnPropertyChanged(nameof(Rest2));
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public int V6Max
        {
            get { return 100 - V5 - V4 - V7; }
        }

        private int _v7 = 0;
        /// <summary>
        /// Вопрос согласно приоритету
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V7
        {
            get { return _v7; }
            set
            {
                _v7 = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(V4Max));
                OnPropertyChanged(nameof(V5Max));
                OnPropertyChanged(nameof(V6Max));
                OnPropertyChanged(nameof(Rest2));
            }
        }

        [XmlIgnore]
        [IgnoreDataMember]
        public int V7Max
        {
            get { return 100 - V5 - V6 - V4; }
        }

        private char[] _p2 = null;

        /// <summary>
        /// Приоритет вопросов (например "12345")
        /// </summary>
        [DataMember]
        public char[] P2
        {
            get { return _p2; }
            set { _p2 = value; OnPropertyChanged(); }
        }

        // Иначе - случайный вопрос
        [XmlIgnore]
        [IgnoreDataMember]
        public int Rest2
        {
            get { return 100 - V4 - V5 - V6 - V7; }
        }

        // Основные игровые характеристики

        /// <summary>
        /// Эрудиция
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int F { get; set; }

        /// <summary>
        /// Смелость по умолчанию
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int B0 { get; set; }

        /// <summary>
        /// Скорость реакции
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int S { get; set; }

        /// <summary>
        /// Вероятность отдать Вопрос с секретом тому, у кого меньше денег
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int V { get; set; }

        /// <summary>
        /// Вероятность сказать "Пас" на вопросе первой категории
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int N1 { get; set; }

        /// <summary>
        /// Вероятность сказать "Пас" на вопросе пятой категории
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int N5 { get; set; }

        /// <summary>
        /// Вероятность сказать "Ва-банк" на вопросе первой категории
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int B1 { get; set; }

        /// <summary>
        /// Вероятность сказать "Ва-банк" на вопросе пятой категории
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int B5 { get; set; }

        // Критическая ситуация

        /// <summary>
        /// Максимальное число вопросов до конца раунда, при котором ситуация может стать критической
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int Nq { get; set; }

        /// <summary>
        /// Минимальная доля собственной суммы по отношению к сумме лидера, при которой ситуация может стать критической
        /// </summary>
        [XmlAttribute]
        [DataMember]
        public int Part { get; set; }

        [DataMember]
        public List<int> LoveNumbers { get; set; }

        public ComputerAccount()
        {
            LoveNumbers = new List<int>();
            IsHuman = false;
        }

        /// <summary>
        /// Создание инфы
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="sex">Пол</param>
        /// <param name="style">Стиль игры</param>
        /// <param name="v1">Вероятность того, что игрок будет выбирать согласно номеру темы, а не вопроса</param>
        /// <param name="v2">Тема та же, что и была</param>
        /// <param name="v3">Тема согласно приоритету тем</param>
        /// <param name="p1">Приоритет тем (например "123456")</param>
        /// <param name="v4">Вопрос дешевле, чем был</param>
        /// <param name="v5">Вопрос дороже, чем был</param>
        /// <param name="v6">Вопрос той же цены</param>
        /// <param name="v7">Вопрос согласно приоритету</param>
        /// <param name="p2">Приоритет вопросов (например "12345")</param>
        /// <param name="f">Эрудиция</param>
        /// <param name="b0">Смелость по умолчанию</param>
        /// <param name="s">Скорость реакции</param>
        /// <param name="v">Вероятность отдать Вопрос с секретом тому, у кого меньше денег</param>
        /// <param name="n1">Вероятность сказать "Пас" на вопросе первой категории</param>
        /// <param name="n5">Вероятность сказать "Пас" на вопросе пятой категории</param>
        /// <param name="b1">Вероятность сказать "Ва-банк" на вопросе первой категории</param>
        /// <param name="b5">Вероятность сказать "Ва-банк" на вопросе пятой категории</param>
        /// <param name="nq">Максимальное число вопросов до конца третьего раунда, при котором ситуация может стать критической</param>
        /// <param name="part">Минимальная доля собственной суммы по отношению к сумме лидера, при которой ситуация может стать критической</param>
        public ComputerAccount(string name, bool sex, PlayerStyle style, int v1, int v2, int v3, string p1, int v4, int v5, int v6, int v7, string p2, int f, int b0, int s, int v, int n1, int n5, int b1, int b5, int nq, int part)
            : base(name, sex)
        {
            IsHuman = false;

            Style = style;
            V1 = v1;
            V2 = v2;
            V3 = v3;
            P1 = p1.ToCharArray();
            V4 = v4;
            V5 = v5;
            V6 = v6;
            V7 = v7;
            P2 = p2.ToCharArray();
            F = f;
            B0 = b0;
            S = s;
            V = v;
            N1 = n1;
            N5 = n5;
            B1 = b1;
            B5 = b5;
            Nq = nq;
            Part = part;
        }

        /// <summary>
        /// Создание игрока со случайными характеристиками
        /// </summary>
        /// <param name="name">Имя игрока</param>
        /// <param name="sex">Пол</param>
        public ComputerAccount(string name, bool sex)
            : base(name, sex)
        {
            IsHuman = false;
        }

        public ComputerAccount(ComputerAccount computerAccount) : base(computerAccount)
        {
            IsHuman = false;

            Style = computerAccount.Style;
            V1 = computerAccount.V1;
            V2 = computerAccount.V2;
            V3 = computerAccount.V3;
            P1 = computerAccount.P1;
            V4 = computerAccount.V4;
            V5 = computerAccount.V5;
            V6 = computerAccount.V6;
            V7 = computerAccount.V7;
            P2 = computerAccount.P2;
            F = computerAccount.F;
            B0 = computerAccount.B0;
            S = computerAccount.S;
            V = computerAccount.V;
            N1 = computerAccount.N1;
            N5 = computerAccount.N5;
            B1 = computerAccount.B1;
            B5 = computerAccount.B5;
            Nq = computerAccount.Nq;
            Part = computerAccount.Part;
        }

        /// <summary>
        /// Задать компьютерному игроку случайные характеристики
        /// </summary>
        public void Randomize()
        {
            var r = new Random();
            int var = r.Next(3);
            if (var == 0)
                Style = PlayerStyle.Agressive;
            else if (var == 1)
                Style = PlayerStyle.Careful;
            else
                Style = PlayerStyle.Normal;

            V1 = r.Next(101);
            V2 = r.Next(101);
            V3 = r.Next(101 - V2);

            var prior = "123456".ToCharArray();
            P1 = "123456".ToCharArray();

            int j;
            for (int i = 0; i < 6; i++)
            {
                j = r.Next(6 - i);
                P1[i] = prior[j];
                for (int k = j; k < 5 - i; k++)
                {
                    prior[k] = prior[k + 1];
                }
            }

            V4 = r.Next(101);
            V5 = r.Next(101 - V4);
            V6 = r.Next(101 - V4 - V5);
            V7 = r.Next(101 - V4 - V5 - V6);

            prior = "12345".ToCharArray();
            P2 = "12345".ToCharArray();

            for (int i = 0; i < 5; i++)
            {
                j = r.Next(5 - i);
                P2[i] = prior[j];
                for (int k = j; k < 4 - i; k++)
                {
                    prior[k] = prior[k + 1];
                }
            }

            F = 50 + r.Next(76);
            B0 = (int)(F * (((double)r.Next(91)) / 100 + 1));
            S = r.Next(5);
            V = r.Next(101);
            N1 = -20 + r.Next(71);
            N5 = r.Next(121);
            B1 = r.Next(121);
            B5 = -20 + r.Next(71);
            Nq = r.Next(31);
            Part = r.Next(150);
        }

        public int CompareTo(ComputerAccount other)
        {
            var result = -F.CompareTo(other.F);
            if (result != 0)
                return result;

            return S.CompareTo(other.S);
        }

        public int CompareTo(object obj) => -1;

        public void LoadInfo(ComputerAccount item)
        {
            B0 = item.B0;
            B1 = item.B1;
            B5 = item.B5;
            F = item.F;
            LoveNumbers = item.LoveNumbers;
            N1 = item.N1;
            N5 = item.N5;
            Nq = item.Nq;
            P1 = item.P1;
            P2 = item.P2;
            Part = item.Part;
            Picture = item.Picture;
            S = item.S;
            IsMale = item.IsMale;
            Style = item.Style;
            V = item.V;
            V1 = item.V1;
            V2 = item.V2;
            V3 = item.V3;
            V4 = item.V4;
            V5 = item.V5;
            V6 = item.V6;
            V7 = item.V7;
        }

        /// <summary>
        /// Коллекция компьютерных участников по умолчанию
        /// </summary>
        private static StoredPersons _storedPersons = null;

        internal static StoredPersons StoredPersons
        {
            get
            {
                if (_storedPersons == null)
                {
                    var serializer = new JsonSerializer();

                    using var personsJsonStream = System.Reflection.Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("SICore.persons.json");
                    using var streamReader = new StreamReader(personsJsonStream);
                    using var reader = new JsonTextReader(streamReader);

                    _storedPersons = serializer.Deserialize<StoredPersons>(reader);
                }

                return _storedPersons;
            }
        }

        public static ComputerAccount[] GetDefaultPlayers(ILocalizer localizer, string photoPath)
        {
            var culture = localizer.Culture.TwoLetterISOLanguageName;

            return StoredPersons.Players
                .Select(player => new ComputerAccount(player)
                {
                    Name = player.Names.ContainsKey(culture) ? player.Names[culture] : "-",
                    IsMale = player.IsMale,
                    Picture = Path.Combine(photoPath, player.Picture)
                })
                .OrderBy(player => player.Name)
                .ToArray();
        }

        public static ComputerAccount[] GetDefaultShowmans(ILocalizer localizer, string photoPath)
        {
            var culture = localizer.Culture.TwoLetterISOLanguageName;

            return StoredPersons.Showmans.Select(showman => new ComputerAccount(showman)
                {
                    Name = showman.Names.ContainsKey(culture) ? showman.Names[culture] : "-",
                    IsMale = showman.IsMale,
                    Picture = Path.Combine(photoPath, showman.Picture)
                })
                .OrderBy(showman => showman.Name)
                .ToArray();
        }

        public ComputerAccount Clone() => new ComputerAccount
        {
            B0 = B0,
            B1 = B1,
            B5 = B5,
            CanBeDeleted = CanBeDeleted,
            F = F,
            IsHuman = IsHuman,
            LoveNumbers = new List<int>(LoveNumbers),
            N1 = N1,
            N5 = N5,
            Name = Name,
            Nq = Nq,
            _p1 = _p1,
            _p2 = _p2,
            Part = Part,
            Picture = Picture,
            S = S,
            IsMale = IsMale,
            Style = Style,
            V = V,
            _v1 = _v1,
            _v2 = _v2,
            _v3 = _v3,
            _v4 = _v4,
            _v5 = _v5,
            _v6 = _v6,
            _v7 = _v7
        };
    }
}
