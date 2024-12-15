using SIPackages;
using SIPackages.Core;

namespace SIQuester.ViewModel.Helpers;

/// <summary>
/// Provides search helper methods.
/// </summary>
public static class SearchExtensions
{
    public static IEnumerable<SearchData> SearchQuestion(this Question question, string value)
    {
        foreach (var item in SearchInfoOwner(question, value))
        {
            yield return item;
        }

        foreach (var item in question.Parameters.SearchParameters(value))
        {
            yield return item;
        }

        foreach (var item in question.Right.SearchList(value))
        {
            item.Kind = ResultKind.Right;
            yield return item;
        }

        foreach (var item in question.Wrong.SearchList(value))
        {
            item.Kind = ResultKind.Wrong;
            yield return item;
        }
    }

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public static IEnumerable<SearchData> SearchParameters(this StepParameters stepParameters, string value)
    {
        foreach (var parameter in stepParameters.Values)
        {
            foreach (var item in parameter.SearchParameter(value))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public static IEnumerable<SearchData> SearchParameter(this StepParameter stepParameter, string value)
    {
        switch (stepParameter.Type)
        {
            case StepParameterTypes.Content:
                if (stepParameter.ContentValue == null)
                {
                    break;
                }

                foreach (var item in stepParameter.ContentValue)
                {
                    foreach (var searchResult in Search(ResultKind.Text, item.Value, value))
                    {
                        yield return searchResult;
                    }
                }
                break;

            case StepParameterTypes.Group:
                if (stepParameter.GroupValue == null)
                {
                    break;
                }

                foreach (var searchResult in stepParameter.GroupValue.SearchParameters(value))
                {
                    yield return searchResult;
                }
                break;

            case StepParameterTypes.NumberSet:
                if (stepParameter.NumberSetValue == null)
                {
                    break;
                }

                foreach (var searchResult in Search(ResultKind.Text, stepParameter.NumberSetValue.ToString(), value))
                {
                    yield return searchResult;
                }
                break;

            default:
                foreach (var searchResult in Search(ResultKind.Text, stepParameter.SimpleValue, value))
                {
                    yield return searchResult;
                }
                break;
        }
    }

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="item">Object to search within.</param>
    /// <param name="value">Value to search.</param>
    /// <returns>Search result or null.</returns>
    public static SearchMatch? SearchFragment(this InfoOwner item, string value)
    {
        var result = item.SearchInfoOwner(value).FirstOrDefault();

        if (result == null)
        {
            return null;
        }

        var match = result.Item;
        var diff = match.Length - result.StartIndex - value.Length;

        return new SearchMatch(
            result.StartIndex > 0 ? match[..result.StartIndex] : "",
            match.Substring(result.StartIndex, value.Length),
            diff > 0 ? match.Substring(result.StartIndex + value.Length, diff) : "");
    }

    public static IEnumerable<SearchData> SearchInfoOwner(this InfoOwner infoOwner, string value)
    {
        foreach (var item in SearchNamed(infoOwner, value))
        {
            yield return item;
        }

        foreach (var item in infoOwner.Info.Authors.SearchList(value))
        {
            item.Kind = ResultKind.Author;
            yield return item;
        }

        foreach (var item in infoOwner.Info.Sources.SearchList(value))
        {
            item.Kind = ResultKind.Source;
            yield return item;
        }

        foreach (var item in Search(ResultKind.Comment, infoOwner.Info.Comments.Text, value))
        {
            yield return item;
        }

        if (infoOwner.Info.ShowmanComments != null)
        {
            foreach (var item in Search(ResultKind.Comment, infoOwner.Info.ShowmanComments.Text, value))
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// Searches a value inside the object.
    /// </summary>
    /// <param name="value">Value to search.</param>
    /// <returns>Search results.</returns>
    public static IEnumerable<SearchData> SearchNamed(this Named named, string value) => Search(ResultKind.Name, named.Name, value);

    /// <summary>
    /// Searches pattern in list values.
    /// </summary>
    /// <param name="list">List to search.</param>
    /// <param name="pattern">Searched pattern.</param>
    /// <returns>Search result.</returns>
    public static IEnumerable<SearchData> SearchList(this List<string> list, string pattern)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var index = list[i].IndexOf(pattern, StringComparison.CurrentCultureIgnoreCase);
            if (index > -1)
            {
                yield return new SearchData(list[i], index, i);
            }
        }
    }

    internal static IEnumerable<SearchData> Search(ResultKind kind, string str, string text)
    {
        if (str == null)
        {
            yield break;
        }

        var index = str.IndexOf(text, StringComparison.CurrentCultureIgnoreCase);

        if (index == -1)
        {
            yield break;
        }

        yield return new SearchData(str, index, kind);
    }
}
