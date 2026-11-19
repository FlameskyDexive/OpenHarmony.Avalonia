using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.OpenHarmony;

public class ClipboardImpl : IClipboard
{
    public Task ClearAsync()
    {
        throw new NotImplementedException();
    }

    public Task SetDataAsync(IAsyncDataTransfer? data)
    {
        throw new NotImplementedException();
    }

    public Task FlushAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IAsyncDataTransfer?> TryGetInProcessDataAsync()
    {
        throw new NotImplementedException();
    }
}
