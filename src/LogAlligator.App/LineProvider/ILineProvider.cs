using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public interface ILineProvider
{
    public Task LoadData(Action<int> progressCallback, CancellationToken token = default);
    public int Count { get; }
    public string this[int index] { get; }
    public int GetLineLength(int index);

    // TODO: Consider making a decorator for BufferedFileLineProvider, pass the original provider and filter
    // Then use LoadData to filter the lines. I can leave the Grep function. Just call it inside LoadData.
    public ILineProvider Grep(Func<string, bool> filter);
}
