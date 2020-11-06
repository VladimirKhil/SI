using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
[assembly: AssemblyTrademark("SIGame")]

// Параметр ComVisible со значением FALSE делает типы в сборке невидимыми 
// для COM-компонентов.  Если требуется обратиться к типу в этой сборке через 
// COM, задайте атрибуту ComVisible значение TRUE для этого типа.
[assembly: ComVisible(false)]

//Чтобы начать построение локализованных приложений, задайте 
//<UICulture>CultureYouAreCodingWith</UICulture> в файле .csproj
//внутри <PropertyGroup>.  Например, если используется английский США
//в своих исходных файлах установите <UICulture> в en-US.  Затем отмените преобразование в комментарий
//атрибута NeutralResourceLanguage ниже.  Обновите "en-US" в
//строка внизу для обеспечения соответствия настройки UICulture в файле проекта.

//[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //где расположены словари ресурсов по конкретным тематикам
    //(используется, если ресурс не найден на странице 
    // или в словарях ресурсов приложения)
    ResourceDictionaryLocation.SourceAssembly //где расположен словарь универсальных ресурсов
    //(используется, если ресурс не найден на странице, 
    // в приложении или в каких-либо словарях ресурсов для конкретной темы)
)]
