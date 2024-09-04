using Microsoft.JSInterop;

namespace CfPerfCalc.Services;

public class UserHandleCache(IJSRuntime jsRuntime)
{
    private string? userHandle { get; set; }
    private bool _initialized;

    private async Task EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        // Load the user handle from localStorage during initialization
        userHandle = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "userHandle");
        _initialized = true;
    }

    public async Task<string?> GetUserHandle()
    {
        await EnsureInitialized();
        return userHandle;
    }

    public async Task SetUserHandleAsync(string? handle)
    {
        userHandle = handle;

        if (!string.IsNullOrEmpty(handle))
        {
            // Store the updated handle in localStorage
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "userHandle", handle);
        }
        else
        {
            // Remove the handle from localStorage if it's null or empty
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", "userHandle");
        }
    }
}