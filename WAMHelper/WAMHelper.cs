namespace WAMHelper;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System.Management.Automation;
using System.Runtime.InteropServices;

[Cmdlet(VerbsCommon.Get, "WAMToken")]
public class WAMHelper : PSCmdlet
{
    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string[] scopes { get; set; }

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public Guid clientId { get; set; }

    [Parameter(Mandatory = false)]
    public string tenantId { get; set; }

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public AzureCloudInstance AzureCloudInstance { get; set; }

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string redirectUri { get; set; }

    [Parameter(Mandatory = false)]
    public string[] extraScopesToConsent { get; set; }

    protected override void ProcessRecord()
    {
        BrokerOptions options = new BrokerOptions(BrokerOptions.OperatingSystems.Windows);
        //required for multitenant app
        var newtenantId = string.IsNullOrEmpty(tenantId) ? "common" : tenantId;

        IPublicClientApplication app =
            PublicClientApplicationBuilder.Create(clientId.ToString())
            .WithAuthority(AzureCloudInstance, newtenantId)
            //.WithDefaultRedirectUri()
            .WithRedirectUri(redirectUri)
            .WithParentActivityOrWindow(GetConsoleOrTerminalWindow)
            .WithBroker(options)
            .Build();

        // Try to use the previously signed-in account from the cache
        //https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/WAM/e8646615d05def02311947e1ab922ecf3fad93f5#troubleshooting
        IEnumerable<IAccount> accounts =  app.GetAccountsAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        IAccount existingAccount = accounts.FirstOrDefault();

        try
        {
            if (existingAccount != null)
            {
                var result = app.AcquireTokenSilent(scopes, existingAccount).ExecuteAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                WriteObject(result);
            }
            // Next, try to sign in silently with the account that the user is signed into Windows
            else
            {
                var result = app.AcquireTokenSilent(scopes, PublicClientApplication.OperatingSystemAccount)
                                    .ExecuteAsync()
                                    .ConfigureAwait(false)
                                    .GetAwaiter()
                                    .GetResult();
                WriteObject(result);
            }
        }
        // Can't get a token silently, go interactive
        catch
        {
            if (extraScopesToConsent != null)
            {
                var result = app.AcquireTokenInteractive(scopes)
                    .WithExtraScopesToConsent(extraScopesToConsent)
                    .ExecuteAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                WriteObject(result);
            }
            else
            {
                var result = app.AcquireTokenInteractive(scopes)
                    .ExecuteAsync()
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();

                WriteObject(result);
            }
        }
    }

    enum GetAncestorFlags
    {
        /// <summary>
        /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
        /// </summary>
        GetParent = 1,
        /// <summary>
        /// Retrieves the root window by walking the chain of parent windows.
        /// </summary>
        GetRoot = 2,
        /// <summary>
        /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
        /// </summary>
        GetRootOwner = 3
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    static extern IntPtr GetAncestor(IntPtr hwnd, GetAncestorFlags flags);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    public IntPtr GetConsoleOrTerminalWindow()
    {
        IntPtr consoleHandle = GetConsoleWindow();
        IntPtr handle = GetAncestor(consoleHandle, GetAncestorFlags.GetRootOwner);

        return handle;
    }
}
