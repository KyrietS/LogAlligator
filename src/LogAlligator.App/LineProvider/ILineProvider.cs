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
}
