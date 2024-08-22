using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public interface ILineProvider
{
    public int LoadData();
    public int Count { get; }
    public string this[int index] { get; }
    public int GetLineLength(int index);
}
