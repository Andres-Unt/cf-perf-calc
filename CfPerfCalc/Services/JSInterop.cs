using Microsoft.JSInterop;
using System.Threading.Tasks;

public class JSInterop
{
    private readonly IJSRuntime _jsRuntime;

    public JSInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T> InvokeAsync<T>(string method, params object[] args)
    {
        return await _jsRuntime.InvokeAsync<T>(method, args);
    }

    public async Task InvokeVoidAsync(string method, params object[] args)
    {
        await _jsRuntime.InvokeVoidAsync(method, args);
    }
}